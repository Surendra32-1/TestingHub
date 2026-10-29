# 08 · Authentication and RBAC

Scope: `src/TeamHub.Auth/**` (16 files, all read in full), the auth-related parts of
`src/TeamHub.API/Program.cs`, the SQL in `authentication-schema.sql` /
`authentication-entra-schema.sql`, and the frontend auth layer in
`TeamHubFrontEnd/src/app/core/auth/**` (6 files, all read in full).

Every claim below is **Verified** against source unless explicitly labelled *Inferred*,
*Unknown*, or *Recommended*.

---

## 1. The module in one paragraph

TeamHub ships **two mutually exclusive sign-in modes selected by configuration**:

| Mode | Selected when | How a token is obtained | Scheme name |
| --- | --- | --- | --- |
| Local JWT (development default) | `Auth:Entra:Enabled = false` | `POST /api/auth/dev-sign-in` (passwordless, Development-only) | `JwtBearerDefaults.AuthenticationScheme` (`"Bearer"`) |
| Microsoft Entra ID | `Auth:Entra:Enabled = true` | MSAL redirect in the SPA; the API validates the Entra access token | `"EntraBearer"` (`TeamHubAuthServiceCollectionExtensions.cs:21, :56`) |

Both modes converge on the **same application claims** (`UserId`, `EmployeeId`, `Role`,
`permission`), so authorization code below never branches on the mode. In local mode the claims
are baked into the HMAC-signed token by `JwtService.CreateAccessToken` (`JwtService.cs:41-53`); in
Entra mode the Entra token carries no app claims, so `EntraClaimsIssuer.OnTokenValidated` looks the
user up in the database and injects them (`EntraClaimsIssuer.cs:39-54`).

There is **no password authentication anywhere in the current code**. `Users.PasswordHash`,
`PasswordSalt`, and `IsFirstLogin` are actively dropped by
`authentication-entra-schema.sql:45-55`. The `BCrypt.Net-Next` 4.0.3 package is still referenced
(`TeamHub.Auth.csproj`, restored in `TeamHub.Auth/obj/project.assets.json:650`) but **no `.cs` file
uses it** — verified by searching all `*.cs` for `BCrypt`, which returned zero source hits.
*Inferred*: vestigial dependency from the pre-Entra password-based design. *Recommended*: remove it.

---

## 2. Composition root

```csharp
// src/TeamHub.API/Program.cs:39-40
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddTeamHubAuth(builder.Configuration, builder.Environment);
```

`AddTeamHubAuth` (`TeamHubAuthServiceCollectionExtensions.cs:23-135`) does, in order:

| Line | Registration | Lifetime |
| --- | --- | --- |
| `:33-38` | **Startup guard** — throws `InvalidOperationException` when `!IsDevelopment() && !entraEnabled` | — |
| `:40-50` | Reads `Authentication:Jwt:Issuer`, `:Audience`, `:SecretKey`; throws if missing; throws if `Encoding.UTF8.GetByteCount(secretKey) < 32` | — |
| `:54-56` | Picks the default scheme: `entraEnabled ? "EntraBearer" : JwtBearerDefaults` | — |
| `:59-75` | `AddJwtBearer("Bearer", …)` — registered **unconditionally**, so the dev bypass can always mint/validate local tokens | — |
| `:77-109` | `AddJwtBearer("EntraBearer", …)` + `OnTokenValidated` hook — only when Entra is enabled | — |
| `:111-126` | `AddAuthorization`: one policy per entry in `RbacPolicies.PolicyPermissions`, plus `FallbackPolicy` outside Development | — |
| `:128` | `IAuthorizationHandler` → `PermissionAuthorizationHandler` | **Singleton** |
| `:129` | `AddControllers().AddApplicationPart(typeof(AuthController).Assembly)` | — |
| `:130-132` | `IAuthRepository`→`AuthRepository`, `IAuthService`→`AuthService`, `IRefreshTokenService`→`RefreshTokenService` | Scoped |
| `:133` | `IJwtService`→`JwtService` | **Singleton** |

Note that the controller lives in the Auth project and is grafted into the API's MVC part list
(`:129`) — `TeamHub.API` therefore has no `AuthController` file of its own. **Verified.**

The request pipeline order matters for the cookie/CORS interaction
(`Program.cs:113-118`):

```
UseExceptionHandler → UseHttpsRedirection → UseStaticFiles → UseCors(FrontendCorsPolicy)
  → UseAuthentication → UseAuthorization → MapControllers
```

`AddInfrastructure(connectionString)` runs **before** `AddTeamHubAuth`, which is required because
`AuthRepository` consumes `IDbConnectionFactory` (`Infrastructure/AuthRepository.cs:8`), registered
as a singleton in `TeamHub.Infrastructure/DependencyInjection.cs:32`. **Verified ordering, no
failure mode observed in the build.**

---

## 3. Local JWT scheme options

```csharp
// TeamHubAuthServiceCollectionExtensions.cs:61-74
options.MapInboundClaims = false;
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,   ValidIssuer = issuer,
    ValidateAudience = true, ValidAudience = audience,
    ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
    ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
    NameClaimType = "Username",
    RoleClaimType = "Role"
};
```

* `MapInboundClaims = false` — claim types stay exactly as written (`"Role"`, not the long WS-Fed
  URI). This is **required** for `context.User.IsInRole("Admin")` and
  `FindAll("permission")` to work, because `RoleClaimType` is set to `"Role"` (`:73`). **Verified.**
* `ClockSkew = 30s` — 15-minute tokens therefore stay accepted for up to 15:30. **Verified.**
* Signing algorithm: `SecurityAlgorithms.HmacSha256` with a `SymmetricSecurityKey`
  (`JwtService.cs:32-34`, scheme-side `:52, :69`). Symmetric ⇒ **any holder of the secret can mint
  tokens for any user, including role `Admin`.** *Recommended*: move to an asymmetric/Entra-only
  signing key in production.

---

## 4. Entra scheme options and the claims bridge

```csharp
// TeamHubAuthServiceCollectionExtensions.cs:86-107
options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
options.Audience = entraAudience;
options.MapInboundClaims = false;
// ValidateIssuer = true (metadata-derived), ValidateIssuerSigningKey = true,
// ValidateLifetime = true, ClockSkew = 30s, NameClaimType = "preferred_username",
// RoleClaimType = "Role"
options.Events = new JwtBearerEvents { OnTokenValidated = EntraClaimsIssuer.OnTokenValidated };
```

Required configuration when enabled: `Auth:Entra:TenantId` (`:80-81`) and `Auth:Entra:Audience`
falling back to `Auth:Entra:ClientId` (`:82-84`); `Auth:Entra:Instance` defaults to
`https://login.microsoftonline.com/` (`:79`). Issuer validation is delegated to the tenant's OIDC
metadata rather than a literal string — the code comment at `:89-90` states exactly that
rationale, so it is **Verified from the comment**, not inferred.

`EntraClaimsIssuer.OnTokenValidated` (`EntraClaimsIssuer.cs:17-55`), verified step by step:

1. Read `tid` and `oid` from the validated principal (`:20-21`).
2. If either is blank → `context.Fail("The Entra token is missing the required tid/oid claims.")`
   (`:23-27`) → request ends 401.
3. `RequestServices.GetRequiredService<IAuthRepository>()` then
   `GetUserByEntraIdAsync(tid, oid, HttpContext.RequestAborted)` (`:29-30`) — resolves the SQL at
   `AuthRepository.cs:28-38` (`WHERE u.EntraTenantId = @TenantId AND u.EntraObjectId = @ObjectId`).
4. If the local user is missing, `IsActive = 0`, or the linked employee is inactive →
   `context.Fail(...)` (`:31-35`). **A valid Entra token alone never grants access.**
5. Otherwise append `UserId`, `EmployeeId`, `Username`, `Email`, `DepartmentId`, `DesignationId`,
   one `Role` claim per role, one `permission` claim per permission (`:37-54`).

*Verified gap:* the injected claims include `Username` but the Entra token already carries
`preferred_username`, so `ClaimsPrincipal.Identity.Name` may resolve from either; nothing in the
codebase reads `Identity.Name` (no hits for `Identity.Name` in `*.cs`), so this has no current
effect. **Unknown** whether intentional.

---

## 5. Access-token contract

`JwtService.CreateAccessToken` (`JwtService.cs:37-67`) — the complete, verified claim set:

| Claim | Value | Source line |
| --- | --- | --- |
| `sub` | `UserId` (invariant culture string) | `:43` |
| `UserId` | same value, duplicated | `:44` |
| `EmployeeId` | `AuthUser.EmployeeId` | `:45` |
| `Username` | `AuthUser.Username` | `:46` |
| `Email` | `AuthUser.Email` | `:47` |
| `DepartmentId` | `Employees.DepartmentId` | `:48` |
| `DesignationId` | `Employees.DesignationId` | `:49` |
| `jti` | `Guid.NewGuid().ToString("N")` | `:50` |
| `Role` (n×) | one per role from `UserRoles`→`Roles` | `:52` |
| `permission` (n×) | one per permission code | `:53` |

* Lifetime `TimeSpan.FromMinutes(15)` (`:14`); returned as `ExpiresIn = 900` seconds inside
  `AuthResponse` (`:63-67`, DTO at `DTOs/AuthRequests.cs:18-22`).
* `notBefore: now`, `expires: now + 15 min` (`:59-60`).
* **No token revocation list**: `jti` is generated but never stored or checked anywhere (verified —
  no `jti` reference outside `JwtService.cs:50`). A stolen access token stays valid for its full
  15 minutes even after `logout-all`. *Recommended*: short-lived tokens are the mitigation the
  design relies on; a `jti` denylist would require storage.
* `AuthResponse` carries roles and permissions in the body (`:66-67`) purely so the SPA can drive
  its guards; the server never trusts client-side copies. **Verified** from the server code path.

### Configuration keys actually consumed

| Key | Read at | Value in `appsettings.json` |
| --- | --- | --- |
| `Authentication:Jwt:Issuer` | `Extensions:40`, `JwtService:21` | `"TeamHub"` |
| `Authentication:Jwt:Audience` | `Extensions:42`, `JwtService:23` | `"TeamHubClient"` |
| `Authentication:Jwt:SecretKey` | `Extensions:44`, `JwtService:25` | `"ThisIsAVerySecureDevelopmentSecretKey123456789"` (`appsettings.json:14`) |
| `Auth:Entra:Enabled` | `Extensions:28`, `AuthController:190-192` | `false` (`:20`) |
| `Auth:Entra:Instance` | `Extensions:79` | `"https://login.microsoftonline.com/"` (`:21`) |
| `Auth:Entra:TenantId` | `Extensions:80` | `"<TENANT_ID>"` placeholder (`:22`) |
| `Auth:Entra:ClientId` | `Extensions:83` (Audience fallback) | `"<API_CLIENT_ID>"` (`:23`) |
| `Auth:Entra:Audience` | `Extensions:82` | `"<API_CLIENT_ID>"` (`:24`) |
| `Auth:DevSignIn:AllowedRoles` | `AuthService:75` | all nine seeded roles (`appsettings.json:26-32`) |
| `Frontend:AllowedOrigins` | `Program.cs:32`, `AuthController:202` | `["http://localhost:4200","https://localhost:4200"]` (`:31-34`) |
| `ConnectionStrings:TeamHub` | `Program.cs:35` | SQLEXPRESS / Integrated Security (`:8-9`) |

**Verified finding (high severity):** the signing secret is committed in `appsettings.json:14`. It
is not overridden anywhere — `appsettings.Development.json` contains only `Logging` (9 lines, no
`Authentication` section). Anyone with repository read access can forge an `Admin` token for any
`UserId`. *Recommended*: environment variable / secret provider; the code comment at
`Extensions:46` already says "using a secure secret provider or environment variable".

`AuthController` and `AuthService` each re-read configuration independently
(`AuthController.cs:190-192, :202`, `AuthService.cs:76`) rather than binding a typed options
class. **Verified**; no `IOptions<…>` appears in `TeamHub.Auth`.

---

## 6. Refresh tokens: generation, storage, rotation, revocation

### Storage model

`dbo.RefreshTokens` (`authentication-schema.sql:43-62`):
`RefreshTokenId BIGINT IDENTITY` PK, `UserId INT` FK, `LoginHistoryId BIGINT` FK →
`UserLoginHistory`, `Token NVARCHAR(500)`, `ExpiryDate DATETIME2`, `CreatedDate`, `RevokedDate`,
`ReplacedByToken NVARCHAR(500)`, `CreatedByIp`, `RevokedByIp`, `UserAgent`.
Indexes: `UNIQUE IX_RefreshTokens_Token ON (Token)` (`:66`),
`IX_RefreshTokens_UserId_Active ON (UserId, RevokedDate, ExpiryDate)` (`:70`).
`dbo.UserLoginHistory` (`:27-40`) holds `LoginDate`, `LogoutDate`, `IpAddress NVARCHAR(50)`,
`UserAgent NVARCHAR(500)`, `IsSuccessful`; index `IX_UserLoginHistory_UserId ON (UserId, LoginDate)`
(`:74`).

### Token material

```csharp
// Services/RefreshTokenService.cs:105-109
internal static string HashToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
private static string GenerateToken() =>
    WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
```

**Verified:** 64 cryptographically random bytes → 86-char Base64Url string handed to the client;
only its SHA-256 hex (128 chars) is stored. A database dump therefore does not yield usable
tokens. `RandomNumberGenerator` (not `Random`/`Guid`) is used. Lifetime
`TimeSpan.FromDays(7)` (`:13`).

### Create path

`AuthService.DevSignInAsync` (`AuthService.cs:16-47`) → `repository.RecordLoginAsync(...)` →
`refreshTokenService.CreateAsync(userId, loginHistoryId, …)` → `jwtService.CreateAccessToken(user)`.
`StoreRefreshTokenAsync` (`AuthRepository.cs:112-140`) inserts the hash with
`ExpiryDate = DateTimeOffset.UtcNow.Add(7 days)` passed as `expiryDate.UtcDateTime`.

`RecordLoginAsync` (`AuthRepository.cs:76-110`) is the only Auth method that opens an explicit
transaction: it inserts `UserLoginHistory` (`OUTPUT INSERTED.LoginHistoryId`) and, **only when
`isSuccessful`**, updates `Users.LastLoginDate`, then commits (`:99-108`). Both statements share
the transaction. `IpAddress`/`UserAgent` are truncated by `Limit(value, 50|500)` (`:356-357`) so a
long UA cannot raise a string-or-binary-data error — **Verified**, and it silently truncates
instead of rejecting.

### Rotate path (`POST /api/auth/refresh-token`)

`RefreshTokenService.RotateAsync` (`:34-87`), verified order of checks:

| # | Condition | Line | Result |
| --- | --- | --- | --- |
| 1 | `session is null \|\| !IsActive \|\| !IsEmployeeActive` | `:42-45` | `null` → 401 |
| 2 | `RevokedDate is not null` | `:47-51` | **reuse detection**: `RevokeAllRefreshTokensAsync(userId)`, then `null` → 401, every session for the user dies |
| 3 | `ExpiryDate <= UtcNow` | `:53-56` | `null` → 401 |
| 4 | `RotateRefreshTokenAsync` returns `false` | `:70-73` | `null` → 401 |
| 5 | success | `:75-87` | rebuild `AuthUser` **from the DB row just read**, issue fresh access token + fresh refresh token |

`RotateRefreshTokenAsync` (`AuthRepository.cs:181-253`) is transactional and **compare-and-swap**:
the UPDATE carries `WHERE RefreshTokenId = @Id AND Token = @CurrentTokenHash AND RevokedDate IS
NULL AND ExpiryDate > SYSUTCDATETIME()` (`:198-199`). If `updated == 0` — i.e. the row moved under
us between the read and the update, the classic double-submit race — it revokes **all** the user's
tokens and closes open login-history rows, commits, and returns `false` (`:213-230`). Otherwise it
inserts the replacement token inside the same transaction (`:232-251`). **Verified** correct
rotation semantics under concurrency; the DB-side `ExpiryDate > SYSUTCDATETIME()` predicate means
expiry is re-checked server-side, not only in C#.

**Verified note:** role/permission changes are picked up on the *next refresh* (up to 7 days), not
on the next request — `RotateAsync` reuses the roles read by `GetRefreshSessionAsync`
(`AuthRepository.cs:161-162`), which queries `UserRoles` live. An admin removed from a role keeps
the old access token for ≤15 min and the old refresh session until it is rotated once.

### Revoke paths

* `RevokeCurrentAsync` (`RefreshTokenService.cs:90-100`) → `RevokeRefreshTokenAsync`
  (`AuthRepository.cs:255-277`): two UPDATEs in **one batch, no explicit transaction** — sets
  `RevokedDate`/`RevokedByIp` on the matching token, then closes `UserLoginHistory.LogoutDate` by
  joining `RefreshTokens ON rt.LoginHistoryId = h.LoginHistoryId`. Guarded by
  `if (!string.IsNullOrWhiteSpace(refreshToken))` (`:96`), so logout without a cookie still returns
  204 (see `AuthController.cs:134-144`).
* `RevokeAllAsync` → `RevokeAllRefreshTokensAsync` (`:279-294`): revokes all unrevoked tokens and
  closes all open successful login-history rows for the user. **Verified.** Same no-transaction
  shape.

**Verified findings:**
* `RefreshTokenLifetime` is declared **twice** with the same value but no shared source:
  `AuthController.cs:23` (used for cookie `MaxAge`) and `RefreshTokenService.cs:13` (used for DB
  `ExpiryDate`). Changing one without the other silently breaks sessions.
* `AuthRepository.cs:391-393` declares `internal sealed class AuthValidationException : Exception`
  and **nothing throws or catches it** (verified: only definition hit across all `*.cs`). Dead type.
* `GetRolesAsync` takes an `employeeId` parameter (`AuthRepository.cs:319`) that the body never
  uses (`:322-334`) — the comment at `:322-324` records that the legacy `Employees.RoleId` fallback
  was removed. Dead parameter, documented intent.

---

## 7. The refresh cookie and the browser-facing contract

```csharp
// Controllers/AuthController.cs:207-228
Response.Cookies.Append("teamhub.refreshToken", token, new CookieOptions
{
    HttpOnly = true, Secure = true, SameSite = SameSiteMode.None,
    Path = "/api/auth", MaxAge = RefreshTokenLifetime, IsEssential = true
});
```

| Property | Value | Verified consequence |
| --- | --- | --- |
| `HttpOnly` | `true` | JS cannot read it; the SPA never handles the refresh token |
| `Secure` | `true` (unconditional) | Cookie is only sent over HTTPS. *Inferred* (browser behavior, not project evidence): Chrome and Firefox treat `http://localhost` as trustworthy, which is why `environment.apiUrl = 'http://localhost:5169/'` works in development. Over plain HTTP on a non-localhost host the cookie would be dropped and refresh would silently fail. **Recommended:** derive `Secure` from `environment.IsDevelopment()`. |
| `SameSite` | `None` | Required for cross-origin XHR (SPA on `:4200`, API on `:5169`); only valid because `Secure = true`. |
| `Path` | `/api/auth` | Narrowest scope that still covers all six auth endpoints; the cookie is not attached to business API calls. |
| `MaxAge` | 7 days | Matches the DB expiry (via a duplicated constant — see findings above). |
| `IsEssential` | `true` | Survives a `CookiePolicy` that strips non-essential cookies (none is configured — see `14-known-issues.md`). |

`ClearRefreshCookie()` (`:220-228`) deletes with matching `Path`/`Secure`/`SameSite`/`HttpOnly`
options — necessary, because `Response.Cookies.Delete` only works when the attributes match.
**Verified correct.**

Every response that sets a token adds `Response.Headers.CacheControl = "no-store"`
(`:62`, `:119`). **Verified.**

---

## 8. Auth endpoints

`AuthController` is `[ApiController] [Route("api/auth")]` with **no class-level `[Authorize]`**
(`:14-16`) — each action declares its own requirement. All six, verified:

| Method + route | Attribute | Guard chain | Success | Failure |
| --- | --- | --- | --- | --- |
| `POST dev-sign-in` (`:30`) | `[AllowAnonymous]` `:29` | `IsDevelopmentBypassAllowed()` → `HasTrustedOrigin()` → `AuthService.DevSignInAsync` | 200 `AuthResponse` + cookie `:61-63` | 404 `:40`; 403 bare `:45`; 403 ProblemDetails "Development sign-in denied" `:55-58` |
| `GET dev-users` (`:68`) | `[AllowAnonymous]` `:67` | `IsDevelopmentBypassAllowed()` | 200 `DevSignInUser[]` `:79` | 404 `:75` |
| `POST refresh-token` (`:84`) | `[AllowAnonymous]` `:83` | `HasTrustedOrigin()` → cookie present → `RotateAsync` | 200 `AuthResponse` + rotated cookie `:118-120` | 403 `:91`; 401 "Refresh token required" `:98-101`; 401 "Refresh token invalid" `:112-115` (cookie cleared both times) |
| `POST logout` (`:125`) | `[Authorize]` `:124` | `HasTrustedOrigin()` → `TryGetUserId` | 204 `:144` | 403 `:131`; 401 from `[Authorize]`/`FallbackPolicy`; **silent no-op** when the `UserId` claim is unparseable (`:134`) |
| `POST logout-all` (`:149`) | `[Authorize]` `:148` | same | 204 `:167` | 403 `:155`; 401 |
| `GET me` (`:172`) | `[Authorize]` `:171` | `TryGetUserId` → `GetCurrentUserAsync` | 200 `CurrentUserResponse` `:183` | 401 `:179` and 401 when the user is null/inactive `:183` |

`TryGetUserId` (`:186-187`) is `int.TryParse(User.FindFirstValue("UserId"), out userId)` — it reads
the **`UserId` claim**, not `sub` or `Identity.Name`, which is why both schemes must emit `UserId`
(`JwtService.cs:44`, `EntraClaimsIssuer.cs:39`). **Verified coupling.**

### Origin gate

```csharp
// AuthController.cs:194-205
private bool HasTrustedOrigin()
{
    var origin = Request.Headers.Origin.ToString();
    if (string.IsNullOrEmpty(origin)) { return true; }        // :197-199
    var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200", "https://localhost:4200"];
    return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
}
```

**Verified:** because the refresh endpoint is cookie-driven and `SameSite=None`, an explicit
`Origin` check is the CSRF defence. But a **missing `Origin` header is accepted** (`:197-199`) —
non-browser clients (curl, Postman, server-to-server) can call `POST /api/auth/refresh-token` with
a stolen cookie value and no origin at all. Browsers omit `Origin` on same-origin GETs but send it
for cross-origin `POST`s, so the practical CSRF hole is closed for browser attackers; the
allow-list is also case-insensitive while CORS (`Program.cs:41-50`) is not, a harmless mismatch.
*Recommended:* require `Origin` (or a custom header) for the cookie-consuming POSTs.

### Development bypass chain (all four gates must pass)

1. Environment is Development **and** `Auth:Entra:Enabled` is false — `IsDevelopmentBypassAllowed()`
   (`:190-192`), else **404** (the endpoint is indistinguishable from missing).
2. Trusted `Origin` (`:194-205`), else 403.
3. `AuthService.DevSignInAsync` re-reads the user from the DB and requires `IsActive` **and**
   `IsEmployeeActive` (`AuthService.cs:24-27`).
4. The user must hold at least one allowed role, compared `OrdinalIgnoreCase` (`:29-33`), against
   `Auth:DevSignIn:AllowedRoles` (in-code fallback `["Admin","NormalUser"]`, `:14`, `:74-77`).
   The config lists every seeded role, and `seed-data.sql` §10 gives each of them its own
   `Users` + `UserRoles` row, so all nine rows of the Role Permission Matrix are reachable.

Only then are the token pair and cookie issued. The request body is
`DevSignInRequest { [Range(1,int.MaxValue)] required int UserId }` (`DTOs/AuthRequests.cs:10-15`) —
**the client cannot supply roles or permissions**, as the XML comment states (`:6-9`).
`GetDevSignInCandidatesAsync` (`AuthRepository.cs:40-74`) applies the same role filter server-side
so the picker only lists accounts the server would actually accept (comment at `:61-62`), by
running `GetRolesAsync` **once per candidate** — verified N+1: `GET /api/auth/dev-users` issues
`1 + N` queries. Acceptable for a Development-only picker. *Recommended*: single set-based query.

---

## 9. RBAC model

### Data

`Roles` ← `UserRoles(UserId, RoleId)` → `Users`; `Permissions(PermissionCode)` ←
`RolePermissions(RoleId, PermissionId)` → `Roles`. Soft-deletable: `Roles.IsActive`,
`Permissions.IsActive`. Junction rows are hard-deleted (see `07-repositories.md`). See
`02-database.md` for full DDL; the RBAC-specific script is `rbac-schema.sql`.

Resolution happens in SQL, once per authentication
(`AuthRepository.cs:316-354`):

```sql
-- GetRolesAsync :325-331
SELECT r.RoleName FROM dbo.UserRoles ur
INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId AND r.IsActive = 1
WHERE ur.UserId = @UserId ORDER BY r.RoleName;

-- GetPermissionsAsync :342-350
SELECT DISTINCT p.PermissionCode FROM dbo.UserRoles ur
INNER JOIN dbo.Roles r  ON r.RoleId  = ur.RoleId AND r.IsActive = 1
INNER JOIN dbo.RolePermissions rp ON rp.RoleId = ur.RoleId
INNER JOIN dbo.Permissions p ON p.PermissionId = rp.PermissionId AND p.IsActive = 1
WHERE ur.UserId = @UserId ORDER BY p.PermissionCode;
```

**Verified consequences:** inactive roles/permissions never reach a token; a user with no rows in
`UserRoles` gets an empty array (never `null`) because `QueryAsync(...).AsList()` is used
(`:333`, `:353`); `DISTINCT` collapses permissions shared by several roles. There is no
`Roles.IsActive`-independent path and no `Employees.RoleId` fallback (`:322-324`).

### Enforcement

```csharp
// Authorization/RbacPolicies.cs:10-20
ManagePermissions      = "rbac:permissions.manage"       → permission "permissions.manage"
AssignUserRoles        = "rbac:user-roles.assign"        → permission "user-roles.assign"
ManageRolePermissions  = "rbac:role-permissions.manage"  → permission "role-permissions.manage"
```

Each becomes `RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(code))`
(`Extensions:113-118`). `PermissionRequirement` holds one string
(`PermissionRequirement.cs:9-12`).

`PermissionAuthorizationHandler` (`PermissionAuthorizationHandler.cs:11-38`) is fail-closed,
verified line by line: unauthenticated → return without `Succeed` (`:20-23`);
`IsInRole("Admin")` → `Succeed` (`:25-29`) — **super-admin bypass**;
otherwise any `permission` claim equal to the required code, `OrdinalIgnoreCase` (`:31-35`); no
`context.Fail()` anywhere, so denial is simply "never satisfied" (`:9-10` comment states this
intent). Registered as a singleton (`Extensions:128`) — stateless, safe.

`RbacClaims` helpers (`:41-52`): `RoleNames()` = all `"Role"` claims, `PermissionCodes()` = all
`"permission"` claims, `IsSuperAdmin()` = `IsInRole("Admin")`. Consumed by the matrix services as
documented in `06-application-services.md`.

### Where policies are applied — and where roles are used instead

Every one of the 22 controllers carries a class-level authorization attribute (verified attribute by
attribute in [09-api-endpoints.md](09-api-endpoints.md) §2):

| Group | Count | Attribute | Effect |
| --- | --- | --- | --- |
| RBAC management | 3 | `[Authorize(Policy = RbacPolicies.…)]` (`PermissionsController.cs:13`, `UserRolesController.cs:13`, `RolePermissionsController.cs:13`) | Admin **or** the matching `permission` claim |
| Master data | 14 | class-level `[Authorize(Policy = RbacPolicies.Master*)]` — one per module (`DepartmentsController.cs:13`, `DesignationsController.cs:13`, `BusinessUnitsController.cs:13`, `DressSizesController.cs:13`, `CotivityDepartments/Designations/EmployeesController.cs:13`, `RolesController.cs:13`, `AddressTypesController.cs:13`, `BloodGroupsController.cs:13`, `SkillsController.cs:13`, `JobTypesController.cs:13`, `TeamsController.cs:13`, `GendersController.cs:13`) | the same code each **Manage** menu item carries (doc 11 §12.1) |
| Lifecycle | 2 | class `[Authorize]` + per-action policies — `EmployeeReleasesController.cs:33, :48, :68, :82` (`resignations.view`), `:157` (`resignations.add`), `:211` (`…exitchecklist`), status transitions resolved per target state at `:174-190`; `EmployeeTerminationsController.cs:16, :31, :50, :64` (`terminations.view`) vs `:85` (`terminations.add`) vs `:108, :125, :145` (`terminations.manage`) | read / file / each workflow edge separated by code |
| Employees | 1 | `[Authorize]` (`EmployeesController.cs:18`) + `PeopleCreate` `:60` / `PeopleUpdate` `:97` + **5 `[AllowAnonymous]` reads** (`:24, :42, :113, :132, :163`); `people.release` is checked in code, not by attribute (`EmployeeReleasesController.cs:110, :129`) | signed-in for writes/masters; **anonymous** for the grid, detail, department list and hierarchy |
| Recruitments | 1 | `[Authorize(Roles = "Admin")]` (`RecruitmentsController.cs:12`) | the only role-only surface left — no permission code behind it |
| Auth | 1 | per-action (`[AllowAnonymous]` × 3, `[Authorize]` × 3) | see §8 |

* `FallbackPolicy = RequireAuthenticatedUser` exists **only outside Development**
  (`Extensions:120-125`). Its practical effect is small — every business controller already declares
  its own `[Authorize]` — but it is what protects any *future* controller and every endpoint that has
  no attribute at all.
* In Development there is no fallback policy (`:120`), yet the attributes still apply, so the API is
  **not** wide open: the same 7 `[AllowAnonymous]` endpoints (`EmployeesController.cs:24, :42, :113,
  :132, :163`; `DepartmentsController.cs:16, :31`) are anonymous in **all** environments. See
  `09-api-endpoints.md` §2 for the data-exposure consequence.
* Role-based protection works in both schemes only because `RoleClaimType = "Role"` and the Entra
  bridge injects `Role` claims; a `[Authorize(Roles = …)]` against an Entra token without the
  `OnTokenValidated` hook would never match.

**Verified (re-audited 2026-10-23):** permission granularity now reaches almost the whole catalog.
`RbacPolicies.PolicyPermissions` (`RbacPolicies.cs:52-86`) defines **32** code-backed policies,
loop-registered in `TeamHubAuthServiceCollectionExtensions.cs:111-118`, and the controllers above apply
them. `rbac-schema.sql:119-161` seeds **36** codes (`:122-157`), and `:164-172` mirrors the entire
catalog onto the `Admin` role:

| Module | Codes seeded | Enforced by a server policy? | Read by the SPA? |
| --- | --- | --- | --- |
| People | `people.create`, `people.update`, `people.release` | ✅ (`EmployeesController.cs:60, :97`; `people.release` programmatically at `EmployeeReleasesController.cs:110, :129`) | ✅ directory row actions (`employee-directory.component.ts:74-76`) |
| People (read) | `people.view`, `people.terminate`, `people.hierarchy.view` | ❌ **no policy exists** for these three | ❌ deliberately unused — `People` is session-gated in the menu, and the Terminate row action checks `terminations.add` (`employee-directory.component.ts:78`) |
| Resignations | `resignations.view`, `resignations.add` + 7 `resignations.action.*` | ✅ all nine (attributes + the status map `EmployeeReleasesController.cs:174-190`) | ✅ menu (`menu.config.ts:53-69`), list actions (`employee-lifecycle-list.ts:48-56, :64-75`), form gates |
| Resignations (aggregate) | `resignations.manage` | ❌ no policy (superseded by the per-action codes) | ❌ unused by the menu |
| Terminations | `terminations.view`, `terminations.add`, `terminations.manage` | ✅ all three — `terminations.add` guards only `POST employee/{id}` (`EmployeeTerminationsController.cs:85`), `manage` covers update/transition/clearance (`:108, :125, :145`) | ✅ menu (`menu.config.ts:70-76`), `terminationAddGuard` on both create routes (`app.routes.ts:73, :95`), "New Termination" button (`employee-lifecycle-list.html:6`) + directory terminate action |
| Settings | 14 `master.<module>` codes | ✅ one policy per module (14 controllers) | ✅ one code per Manage menu child (`menu.config.ts:83-113`) + `masterGuard` (`auth.guard.ts`) |
| Settings (legacy) | `master.view`, `master.manage` — **not in the seed any more**, still present in the live `Permissions` table | ❌ | ❌ unused |
| RBAC | `permissions.manage`, `user-roles.assign`, `role-permissions.manage` | ✅ all three | ✅ menu (`menu.config.ts:107, :126, :132`), `rbacGuard` (`auth.guard.ts:52-63`), inline `canEdit` getters (`employee-role-matrix.component.ts:53`, `role-permission-matrix.component.ts:53`) |

Six catalog codes are therefore **grantable in the Role Permission Matrix and carried in the JWT, but
gate nothing**: `people.view`, `people.terminate`, `people.hierarchy.view`, `resignations.manage`, plus
the two legacy `master.*` rows. The navigation never references them (§10, doc 11 §12.1), which is why
hiding an item can never disagree with the API. *Recommended*: retire them (`IsActive = 0` keeps the
`RolePermissions` FKs valid) or bind `people.view` to the five `[AllowAnonymous]` reads. **Unknown**
whether they are deliberately staged or abandoned.

**Verified (seeded since 2026-10-23):** the non-admin roles no longer rely on hand-ticked grants —
`seed-data.sql:429-493` grants enforced subsets idempotently (natural keys `RoleName` + `PermissionCode`,
`WHERE NOT EXISTS`, `IsActive = 1` on both sides): `NormalUser` → `resignations.add`; `Manager` →
`resignations.view`/`.add` + `submit`/`sendforapproval`/`approve`/`reject`/`cancel` + `master.team`;
`HR` → `people.create`/`.update`/`.release`, `resignations.view`/`.add` + `submit`/`sendforapproval`/
`cancel`/`exitchecklist`/`release` and all three termination codes — `terminations.add` to file a record,
`terminations.manage` to edit and transition it. Both "Add" buttons therefore have a code of their own
(`resignations.add`, `terminations.add`), so filing can be granted without granting rewrite access.
None of the six inert codes is granted to any role. Applied to the live DB: `NormalUser` 1, `Manager` 8,
`HR` 13, `Admin` 38 grants. Caveats: grants reach a client only at the next sign-in or token refresh
(doc 12 §2.3a) — and because the SPA has no Admin role fallback, an already-signed-in Admin loses the
"New Termination" / Terminate affordances until that refresh; `Manager`/`HR` still have no `Users` rows
and sit outside `Auth:DevSignIn:AllowedRoles`, so they cannot be signed in as yet (doc 14 M9).

### Writing assignments (the privileged path)

| Path | Authorization | Server-side guard | Audit value written |
| --- | --- | --- | --- |
| Employee create/update → role assignment (`POST`/`PUT /api/Employees`) | `[Authorize(Roles = "Admin")]` (`EmployeesController.cs:59, :96`) | **none** — `EmployeeRepository.AssignEmployeeRolesAsync` (`:878-931`) silently drops inactive role ids (`WHERE r.IsActive = 1` `:926`) but does not compare the requested roles against the caller | `CreatedBy = "System"` (`EmployeeService.cs:67, :98`) |
| `PUT api/user-roles/**` | policy `rbac:user-roles.assign` | `UserRoleMatrixService` privilege-escalation check → `PrivilegeEscalationException` (403) | `CreatedBy = actingRoleNames.FirstOrDefault() ?? "System"` (`UserRoleMatrixService.cs:45`) — a **role name** |
| `PUT api/role-permissions/**` | policy `rbac:role-permissions.manage` | `RolePermissionMatrixService` escalation check → `RbacValidationException` (400) | `CreatedBy = "System"` (`RolePermissionMatrixService.cs:50`) |

Verified in detail in [06-application-services.md](06-application-services.md) and
[07-repositories.md](07-repositories.md). RBAC is validated **twice** on the matrix paths (service
guard, then repository transaction which throws again), and the repository's `SaveAsync`
delete-then-insert-per-id shape means a matrix save is 1 `DELETE` + N `INSERT`s in one transaction
(`UserRoleRepository.cs:105-140`).

**Verified asymmetry:** the employee form's role picker is the *only* role-assignment path with no
privilege-escalation guard — `CreateOrUpdateEmployeeRequest.RoleIds` is written straight into
`UserRoles` (`EmployeeService.cs:98`). It is nonetheless Admin-gated (`EmployeesController.cs:59,
:96`), so an escalator would already hold the super-admin role. *Inferred*: the guard was added only
to the two matrix endpoints because they were designed for non-Admin delegates. **Unknown** from
repository evidence.

---

## 10. Frontend auth layer (all six files read; line refs re-verified after the 2026-10-23 session-signal change)

### State: `AuthService` (`core/auth/auth.service.ts`, 120 lines)

| Key | Storage | Line |
| --- | --- | --- |
| `teamhub.accessToken` | **`sessionStorage`** | `:11`, read `:21` / `:36`, written `:114` |
| `teamhub.roles` | `sessionStorage`, `\|`-joined | `:12, :115` |
| `teamhub.permissions` | `sessionStorage`, `\|`-joined | `:13, :116`, read `:38` |
| refresh token | HttpOnly cookie held by the browser | never in JS |

`isAuthenticated` is `accessToken !== null` (`:24-26`) — **presence only, no expiry check**; the
client cannot know that a token expired 15 minutes ago and relies on the 401 → refresh flow
(`:72-86`). `hasRole` / `hasPermission` compare lower-cased (`:46-56`), matching the server's
`OrdinalIgnoreCase`. `refreshInFlight$` + `shareReplay({bufferSize:1, refCount:false})` +
`finalize` clear (`:14, :72-86`) collapses concurrent 401s into one round-trip; **verified** the
in-flight handle is cleared in `finalize`, so a later 401 starts a fresh request.
`restoreSession()` is an alias for `refreshAccessToken()` (`:88-90`).
`logout`/`logoutAll` clear local state in `finalize`, i.e. **even on HTTP failure** (`:96-104`).

**Two reactive members were added for the navigation menu** (`auth.models.ts:31-34` defines the
`AuthSession` contract): a private `sessionRevision = signal(0)` (`:16`) that `storeSession` (`:117`)
and `clearSession` (`:110`) each increment, and `sessionState = computed<AuthSession>` (`:33-44`)
which reads the revision for the dependency and returns `{ isAuthenticated, permissions: Set<string> }`
with the codes trimmed and lower-cased. Because every write path funnels through those two methods,
sign-in, token rotation, account switching and logout all invalidate permission-driven UI in one step,
and nothing outside `AuthService` has to know when sessionStorage changed. `hasPermission()` is
unchanged, so the guards and the pages that still call it behave exactly as before.

**Verified finding:** the access token lives in `sessionStorage`, readable by any XSS payload —
mitigated only by the 15-minute lifetime and by keeping the refresh token HttpOnly.
*Recommended:* keep the access token in memory and rely on `restoreSession()` on boot.

### Interceptor (`auth.interceptor.ts`, 37 lines)

* Attaches `Authorization: Bearer ${token}` only when a token exists (`:13-16`).
* Sets `withCredentials: true` for any URL containing `/api/auth/` (`:11, :16`) so the refresh
  cookie is sent — **verified necessary** because the cookie is `SameSite=None`.
* On 401, unless the failing call was `dev-sign-in` or `refresh-token` (`:12, :21`), it calls
  `refreshAccessToken()` and replays the original request with the new bearer (`:25-28`). Note the
  replay clones the **original** `request` and sets only `Authorization` (`:26-27`) — so `
  withCredentials` is not re-applied; harmless because the replayed request is never an auth
  endpoint (those are excluded from the retry path).
* If refresh fails: `clearSession()` + navigate `/login` + rethrow (`:29-33`).
* **Verified limitation:** the interceptor does not serialise *which* request is retried against a
  *new* token beyond the single in-flight refresh; and a 401 on a non-auth endpoint whose refresh
  succeeds is retried exactly once (no loop, since `catchError` wraps only the refresh).
* **Verified gap:** requests to `/api/auth/*` that are not `dev-sign-in`/`refresh-token`
  (e.g. `logout`, `me`) are still excluded from retry only via the regex at `:12`, which matches
  just those two — `POST logout` returning 401 will trigger a refresh and a replay.

### Guards (`auth.guard.ts`, 95 lines)

| Guard | Behaviour | Line |
| --- | --- | --- |
| `authenticatedGuard` (`= authGuard`, `:24`) | pass if token present; else `restoreSession()`; on failure clear session + redirect `/login?returnUrl=…` | `:7-22` |
| `signedInLandingGuard` | already signed in → `/people`; else try restore → `/people`, failure → stay (`of(true)`) | `:26-41` |
| `adminGuard` | `hasRole('Admin')` → pass, else `/home` | `:43-48` — **dead export**: no route references it any more (search-verified across `src/app/*.ts`), it is only cited in the Entra finding in doc 11 §13.15 |
| `rbacGuard(code)` (private factory) | `hasPermission(code)` → pass, else `/home` | `:52-59` |
| `employeeRoleMatrixGuard` / `rolePermissionMatrixGuard` | `user-roles.assign` / `role-permissions.manage` | `:61, :63` |
| `resignationViewGuard` / `resignationAddGuard` | `resignations.view` / `resignations.add` | `:66, :68` |
| `peopleCreateGuard` / `peopleUpdateGuard` / `peopleReleaseGuard` | `people.create` / `people.update` / `people.release` | `:71, :73, :75` |
| `terminationViewGuard` / `terminationAddGuard` | `terminations.view` / `terminations.add` | `:79, :81` |
| `masterGuard` | per-module: `paramMap.masterKey` → `master.<key>`, with `permission` → `permissions.manage` | `:85-94` |

**Verified:** the client-side permission strings are the *same codes* as
`RbacPolicies.PolicyPermissions` (`"user-roles.assign"`, `"role-permissions.manage"`,
`"resignations.add"`, `"terminations.add"`, `"people.create"`, `master.<key>`), so the two
sides cannot drift silently only because a developer kept them in sync by hand — there is no
generated contract. Guards are UX only; the server re-checks with its own policy.

### Navigation menu (`core/config`, `shared/components/nav-menu`) — visibility, not enforcement

Since 2026-10-23 the topbar renders a **permission-driven recursive menu** instead of hardcoded `@if`
blocks (full contract in `11-frontend.md` §12.1). From an auth point of view three properties matter:

| Property | Verified detail |
| --- | --- |
| Source of truth | `MenuService.visibleMenu` derives from `AuthService.sessionState()` (`menu.service.ts:20, :25-31`) — the effective codes the API issued, never a role name or a per-role menu |
| Codes used | `resignations.view/add`, `terminations.view`, `terminations.add`, the 14 `master.*` / `permissions.manage`, `user-roles.assign`, `role-permissions.manage` — **all** present in `RbacPolicies.PolicyPermissions`; the 4 unenforced catalog codes (§12 finding 2) are deliberately *not* used, so no item is shown or hidden by a code the API ignores |
| Security role | UI only. A hidden item does not protect an endpoint, and a visible item does not grant one: the route guards above re-check the same code, and every controller re-checks its policy. Direct navigation to an unauthorized URL is answered by the guard (`→ /home` `UrlTree`), asserted in `auth.guard.spec.ts` |
| Freshness | `sessionRevision` bumps on every write/clear, so a rotation that changes the grant set re-renders the nav without re-login; `clearSession()` in `finalize` guarantees no stale item after logout |
| Known asymmetry | the server also succeeds any policy for the `Admin` **role** (`PermissionAuthorizationHandler.cs:25-29`) while the menu has no role fallback (added deliberately: role-specific menu logic was out of scope). Today both agree because `rbac-schema.sql:164-172` grants Admin the whole catalog |

### Entra path (`entra-auth.service.ts` 24 lines, `msal.config.ts` 50 lines, `app.config.ts` 39 lines)

`app.config.ts:19-30` swaps the entire HTTP provider set on `environment.enableEntra`:

* `false` (current default, `environment.ts:7`) → `provideHttpClient(withInterceptors([authInterceptor]))`.
* `true` → `provideHttpClient(withInterceptorsFromDi())` + `MsalModule.forRoot(instance, guard,
  interceptor)`.

So with Entra enabled **`authInterceptor` is not registered at all**: no `sessionStorage` bearer,
no 401→refresh rotation. MSAL supplies the API access token instead
(`msal.config.ts:43-49`, protected-resource map `environment.apiUrl → [apiScope]`), using
authorization-code + PKCE via redirect for a public SPA client (comment `:11-15`).
`EntraAuthService` is only `loginRedirect({scopes:[apiScope]})` /
`logoutRedirect({postLogoutRedirectUri})` (`entra-auth.service.ts:16-23`). MSAL cache location is
`sessionStorage` (`msal.config.ts:25`).

**Verified:** `entra-auth.service.ts` is never referenced from `AuthService`; the dev and Entra
flows are parallel, not layered. **Unknown:** whether `AuthService.devSignIn`/`refreshAccessToken`
are still *called* when Entra is on (the login component decides; see `11-frontend.md`), and no
`environment.production` file exists in `src/environments/` (only `environment.ts`), so the
`apiUrlProduction` field at `environment.ts:4` is currently unused — verified by searching for
`apiUrlProduction` (no consumer).

---

## 11. End-to-end trace: development sign-in (verified across both repos)

```
Login component → AuthService.devSignIn(userId)                    auth.service.ts:67-70
  POST http://localhost:5169/api/auth/dev-sign-in  { userId }      withCredentials: true
    CORS: Program.cs:41-50 (WithOrigins + AllowCredentials)
    → AuthController.DevSignIn                                     AuthController.cs:34
      IsDevelopmentBypassAllowed()                                 :38 → :190-192
      HasTrustedOrigin()                                           :43 → :194-205
      → AuthService.DevSignInAsync                                 :48 → AuthService.cs:16
          AuthRepository.GetUserByIdAsync   (Users ⋈ Employees)     AuthRepository.cs:13-22
          MapUserAsync → GetRolesAsync + GetPermissionsAsync        :296-314, :316-354
          role allow-list check                                     AuthService.cs:29-33
          RecordLoginAsync (transaction: history + LastLoginDate)   AuthRepository.cs:76-110
          RefreshTokenService.CreateAsync → StoreRefreshTokenAsync  RefreshTokenService.cs:22-31
          JwtService.CreateAccessToken (15 min, roles+permissions)  JwtService.cs:37-67
    ← 200 AuthResponse { accessToken, expiresIn:900, roles, permissions }
    + Set-Cookie teamhub.refreshToken=…; HttpOnly; Secure; SameSite=None; Path=/api/auth; MaxAge=7d
  → storeSession(): sessionStorage accessToken/roles|permissions     auth.service.ts:113-118
  → sessionRevision++ → sessionState → MenuService.visibleMenu       :117 / :33-44 / menu.service.ts:20
  → topbar re-renders the filtered tree (UI only, §10)               nav-menu.component.html:2-29
  → guard/router navigates                                          auth.guard.ts:11-12
Each later API call: interceptor adds Bearer + validates server-side
  JwtBearerHandler (TokenValidationParameters, Extensions:59-75)
  → [Authorize]/FallbackPolicy → PermissionAuthorizationHandler when a policy applies
On 401: refreshAccessToken() → POST /api/auth/refresh-token (cookie only)
  → RotateAsync compare-and-swap → new cookie + new AuthResponse → replay once
```

Failure paths verified: 404 (mode/environment), 403 (origin, or account
missing/inactive/not-approved), 401 (missing/invalid/rotated cookie), 401 (token expired —
`ValidateLifetime` with 30s skew). Modelled in `TeamHub.API/auth.http` for manual checks
(see `13-testing.md`).

---

## 12. Verified findings (auth)

1. **Committed JWT signing secret** (`appsettings.json:14`, never overridden) — forgeable `Admin`
   tokens. Severity: high. *Recommended*: secret provider.
2. **Fine-grained permissions cover 32 of the 36 catalog codes** (`RbacPolicies.PolicyPermissions:52-86`;
   §9). Six codes remain unenforced and mislead the Role Permission Matrix: `people.view`,
   `people.terminate`, `people.hierarchy.view`, `resignations.manage` (no policy was ever written) and
   the legacy `master.view` / `master.manage` (still in the live table, gone from the seed). Employee
   reads stay `[AllowAnonymous]`, so `people.view` in particular protects nothing today. The navigation
   uses **only** enforced codes, so the menu can never show or hide an item based on a dead code
   (doc 11 §12.1). Severity: medium. See §9 and `09-api-endpoints.md` §2.
3. **`HasTrustedOrigin()` accepts a missing `Origin` header** (`AuthController.cs:197-199`) —
   non-browser callers bypass the CSRF gate. Severity: medium.
4. **`Secure = true` unconditional** (`:212`) — refresh breaks over plain HTTP on non-localhost
   hosts. Severity: medium (deployment-dependent). Browser behavior is *Inferred*.
5. **Access token in `sessionStorage`** (`auth.service.ts:21, :114`) — XSS-readable; 15-minute
   exposure window. Severity: medium.
6. **`RefreshTokenLifetime` duplicated** in `AuthController.cs:23` and `RefreshTokenService.cs:13`.
   Severity: low (latent drift).
7. **No `jti` denylist** — logout cannot revoke an access token for ≤15 min. Severity: low, by
   design of short-lived tokens.
8. **Role/permission changes propagate only on refresh** (≤7 days for the refresh session, ≤15 min
   for the access token) — `AuthRepository.cs:161-162`. Severity: low.
9. **`GET /api/auth/dev-users` is N+1** (`AuthRepository.cs:64-71`). Severity: low, Development
   only.
10. **Dead code:** `AuthValidationException` (`AuthRepository.cs:391-393`) never thrown;
    unused `employeeId` parameter in `GetRolesAsync` (`:319`); `BCrypt.Net-Next` package
    unreferenced in source; `environment.apiUrlProduction` (`environment.ts:4`) has no consumer.
    Severity: cosmetic.
11. **`authInterceptor` is unregistered in Entra mode** (`app.config.ts:19-30`) — the refresh
    rotation path becomes unreachable; verified behavior, but whether `AuthService` methods are
    still invoked from components in that mode is **Unknown** pending `11-frontend.md` cross-check.
12. **`POST logout` with an expired access token triggers a refresh + replay** (regex at
    `auth.interceptor.ts:12` covers only `dev-sign-in`/`refresh-token`). Severity: cosmetic.

---

Related: [01-architecture.md](01-architecture.md) · [06-application-services.md](06-application-services.md) ·
[07-repositories.md](07-repositories.md) · [09-api-endpoints.md](09-api-endpoints.md) ·
[10-error-handling.md](10-error-handling.md) · [11-frontend.md](11-frontend.md) ·
[14-known-issues.md](14-known-issues.md)
