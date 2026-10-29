# Microsoft Entra ID sign-in setup

TeamHub authenticates with **Microsoft Entra ID** (OpenID Connect). Passwords are no longer
stored anywhere: the `Users.PasswordHash`, `Users.PasswordSalt` and `Users.IsFirstLogin`
columns have been dropped and every password endpoint/flow has been removed.

While you do not yet have a configured tenant, Entra is **disabled** and a **Development-only,
role-restricted, passwordless bypass** keeps the app usable. This document explains how to go
live with Entra and how to remove the bypass afterwards.

> Never commit real Tenant IDs, Client IDs, secrets or certificates. The placeholders below
> (`<TENANT_ID>`, `<SPA_CLIENT_ID>`, `<API_CLIENT_ID>`) are intentional.

---

## 1. Current state (Entra disabled)

- Backend: `Auth:Entra.Enabled = false` in `src/TeamHub.API/appsettings.json`.
- Frontend: `environment.enableEntra = false` in `TeamHubFrontEnd/src/environments/environment.ts`.
- The login page shows a **"Development sign-in"** picker listing only accounts the API approves
  (`GET /api/auth/dev-users`), and signs in passwordlessly (`POST /api/auth/dev-sign-in`).

### Behaviour of the development bypass
- Available **only** when the ASP.NET Core environment is `Development` **and**
  `Auth:Entra.Enabled = false`. Otherwise both endpoints return **404**.
- Accepts **only** a `userId`. Roles/permissions are always loaded from SQL Server on the
  server; client-supplied roles are never trusted.
- Sign-in is refused (403) when the user is missing, inactive, or the employee is inactive, and
  when the user does not hold at least one role in `Auth:DevSignIn.AllowedRoles`
  (in-code fallback `["Admin", "NormalUser"]`). The shipped `appsettings.json` lists every
  seeded role — `Super Admin`, `Admin`, `Director`, `Manager`, `HR`, `Developer`, `IT`, `Trainee`,
  `NormalUser` — and `seed-data.sql` (§10) creates one account per role, so each row of the Role
  Permission Matrix can be signed in as.
- On success it issues the same local JWT (identical claims) used by the rest of the app, so
  `[Authorize(Policy = RbacPolicies.*)]` endpoints and the Angular guards keep working unchanged.

### Authentication mode matrix
| Environment   | `Auth:Entra.Enabled` | Sign-in path                                             |
|---------------|----------------------|----------------------------------------------------------|
| Development   | `false`              | Role-restricted passwordless bypass (current default)    |
| Development   | `true`               | Entra only. Bypass endpoints are **not** mapped.         |
| Production    | `true`               | Entra only. Local user resolved by `tid` + `oid`.        |
| Production    | `false`              | **Application startup fails** with a clear config error. |

There is no silent fallback from a failed Entra sign-in to the bypass.

---

## 2. Register the two Entra applications

In the [Microsoft Entra admin center](https://entra.microsoft.com) → **App registrations**,
create **two** applications.

### (a) SPA app — for the Angular front end
- **Supported account types:** your organization (single tenant).
- **Platform:** Single Page Application.
- **Redirect URI:** `http://localhost:4200` (add your production origin too, e.g.
  `https://your-host`).
- Note its **Application (client) ID** → `<SPA_CLIENT_ID>`.

### (b) Web API app — for the .NET back end
- **Supported account types:** same directory (single tenant).
- **Platform:** Web (no redirect URI needed for token validation).
- Note its **Application (client) ID** → `<API_CLIENT_ID>`.
- Under **Expose an API**:
  - Set the **Application ID URI** to `api://<API_CLIENT_ID>`.
  - Add a scope named `access_as_user` ("Act as the signed-in user"), **Admins and users** can
    consent. This is the `apiScope` the SPA requests.
- Under **API permissions**, add a **Delegated** permission for `api://<API_CLIENT_ID>/access_as_user`
  so the SPA can request a token for the API. Grant admin consent.

Record your **Directory (tenant) ID** → `<TENANT_ID>`.

---

## 3. Configure the backend

Edit `TeamHubBackEnd/src/TeamHub.API/appsettings.json` (use environment variables or a secret
store in Production rather than committing values):

```jsonc
"Auth": {
  "Entra": {
    "Enabled": true,                                   // flip to true to go live
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<TENANT_ID>",
    "ClientId": "<API_CLIENT_ID>",
    "Audience": "<API_CLIENT_ID>"                       // the API's App ID URI / client id
  },
  "DevSignIn": {
    "AllowedRoles": [
      "Super Admin", "Admin", "Director", "Manager",
      "HR", "Developer", "IT", "Trainee", "NormalUser"
    ]
  }
}
```

When `Enabled = true` the API registers an additional `"EntraBearer"` JWT bearer scheme that
validates Entra access tokens via the tenant's OpenID Connect metadata
(`{Instance}/{TenantId}/v2.0`) and the configured `Audience`. The local symmetric-key scheme is
still present for backward compatibility but is no longer the default.

### Mapping Entra identities to local users
Entra tokens do not carry TeamHub's `Role`/`permission` claims, so on `OnTokenValidated` the API:
1. reads the `tid` (tenant) and `oid` (object) claims;
2. looks up the local user by `(EntraTenantId, EntraObjectId)` in `dbo.Users`;
3. **fails the request** if there is no match or the user/employee is inactive (sign-in never
   grants access on its own);
4. otherwise injects the same `UserId`/`EmployeeId`/`Username`/`Role`/`permission` claims the
   rest of the app already relies on.

**No automatic provisioning.** After enabling Entra you must link each real Entra user to their
existing local row yourself (there are no password flows to fall back on):

```sql
UPDATE dbo.Users
SET EntraTenantId = '<TENANT_ID>',
    EntraObjectId = '<USER_OBJECT_ID>'   -- the "oid" claim from the user's token
WHERE EmployeeId = <employee id>;
```

The `EntraTenantId`/`EntraObjectId` columns are nullable with a filtered unique index
(`UQ_Users_EntraIdentity`); users are matched **only** by `tid` + `oid`, never by email.

---

## 4. Configure the frontend

Edit `TeamHubFrontEnd/src/environments/environment.ts`:

```ts
enableEntra: true,                      // flip to true to go live
entra: {
  authority: 'https://login.microsoftonline.com/<TENANT_ID>',
  clientId: '<SPA_CLIENT_ID>',
  apiScope: 'api://<API_CLIENT_ID>/access_as_user',
  redirectUri: 'http://localhost:4200',
  postLogoutRedirectUri: 'http://localhost:4200'
}
```

When `enableEntra` is true:
- `app.config.ts` registers the MSAL providers (`MsalModule.forRoot(...)`) and DI-based HTTP
  interceptors instead of the sessionStorage JWT flow.
- The login page shows a single **"Sign in with Microsoft"** button that drives the MSAL
  redirect login; the development picker is hidden entirely.
- `msal.config.ts` maps `environment.apiUrl` → `[apiScope]`, so an Entra access token for the API
  is attached only to requests targeting the TeamHub API.

---

## 5. Database

The Entra schema migration is
`TeamHubBackEnd/src/TeamHub.Infrastructure/authentication-entra-schema.sql`. It is idempotent and:
- adds `EntraTenantId` / `EntraObjectId` (with the filtered unique index);
- drops `PasswordHash`, `PasswordSalt`, `IsFirstLogin` (and any dependent default constraint).

The fresh-install seed (`authentication-schema.sql`) already inserts `Admin`/`NormalUser` without
any password columns. It has already been applied to the live `TeamHub` database in this work;
re-run it only on a new environment.

---

## 6. Going live checklist

1. Complete sections 2–4 with real `<TENANT_ID>` / `<SPA_CLIENT_ID>` / `<API_CLIENT_ID>`.
2. Map every active user's `EntraObjectId` in `dbo.Users` (section 3).
3. Set `Auth:Entra.Enabled = true` and `environment.enableEntra = true`.
4. Verify: sign in with Microsoft, hit an `[Authorize(Policy = RbacPolicies.*)]` endpoint, and
   confirm a not-yet-mapped/inactive user is rejected.
5. Deploy to Production (Entra **must** be enabled or startup fails by design).

## 7. Removing the development bypass

Once Entra is live the bypass is already unreachable: the `dev-sign-in`/`dev-users` endpoints are
gated on `IsDevelopment() && !Auth:Entra.Enabled`, so enabling Entra or leaving Development
removes access automatically. To fully delete the code, remove:
- **Backend:** the `dev-sign-in`/`dev-users` actions in `AuthController`, `DevSignInAsync`/
  `GetDevSignInUsersAsync` in `AuthService`, `GetDevSignInCandidatesAsync` in `AuthRepository`, and
  the `Auth:DevSignIn` configuration section.
- **Frontend:** `getDevUsers`/`devSignIn` in `auth.service.ts` and the `@else` development branch
  in `login.component.html` (plus its `.dev-accounts` styles).
