# 11 · Frontend (Angular SPA)

Source: `TeamHubFrontEnd`. Every statement below is **Verified** against the file and line cited,
unless labelled *Inferred*, *Unknown*, or *Recommended*. All 33 TypeScript sources that implement
behaviour were read in full; templates were read or searched where a claim depends on them.

*Updated 2026-10-23 — navigation refactor.* The hardcoded topbar navigation was replaced by a
permission-driven recursive menu. Four behaviour-bearing sources were added
(`core/config/menu.config.ts` 137 lines, `core/config/menu-filter.ts` 74, `core/config/menu.service.ts` 32,
`shared/components/nav-menu/nav-menu.component.ts` 56), taking the audited count to **37**, and
`core/auth/auth.service.ts` (97 → 120) plus `shared/components/topbar/*` (88 → 10 template lines) were
changed. The full contract is **§12.1**; the guards (§3) and the route table (§2) are unchanged.

Analysis status: **complete for `src/app/**`** (app shell, `core/auth`, `features/employees`,
`features/auth`, `features/master-settings`, `features/rbac`, `shared`). Style-only files
(`*.css`, `app.css`, `rbac-matrix.css`, `material-theme.scss`) were inventoried, not line-audited —
see [15-coverage-report.md](15-coverage-report.md).

---

## 1. Build and runtime composition

| Concern | Verified state |
| --- | --- |
| Framework | Angular 22, standalone components only — no `NgModule` in `src/app` except the MSAL `MsalModule.forRoot` call in `app.config.ts:23` |
| Bootstrap | `main.ts` → `bootstrapApplication(App, appConfig)` |
| Root component | `app.ts:5-12` — `imports: [RouterOutlet, ToastHostComponent]`; `app.html` is 3 lines: `<router-outlet>` + `<app-toast-host />`. There is **no** global layout/shell component: the topbar is repeated inside each page component |
| Navigation | No menu table, no menu API, no menu CRUD screen — the whole tree is one frontend constant `APP_MENU` (`menu.config.ts:50-137`), filtered per session by the pure function `filterMenu()` (`menu-filter.ts:50-74`) and rendered by one self-referencing component (`app-nav-menu` inside its own template, `nav-menu.component.html:15`). Unlimited nesting, no dynamic component loading from JSON. §12.1 |
| Change detection | Mixed. `OnPush` is used by `HierarchyNodeComponent` (`hierarchy-node.component.ts:79`), `EmployeeHierarchyComponent` (`employee-hierarchy.component.ts:41`) and `NavMenuComponent` (`nav-menu.component.ts:22`) — the three newest components. **Every other component uses the default strategy** and calls `markForCheck()` as if it were OnPush (e.g. `employee-directory.component.ts:87`, `toast-host.component.ts:22`). Harmless but indicates the strategy was not applied uniformly — *Inferred* |
| Injection style | Mixed and consistent per file: constructor injection in the older page components (`employee-directory.component.ts:59-66`, `departments-view.component.ts:31-35`, `pagination.component.ts`), `inject()` in newer ones (`directory-view.component.ts:19-21`, `rbac` components, `employee-add-form.ts:45-53`) |
| Signals | Hierarchy cluster (`employee-hierarchy.component.ts:44-63`, `hierarchy-node.component.ts:82-83`) plus the session/menu layer: `AuthService.sessionRevision` + `sessionState` (`auth.service.ts:16, :33-44`), `MenuService.visibleMenu` (`menu.service.ts:20`) and signal inputs / `toSignal(router.events)` in `nav-menu.component.ts:26-38`. Other state is plain class fields |
| Template syntax | Mixed: control-flow `@if`/`@for` in `nav-menu.component.html:2-29` (recursive) and `topbar.component.html:7-9`; structural `*ngIf`/`*ngFor` in `directory-view.component.html`, `employee-directory.component.html`, `employee-add-form.html`, the RBAC grids |
| Forms | Reactive forms everywhere. Two styles: `FormBuilder.group({...})` with explicit validators (`employee-add-form.ts:95-148`) and a **metadata-driven** form built at runtime (`master-settings.ts:332-365`) |
| HTTP | `provideHttpClient(withInterceptors([authInterceptor]))` or the MSAL DI interceptor set, chosen at bootstrap (`app.config.ts:19-30`) |
| Dates | `provideNativeDateAdapter()` globally (`app.config.ts:37`) + a locally provided `NepaliDateAdapter` (§8) |

No state-management library, no NgRx/Elf, no route-level lazy loading except the two RBAC routes,
which use `loadComponent` (`app.routes.ts:104, :112`).

---

## 2. Route table (16 route entries: 14 named routes + the empty path + the wildcard)

`app.routes.ts` (126 lines) — read in full. `title` is set on every route.

| Path | Component | Guards | `data` |
| --- | --- | --- | --- |
| `login` | `LoginComponent` | `signedInLandingGuard` | — |
| `` (empty, `pathMatch: 'full'`) | `EmployeeDirectoryComponent` | `signedInLandingGuard` | `readOnlyHome: true` |
| `people` | `EmployeeDirectoryComponent` | `authenticatedGuard` | `layout: 'table'` |
| `home` | `EmployeeDirectoryComponent` | `signedInLandingGuard` | `readOnlyHome: true` |
| `employees/add` | `EmployeeAddFormComponent` | `authGuard, administrationCreateGuard` | — |
| `employees/edit/:employeeId` | `EmployeeAddFormComponent` | `authGuard, administrationUpdateGuard` | — |
| `employees/release/:employeeId` | `EmployeeLifecycleFormComponent` | `authGuard, administrationReleaseGuard` | `mode: 'release'` |
| `employees/resignations/new` | `EmployeeLifecycleFormComponent` | `authGuard, resignationAddGuard` | `mode: 'resignation'` |
| `employees/terminations/new` | `EmployeeLifecycleFormComponent` | `authGuard, terminationAddGuard` | `mode: 'termination'` |
| `employees/resignations` | `EmployeeLifecycleListComponent` | `authGuard, resignationViewGuard` | `mode: 'resignation'` |
| `employees/terminations` | `EmployeeLifecycleListComponent` | `authGuard, terminationViewGuard` | `mode: 'termination'` |
| `employees/terminate/:employeeId` | `EmployeeLifecycleFormComponent` | `authGuard, terminationAddGuard` | `mode: 'termination'` |
| `master/:masterKey` | `MasterSettings` | `authGuard, masterGuard` | key resolved in the component (`master-settings.ts:205-206`) |
| `settings/employee-role-matrix` | `EmployeeRoleMatrixComponent` (**lazy**) | `authGuard, employeeRoleMatrixGuard` | — |
| `settings/role-permission-matrix` | `RolePermissionMatrixComponent` (**lazy**) | `authGuard, rolePermissionMatrixGuard` | — |
| `**` | `redirectTo: ''` | — | — |

**Verified observations**

1. `authGuard` is an **alias** of `authenticatedGuard` (`auth.guard.ts:24`) — every two-guard route is
   therefore "session check, then permission check", and the second guard is always a `rbacGuard`
   instance (`masterGuard` for master data) except on the two lazy RBAC matrix routes.
2. **No route uses a role check any more.** `adminGuard` (`auth.guard.ts:43-48`, `hasRole('Admin')`) is
   still exported but is a **dead symbol** — searched across `src/app/*.ts`, nothing imports it since the
   per-code guards replaced it (`app.routes.ts:8` does not list it). Both are UX guards only; the server
   enforces its own authorization independently ([09-api-endpoints.md](09-api-endpoints.md) §2).
   *Recommended*: delete the export, or keep it solely if an Admin-only surface is added back.
3. `signedInLandingGuard` (`auth.guard.ts:26-41`) is used on the three "landing" routes. When a
   session **cannot** be restored it returns `of(true)` (`:38`) — i.e. it deliberately lets anonymous
   visitors render `/`, `/home` and `/login`. Combined with the seven `[AllowAnonymous]` backend
   actions this is what makes the read-only home page work without signing in.
4. `/people` is the People search page and uses `authGuard` plus `peopleSearchViewGuard`;
   `/administration` is the employee-management page and uses `authenticatedGuard` plus
   `administrationViewGuard`. `/employee` redirects to `/people`.
5. The employee-directory `tabs` array exposes only `directory` and `departments`
   (`employee-directory.component.ts:54-57`), while `activeView` has four states
   (`:17` type, `:53` default). `hierarchy` and `details` are entered only from the row action menu
   and left only via their own back buttons (`employee-hierarchy.component.html:4`,
   `employee-details.component.html:2`).
6. There is no route for recruitment master data, matching the missing `masterConfigs` entry (§7) and
   the unused `RecruitmentsController` ([09-api-endpoints.md](09-api-endpoints.md) §11 finding 12).

---

## 3. Guards in detail

```csharp
// auth.guard.ts:26-41 (representative)
export const signedInLandingGuard: CanActivateFn = () => {
  if (authService.isAuthenticated) return router.createUrlTree(['/people']);
  return authService.restoreSession().pipe(
    map(() => router.createUrlTree(['/people'])),
    catchError(() => { authService.clearSession(); return of(true); })
  );
};
```

| Guard | Verified behaviour |
| --- | --- |
| `authenticatedGuard` `:7-22` | If a token is in sessionStorage → allow. Otherwise try `restoreSession()` (a refresh round-trip) → allow on success; on failure clear the session and redirect to `/login?returnUrl=<url>` |
| `signedInLandingGuard` `:26-41` | Signed in (or restorable) → People `/people`, Administration `/administration`, or `/home` according to grants; otherwise **allow** the read-only landing page |
| `adminGuard` `:43-48` | `hasRole('Admin')` → allow, else redirect to `/home` — **dead export**, no route references it (§2 observation 2) |
| `rbacGuard(permission)` `:52-59` | `hasPermission(permission)` → allow, else `/home`. **No role check** (the server's Admin bypass is not mirrored client-side — §13 finding 16). Instantiated as `employeeRoleMatrixGuard` = `user-roles.assign` (`:61`), `rolePermissionMatrixGuard` = `role-permissions.manage` (`:63`), `resignationViewGuard`/`resignationAddGuard` (`:66, :68`), `administrationCreateGuard`/`administrationUpdateGuard`/`administrationReleaseGuard` and `administrationViewGuard` (`:87-93`) and `terminationViewGuard`/`terminationAddGuard` (`:101, :103`) |
| `masterGuard` `:85-94` | Reads `paramMap.masterKey` and derives the code: `permission` → `permissions.manage`, otherwise `master.<key>` |

*Recommended:* the two RBAC permission strings are duplicated between `auth.guard.ts:61-63`, the
`canEdit` getters in both matrix components (`employee-role-matrix.component.ts:53`,
`role-permission-matrix.component.ts:53`), the menu tree (`menu.config.ts:126, :132`) and the backend
policy names — a single shared constant would prevent drift. **Verified** today: all five spellings
agree, and `auth.guard.spec.ts` + `menu.service.spec.ts` now pin the codes used by the guards and the
navigation against each other.

**Verified relationship between guards and menu:** the menu never loosens access. Every gated item
carries the *same* code its route guard checks (`resignations.view` ↔ `resignationViewGuard`,
`terminations.view` ↔ `terminationViewGuard`, `master.<key>` ↔ `masterGuard`, `permissions.manage` ↔
the `masterKey === 'permission'` branch, `user-roles.assign` / `role-permissions.manage` ↔ the two
matrix guards), so an item is visible only where navigation would succeed. Buttons inside a page follow
the same rule one level finer: the "New Termination" button and the directory "Terminate" row action
carry `terminations.add` — the code that guards the create routes — while the transition menu on the
same list carries `terminations.manage` (`employee-lifecycle-list.ts:72-80`). Items with no code
(`People`) are visible exactly where the guard is only `authenticatedGuard`. See §12.1 for the two
deliberate exceptions.

---

## 4. `core/auth` — session, interceptor, MSAL

Six files, all read in full: `auth.service.ts` (120), `auth.interceptor.ts` (38), `auth.guard.ts` (95),
`auth.models.ts` (35), `entra-auth.service.ts` (25), `msal.config.ts` (51). Line references below were
re-verified against the current files: the session signal added ~22 lines to `auth.service.ts` and
shifted everything after `isAuthenticated`. The permission-driven navigation (§12.1) reads this layer
and lives in a new sibling folder, `core/config` (`menu.config.ts` 137, `menu-filter.ts` 74,
`menu.service.ts` 32).

### 4.1 `AuthService` — storage and refresh

| Item | Verified detail |
| --- | --- |
| Storage | **sessionStorage** only, three keys: `teamhub.accessToken`, `teamhub.roles`, `teamhub.permissions` (`:11-13`). Roles/permissions are stored as one `\|`-joined string (`:115-116`) |
| `isAuthenticated` | `accessToken !== null` (`:24-26`) — an *existence* check; the JWT `exp` claim is never inspected client-side |
| `sessionRevision` | private `signal(0)` (`:16`) bumped by `storeSession` (`:117`) and `clearSession` (`:110`) — the only way sessionStorage writes become observable to signals |
| `sessionState` | `computed<AuthSession>` (`:33-44`): reads `sessionRevision()` for the dependency, then returns `{ isAuthenticated, permissions: Set<string> }` with codes trimmed and **lower-cased**. This is what `MenuService` consumes (`menu.service.ts:25-31`); `hasPermission()` (`:52-56`) is left intact for the guards and the pages that still call it |
| `hasRole` / `hasPermission` | split on `\|`, case-insensitive compare (`:46-56`) |
| `getDevUsers()` | `GET {apiUrl}api/auth/dev-users` (`:59-61`) — **no** envelope type, the backend returns a bare array |
| `devSignIn(userId)` | `POST api/auth/dev-sign-in` with `withCredentials: true`, then `storeSession` (`:67-70`) |
| `refreshAccessToken()` | `POST api/auth/refresh-token`, `{}` body, `withCredentials: true`; wrapped in `shareReplay({bufferSize: 1, refCount: false})` with `finalize` clearing the in-flight field (`:72-86`) — concurrent 401s share one request |
| `restoreSession()` | alias for `refreshAccessToken()` (`:88-90`) |
| `logout()` / `logoutAll()` | `POST …/logout` / `…/logout-all`, `clearSession()` in `finalize` so it runs even on error (`:96-104`) |
| `storeSession` | writes `response.accessToken` **unconditionally** — if the API ever returned `accessToken: null`, sessionStorage would hold the string `"null"` (`:113-118`). *Recommended*: guard. `AuthResponse.accessToken` is typed non-nullable (`auth.models.ts:2`), so TS accepts it. `permissions` is read as `response.permissions ?? []` (`:116`) |
| `AuthSession` | new contract in `auth.models.ts:31-34` (`isAuthenticated` + `ReadonlySet<string>` of codes) — documented as UI-visibility only, not a security decision |

`getCurrentUser()` (`:92-94`) calls `GET api/auth/me` but **has no caller anywhere in `src/app`**
(verified by searching for `getCurrentUser`) — the SPA relies on the claims captured at sign-in
instead.

### 4.2 `authInterceptor` (`auth.interceptor.ts:8-37`)

```csharp
const isAuthEndpoint = request.url.includes('/api/auth/');
const isLoginOrRefresh = /\/api\/auth\/(dev-sign-in|refresh-token)$/.test(request.url);
```

* Attaches `Authorization: Bearer <token>` whenever a token exists (`:13-17`).
* Sets `withCredentials: true` **only** for `/api/auth/` requests (`:16`) — needed for the HttpOnly
  refresh cookie, which is scoped to `Path=/api/auth` ([08-auth-and-rbac.md](08-auth-and-rbac.md) §7).
* On 401 (and not a login/refresh call) it refreshes once and replays the request (`:19-34`); if the
  refresh fails it clears the session and navigates to `/login`.
* **Verified defect:** the replay re-derives the request from the *original* `request`, not from
  `authenticatedRequest` (`:26`). It sets the new bearer token correctly, but for a 401 on an
  `/api/auth/*` endpoint (e.g. `/api/auth/me`, `/api/auth/logout`) the replay loses
  `withCredentials`. Severity low: those endpoints are not on the retry path in practice
  (`dev-sign-in`/`refresh-token` are excluded, and `me`/`logout` are only called after a token exists).
* **Verified:** the interceptor is registered only in the non-Entra mode. With
  `environment.enableEntra === true` (`app.config.ts:19-30`) the app uses
  `withInterceptorsFromDi()` + `MsalInterceptor` and this file is inert — the sessionStorage session
  model is then never populated, so `authenticatedGuard` and every `rbacGuard` instance would deny
  everything until a separate session bridge exists (`adminGuard` would too, but no route uses it — §2
  observation 2). *Unknown* whether that bridge was planned; the Entra mode is not the
  default (`environment.ts:11` `enableEntra: false`).

### 4.3 MSAL configuration (`msal.config.ts:16-50`)

`PublicClientApplication` with `cacheLocation: 'sessionStorage'` (`:24-26`), redirect interaction,
`loginFailedRoute: '/login'` (`:35`), and a `protectedResourceMap` entry mapping exactly
`environment.apiUrl` → `[environment.entra.apiScope]` (`:44-45`). `EntraAuthService`
(`entra-auth.service.ts:16-23`) is a two-method wrapper (`loginRedirect` / `logoutRedirect`) and is
injected conditionally in `login.component.ts:25`.

---

## 5. Environment and URL construction

`environments/environment.ts` (17 lines) is the only environment file — there is **no**
`environment.prod.ts` and `angular.json` has no file replacements (verified against the project
layout). Keys:

| Key | Value / role |
| --- | --- |
| `apiUrl` | Base URL for every HTTP call; all services build `${environment.apiUrl}api/...` |
| `enableEntra` | `false` — selects the JWT/refresh-cookie flow at bootstrap |
| `entra.clientId`, `entra.authority`, `entra.redirectUri`, `entra.postLogoutRedirectUri`, `entra.apiScope` | consumed by `msal.config.ts` and `EntraAuthService` |

`apiUrl` ends with a `/`, and services concatenate without one (`employee.service.ts:293`
`${environment.apiUrl}api/employees`) — **Verified** consistent across all five API clients (§9.1).

---

## 6. Employee directory feature

Files (all read in full): `pages/employee-directory/employee-directory.component.ts` (173) + `.html`
(56); `components/directory-view` (237) + `.html` (235); `directory-controls` (27);
`results-header` (17); `employee-tab` (18); `departments-view` (113); `employee-details` (49);
the `employee-hierarchy` cluster (331 + 25 + 96 + 94 + 49).

### 6.1 `EmployeeDirectoryComponent` — the page state holder

Owns all list state and orchestrates the four views (`:36-57`): `employees`, `isLoading`,
`pageNumber`/`pageSize` (default 1/10), `totalCount`, `searchTerm`, `searchField`
(`'all' | 'name' | 'designation' | 'department'`), `activeView`, plus hierarchy/details selection ids.

| Behaviour | Verified detail |
| --- | --- |
| Route data mapping | `tableLayout` from `data.layout === 'table'`, `readOnlyHome` from `data.readOnlyHome === true` (`:70-71`), evaluated once in the constructor via `route.snapshot` |
| Permissions | Administration actions use `administration.create/update/release/terminate`, `administration.hierarchy.view`, `administration.delete`, and `administration.cardview`; `terminations.add` also permits employee-specific termination. No `hasRole('Admin')` fallback; checks are captured once, so a permission change mid-session does not re-render row actions. The API applies its own policies where configured (`08-auth-and-rbac.md` §9) |
| Load | `loadEmployees()` (`:85-105`) with a **monotonic request id** guard (`loadRequestId`, `:86, :92, :99`) so a slow response cannot overwrite a newer page |
| Search | `scheduleSearch()` debounces 300 ms and always resets to page 1 (`:117-120`) |
| Tab switch | `selectView()` resets `departmentSelected` because `*ngIf` destroys and recreates `DepartmentsViewComponent` (`:122-126`, `employee-directory.component.html:42`) |
| Drill-down | `backToDepartments()` calls a method on the child through `@ViewChild(DepartmentsViewComponent)` (`:128-130`) |
| Row actions | `editEmployee`/`releaseEmployee`/`terminateEmployee` navigate to `/employees/edit/:id`, `/employees/release/:id`, `/employees/terminate/:id` (`:161-171`) |

### 6.2 `DirectoryViewComponent` — presentation + two interactive overlays

Pure `@Input`/`@Output` component (11 inputs, 10 outputs, `:25-45`); renders **either** a table
(`directory-view.component.html:16-109`, when `tableLayout`) **or** a card grid (`:111-186`), then one
shared `<app-pagination>` (`:190-197`) and the hover preview dialog (`:199-234`).

*Hover preview* (`:58-152`): 350 ms show delay, 260 ms hide grace period so the pointer can travel
from the avatar into the card (`keepPreview()` `:89-94`); results cached per employee id in a
`Map<number, EmployeeDetails>` (`:22`) and never evicted for the component's lifetime; stale
responses ignored by comparing `previewEmployeeId` (`:115`). Positioned with hard-coded
`width = 320`, `height = 420` against `window.innerWidth/innerHeight` (`:131-144`).
`@HostListener` set: `document:click` closes the action menu (`:216-219`),
`window:scroll.capture` + `window:resize` close both overlays (`:222-227`),
`document:keydown.escape` closes only the preview (`:229-234`).

*Action menu* (`:156-173`): fixed-position, computed from `getBoundingClientRect()`; menu items are
full hierarchy, direct hierarchy (always visible) and edit / release / terminate, each gated by its own
input (`canEditEmployee` / `canReleaseEmployee` / `canTerminateEmployee`, declared
`directory-view.component.ts:28-30`; used at `:78, :84, :91` in the table view and `:163, :169, :176`
in the card view). Release and terminate route through
`toastService.confirm(...)` before emitting (`:180-200`).

### 6.3 `DepartmentsViewComponent` — N+1 by design

```csharp
// departments-view.component.ts:38-55
this.employeeService.getDepartments().subscribe({ next: (departments) => {
  this.departments = departments;
  for (const department of departments) {
    this.employeeService.getActiveEmployeesByDepartmentId(department.departmentId, 1, 1).subscribe(/* leader */);
  }
}});
```

* **Verified:** one request per department, issued eagerly on `ngOnInit`, to display the
  highest-ranked active employee as the department "leader" (relies on the server's
  `designationRank` ordering — see [07-repositories.md](07-repositories.md)).
* `getDepartments()` itself is a **client-side full scan**: `expand` + `reduce` over
  `hasNextPage` with `pageSize = 100` (`employee.service.ts:328-341`), so N/100 requests before the
  first render.
* Race guard identical in style to the directory (`loadRequestId`, `:29, :92-98`);
  `showDepartments()` increments the id to invalidate in-flight page loads (`:72-78`).
* Error path: a leader lookup failure toasts `Could not load the highest-ranked employee for <name>.`
  (`:52`) and leaves the card without a leader.

### 6.4 `EmployeeDetailsComponent` (49)

Single `@Input({required: true}) employeeId` (`:16`), one `GET …/{id}/details` in `ngOnInit` (`:29-43`),
`trackByAddressId` for the address list (`:45-47`). It stores `errorMessage` **and** raises a toast
for the same failure (`:37-39`) — double reporting. No reload/refresh path: re-entering the view
recreates the component.

### 6.5 Hierarchy cluster (the most modern code in the SPA)

| File | Role |
| --- | --- |
| `models.ts` (49) | `EmployeeHierarchyPerson`, `EmployeeHierarchyTreeNode`, `HierarchyNodeView`, `HierarchyConnection`, `CotivityHierarchyPerson`, `EmployeeHierarchyResponse` (`employeeManagers`, `selectedEmployee`, `cotivityHierarchy`, `reportingHierarchy`) — mirrors the backend DTO in [04-dtos.md](04-dtos.md) |
| `employee-hierarchy.service.ts` (25) | `GET api/employees/{id}/hierarchy`, unwraps `.data` (`:19-23`) |
| `hierarchy-node.component.ts` (94) | OnPush, `input.required<HierarchyNodeView>()` + `input(false)`, inline template/styles, initials fallback (`:85-92`) |
| `hierarchy-connector.service.ts` (96) | Pure DOM measurement: reads `[data-node-id]` rects, sizes the SVG `viewBox`, and emits cubic-Bézier or straight `d` strings; horizontal routing when `|Δx| > |Δy|`, otherwise vertical; `route: 'direct'` draws a straight horizontal line (`:65-72`) |
| `employee-hierarchy.component.ts` (331) | Signals + computed + `afterNextRender` |

Verified mechanics of `EmployeeHierarchyComponent`:

* Two `effect()`s in the constructor (`:78-107`): the first loads data on `employeeId()` change
  (guarded by `Number.isInteger && > 0`, unsubscribed through `onCleanup`), the second re-runs
  `scheduleConnectorRefresh()` via `afterNextRender({ injector })` whenever the hierarchy changes.
* `visibleManagers` truncates to the last element in direct mode (`:52-55`); `cotivityNodes` returns
  `[]` in direct mode (`:59-63`); `reportingTree` builds a tree from the flat list
  (`createReportingTree` `:183-223`) and sorts children by name with `localeCompare` (`:216-218`).
* `connectionSpecs()` (`:225-292`) links employee→employee, cotivity→cotivity, then one
  `route: 'direct'` edge from the cotivity root (`level === 0`, `:281`) to the selected employee.
* Connector refresh is coalesced with `requestAnimationFrame` and cancelled in
  `destroyRef.onDestroy` (`:109-114, :312-329`); a `ResizeObserver` watches the canvas and every
  `[data-node-id]` node, tracked in a `WeakSet` so elements are observed once (`:294-310`).
* Image URLs: `imageUrl()` passes `data:`/`http(s):` through and otherwise prefixes `environment.apiUrl`
  and normalises backslashes (`:171-181`) — the hierarchy endpoint returns **paths**, unlike the
  directory/detail endpoints which return base64 data URLs
  ([07-repositories.md](07-repositories.md)).
* **Verified minor inefficiency:** `createReportingTree` iterates the *unfiltered* `flatNodes` when
  wiring children (`:201`) although only `scopedNodes` are in the `nodes` map; lookups for
  out-of-scope children miss and are skipped, so the result is correct and the extra iterations are
  wasted work. Severity: cosmetic.

---

## 7. Master settings — one metadata-driven screen for 15 datasets

`master-settings.ts` (617) plus `master-settings.html`/`css`.

`MasterKey` union (`:15-30`) and `masterConfigs` (`:70-164`) define **15** datasets. Each entry is
`{ title, singular, endpoint, idField, fields[] }`; each `MasterField` is
`{ key, label, type: 'text' | 'number' | 'checkbox' | 'select', required?, minLength?, maxLength?,
min?, max?, integer?, displayKey?, optionsSource? }`.

| masterKey (route) | endpoint (API) | idField | extra fields beyond name+isActive |
| --- | --- | --- | --- |
| `department` | `departments` | `departmentId` | — (`minLength 2`) |
| `designation` | `designations` | `designationId` | `rank` (number, 0…2147483647, integer) |
| `business-unit` | `business-units` | `businessUnitId` | — |
| `dress-size` | `dress-sizes` | `dressId` | `size` (text, ≤10) |
| `cotivity-department` | `cotivitydepartments` | `cotivityDepartmentId` | — |
| `cotivity-designation` | `cotivitydesignations` | `cotivityDesignationId` | `cotivityRank` |
| `cotivity-employee` | `cotivityemployees` | `cotivityEmployeeId` | 3 `select` fields (`optionsSource`) + image upload |
| `role` | `roles` | `roleId` | — |
| `permission` | `permissions` | `permissionId` | `permissionCode`, `permissionName`, `module`, `description` |
| `address-type` | `address-types` | `addressTypeId` | — |
| `blood-group` | `bloodgroups` | `bloodGroupId` | name maxLength 5 |
| `skills` | `skills` | `skillId` | — |
| `job-type` | `job-types` | `jobTypeId` | — |
| `team` | `teams` | `teamId` | — |
| `gender` | `genders` | `genderId` | — |

Endpoint strings are deliberately irregular (`cotivitydepartments`, `bloodgroups`, `genders` are
lowercase/concatenated) because they must match the backend's `[controller]` routing, while
`business-units`, `address-types`, `job-types`, `dress-sizes` match their explicit `[Route]`
templates — see [09-api-endpoints.md](09-api-endpoints.md) §1 "two route styles".

Lifecycle: `ngOnInit` subscribes to `route.paramMap` (`:204-220`) so navigating between master pages
re-resolves the config, resets state, rebuilds the form (`resetForm()` `:332-365`), loads rows and —
for `cotivity-employee` only — `loadCotivityLookups()` (`:582-611`, a `forkJoin` of the three
Cotivity endpoints at `pageSize = 100`). An unknown `masterKey` yields
`errorMessage = 'This master-data page is not available.'` (`:211`) with no HTTP call.

CRUD:

| Action | Verified behaviour |
| --- | --- |
| Create | `POST {apiBaseUrl}/{endpoint}` with the normalised payload (`:263-265`) |
| Update | `PUT {apiBaseUrl}/{endpoint}/{editingId}` (`:265`), `editingId` set by `edit(row)` from `config.idField` (`:291`) |
| Delete | `toastService.confirm('Delete <label>?', …)` then `DELETE …/{id}` (`:306-330`) — the confirm default label is `'Delete'` (`toast.service.ts:33`) |
| Read | `loadRows()` `GET` with `pageNumber`/`pageSize` (`:549-580`), `pageSize` fixed at **5** (`:191`); response read as `page?.items ?? []` / `page?.totalCount ?? 0` |
| Image | only sent when `config.endpoint === 'cotivityemployees'` **and** a file was chosen (`:256-258`); same 5 MB / JPEG-PNG-GIF client guard as the employee form (`:494-533`) |

**Verified quirks**

1. **`'null'` string scrubbing.** Four mechanisms exist to turn the literal string `"null"` into a
   blank: `normalizeNullInput` on `(input)` (`:395-407`), `normalizeRenderedNullInputs()` which
   queries the DOM for `[formControlName]` after view init (`:409-420`, called from `ngAfterViewInit`
   `:224` and a `setTimeout` in `ngOnInit` `:213`), `normalizeNullFormValues()` (`:428-441`) and
   `normalizeMasterValue()` (`:482-492`). The same pattern is repeated in
   `employee-add-form.ts:432-448, :520-522`. **Root cause Verified:** `seed-data.sql:197-206` inserts
   the literal `'NULL'` into `PanNumber` for all ten seeded employees. *Recommended*: fix the seed and
   drop this defensive layer.
2. **Nullable text columns become empty strings.** `save()`'s payload normalises with fallback `''`
   for any field that is not `number`/`select` (`:249-255`), so clearing `module` or `description`
   sends `""`, not `null` — the DB stores an empty string.
3. **`nameAndActive()` does not set `required` on the name field** (`:65-68`), and `resetForm()` only
   adds `Validators.required` when `field.required !== false` **and** the type is not `checkbox`
   (`:340-342`) — so names are required client-side by default, but `permissionCode`/`permissionName`
   etc. rely on `minLength`. Submitting a blank name therefore reaches the server and is answered by
   the FluentValidation 400 path ([10-error-handling.md](10-error-handling.md) §4a).
4. **No master config for `recruitment`** although `RecruitmentsController` exists server-side —
   *Verified* there is no SPA consumer ([09-api-endpoints.md](09-api-endpoints.md) §11 finding 12).
5. `optionsFor()` filters the self-reference out of the "Reports to Cotivity employee" dropdown while
   editing (`:461-466`), matching the server-side rule in `CotivityEmployeesController`
   ([10-error-handling.md](10-error-handling.md) §4c) — client and server enforce the same rule twice.

---

## 8. Employee add / edit form

`employee-add-form.ts` (688) + `.html` — the largest component in the SPA. Single flat `FormGroup`
(`:95-148`), 47 controls, template-driven sub-sections (`*ngIf`, `formControlName`).

### 8.1 Client-side validation (verified list)

| Control | Validators (`:96-147`) |
| --- | --- |
| `eNumber` | `required`, `min(1)`, `max(2147483647)`, `pattern(/^\d+$/)` |
| `firstName`, `lastName`, `email`, `emergencyContactPerson`, `fatherName`, `citizenshipNumber`, `panNumber`, `extensionNumber`, `joinedDate`, `joinedDateBS`, `departmentId`, `designationId`, `jobTypeId`, `bloodGroupId`, `genderId`, `teamId` | `required` + length/pattern as declared (`maxLength(100/150/20/50)`, `Validators.email`, `min/max` on `extensionNumber`) |
| `middleName`, `alternateEmail`, `phoneNumber`, `mobileNumber`, `emergencyContactNumber`, `motherName`, `spouseName`, `lockerNumber`, `stationNumber`, `address`, `addressLine2`, `city`, `state` | length-only (and `email` for `alternateEmail`) |
| `dateOfBirthBS` | `pattern(/^\d{4}-\d{2}-\d{2}$/)` + custom `validateNepaliDate` |
| `dateOfBirth` | none in code; the template binds `[max]="maxDateOfBirth"` (`employee-add-form.html:136`), and `joinedDate` binds `[min]="employeeId === null ? today : null"` (`:122`) |
| `roleIds`, `skillIds`, `dressIds` | none (arrays of stringified ids) |
| `createLoginAccount`, `username`, `initialPassword`, `confirmInitialPassword` | **none**, and never sent (§8.4) |

Messages are produced by a single `validationMessage(controlName)` with a hard-coded label map
(`:377-430`) and are surfaced **as one toast** — `onSubmit()` collects every invalid control's message
and joins them with spaces (`:282-288`, `:516-518`). `showFieldError(name)` on `(blur)` toasts a
single control's message (`:450-461`). *Recommended*: inline `<mat-error>`/field text instead of toasts.

### 8.2 Nepali (B.S.) ⇔ Gregorian (A.D.) date pairing

Two paired controls per date: `joinedDate` (Date, Material datepicker with the **native** adapter) +
`joinedDateBS` (string, `NepaliDatePickerComponent`), and the same for `dateOfBirth`.

* `watchDatePair(ad, bs)` (`:593-624`): AD → BS via `formatNepaliDate()`; BS → AD only when the string
  matches `^\d{4}-\d{2}-\d{2}$`, re-formats identically (round-trip check) and converts with
  `new NepaliDate(value).toJsDate()`. All writes use `emitEvent: false`, so there is **no feedback
  loop**. Subscriptions are scoped with `takeUntilDestroyed(this.destroyRef)`.
* `nepali-date-converter` is loaded **asynchronously** (`import(...)` in
  `loadNepaliDateConverter()` `:650-662`), so `nepaliDate` is `null` during the first synchronous
  pass. Two consequences, verified: `formatNepaliDate()` returns `''` while unavailable (`:626-633`),
  and `validateNepaliDate` reports `{ conversionUnavailable: true }` (`:637`) — an error key that
  `validationMessage` does not map, so the user sees `"<label> is invalid."` (`:429`).
* After the module resolves, `synchronizeDatePairAfterLoad()` (`:664-687`) reconciles each pair and
  calls `bsControl.updateValueAndValidity()`, which clears the `conversionUnavailable` error.
* The custom validator (`:635-648`) treats a Nepali date that does not round-trip as
  `invalidNepaliDate`.

### 8.3 `NepaliDateAdapter` and the picker component

`nepali-date-adapter.ts` (187) implements Angular Material's `DateAdapter<Date>` **in Nepali calendar
terms while the underlying value stays a JS `Date`**: `getYear/getMonth/getDate/getDayOfWeek` delegate
to `NepaliDate` (`:12-26`); month/day/weekday names are generated from fixed reference dates
(`2080` — `:28-42`); `getNumDaysInMonth` probes days 1…32 until the month rolls over (`:52-69`);
`createDate` validates twice, including a round-trip (`:75-85`); `addCalendarYears/Months` clamp the
day downwards via `clampDay()` (`:118-130, :164-175`); `parse`/`format` use `YYYY-MM-DD`; `toIso8601`
returns the **Nepali** string (`:138-140`). `NEPALI_DATE_FORMATS` (`:178-186`) maps the four display
formats the adapter's `format()` switch understands.

`NepaliDatePickerComponent` (74) provides `DateAdapter` and `MAT_DATE_FORMATS` **at component level**
(`:26-34`), so the global `provideNativeDateAdapter()` (`app.config.ts:37`) still governs every other
`matDatepicker` in the app — this is why the A.D. inputs and the B.S. picker can coexist on one form.
It is a `ControlValueAccessor` whose external value is a **string**:
`writeValue` parses via the adapter (`:42-48`), `onDateChange` formats back to `YYYY-MM-DD` and calls
`onChange` (`:63-66`), `setDisabledState` maps to `dateControl.disable/enable` (`:58-61`).

**Verified:** `writeValue` is only correct for string values; the paired form controls are always
strings (`''` or a formatted date) in every code path (`:118-120`, `:207`), so no mismatch occurs
today. *Inferred* reason for the string-based CVA: the backend columns `JoinedDateBS` / `DateOfBirthBS`
are `string`/`char(10)` (`EmployeeRepository.EmployeeDetailRow:975, :983`).

### 8.4 Submit path and the dead "Login Account" section

`onSubmit()` (`:274-375`) order: guard `isSubmitting` → `markAllAsTouched` → aggregate all invalid
control messages and abort → optional-address rule → build `CreateEmployeeRequest` → `POST`
(create) or `PUT` (edit) → toast + `navigate(['/people'])`.

* **Address is single-valued in the UI but array-valued on the wire**: five flat controls are packed
  into a one-element `addresses[]` with `latitude: 0`, `longitude: 0`, `isPrimary: true` (`:335-344`),
  and the whole array is omitted (`[]`) when every address field is blank (`:290-297`). On edit, only
  `addresses[0]` is read back (`:221-225`) — **Verified** that a second address row stored on the
  server is invisible in this form and is **wiped on save** because `EmployeeService.UpdateAsync`
  replaces the child collections (see [06-application-services.md](06-application-services.md)).
* Skills carry a per-skill level collected separately in `skillLevels` (`:81`, `:468-486`), default 1
  (`:472`, `:481`), and `isPrimary: false` for every entry (`:348`).
* `dressSizes` is sent as the *whole* master list filtered by the selected ids, including `dressName`,
  `size`, `isActive` (`:350-357`) — the client re-sends master data rather than ids.
* Images: `FileReader.readAsDataURL` → `imageBase64` is a full `data:` URL (`:548-567`), guarded
  client-side to JPEG/PNG/GIF ≤ 5 MB (`:535-546`). The backend re-derives the extension from magic
  bytes and writes to disk ([10-error-handling.md](10-error-handling.md) §5).
* **Dead UI (high-value finding):** the `Login Account` section
  (`employee-add-form.html:332-355`, shown only when `employeeId === null`, i.e. on create) renders a
  `createLoginAccount` checkbox plus `username` / `initialPassword` / `confirmInitialPassword` inputs
  (`minlength="12"` in the template). None of these four controls appear in `CreateEmployeeRequest`
  (`employee.service.ts:18-66`) and **no `CreateLoginAccount`/`InitialPassword` member exists anywhere
  in the backend** (searched all `*.cs` in `src/`). Accounts are instead provisioned as a side effect of
  role assignment, keyed on the **email**: `AssignEmployeeRolesAsync` skips entirely when `email` is
  blank, otherwise inserts `dbo.Users (EmployeeId, Username = @Email, Email)` and then replaces
  `UserRoles` (`EmployeeRepository.cs:878-930`). **Verified consequence:** an Admin who ticks the
  checkbox and types a username and a 12-character password sees it silently discarded. Severity: high
  (misleading UI). *Recommended*: remove the section or implement the contract on both sides.
* `createLoginAccount` also has no client validation, so the password `minlength="12"` is an HTML
  attribute only — with a reactive form and no `Validators.required`/`minlength` on those controls it
  is not enforced (`:144-147`).

### 8.5 Edit-mode loading

`ngOnInit` reads `:employeeId` from the route (`:86-90`), calls `loadMasters(employeeId)` and, in its
success handler only, `loadEmployee(employeeId)` (`:252-254`) — so masters always precede the employee
payload. `patchEmployee` (`:174-230`) aborts with `'The employee API returned an invalid date.'` if
`joinedDate` (or `dateOfBirth`) is unparseable (`:180-184`), maps ids to **strings** for the `<select>`
bindings (`normalizeFormId` `:524-526`), and patches with `emitEvent: false` (`:227`) — the date-pair
watchers therefore do not fire on load; reconciliation is done by
`synchronizeDatePairAfterLoad()` instead. `currentImageBase64` is kept for display (`:229`) but a
removed image can only be cleared client-side (`clearImage` `:570-574`); sending `imageBase64: null`
does not delete the stored file (no delete path exists — [10-error-handling.md](10-error-handling.md) §5).

---

## 9. API clients — five of them, no shared base

| Client | Endpoints | Notes |
| --- | --- | --- |
| `EmployeeService` (`employee.service.ts`, 522) | `api/employees` (+`/{id}`, `/details`, `/department/{id}`, `/tree`, `/employee-master`), `api/departments`, `api/employee-releases*`, `api/employee-terminations*` | 24 public methods; private `EmployeeApiRecord` mapping; SVG avatar generator |
| `EmployeeHierarchyService` (25) | `api/employees/{id}/hierarchy` | unwraps `.data` |
| `RbacService` (47) | `api/user-roles/matrix` GET/PUT, `api/role-permissions/matrix` GET/PUT | unwraps `.data` on GET only |
| `AuthService` (98) | `api/auth/*` | §4.1 |
| `MasterSettings` (617) | 15 master endpoints, inline `HttpClient` calls | §7 |

### 9.1 The envelope is read inconsistently — verified

There is no shared `ApiClient`. Each file re-declares its own `ApiResponse<T>`:

```csharp
// employee.service.ts:12-16, rbac.models.ts:58-62, employee-hierarchy.service.ts:8-12, master-settings.ts:54-58
interface ApiResponse<T> { data: T; success: boolean; message: string; }
```

and the access patterns differ:

* `EmployeeService.getEmployees` keeps the whole body and maps `response.data` (`:309-311`);
  `createEmployee` returns the **raw envelope** so callers must read `.data`
  (`:343-345`; `employee-add-form.ts:362` maps it away).
* `getEmployeeMasters()` is typed `Observable<any>` (`:515-520`) and its consumer indexes `response.data`
  with no type at all (`employee-add-form.ts:234-251`) — **Verified** the only untyped call in the SPA.
* `MasterSettings` ignores the envelope type on writes: `this.http.post(endpoint, payload)` with no
  generic (`:264-265`), and `this.http.delete(endpoint)` (`:319`).
* `AuthService` reads responses that have **no** envelope at all (`AuthResponse`, `DevSignInUser[]`,
  `CurrentUser`) because `AuthController` returns bare objects
  ([08-auth-and-rbac.md](08-auth-and-rbac.md) §8).

`PagedResult<T>` (`shared/models/paged-result.model.ts`, 9 lines) declares
`items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage`; the backend
`TeamHub.Shared.Responses.PagedResult<T>` exposes `HasPreviousPage`/`HasNextPage` as
**computed properties** (`PagedResult.cs:15-17`) so they do serialise — **Verified** the two shapes
match, and `getDepartments()` (`employee.service.ts:335-341`) and `loadEmployees()` in the lifecycle
form (`employee-lifecycle-form.ts:187-189`) both rely on `hasNextPage` for their full-scan loops.

### 9.2 `EmployeeService` interface inventory (verified fields)

`CreateEmployeeRequest` (`:18-66`, 36 fields incl. three child arrays), `EmployeeEditResponse`
(`:68-106` — `Omit<CreateEmployeeRequest, 'addresses' | 'skills' | 'dressSizes'>` plus `employeeId`,
11 display names, and the three arrays re-typed with ids/names), `EmployeeDetails` (`:216-274`),
`EmployeeReleaseRequest`/`ReleaseClearanceUpdate`/`EmployeeReleaseRecord` (`:108-185`),
`EmployeeTerminationRequest`/`TerminationClearanceUpdate`/`EmployeeTerminationRecord` (`:139-202`),
`ReleasePage<T>` (`:205-211`), `EmployeeApiRecord` (`:276-284`).

Two **Verified** drift findings:

1. `EmployeeMastersResponse` (`models/employee-master.model.ts:52-63`) declares **10** collections,
   while `/api/employees/employee-master` actually returns **14** (`departments, designations, jobTypes,
   bloodGroups, genders, roles, teams, skills, recruitments, employees, cotivityEmployees, businesses,
   dressSizes, addressTypes` — `EmployeeRepository.cs:855-876`, consumed at
   `employee-add-form.ts:238-251`). The interface is **not imported by any file** (verified by search) —
   dead and incomplete.
2. `EmployeeReleaseRequest.status`/`EmployeeTerminationRequest.status` unions contain values the
   server rejects: `'Pending Approval'`, `'Completed'`, `'UnderInvestigation'` (`:156-157`) are not in
   `TerminationStatusWorkflow.All` (`TerminationStatusWorkflow.cs:22-25`, which allows exactly
   `Draft, Submitted, PendingApproval, Approved, Terminated, Rejected, Cancelled`). The lifecycle UI
   never offers them (`employee-lifecycle-list.ts:33-35`), so it is dead type surface today;
   *Recommended*: delete.

`getEmployees()` sends `pageNumber, pageSize, searchTerm, searchField`
(`:303-307`) and `mapEmployeePage` (`:496-513`) fills `imageBase64` with a generated inline **SVG
avatar** (`createAvatar` `:476-494`, `data:image/svg+xml;charset=UTF-8,…`) and assigns a rotating
`accent` colour from a 7-entry palette (`:291`).

---

## 10. Lifecycle screens (resignations and terminations)

`employee-lifecycle-form.ts` (370) and `employee-lifecycle-list.ts` (524), both read in full. The list
component holds the permission getters that gate its actions: `can()` (`:60-62`), `canAdd` =
`resignations.add` (`:64-66`), `canAddTermination` = `terminations.add` (`:72-75`),
`canManageTerminations` = `terminations.manage` (`:77-80`), and the per-transition map
(`:48-56`); the toolbar renders one button for both modes with
`*ngIf="isResignation ? canAdd : canAddTermination"` (`employee-lifecycle-list.html:6`).

### 10.1 `EmployeeLifecycleFormComponent`

* `mode` comes from route `data`; `isRelease` is true for `'release'` **and** `'resignation'`
  (`:33`) — the two routes share one form.
* Employee picker: `loadEmployees()` pages `getEmployees(page, 100)` with `expand`/`reduce` until
  `hasNextPage` is false (`:185-206`) — **Verified** the whole directory is fetched to fill one
  `<select>`, even when `employeeId` came from the route.
* Release form (`:209-217`): `employeeId`, `resignationDate`, `noticePeriodDays`
  (`required, min 0, max 180, ^\d*$`), `lastWorkingDate`, `reason`, `earlyReleaseReason`, `remarks`;
  two cross-field validators at group level: `lastWorkingDateValidator` (`:311-319`) and
  `reasonOrEarlyReleaseValidator` — an **either/or** rule: `earlyReleaseReason` non-empty **or**
  `reason.length >= 10` (`:222-229`).
* Derived getters: `servedNoticeDays` = whole days between `resignationDate` and `lastWorkingDate`
  (`:337-346`, parsed as UTC midnight so DST cannot shift it), `noticePeriodShortfallDays` (`:54-59`),
  `expectedReleaseDate` (`:61-67`), `isEarlyRelease` (`:69-74`).
* Termination form (`:231-238`): `terminationDate` (`required` + `notFutureDateValidator` `:321-328`),
  `terminationType` from a **7-value** list (`:36`), `reason` via `trimmedReasonValidator(1)` — note
  the explicit minimum of **1** character, unlike the release form's 10 (`:236` vs `:226`), and
  `remarks`.
* Only these fields are sent. Everything else in the request DTO is hard-coded
  (`:136-153`, `:161-178`): `releaseType: 'Resignation'` always, all clearance flags `false`,
  `settlementAmount: null`, `investigationRequired: false`, `warningCount: null`,
  `eligibleForRehire: false`. **Verified:** the SPA never populates the investigation/warning/
  settlement columns even though the backend accepts them
  ([04-dtos.md](04-dtos.md)).
* `saveDraft()` → `status: 'Draft'`, `submitWorkflow()` → `status: 'Submitted'` (`:100-108`) — the only
  two initial states, matching `AllowedInitial` in both workflows
  ([06-application-services.md](06-application-services.md)).
* Early release is announced by an **info toast** (`:131-135`) but does not block the save; the server
  likewise does not require `earlyReleaseReason` (verified in
  [05-validators.md](05-validators.md)) — the rule exists only as a client-side either/or.
* `getValidationMessage()` (`:247-299`) is a hand-written priority ladder returning one sentence for
  the whole form, again surfaced as a single toast (`:122`).

### 10.2 `EmployeeLifecycleListComponent`

* One component, two screens, chosen by `data.mode` (`:30`); releases and terminations are loaded
  through the **server-paged** endpoints `GET api/employee-releases/paged` and
  `…/employee-terminations/paged` with `search`, `department`, `status` (+`releaseType` for
  resignations) (`:121-147`). `releaseType: 'Resignation'` is always sent on the resignation screen
  (`:127`).
* Client-side filters/`pageSize`/paging state exist **in addition** (`:89-102`, `:160-190`):
  `filteredReleases`/`filteredTerminations` re-filter the already-filtered page, commented as a
  "safety net" (`:160`), and `pagedReleases` is just `filteredReleases` (`:179-186`).
  *Recommended*: delete — the double filter can only hide rows, never restore them.
* Search uses a `Subject` + `debounceTime(350)` + `distinctUntilChanged()` +
  `takeUntilDestroyed` (`:28, :114-115`); the two `<select>` filters re-query immediately
  (`resetPage()` `:205-207`).
* **Workflow graph mirrored in the client** (`:36-55`), keyed exactly like the server graphs
  (`ReleaseStatusWorkflow.cs:33-42`, `TerminationStatusWorkflow.cs:34-43`):
  `Draft → [Submitted, Cancelled]`, `Submitted → [PendingApproval, Approved, Rejected, Cancelled]`,
  `PendingApproval → [Approved, Rejected, Cancelled]`, `Approved → [Released|Terminated, Cancelled]`,
  terminals `[]`. Action labels (`:58-67`), icons (`:297-309`), a destructive/neutral menu split
  (`primaryActions`/`dangerActions` `:289-295`) and a `PendingApproval → "Pending Approval"` display
  override (`:69-71, :220-222`). **Verified** 1:1 today; *Recommended* treat as duplicated business
  logic — the server already returns 409 with the allowed next states
  ([10-error-handling.md](10-error-handling.md) §3), so the client copy exists for UX only and will
  silently drift if either side changes.
* Transitions and clearance: `transition()` → `POST …/{id}/status` with `{ status }` (`:347-362`);
  `toggleClearance()` → `PUT …/{id}/clearance` sending the **full** checklist, taking untouched values
  from the current row (`:364-384`); terminations have the parallel pair (`:394-430`). On success the
  row is replaced in place from the response (`replaceRelease` `:386-392`) and no reload is issued —
  so a stale row after a concurrent edit is never detected.
* `busyIds` is a `Set<number>` guarded per record (`:88, :224-226, :349`) which prevents double clicks
  on the same row but not on the same *page*.
* Clearance progress is rendered client-side: `isCleared()`/`clearedCount()`
  (`:240-254`) over the 5 release fields / 4 termination fields declared at `:73-86`;
  `settlementAmount` is **not** part of the client checklist even though
  `ReleaseClearanceUpdate` allows it (`employee.service.ts:130-137`).
* A details modal is driven by `detailId` with live getters into the loaded page (`:256-274`), Escape
  handling that prefers closing the modal over the menu (`:282-286`), and fixed-position menus that
  close on scroll/resize/outside click (`:312-345`).

---

## 11. RBAC screens

`rbac.service.ts` (47), `rbac.models.ts` (62), `employee-role-matrix.component.ts` (220) + `.html`
(74), `role-permission-matrix.component.ts` (231) + `.html` (75), shared `rbac-matrix.css` (304).
The two components are near-identical, differing only in the row key (`userId` vs `permissionId`) and
the save payload shape.

* `canEdit` (`employee-role-matrix.component.ts:52-54`, `role-permission-matrix.component.ts:52-54`)
  = `hasRole('Admin') || hasPermission('<code>')`, the same codes the guards use (§3).
* **Dirty tracking with two `Map<number, Set<number>>`** — `selections` (pending) and `originals`
  (as loaded) — plus `setsEqual()` (`:207-219` / `:218-230`). `save()` diffs the two maps and sends
  only changed rows as `{ assignments: [{ userId|permissionId, roleIds }] }` (`:84-98` / `:169-176`),
  after a `toastService.confirm` naming the count (`:93-97`).
* Pagination/search reuse the same pattern as the lifecycle list (`pageSize = 10`, 300 ms debounce,
  reset to page 1).
* **Verified cross-page behaviour** (a deliberate design, and a UX hazard): `load()` seeds only rows
  not already in `originals` so "pending edits on other pages survive paging"
  (`:177-185` / `:188-196`). Consequently (a) a later `load()` of a visited row does **not** refresh
  its server values, (b) after a successful save both maps are cleared and reloaded (`:109-112`), and
  (c) the confirm dialog counts changed rows across **all visited pages**, some of which are not
  visible at click time. *Recommended*: show the diff before saving.
* `EmployeeRoleMatrixRow` (`rbac.models.ts:6-12`) is keyed by **both** `userId` and `employeeId`;
  the save request sends `userId` only (`:28-30`), matching `SaveUserRolesRequest` on the server
  ([04-dtos.md](04-dtos.md)).
* Both `matrix` GETs read `matrix?.rows ?? []` etc. — **Verified** defensive null handling, required
  because the RBAC matrix endpoints return `200` with `data: null` in the empty-permission case
  ([09-api-endpoints.md](09-api-endpoints.md) §11 finding 8).

---

## 12. Shared layer

| File | Verified contract |
| --- | --- |
| `shared/models/paged-result.model.ts` (9) | `PagedResult<T>` mirroring the backend (§9.1) |
| `shared/services/toast.service.ts` (48) | `providedIn: 'root'`; `ToastKind = 'success' \| 'error' \| 'info' \| 'confirm'`; `messages$` is a plain `Subject` (no replay — a message emitted before subscription is lost); durations: success/info **4000 ms**, error **6000 ms**, confirm **7000 ms** (`:26, :38, :44`); `confirm(message, onConfirm, actionLabel = 'Delete')` (`:33-42`) |
| `shared/components/toast-host/toast-host.component.ts` (37) | Subscribes with `takeUntilDestroyed`, appends immutably, `setTimeout(dismiss, duration)` (`:20-24`); `confirm()` dismisses then invokes `onAction` (`:32-35`). **Verified consequence:** an unacknowledged confirm simply disappears after 7 s — e.g. "Create a release workflow for … ?" (`directory-view.component.ts:184-188`) never fires. There is no backdrop or focus handling, so a confirm is a dismissible notification, not a modal |
| `shared/components/pagination/pagination.component.ts` (47) | `@Input() pageNumber/pageSize (default 5)/totalCount/isLoading`; `@Output() pageChange/pageSizeChange`; `totalPages = ceil(totalCount / pageSize)` (**can be 0**, unlike the `|| 1` used in the RBAC components); `startItem`/`endItem` for the "showing x–y of z" line; `previous()`/`next()` no-op while `isLoading` |
| `shared/components/topbar/topbar.component.ts` (36) + `.html` (10) | Thin shell now: injects `MenuService` (`:18`), exposes `isAuthenticated` (`:22-24`) and `logout()` which navigates to `/login` on both success **and** error (`:30-35`). The template is one `<app-nav-menu [items]="menu.visibleMenu()" />` (`topbar.component.html:5`) plus the Sign-out button (`:7-9`). **All** per-permission getters and the hardcoded `masterModules` list were removed — see §12.1 |
| `shared/components/nav-menu/nav-menu.component.ts` (56) + `.html` (30) + `.css` (123) | The recursive renderer. Signal inputs `items` (required) and `level` (`:26, :29`), `OnPush` (`:22`), renders itself for children (`nav-menu.component.html:15`). Imports only `RouterLink`/`RouterLinkActive`; **no** dynamic component loading. Full contract §12.1 |
| `shared/components/results-header` (17), `employee-tab` (18), `directory-controls` (27) | Presentational, `@Input`/`@Output` only. `directory-controls` owns the `EmployeeSearchField` type (`:4`) re-used by the directory and page components |
| `shared/services/api-error-message.ts` (47) | Single error reader used by 26 call sites in 10 files; precedence `errors` → `error` → `message ?? detail` → fallback — documented in [10-error-handling.md](10-error-handling.md) §6 |

### 12.1 Permission-driven recursive navigation

Added 2026-10-23, replacing the hardcoded `@if` blocks that used to sit in `topbar.component.html`.
Five files, all read in full: `core/config/menu.config.ts` (137), `core/config/menu-filter.ts` (74),
`core/config/menu.service.ts` (32), `shared/components/nav-menu/nav-menu.component.ts` (56) +
`.html` (30) + `.css` (123).

**Data flow (verified end to end):**

```text
AuthResponse.permissions  (POST dev-sign-in / refresh-token, doc 08 §5)
  → AuthService.storeSession() → sessionStorage["teamhub.permissions"]  (auth.service.ts:113-118)
  → sessionRevision.update(+1)                                         (:117 and :110 on clear)
  → sessionState computed { isAuthenticated, permissions:Set }         (:33-44)
  → MenuService.visibleMenu computed                                   (menu.service.ts:20)
  → filterMenu(APP_MENU, context) — pure, unlimited depth              (menu-filter.ts:50-74)
  → <app-nav-menu [items]> renders leaves and recurses for groups      (nav-menu.component.html:2-29)
  → click → routerLink → route guard re-checks the same code → API policy
```

**`MenuItem` contract** (`menu.config.ts:17-43`) — `id` (track key, unique among siblings), `label`,
`icon` (declared, not rendered — see decision 3), `route`, `exact`, `permission`, `requiresAll`,
`requiresAny`, `hideIfPermission`, `visibility: 'any' | 'anonymous' | 'authenticated'`, `alignEnd`,
`children`. Every field is `readonly`; the tree is `readonly MenuItem[]`.

**The tree as configured** (order here is render order):

| Item | Route | Gating | Line |
| --- | --- | --- | --- |
| Home | `/home` | `visibility: 'anonymous'` | `:51` |
| People | `/people` | `people.allemployee.search.view` | route `people` |
| Administration | `/administration` | `administration.view` | route `administration` |
| Apply Resignation | `/employees/resignations/new` | `resignations.add` **and** `hideIfPermission: resignations.view` | `:53-62` |
| Resignations | `/employees/resignations` | `resignations.view` | `:63-69` |
| Terminations | `/employees/terminations` | `terminations.view` | `:70-76` |
| Organization Master (group) | — | visible when at least one child has its `.view` grant | `:47-60` |
| └ 10 organization masters | `/master/<key>` | `organization.<master>.view` | `:49-58` |
| Cotivity (group) | — | visible when at least one child has its `.view` grant | `:62-69` |
| └ 3 Cotivity masters | `/master/<key>` | `master.cotivity-<key>.view` | `:66-68` |
| Settings gear: Role | `/master/role` | `master.role.view` | `topbar.component.html` |
| Settings gear: Manage Permissions | `/master/permission` | `permissions.manage` (not `master.permission` — mirrors `masterGuard`) | `topbar.component.html` |
| Settings gear: Employee Role Matrix | `/settings/employee-role-matrix` | `setting.gear.EmployeeRole` | `topbar.component.html` |
| Settings gear: Role Permission Matrix | `/settings/role-permission-matrix` | `setting.gear.RolePermission` | `topbar.component.html` |
| Login | `/login` | `anonymous` + `alignEnd` (pushed right) | `:136` |

The Organization master routes and actions use the supplied
`organization.<master>.view/create/edit/delete` codes (for example,
`organization.businessunit.*` and `organization.bloodgroup.*`). These granular codes control
frontend visibility only: the API still uses its existing broad `master.<key>`
policies, so keep granting those legacy codes for backend requests. This does not provide
server-side per-action enforcement.

Role and Manage Permissions are shown in the gear only with
`setting.gear.role.view` and `setting.gear.manage.permissions.view`. Their CRUD controls use
the corresponding `.create`, `.edit`, and `.delete` grants. The APIs still require the existing
`master.role` and `permissions.manage` grants respectively.

Cotivity navigation and page actions use `cotivity.department.*`, `cotivity.designation.*`, and
`cotivity.employee.*` grants (View/Create/Edit/Delete). Backend API authorization remains on the
existing `master.cotivity-department`, `master.cotivity-designation`, and `master.cotivity-employee`
permissions.

**Filtering rules** (`menu-filter.ts`, pure function, no Angular dependency):

| Rule | Implementation |
| --- | --- |
| Session gate | `matchesVisibility()` `:13-22` — `anonymous` ⇔ `!isAuthenticated`, `authenticated` ⇔ `isAuthenticated`, undefined ⇒ always |
| Code gate | `matchesPermissions()` `:24-38` — `permission` (all-of), `requiresAll.every`, `requiresAny.some`, `hideIfPermission` negation |
| Unlimited depth | `filterItem()` `:63-74` recurses through `filterMenu()`; no level cap, no per-level code |
| Parent retention | a group survives iff ≥ 1 filtered child survives (`:72-73`) — empty groups are dropped |
| Order | preserved by the single `for` loop appending in config order (`:53-58`) |
| No mutation | every returned node is a fresh copy `{ ...item }` (`:69, :73`); `APP_MENU` is never written, so the next user in the same tab starts from the pristine tree |

**Rendering and active state.** A leaf is a real `routerLink` with `routerLinkActive="active"` and
`[routerLinkActiveOptions]="{ exact }"` (`nav-menu.component.html:20-27`); a group is a `<button>`
toggler whose panel contains another `app-nav-menu` (`:10-16`). Group highlighting **cannot** use
`routerLinkActive`: that directive queries `@ContentChildren(RouterLink)`, which does not cross into a
nested component's view, so the descendant links rendered by the child instance are invisible to it.
Instead `isBranchActive()` walks the (already filtered) subtree against `Router.url`, which is tracked
as a signal via `toSignal(this.router.events…)` (`nav-menu.component.ts:33-46`), stripping query and
fragment (`:53-55`). Dropdowns are CSS-driven (hover + `:focus-within` on the same `level() === 0` /
`level() > 0` classes), and `.menu-level--root` reproduces the previous bar layout, so logo/user info,
logout, responsive wrap and the highlight colours are unchanged (`topbar.component.css` shrank to 46
lines once the dead dropdown styles were removed).

**Session lifecycle — verified behaviour, each case covered by a spec:**

| Event | Menu outcome |
| --- | --- |
| Sign-in | `storeSession` bumps the revision → `visibleMenu()` recomputes for the new code set |
| Reload with an expired access token | guard calls `restoreSession()`; failure clears the session → nav collapses to `Home`/`Login` before the `/login?returnUrl` redirect |
| Token refresh (401 replay or manual) | the API re-queries `UserRoles → Roles → RolePermissions → Permissions` on every rotation (doc 08 §6), and `storeSession` re-stores it, so grants/revocations appear without re-login |
| Sign-out / account switch | `clearSession()` in `finalize` runs **even on HTTP failure** (`auth.service.ts:96-104`), so no stale item can survive; the next sign-in writes a different set |
| API error while loading | nothing is cached per-item: the tree is derived from the stored session on each revision, and `MenuService` has no local copy to invalidate |

**Four deliberate decisions** (all *Verified* against the code cited):

1. **Administration is permission-gated in the SPA.** `/administration` requires `administration.view`
   in its menu and route guard. The employee read API endpoints remain `[AllowAnonymous]`, so this permission
   does not itself protect API reads. `administration.hierarchy.view` gates the hierarchy controls;
   employee-specific termination creation accepts `administration.terminate` or `terminations.add`.
2. **Self-service mutual exclusion is preserved** via `hideIfPermission: 'resignations.view'` (`:60`),
   the config-level equivalent of the old `@if (!canViewResignations)`. Unlike the old bar, the entry
   also requires `resignations.add` — **a fix**: the previous markup showed "Apply Resignation" to
   permission-less users whose click was then bounced to `/home` by `resignationAddGuard`.
3. **`icon` is declared but not rendered** (`:21-25`) — the previous bar had no icons, so omitting them
   keeps the visual contract; new items can opt in without reshaping `MenuItem`.
4. **No `isAdmin` fallback in the menu.** `sessionState` carries codes only (roles are not exposed), so
   the filter is role-agnostic by construction. Consequence recorded as §13 finding 16.

**Verified consequence of the code-driven menu on the termination side:** the Terminations entry requires
`terminations.view` (`menu.config.ts:70-76`), so a hypothetical role granted **only** `terminations.add`
would see no menu path to `/employees/terminations/new` even though `terminationAddGuard` lets it through
— the button it unlocks lives on a list screen it cannot navigate to. No seeded role is in that position
(`HR` holds `view` + `add` + `manage`, `seed-data.sql:482-484`), and resignations avoid the trap by
giving filers their own entry (`:53-69`, "Apply Resignation" = `resignations.add` +
`hideIfPermission: 'resignations.view'`). *Recommended* if an add-only termination role is ever introduced:
mirror that pattern with a "File Termination" entry rather than loosening the list item.

**Test inventory for this cluster** — 5 spec files, 40 of the SPA's 63 tests:
`menu-filter.spec.ts` (11: nested gating, ancestor retention at 4 levels, empty-group removal,
ordering, `requiresAll`/`requiresAny`/`hideIfPermission`, immutability via snapshot + identity checks,
a single-`master.*` grant on the real `APP_MENU`), `menu.service.spec.ts` (11: full-catalog admin tree
with exact child labels, self-service exclusion, empty-permission account, multi-role union,
case-insensitivity, logout staleness, refresh-changed permissions, account switching),
`nav-menu.component.spec.ts` (6: leaf vs group rendering, 3-level recursion in one component, real
router navigation, active leaf **and** every ancestor group, `alignEnd`, empty tree),
`topbar.component.spec.ts` (4) and `auth.guard.spec.ts` (8: unauthorized direct navigation → `/home`
`UrlTree`, re-check after `clearSession`, anonymous → `/login?returnUrl`, `masterGuard` per-module
codes, `signedInLandingGuard`, and the termination create routes staying on `terminations.add` when
`terminations.manage` is held). Because the session lives in `sessionStorage`, which is global across
suites in one jsdom worker, the three session-writing specs each run `afterEach(() => sessionStorage.clear())`
— without it they pollute unrelated tests.

---

## 13. Verified findings (frontend)

| # | Finding | Severity |
| --- | --- | --- |
| 1 | **Login Account section in the employee form is dead UI** — 4 controls never sent, no backend fields exist; account creation actually happens implicitly from the email when roles are assigned (`employee-add-form.html:332-355`, `EmployeeRepository.cs:878-930`) | high |
| 2 | **Full-scan list loads**: `getDepartments()` and `EmployeeLifecycleFormComponent.loadEmployees()` page through the whole dataset with `pageSize = 100` (`employee.service.ts:328-341`, `employee-lifecycle-form.ts:185-206`); `loadCotivityLookups()` caps lookups at 100 rows and silently truncates beyond it (`master-settings.ts:583`) | medium |
| 3 | **N+1 on the Departments tab** — one extra request per department, eager, on `ngOnInit` (`departments-view.component.ts:42-55`) | medium |
| 4 | **Workflow graph duplicated client-side** (`employee-lifecycle-list.ts:38-46` and `:82-91` vs `ReleaseStatusWorkflow.cs:33-42`, `TerminationStatusWorkflow.cs:34-43`) — correct today, no shared source of truth | medium |
| 5 | **Cross-page dirty state in both RBAC matrices** can save rows the user is not currently looking at, and visited rows are never refreshed (`employee-role-matrix.component.ts:177-185`, `role-permission-matrix.component.ts:188-196`) | medium |
| 6 | **`getEmployeeMasters()` is `Observable<any>`** and the only typed contract for it (`EmployeeMastersResponse`) is dead and missing 4 of 14 collections (`employee.service.ts:515-520`, `employee-master.model.ts:52-63`) | medium |
| 7 | **Interceptor replay loses `withCredentials`** for `/api/auth/*` retries (`auth.interceptor.ts:26`) | low |
| 8 | **Only 3 components in the whole SPA use `OnPush`** — the hierarchy pair plus `NavMenuComponent` (`nav-menu.component.ts:22`), searched all `*.ts` in `src/app` for `ChangeDetectionStrategy.OnPush` — while every other component calls `markForCheck()` as if it did, so change detection remains global (a keystroke in the debounced search re-renders the tree) | low (perf) |
| 9 | **Confirm toasts are not modal** and expire after 7 s without acting (`toast.service.ts:33-42`, `toast-host.component.ts:23`) — delete/release/terminate confirmations can silently no-op | low-medium |
| 10 | **Validation is toast-only** everywhere (employee form, lifecycle forms, master settings) — no per-field inline errors, and messages are concatenated into one sentence (`employee-add-form.ts:516-518`, `employee-lifecycle-form.ts:247-299`, `master-settings.ts:239-243`) | low |
| 11 | **Nepali date errors are mislabelled while the converter is still loading**: `conversionUnavailable` is not in the label/error ladder and falls through to `"… is invalid."` (`employee-add-form.ts:637`, `:429`) | low |
| 12 | **The edit form edits only the primary address and discards the rest** (`employee-add-form.ts:221-225, :335-344`) | medium |
| 13 | **Master-data writes send `""` for cleared nullable text columns** (`master-settings.ts:249-255`) | low |
| 14 | **`AuthService.getCurrentUser()` and the whole `CurrentUser` model are unused** in the SPA (`auth.service.ts:92-94`, search-verified) | low |
| 15 | **Entra mode has no session bridge** — with `enableEntra: true` the `authInterceptor` is not registered and sessionStorage is never written, so `authenticatedGuard` and every `rbacGuard` instance deny (`app.config.ts:19-30`, `auth.guard.ts:11`, `:56`) **and** the navigation collapses to its anonymous items (`Home`, `Login`) because `sessionState.isAuthenticated` reads the same storage (`auth.service.ts:33-44`) | high (only if Entra is switched on) |
| 16 | **[AUTH/UX] The menu is code-driven while the API also honours an `Admin` role bypass** (`PermissionAuthorizationHandler.cs:25-29` succeeds every policy on the `Role` claim alone). A trimmed Admin grant set would still allow all endpoints but shrink the nav — no frontend role fallback was added, because role-specific menu logic was explicitly out of scope (§12.1 decision 4). It was harmless while the mirror held; Admin's live set has since drifted by one code, which is now visible as missing termination row actions (finding 19, doc 14 L15) | low (latent) |
| 17 | **[DATA] Seed role grants are coarse at the top and unreachable in the middle** — Admin still receives the whole catalog (`rbac-schema.sql`), and `seed-data.sql` grants real per-role subsets (`NormalUser`: `resignations.add`; `Manager`: resignation view/add/action codes + `master.team`; `HR`: administration create/update/release + resignation steps + termination codes), so partial-menu grants are representable. `administration.view` gates the Administration route; `administration.terminate` and `administration.hierarchy.view` gate their corresponding actions, while employee read endpoints remain anonymous. Two limits remain: `Manager`/`HR` have no `Users`/`UserRoles` rows and are outside `DevSignIn.AllowedRoles` (`["Admin","NormalUser"]`), so those grants cannot be signed in as; and `resignations.manage` plus legacy `master.view`/`master.manage` are unused | low (was medium; testability) |
| 18 | **[UX/CD] `LoginComponent` was the one component that did not mark itself dirty after an HTTP callback** — the SPA runs zoneless (`zone.js` is not a dependency and `angular.json` declares no `polyfills`), so a `devUsers` assignment inside `ngOnInit`'s subscribe never reached the template: a cold `/login` rendered "No approved development accounts are available." with 0 buttons even though `GET /api/auth/dev-users` answered 200 with both accounts. Every other page already pairs its callback with `markForCheck()` (`employee-lifecycle-list.ts:144/:168/:178`, `employee-directory.component.ts:96/:102`). *Fixed 2026-10-23* — `login.component.ts:22-24` injects `ChangeDetectorRef`, `:40-49` and `:65-75` mark dirty on next/error and on the sign-in error/complete paths | high (blocked sign-in; resolved) |
| 19 | **[AUTH] The "Add" buttons are per-code and were confirmed live, but Admin's live grant set is one code short** — `canAdd`/`canAddTermination` read only `resignations.add`/`terminations.add` (`employee-lifecycle-list.ts:64-75`, template `:6`), and driving the app in a browser confirmed each code alone toggles its own button with the list still rendering. Separately, Admin's token carried 37 codes with `terminations.manage` absent, so `nextTerminationActions()` (`:276-280`) shows Admin no termination row actions at all — a data drift, not a frontend defect (doc 14 L15) | low (data drift; UI fails closed) |

*Unknown:* whether findings 1, 6 and 14 are unfinished work or deliberately removed features — no
issue tracker, TODO, or comment in the repository states an intent.

---

Related: [01-architecture.md](01-architecture.md) · [08-auth-and-rbac.md](08-auth-and-rbac.md) ·
[09-api-endpoints.md](09-api-endpoints.md) · [10-error-handling.md](10-error-handling.md) ·
[12-workflows.md](12-workflows.md) · [14-known-issues.md](14-known-issues.md)
