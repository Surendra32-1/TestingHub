# TeamHub Frontend

TeamHub Frontend is an Angular 22 single-page application for employee directory and administration, employee lifecycle workflows, master settings, and role-based access management. It uses standalone components, Angular Material, reactive forms, and an ASP.NET Core API.

## Requirements

- Node.js supported by the installed Angular 22 toolchain
- npm 11 (the package manifest pins `npm@11.16.0`)
- TeamHubBackEnd running locally for API-backed pages

## Setup and run

From `TeamHubFrontEnd`:

```powershell
npm ci
npm start
```

The development server is available at `http://localhost:4200`. The local API URL is configured in `src/environments/environment.ts` (`http://localhost:5169/`). Update the environment configuration when using a different API host; do not put credentials or production secrets in frontend source.

## Build and tests

```powershell
npm test -- --watch=false
npm run build
```

`npm test` runs the Angular test target with Vitest. Component and service specs are colocated with the implementation under `src/app`. `npm run build` creates the deployable application under `dist/`. No end-to-end test runner is currently configured in this project.

## Application structure

| Area | Purpose |
| --- | --- |
| `src/app/core/auth` | Session restoration, authentication, permission checks, route guards, and the HTTP auth interceptor |
| `src/app/core/config` | Permission-filtered navigation configuration |
| `src/app/features/auth` | Sign-in UI, including optional Microsoft Entra sign-in |
| `src/app/features/employees` | Directory/search, employee administration and profile forms, employee skills, and resignation/release/termination workflows |
| `src/app/features/master-settings` | Organization and Cotivity master-data screens |
| `src/app/features/rbac` | Employee-role and role-permission matrices |
| `src/app/shared` | Reusable navigation, pagination, date picker, and toast UI |

`src/main.ts` bootstraps the standalone app. `src/app/app.routes.ts` defines route-to-feature mappings and permission guards. The server independently enforces authorization; hiding a menu item or blocking a route in the browser is not a substitute for API authorization.

## Routes and access

- `/home` and `/` — read-only employee directory landing page
- `/login` — sign-in
- `/people` — employee search and directory (requires People view permission)
- `/administration` — employee administration (requires Administration view permission)
- `/employees/add`, `/employees/edit/:employeeId` — create or edit an employee
- `/profile/edit` — update the signed-in employee's profile
- `/employees/resignations` and `/employees/terminations` — lifecycle lists
- `/employees/resignations/new`, `/employees/terminations/new`, `/employees/release/:employeeId`, `/employees/terminate/:employeeId` — lifecycle forms
- `/master/:masterKey` — permission-guarded master settings
- `/settings/employee-role-matrix` and `/settings/role-permission-matrix` — role assignment and permission administration

After sign-in, the landing guard routes users to Administration when they have its view permission, otherwise to People when they have its view permission, and otherwise to the home page.

## Authentication configuration

`src/environments/environment.ts` sets `enableEntra` to `false` by default, so local development uses the application's configured non-Entra sign-in flow. To enable Microsoft Entra, set `enableEntra` and provide the tenant, SPA client, API scope, and redirect values for the target environment. Use environment-specific configuration and keep secrets out of the browser bundle; a SPA client ID is public configuration, not a secret.

The authentication service restores the session on navigation/reload, and the HTTP interceptor attaches the available access token to API requests. Route guards and the permission-filtered navigation use the session's permissions.

## Validation and API behavior

Employee Add, Edit, and Profile Update use reactive forms and share employee-field validation. Skill selections are persisted with their selected skill level (1–5). Date validation provides toast feedback for invalid date of birth and joining-date values. Master settings and lifecycle forms use their own validation rules.

The API base URL and auth mode are environment-configured. API routes, payloads, response envelopes, authorization requirements, and validation rules are documented in the backend repository:

- [API endpoints](../TeamHubBackEnd/docs/09-api-endpoints.md)
- [Validators](../TeamHubBackEnd/docs/05-validators.md)
- [Frontend architecture](../TeamHubBackEnd/docs/11-frontend.md)
- [Test inventory and execution notes](../TeamHubBackEnd/docs/13-testing.md)
- [Module test-case workbooks](../TeamHubBackEnd/Test%20Cases/)

## Responsive UI

The application styles shared navigation and feature pages for narrow and wide viewports. When changing a page, verify its forms, tables, dropdowns, and action controls at mobile, tablet, and desktop widths; keep permission-based visibility consistent at each size.
