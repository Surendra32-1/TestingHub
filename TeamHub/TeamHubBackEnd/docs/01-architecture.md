# 01 · Architecture and Composition Root

All facts below were read from source files; line references are given so each claim can be checked.

## Backend projects and target framework

All six projects target **`net10.0`** (verified from every `.csproj`; not .NET 8).

| Project | Role | Package references (verified) |
| --- | --- | --- |
| `TeamHub.Domain` | POCO entities only | none |
| `TeamHub.Shared` | `ApiResponse<T>`, `PagedResult<T>`, image/file helpers | `SixLabors.ImageSharp` 4.1.2 |
| `TeamHub.Application` | DTOs, service interfaces + implementations, validators, workflows, MediatR features | `FluentValidation` 11.11.0, `FluentValidation.DependencyInjectionExtensions` 11.11.0, `MediatR` 12.4.1 |
| `TeamHub.Infrastructure` | Dapper repositories, SQL scripts, connection factory | `Dapper` 2.1.66, `Microsoft.Data.SqlClient` 6.1.3, `System.Security.Cryptography.Pkcs` 9.0.21 |
| `TeamHub.Auth` | `AuthController`, JWT/refresh services, RBAC handlers, Entra claims | `Dapper` 2.1.66, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11, `BCrypt.Net-Next` 4.0.3 |
| `TeamHub.API` | Controllers + composition root | `FluentValidation.AspNetCore` 11.3.1, `Microsoft.AspNetCore.OpenApi` 10.0.11, `Swashbuckle.AspNetCore` 10.2.3 |

### Dependency direction (from `ProjectReference` entries)

```
Domain  ←  Shared
   ↑         ↑
Application ←┘        (Application → Domain, Shared)
   ↑        ↑
Infrastructure → Application, Domain
   ↑
Auth → Application, Domain, Infrastructure
   ↑
API → Application, Auth, Infrastructure, Shared
```

**Verified anomaly worth knowing:** `TeamHub.Auth` references `TeamHub.Infrastructure`
(`src/TeamHub.Auth/TeamHub.Auth.csproj`). The auth module therefore sits *above* persistence rather
than behind an abstraction; `AuthRepository` in `TeamHub.Auth/Infrastructure/` uses Dapper directly.
**Inferred:** this was done so the whole auth vertical (controller + services + repository) could be
packaged into one class library that the API mounts via `AddApplicationPart`
(`src/TeamHub.Auth/TeamHubAuthServiceCollectionExtensions.cs:129`). **Unknown:** whether this was a
deliberate layering choice or the shortest path to a working module.

There is **no Entity Framework** anywhere. Persistence is hand-written SQL executed by Dapper against
`IDbConnectionFactory`. Schema is managed by `.sql` scripts in `src/TeamHub.Infrastructure/`
(see [02-database.md](02-database.md)).

## Composition root — `src/TeamHub.API/Program.cs`

Ordered as executed:

1. **CORS origins** from `Frontend:AllowedOrigins`, defaulting to
   `["http://localhost:4200", "https://localhost:4200"]` (`Program.cs:31-33`).
2. **Connection string** `ConnectionStrings:TeamHub`; startup throws if missing (`Program.cs:35-37`).
3. `builder.Services.AddInfrastructure(connectionString)` (`Program.cs:39`) →
   `src/TeamHub.Infrastructure/DependencyInjection.cs:30-56`:
   - `IDbConnectionFactory` registered as a **singleton** instance of `SqlConnectionFactory` (`:32`);
   - 21 repositories registered **scoped**: BloodGroup, Department, Designation, Gender, AddressType,
     Role, Team, Skill, Recruitment, JobType, CotivityDepartment, CotivityDesignation,
     CotivityEmployee, Employee, EmployeeRelease, EmployeeTermination, BusinessUnit, DressSize,
     Permission, UserRole, RolePermission (`:33-53`).
4. `builder.Services.AddTeamHubAuth(configuration, environment)` (`Program.cs:40`) → see
   [08-auth-and-rbac.md](08-auth-and-rbac.md).
5. CORS policy `FrontendCorsPolicy`: allowed origins + any header + any method +
   **`AllowCredentials()`** (needed for the refresh-token cookie) (`Program.cs:41-50`).
6. 21 application services registered scoped (`Program.cs:51-73`): the 18 lookup/employee services,
   `IEmployeeReleaseService`, `IEmployeeTerminationService`, `IPermissionService`,
   `IUserRoleMatrixService`, `IRolePermissionMatrixService`.
7. **MediatR** scans the assembly containing
   `TeamHub.Application.Features.Employees.GetEmployeeHierarchyQuery` (`Program.cs:67-68`).
8. **FluentValidation**: 16 separate `AddValidatorsFromAssemblyContaining<T>` calls (one per validator
   family, `Program.cs:74-89`) plus `AddFluentValidationAutoValidation()` (`Program.cs:90`).
   **Verified:** auto-validation only fires on MVC model binding, which is why `EmployeesController`
   POST runs its validator manually with a ruleset selector (see
   [09-api-endpoints.md](09-api-endpoints.md)).
9. Error handling: `AddApiErrorHandling()` then `AddExceptionHandler<ApiExceptionHandler>()`
   (`Program.cs:91-92`) — see [10-error-handling.md](10-error-handling.md).
10. Swagger (`AddSwaggerGen`, title "TeamHub API", version v1) and .NET 10 `AddOpenApi()`
    (`Program.cs:93-106`).
11. After `builder.Build()`: `FileExtension.Initialize(app.Environment)` and
    `FileHelper.Initialize(app.Environment)` (`Program.cs:110-111`) — static helpers in
    `TeamHub.Shared` that capture `IWebHostEnvironment` for employee-image storage under `wwwroot`.

### Middleware order (`Program.cs:113-131`)

```
UseExceptionHandler → UseHttpsRedirection → UseStaticFiles → UseCors(FrontendCorsPolicy)
→ UseAuthentication → UseAuthorization → [MapOpenApi/UseSwagger/UseSwaggerUI in Development]
→ MapControllers
```

`UseStaticFiles` is what serves uploaded employee images back to the SPA: the API returns a path and
`EmployeeRepository` also reads the file to produce `ImageBase64` (see `07-repositories.md`).

**Verified security-relevant fact:** `FallbackPolicy` (deny anonymous) is added **only outside
Development** (`TeamHubAuthServiceCollectionExtensions.cs:120-125`). Every one of the 21 business
controllers nevertheless declares its own class-level `[Authorize]` or
`[Authorize(Policy = RbacPolicies.…)]` attribute (17 of them are policy-based per-module/per-action
gates — see [09-api-endpoints.md](09-api-endpoints.md) §2), so the fallback is not what protects them.
The seven `[AllowAnonymous]` actions
(`EmployeesController.cs:24, :42, :113, :132, :163`; `DepartmentsController.cs:16, :31`) override the
fallback and are reachable without a token **in every environment**, not just in Development (see
[14-known-issues.md](14-known-issues.md)).

## Configuration

`src/TeamHub.API/appsettings.json`:

```jsonc
"ConnectionStrings": { "TeamHub": "Server=.\\SQLEXPRESS;Database=TeamHub;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;" },
"Authentication": { "Jwt": { "Issuer": "TeamHub", "Audience": "TeamHubClient",
                            "SecretKey": "ThisIsAVerySecureDevelopmentSecretKey123456789" } },
"Auth": { "Entra": { "Enabled": false, "Instance": "https://login.microsoftonline.com/",
                     "TenantId": "<TENANT_ID>", "ClientId": "<API_CLIENT_ID>", "Audience": "<API_CLIENT_ID>" },
          "DevSignIn": { "AllowedRoles": ["Super Admin", "Admin", "Director", "Manager",
                                           "HR", "Developer", "IT", "Trainee", "NormalUser"] } },
"Frontend": { "AllowedOrigins": ["http://localhost:4200", "https://localhost:4200"] }
```

The JWT secret in this file is a development placeholder and the same key is used to sign tokens;
`AddTeamHubAuth` rejects any secret shorter than 32 UTF-8 bytes
(`TeamHubAuthServiceCollectionExtensions.cs:47-50`). Startup **throws** if `Auth:Entra:Enabled` is
false in a non-Development environment (`:33-38`), so the passwordless dev bypass cannot leak into
production. See `docs/18`-equivalent details in [08-auth-and-rbac.md](08-auth-and-rbac.md) and
[14-known-issues.md](14-known-issues.md).

## Frontend structure

Angular 22 standalone-component app (`TeamHubFrontEnd/package.json`): Angular `^22.0.0`
(`@angular/animations ^22.1.7`, `@angular/cdk ^22.2.0`, `@angular/material ^22.2.0`),
TypeScript `~6.0.2`, RxJS `~7.8.0`, `@azure/msal-angular ^6.2.4`, `@azure/msal-browser ^5.26.0`,
`nepali-date-converter ^3.4.0`. Tests run on **Vitest `^4.0.8` + jsdom `^28.0.0`** through
`ng test` — there is no Karma/Jasmine config in the repo. Build tooling: `@angular/build` and
`@angular/cli` `^22.0.5`. Package manager pinned as `npm@11.16.0`.

Layout: `src/app/core/auth` (auth state + session signal, guards, interceptor, MSAL config),
`src/app/core/config` (the declarative permission-driven menu tree, its pure recursive filter and
`MenuService`), `src/app/features` (`auth/login`, `employees`, `master-settings`, `rbac`),
`src/app/shared` (topbar, recursive `nav-menu`, pagination, toast host, models, services). Full detail
in [11-frontend.md](11-frontend.md) (§12.1 for the navigation).

**Verified:** the HTTP/auth mode is chosen at bootstrap — `src/app/app.config.ts` registers
`provideHttpClient(withInterceptors([authInterceptor]))` **or** the MSAL providers/interceptor based
on `environment.enableEntra`. `src/environments/environment.ts` currently sets `enableEntra: false`,
`apiUrl: 'http://localhost:5169/'`, and placeholder Entra ids.
