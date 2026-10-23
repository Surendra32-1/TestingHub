# TeamHub Documentation

TeamHub is an employee and organization management system: an ASP.NET Core Web API
(`TeamHubBackEnd`) backed by SQL Server, and an Angular single-page app (`TeamHubFrontEnd`).

## How to read these docs

| Doc | Contents |
| --- | --- |
| [01-architecture.md](01-architecture.md) | Projects, dependency direction, composition root, request pipeline, configuration |
| [02-database.md](02-database.md) | Tables, columns, constraints, SQL script execution order, permission catalog + role grants, hierarchy stored procedure |
| [03-domain-entities.md](03-domain-entities.md) | The 24 entity classes and their properties |
| [04-dtos.md](04-dtos.md) | Request/response DTOs, field types, nullability, defaults |
| [05-validators.md](05-validators.md) | Every FluentValidation rule and ruleset |
| [06-application-services.md](06-application-services.md) | Service interfaces, implementations, workflows, MediatR feature |
| [07-repositories.md](07-repositories.md) | Dapper repositories, actual SQL, transactions |
| [08-auth-and-rbac.md](08-auth-and-rbac.md) | Dev sign-in, Entra ID, JWT, refresh-token rotation, RBAC model, which permission codes actually gate an endpoint, menu visibility vs enforcement |
| [09-api-endpoints.md](09-api-endpoints.md) | Every controller (22, incl. `AuthController`), route, authorization attribute/policy, response shape, the full per-controller authorization matrix |
| [10-error-handling.md](10-error-handling.md) | ProblemDetails envelope, exception → status mapping |
| [11-frontend.md](11-frontend.md) | Angular structure, all 16 route entries (14 named + empty path + wildcard), guards, interceptors, every component, API clients, the permission-driven recursive navigation menu (§12.1), verified findings |
| [12-workflows.md](12-workflows.md) | End-to-end traces (frontend → database → UI) for each major feature, incl. sign-in/session signals, permission refresh and navigation rendering |
| [13-testing.md](13-testing.md) | Test inventory and build status (both backend test projects verified non-compiling; frontend `ng test` run — 10 files / 63 tests, 2026-10-23) |
| [14-known-issues.md](14-known-issues.md) | Severity-ranked index of every verified finding + labeled recommendations |
| [15-coverage-report.md](15-coverage-report.md) | What is verified, partial, or unverified, and why — incl. the 2026-10-23 navigation and role-grants/`terminations.add` revisions |

## Evidence rules used to write these docs

Every statement in these files is traceable to a source file. Each doc cites paths such as
`src/TeamHub.API/Program.cs:39`. Where the original reason for a design is not present in the
code, comments, or config, the doc labels it explicitly:

- **Verified** — supported by explicit code, config, SQL, or a build/run result.
- **Inferred** — a reasonable reading of the implementation, not a stated reason.
- **Unknown** — the reason cannot be established from the repository.
- **Recommended** — a proposed improvement, *not* current behavior.

Nothing here is filler. If a table, field, or endpoint does not appear, it was not found in the
source; see [15-coverage-report.md](15-coverage-report.md) for gaps that remain open.
