# 12 · End-to-End Workflows

Every trace in this document was built by reading the actual implementation files end to end. Each step cites the file and (where useful) the line range so it can be re-verified. Nothing here is inferred from names alone; where a behavior could not be verified it is explicitly marked.

Companion documents: [08-auth-and-rbac.md](08-auth-and-rbac.md) (auth internals), [09-api-endpoints.md](09-api-endpoints.md) (full endpoint catalog), [10-error-handling.md](10-error-handling.md) (exception→HTTP mapping), [11-frontend.md](11-frontend.md) (component/client detail).

---

## 1. Request pipeline (applies to every workflow)

Backend pipeline order, from `TeamHub.API/Program.cs` (verified lines 113–131):

```
app.UseExceptionHandler()      // global ApiExceptionHandler → Problem JSON (doc 10)
app.UseHttpsRedirection()
app.UseStaticFiles()           // serves uploaded images from wwwroot
app.UseCors(FrontendCorsPolicy) // withCredentials → cookies flow on CORS
app.UseAuthentication()        // JWT bearer reads the short-lived access token
app.UseAuthorization()
app.UseSwagger() / UseSwaggerUI()   (dev only)
app.MapControllers()
```

Frontend side, per HTTP request (`core/auth/auth.interceptor.ts`, 38 lines):
1. Requests matching `/\/api\/auth\/(dev-sign-in|refresh-token)$/` skip token attachment but get `withCredentials: true` (line 12).
2. All other requests get `Authorization: Bearer <sessionStorage['teamhub.accessToken']>` when present.
3. On `401`: one refresh attempt via `AuthService.refreshSession()`; concurrent 401s are deduped through a `shareReplay({bufferSize:1, refCount:false})` shared observable.
4. **Verified defect**: the replay resends the *original* request (`next.handle(request.clone...)`, line 26) instead of the prepared `authenticatedRequest`, so a `/api/auth/*` call that needed `withCredentials` loses it on replay. Impact and fix options are in doc 14.

---

## 2. Sign-in (current production path: dev passwordless)

The only password-based flow that exists today is the **Development-only** one. In Production (or with Entra enabled) `IsDevelopmentBypassAllowed()` returns false and `dev-sign-in` answers `404` — verified in `TeamHub.Auth/Controllers/AuthController.cs:38-41`. There is **no non-Entra credential endpoint** (full endpoint list in doc 08 §AuthController; `Http*` grep shows only `dev-sign-in`, `dev-users`, `refresh-token`, `logout`, `logout-all`, `me`).

### 2.1 Interactive sign-in trace

```
login.component.ts (user picks an account from the dev-users list)
  → AuthService.loadDevUsers()      GET  /api/auth/dev-users        (auth.service.ts:40)
      AuthController.DevUsers [AllowAnonymous]  → returns 404 outside Development (AuthController.cs:68-80)
  → AuthService.signIn(userId)      POST /api/auth/dev-sign-in { userId }  withCredentials (auth.service.ts:48)
      AuthController.DevSignIn [AllowAnonymous]
        1. IsDevelopmentBypassAllowed()  — else 404
        2. HasTrustedOrigin()            — else 403 (origin/anti-leak check, AuthController.cs:43-46)
        3. AuthService.DevSignInAsync(userId, ip, userAgent)
             — re-resolves the account FROM THE DATABASE (not from the request): the
               comment at AuthController.cs:25-27 states this; session is null → 403
               "account is missing, inactive, or not approved" (lines 53-59)
             — builds claims (roles, permissions) and returns AuthResponse + refresh token
        4. SetRefreshCookie(session.RefreshToken)   cookie "teamhub.refreshToken", 7 days (lines 22-23, 61)
        5. Response.Headers.CacheControl = "no-store"; return Ok(AuthResponse)
  → AuthService stores  sessionStorage: teamhub.accessToken | teamhub.roles | teamhub.permissions
      storeSession() then bumps sessionRevision → sessionState recomputes (auth.service.ts:113-118, :16, :33-44)
  → Router.navigate('/home')  → authenticatedGuard passes
  → MenuService.visibleMenu re-filters APP_MENU against the new codes → topbar renders the nav
    (menu.service.ts:20, nav-menu recursion — UI only, the guards still decide access)
```

Response shape and JWT claim contents are documented in doc 08; they are not re-derived here.

### 2.2 Silent session restore (page reload)

```
app.routes.ts guards run
  → AuthService.isAuthenticated() sees no accessToken but refresh cookie may exist
  → AuthService.refreshSession()   POST /api/auth/refresh-token  {}, withCredentials (auth.service.ts:55)
      AuthController.RefreshToken [AllowAnonymous] (line 84)
        — reads cookie, validates + ROTATES the stored refresh token (docs 08), returns new AuthResponse
        — 401 if the token is missing/revoked → guard redirects to /login
  → tokens re-stored → route activates
```

### 2.3 Sign-out

```
topbar.component.ts sign-out click          (:30-35)
  → AuthService.logout()   POST /api/auth/logout (withCredentials, auth.service.ts:96-99)
      AuthController.Logout → revokes the refresh token server-side, deletes the cookie
  → clearSession() removes the three sessionStorage keys and bumps sessionRevision (:106-111)
      ⇒ the computed sessionState turns invalid → visibleMenu() collapses to the anonymous items
        (Home, Login) before navigation — no stale entry can survive a logout, even if the HTTP call
        fails, because the clear runs in finalize
  → navigate /login
```

### 2.3a Permission refresh (mid-session grant or revoke)

```
Role Permission Matrix save → PUT /api/role-permissions  (doc 08 §9, policy rbac:role-permissions.manage)
  …affected users keep their old claims until the access token expires or is refreshed (≤15 min)
next 401 → authInterceptor → refreshAccessToken() → POST /api/auth/refresh-token
  → AuthRepository.GetRefreshSessionAsync re-queries UserRoles→Roles→RolePermissions→Permissions
    (AuthRepository.cs:156-179, :337-354) → new AuthResponse carries the *current* codes
  → storeSession() overwrites teamhub.permissions + bumps sessionRevision → the navigation re-filters
```

So the menu follows the server's grant set without a re-login, but with the same ≤15-minute lag as the
claims themselves (tracked as `14-known-issues.md` M3).

### 2.4 Entra mode (configured but not wired end to end)

`environment.enableEntra` (default `false`) swaps the HTTP providers in `app.config.ts` to `EntraAuthService` (MSAL popup login, `msal.config.ts`). **Verified gap**: no code bridges an MSAL-authenticated user into the TeamHub session/claims pipeline that `authenticatedGuard` and the `rbacGuard` instances read (doc 11 finding; doc 08 covers the backend Entra stub). Marked *Unknown* why the bridge was never added — cannot establish original intent. **Verified consequence for navigation:** the menu derives from the same `sessionStorage` session (`auth.service.ts:33-44`), so with Entra on it renders only the anonymous items — identical to the guards denying, i.e. no new failure mode.

### 2.5 Navigation rendering (permission-driven recursive menu)

```
APP_MENU (menu.config.ts:50-137)                       ← one frontend constant; no MenuItems table,
                                                          no menu API, no menu CRUD screen
  → MenuService.visibleMenu computed                    (menu.service.ts:20)
      context = { isAuthenticated, hasPermission }      (:25-31)  ← AuthService.sessionState()
  → filterMenu(items, context)                          (menu-filter.ts:50-61)
      per item: visibility gate (:13-22) → permission gate (:24-38) → recurse children (:63-74)
      leaf kept when both gates pass · group kept when ≥1 child survives (empty groups dropped)
      order preserved · fresh copies only, APP_MENU never mutated
  → <app-nav-menu [items]="…" [level]="0">              (topbar.component.html:5)
      group → <button class="nav-item menu-toggle"> + panel containing another <app-nav-menu>
      leaf  → <a [routerLink] routerLinkActive="active">            (nav-menu.component.html:3-28)
      group active state = isBranchActive(item) walking the subtree against Router.url
                                                          (nav-menu.component.ts:41-51)
  → click → Angular Router → route guard re-checks the same code (auth.guard.ts) → API policy
```

Nesting is unlimited: adding a third level is a config edit only, no component change. Full contract
and the four deliberate decisions: `11-frontend.md` §12.1.

---

## 3. Employee directory read (main browsing workflow)

```
EmployeeDirectoryComponent.ngOnInit / search / page change   (employee-directory.component.ts, 173 lines)
  → EmployeeService.getPaged(pageNumber, pageSize, searchTerm, searchField)
       GET /api/employees?pageNumber=&pageSize=&searchTerm=&searchField=      [AllowAnonymous] (EmployeesController.cs:112-129)
    → EmployeesController.GetAll
        → EmployeeService.GetAllAsync            (pass-through, EmployeeService.cs:12-16)
        → EmployeeRepository.GetAllAsync → GetEmployeesAsync (EmployeeRepository.cs:148-253)
            • Single Dapper query, NO transaction (line 207: `using var connection`, `QueryAsync`)
            • SQL (lines 157-205): Employees e
                LEFT JOIN Departments / Designations / Gender / Employees reports /
                       Employees leads / Teams / CotivityEmployees /
                       EmployeeAddresses / EmployeeSkills→Skills / EmployeeImages
              WHERE e.IsActive = 1
                AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                AND search matched over name / designation / department / team (LOWER LIKE)
              ORDER BY (Designation.Rank when department-filtered), first name
            • The three multi-row LEFT JOINs produce a CARTESIAN row explosion per employee;
              dedup happens in C#: rows.GroupBy(row => row.Id) then
              Addresses = group…Distinct(), Skills = group…Distinct() (lines 211-250)
            • RoleName via correlated STRING_AGG subquery over UserRoles→Users→Roles (line 175)
            • ImageBase64 = ToBase64DataUrl(first.ImagePath) — reads the file from disk and
              base64-encodes it per row-group (lines 239-241; ToBase64DataUrl at :946-965)
        → Controller wraps the FULL list in PagedResult<GetGridEmployeeDto>.Create(employees, pageNumber, pageSize)
          — i.e. server-side paging is in-memory slicing of the complete result set (line 127)
  → mapEmployeePage (employee.service.ts:476-494): null photos replaced by an inline SVG avatar
  → component state (employees signal) → DirectoryViewComponent renders cards
```

**Verified consequence**: every directory request scans and materializes *all* active employees (plus their address/skill/image cross products) regardless of `pageSize`; the Angular client additionally slices again. Marked in doc 14 as a performance issue.

Department-filtered variant: `GET /api/employees/department/{departmentId}` (`[AllowAnonymous]`, controller:131-147) → same repository method with `@DepartmentId` set → used by `DepartmentsViewComponent` — which then performs an **N+1** follow-up: one extra `getByDepartment` call per department leader hover (doc 11 §master-settings/directory findings, `departments-view.component.ts:113`).

Organization tree (`/api/employees/tree`, authorized but not role-restricted, controller:149-160): flat `SELECT … WHERE e.IsActive = 1` (repository:23-63), grouped in C# into Department→Designation→Employee nodes.

---

## 4. Hierarchy view (recursive CTE workflow)

```
EmployeeHierarchyComponent (331 lines, signals + afterNextRender)
  → EmployeeHierarchyService.get(employeeId)   GET /api/employees/{id}/hierarchy   [AllowAnonymous] (controller:162-180)
    → EmployeesController.GetHierarchy → mediator.Send(new GetEmployeeHierarchyQuery(employeeId))
        — the ONLY MediatR-mediated read in the codebase (Features/Employees);
          every other controller calls its service directly
    → EmployeeRepository.GetHierarchyAsync (repository:65-107)
        connection.QueryMultiple → STORED PROCEDURE dbo.sp_GetEmployeeOrganizationHierarchy
        (EmployeeHierarchy.sql:53-215), four result sets read in fixed order:
          1. Managers  — recursive CTE walking ReportsToEmployeeId UPWARD, Level negative;
             cycle protection: VisitedPath string + "NOT LIKE '%/'+id+'/%'" guard; MAXRECURSION 32767
          2. Selected employee (single row; null ⇒ controller returns 404)
          3. Cotivity managers — same walk over CotivityEmployees.ReportsToCotivityEmployeeId
          4. Reportees — recursive CTE walking DOWNWARD, Level positive
        Each set attaches the newest photo via OUTER APPLY (SELECT TOP 1 … ORDER BY ImageId DESC)
  → component builds a reporting tree in memory (createReportingTree from flat ParentId rows)
    and draws Bézier connectors with HierarchyConnectorService (96 lines) on resize
```

Supporting indexes created by the same script (`EmployeeHierarchy.sql:1-51`): covering indexes on `Employees.ReportsToEmployeeId`, `CotivityEmployees.ReportsToCotivityEmployeeId`, `EmployeeImages(EmployeeId, ImageId DESC) INCLUDE (ImagePath)`, `CotivityEmployeeImages(…)` — i.e. the recursive walks and newest-image lookups are index-backed.

---

## 5. Employee create

```
EmployeeAddFormComponent.onSubmit  (employee-add-form.ts:274-375, mode = 'create')
  1. form guards → markAllAsTouched → aggregates ALL invalid messages → toast-only feedback (doc 11)
  2. Builds CreateEmployeeRequest — 36 fields (employee.service.ts).
     Addresses: the single address form fields are packed into a ONE-element array with
     latitude:0, longitude:0, isPrimary:true (:335-344).
     The "Login Account" controls (createLoginAccount/username/initialPassword/…) are in the
     FormGroup but NEVER included in the request — verified dead UI (doc 11 finding).
  → EmployeeService.create(request)   POST /api/employees     [Authorize(Policy = RbacPolicies.PeopleCreate)] (controller:60)
      → Manual FluentValidation with rulesets ["default","Create"] via ValidationContext
        (controller:63-67) — model-state auto-validation is NOT relied on.
        Invalid → hand-built ProblemDetails 400 with extensions.errors + joined message (:70-84)
      → EmployeeService.CreateAsync (EmployeeService.cs:29-176)
          a. request → Employee domain entity; CreatedBy = "System", CreatedDate = UtcNow for
             the employee AND every child row (:66-94) — the acting user is never recorded (verified)
          b. repository.CreateAsync — TRANSACTIONAL (EmployeeRepository.cs:450-562):
             connection.Open(); BeginTransaction;
             INSERT Employees … OUTPUT INSERTED.EmployeeId (:408-426),
             then loop INSERT EmployeeAddresses / EmployeeSkills / EmployeeDresses with the new FK;
             Commit; catch → Rollback + rethrow (:557-561)
          c. repository.AssignEmployeeRolesAsync (NOT inside that transaction — separate connection, :878-930):
               resolves account BY EMAIL:
               IF @UserId IS NULL AND NOT EXISTS (Users WHERE Username=@Email)
                 INSERT INTO Users (EmployeeId, Username=@email, Email=@email, IsActive=1)
               then DELETE FROM UserRoles WHERE UserId; INSERT active selected roles
               (WHERE r.IsActive = 1 AND r.RoleId IN @RoleIds)
          d. Image: if request.ImageBase64 present →
               ToFileUrl(FolderName.Infinite_Employee_FOLDER, "employee-{id}-{guid}") writes a file
               under wwwroot (Shared/Common/services extension method), then
               INSERT INTO EmployeeImages — append-only, old rows never removed (:100-115)
          e. Controller returns 201 Created + ApiResponse<int>.Data = EmployeeId (:88-93)
  → Frontend: success toast → router.navigate to directory; list reload re-runs workflow §3
```

Error path: any SQL/exception failure surfaces as the global Problem JSON (doc 10); role-assignment failure after step b leaves the employee row committed but with no/partial roles — no compensating action exists (verified; listed in doc 14).

---

## 6. Employee edit

```
route employees/edit/:id → EmployeeAddFormComponent mode='edit'
  → EmployeeService.getById → GET /api/employees/{id}  [AllowAnonymous] (controller:23-39)
      EmployeeRepository.GetByIdAsync (repository:255-404):
        • Header query: 11 LEFT/OUTER joins incl. OUTER APPLY newest image (:283-288)
        • Addresses: CTE ROW_NUMBER dedup keyed on (AddressTypeId + trimmed-upper address parts),
          keeping IsPrimary DESC, AddressId DESC (:300-322) — duplicate-address scrubbing in SQL
        • Separate queries for Skills, DressSizes, RoleIds (:324-349) — 5 round-trips, no transaction
  → form patch; edit view reads ONLY addresses[0] into the single address block (:221-225)

save → PUT /api/employees/{employeeId}   [Authorize(Policy = RbacPolicies.PeopleUpdate)] (controller:97)
  → Validation difference vs create, verified against Program.cs:90
    (`builder.AddFluentValidationAutoValidation()`): PUT receives FluentValidation **auto-validation
    of the "default" ruleset** (automatic 400 ValidationProblem), but NOT the extra **"Create"
    ruleset** (`JoinedDate ≥ today`, validator line 27-29) that POST invokes manually with
    RulesetValidatorSelector(["default","Create"]) (controller:63-67). So historical join dates
    are editable on update while create blocks past-dated entries — asymmetric by design of the
    ruleset selectors.
  → EmployeeService.UpdateAsync (EmployeeService.cs:178-270)
      a. entity rebuild; LastModifiedBy = "System", LastModifiedDate = UtcNow (:216-217)
      b. repository.UpdateAsync — TRANSACTIONAL (EmployeeRepository.cs:564-734):
           UPDATE Employees SET <34 columns, no WHERE IsActive check> WHERE EmployeeId;
           affected == 0 → Rollback, return false (→ 404) (:673-677)
           DELETE FROM EmployeeAddresses/EmployeeSkills/EmployeeDresses WHERE EmployeeId (:605-607)
           re-INSERT every child row from the request (:684-724)  ← replace-not-merge strategy
      c. on true → AssignEmployeeRolesAsync (same implicit email-provisioning as §5c)
      d. on true + ImageBase64 → new EmployeeImages row (append-only; old image file + row kept;
         the newest ImageId is what reads pick, so the old one is simply shadowed)
  → 204 NoContent → frontend toast + navigate back; detail refetch re-runs this §6 read
```

---

## 7. Resignation (release) workflow

Statuses and allowed edges (backend source of truth, `Application/Workflows/ReleaseStatusWorkflow.cs:11-42`). Seven states; directed graph:

| From | Allowed next |
|---|---|
| Draft | Submitted, Cancelled |
| Submitted | PendingApproval, Approved, Rejected, Cancelled |
| PendingApproval | Approved, Rejected, Cancelled |
| Approved | Released, Cancelled |
| Released / Rejected / Cancelled | (terminal) |

`AllowedInitial = {Draft, Submitted}`; `RequiresClearance` is true only for `Released` (:56). `EnsureTransition` also rejects unknown-current, unknown-target, and same-state moves with dedicated messages, and hints "allowed next: …" or "final state" on illegal edges (:62-88). The frontend `employee-lifecycle-list.ts:38-46` duplicates this graph 1:1 (verified identical today; drift risk noted in doc 11).

**Verified bypass**: `PUT /api/releases/{id}` (§7.1 form edit path) updates `Status` directly in SQL without any `EnsureTransition` call — the service `UpdateAsync` is a repository pass-through (`EmployeeReleaseService.cs:49-53`). Only *create* (AllowedInitial gate) and */status* (graph gate) enforce the machine; a direct PUT with an arbitrary status string (e.g. `Released`) would skip the graph and even fire the `IsActive=0` batch statement, limited only by the DTO regex over the 7 known values (doc 04/05).

### 7.1 Create resignation

```
EmployeeLifecycleFormComponent (370 lines) submit
  → POST /api/releases/employee/{employeeId}   (EmployeeReleasesController.cs:79-99)
    → EmployeeReleaseService.CreateAsync (EmployeeReleaseService.cs:32-47):
        gate: status must be in AllowedInitial {Draft, Submitted} else
              ReleaseWorkflowException → 409 (doc 10 mapping)
    → EmployeeReleaseRepository.CreateAsync (EmployeeReleaseRepository.cs:107-129):
        ONE batch, TWO statements, NO transaction (single connection, no BeginTransaction — verified):
          IF NOT EXISTS (Employees WHERE EmployeeId=…) RETURN;          → controller sees null → 404
          INSERT INTO EmployeeReleases (…) OUTPUT INSERTED.ReleasedId    (:111-121)
          IF @Status='Released' UPDATE Employees SET IsActive=0          (:122-123)
             — unreachable through the service gate for 'Released' (not an AllowedInitial), but
               'Draft'/'Submitted' pass; the Employees deactivation in the same batch therefore
               only fires if the gate is ever bypassed (e.g. a future AllowedInitial change)
        ServedNoticeDays computed server-side = lastWorkingDate − resignationDate in days (:217-219)
        CreatedBy = actorName = "System" (service constant, EmployeeReleaseService.cs:11)
    → GetByIdAsync re-reads via BaseSelect join (employee name/ENumber/dept/designation/manager, :14-24)
  → 201 Created + full EmployeeReleaseResponse
```

### 7.2 Move a status (the state machine in action)

```
EmployeeLifecycleListComponent row action button (only edges from the mirrored graph are offered)
  → POST /api/releases/{releasedId}/status { status }   (controller:117-133)
    → EmployeeReleaseService.TransitionStatusAsync (service:55-78):
        1. current = repository.GetByIdAsync           — null → 404
        2. ReleaseStatusWorkflow.EnsureTransition(current.Status, newStatus)
             illegal edge → ReleaseWorkflowException → 409 with the allowed-next message
        3. if RequiresClearance(newStatus) && !IsFullyCleared(current)
             → ReleaseWorkflowException → 409 "knowledge transfer, asset return, exit
               interview, relieving letter and experience letter must all be completed"
             IsFullyCleared = the 5 clearance booleans are all TRUE (service:86-91)
        4. repository.TransitionStatusAsync (:159-175), again NO transaction:
             UPDATE EmployeeReleases SET Status=@Status,
               ApprovedBy/ApprovedDate stamped when @Status IN ('Approved','Released'),
               LastModifiedBy='System', LastModifiedDate=UtcNow
             IF @Status='Released' UPDATE Employees SET IsActive=0  ← deactivation side effect
  → 200 + re-read response → list refresh (GET /api/releases paged, §7.4)
```

**Verified race**: steps 1–3 read on one connection and step 4 writes on another without a transaction or row lock; a concurrent clearance edit between read and transition is not detected. Also, if the second statement (`IsActive=0`) failed, the status UPDATE would already be committed. Both are in doc 14.

### 7.3 Clearance checklist edit

```
lifecycle form clearance section
  → PUT /api/releases/{releasedId}/clearance   (controller:136-152)
    → service pass-through → repository.UpdateClearanceAsync (:177-206):
        UPDATE the 5 booleans + SettlementAmount = COALESCE(@SettlementAmount, SettlementAmount)
        (null amount keeps the stored value) ; no status change; no transaction (single statement)
```

### 7.4 Listing

`GET /api/releases?page=&pageSize=&search=&department=&status=&releaseType=` → `GetPagedAsync` (:35-87): dynamic WHERE built from non-empty filters; `OFFSET/FETCH`; **COUNT only executed when the page is exactly full** (`if (items.Count == pageSize)`, :71-78) — a short page reports `TotalCount = items.Count`. Same heuristic in terminations (:65-74). Consequences documented in docs 11/14.

### 7.5 Interim succession gate on `Released` — REMOVED

The interim-succession gate (blocking a `Released` transition while the departing employee still had
active direct reports, the handover dialog, the `Successions` audit table and the
`successions.view/add/manage` codes) was removed from backend and frontend on 2026-10-27.
`POST /api/releases/{releasedId}/status` now takes `{ status }` only: `TransitionStatusAsync` runs
the §7.2 graph + clearance gates, updates `EmployeeReleases` and sets `Employees.IsActive = 0` on
`Released`. Repointing an ex-manager's active reports is a normal employee edit afterwards.

---

## 8. Termination workflow

Statuses and edges are the same 7-state shape with `Terminated` replacing `Released` (`TerminationStatusWorkflow.cs:35-43`: `Approved → [Terminated, Cancelled]`, `RequiresClearance(status) => status == Terminated`), and the frontend mirror at `employee-lifecycle-list.ts:82-91` is again 1:1. Verified deltas:

| Aspect | Release | Termination |
|---|---|---|
| Table | `EmployeeReleases` | `EmployeeTerminates` |
| Initial gate | {Draft, Submitted} | {Draft, Submitted} |
| Terminal/deactivation states | `Released` | `Terminated`; the repository SQL **also checks a `"Completed"` value that is not a workflow state** (absent from `TerminationStatusWorkflow.All`, so `EnsureTransition` can never produce it) — create/update batches test `@Status IN ('Completed','Terminated')` (repo :120-121, :144-147) while the transition batch tests only `= 'Terminated'` (:164-166). The `Completed` branch is dead code unless someone PUTs it directly, and such a PUT would trip the DTO regex — so today it is *Verified unreachable*; original intent *Unknown* |
| Clearance gate | 5 booleans | 4 booleans: AssetRecovered, SystemAccessRevoked, ExitInterviewCompleted, TerminationLetterIssued (service:79-83) |
| Approval stamp | `Approved`/`Released` | `Approved`/`Completed`/`Terminated` (Parameters :227-229 — again incl. the unreachable `Completed`) |
| JoinedDate in read | not joined | BaseSelect adds `e.JoinedDate` (:18) → response includes it |
| Exception type | `ReleaseWorkflowException` | **same** `ReleaseWorkflowException` class is reused (no termination-specific type exists — verified) |

Like releases, `PUT /api/terminations/{id}` updates `Status` with no graph check (service pass-through, `EmployeeTerminationService.cs:45-46`).

**Permission split (verified 2026-10-23):** filing the record and driving it through the graph are two
codes — `POST employee/{id}` requires `terminations.add` (`EmployeeTerminationsController.cs:85`) while
`PUT`/`/status`/`/clearance` require `terminations.manage` (`:108, :125, :145`). The frontend matches:
the "New Termination" button and the directory "Terminate" row action check `terminations.add`
(`employee-lifecycle-list.ts:72-75`, `employee-directory.component.ts:78`), the per-edge action menu
checks `terminations.manage` (`:77-80`, `:276-280`). Contrast resignations, where the same code
(`resignations.add`) gates both create and update (`EmployeeReleasesController.cs:157`).

---

## 9. RBAC matrix workflows

### 9.1 Employee→Role matrix

```
Load: EmployeeRoleMatrixComponent (220 lines)
  → rbac.service.ts  GET /api/user-roles/matrix?pageNumber=&pageSize=&search=
      [Authorize(Policy = RbacPolicies.AssignUserRoles)] (UserRolesController.cs:13)
        — policy = authenticated + required permission claim (Authorization/ policies, doc 08)
  → UserRoleMatrixService.GetMatrixAsync → UserRoleRepository.GetMatrixAsync (:11-95), 4 queries:
      active Roles; COUNT of active users on active employees; the user page
      (OFFSET/FETCH, name search); role ids for that page's UserIds (IN @UserIds)
      → assembled into EmployeeRoleMatrixDto { Roles (columns), Rows (userId, employeeId,
        username, fullName, roleIds) , paging }
      → SERVER-side pagination (contrast §3: employees grid pages in memory)

Save: component accumulates checkbox toggles into dirty assignments, Save button
  → PUT /api/user-roles/matrix { assignments:[{userId, roleIds}] }
    → UserRolesController.SaveMatrix passes User.RoleNames() + IsSuperAdmin() (:31)
    → UserRoleMatrixService.SaveAsync (UserRoleMatrixService.cs:15-46):
        empty/null assignments → no-op return
        non-superadmin: EVERY role id in every assignment must (a) exist+active
          else RbacValidationException → 400, (b) be a role the actor currently HOLDS
          else PrivilegeEscalationException → 403. Revokes (empty roleIds) are unchecked.
    → UserRoleRepository.SaveAsync (:97-144): TRANSACTIONAL — BeginTransaction;
        per assignment: user-exists check, role-validity check (count match),
        DELETE all UserRoles for the user, INSERT each role (CreatedBy = actor's first
        role name ?? "System"). Any RbacValidationException → exception escapes BEFORE
        Commit → rollback via `using` disposal.
  → 200 "Role assignments saved successfully."
  → *** Verified UX consequence: the actor's own JWT claims are NOT re-issued; revoked or
      added roles take effect only after the next token refresh/sign-in, while the matrix
      screen shows the new DB state immediately (stale-claims window; doc 14).
```

### 9.2 Role→Permission matrix

Same shape with verified deltas (`RolePermissionMatrixService.cs:15-51`, `RolePermissionsController.cs:13,28-37`): direction is per-permission (`assignments[{permissionId, roleIds}]`); escalation check compares the actor's **permission codes** (`User.PermissionCodes()`), skipping assignments with empty role sets (pure revokes); repository actor string is the literal `"System"` (:50); route policy is `RbacPolicies.ManageRolePermissions`.

---

## 10. Master-data CRUD (one metadata-driven workflow ×15 datasets)

```
topbar menu item (gated by master.<key>) → route master/:masterKey → MasterSettingsComponent (617 lines)
  resolves masterConfigs[masterKey] → e.g. 'departments' → endpoint 'api/departments'
Load    GET  /api/departments        [AllowAnonymous] at action level (DepartmentsController.cs:15-16)
        → service GetAllAsync → repository SELECT (no IsActive filter — inactive rows returned,
          DepartmentRepository.cs:13-24) → controller in-memory PagedResult.Create (:20-27)
        → component holds the full list; its own paging/filtering is client-side
Create  POST /api/departments        class-level [Authorize(Policy = RbacPolicies.MasterDepartment)] (:13)
        → DepartmentsController.Create has NO explicit validator call (contrast Employees §5) —
          validation runs only through FluentValidation's automatic model-state pipeline
          (doc 10: invalid → ValidationProblem 400)
        → DepartmentService.CreateAsync: entity with CreatedBy="System" → INSERT … OUTPUT id
          (single statement, no transaction needed) → 201 CreatedAtAction(GetById)
Update  PUT  /api/departments/{id}   → UPDATE 3 audit/name columns → 204 / 404
Delete  DELETE /api/departments/{id} → SOFT delete: UPDATE Departments SET IsActive=0
          (DepartmentRepository.cs:70-83) — rows are never hard-deleted; referenced ids stay valid
  → component toast + reload
```

Verified notes across the 15 datasets:
- The irregular URL spellings in the frontend config (`cotivitydepartments`, `bloodgroups`, `genders`, …) exactly match each backend `[Route("api/[controller]")]` name; there is no route-normalization layer (doc 11 §master-settings, doc 09).
- `recruitment` has no frontend master config, matching the fact that `RecruitmentsController` exists but is unused by the SPA (verified both sides).
- Uniqueness rules live in validators (doc 05); the repositories do no duplicate checking, so a race between two identical creates surfaces as a raw SQL exception → 500 Problem via `ApiExceptionHandler` unless a unique constraint absorbs it as 400 per doc 10's mapping.

---

## 11. Verified cross-workflow observations

1. **Transaction usage is selective and verified per method**: transactional — `EmployeeRepository.CreateAsync`/`UpdateAsync`, `UserRoleRepository.SaveAsync`, (pattern-equivalent) `RolePermissionRepository.SaveAsync`; NOT transactional despite multi-statement batches — both lifecycle repositories' Create/Update (two statements each, roles-deactivation coupling), release and termination transitions (the release `Released` edge lost its `BeginTransaction` when the succession gate was removed, §7.5), and everything read-side.
2. **POST runs an extra "Create" ruleset; PUT relies on auto-validation of "default" only** (§5 vs §6): the create-only rule is `JoinedDate ≥ today`. Verified via `Program.cs:90 AddFluentValidationAutoValidation()` + `RulesetValidatorSelector` in the POST action. Not a validation *absence* — a deliberate ruleset asymmetry.
3. **Audit columns never record a human**: `CreatedBy`/`LastModifiedBy` are the constants `"System"` (services) or the actor's first role name (UserRoles save). The authenticated user identity is available (claims) but not plumbed into writes.
4. **EmployeeImages is append-only**: every save with a photo INSERTs a new row; all readers choose the newest via `ImageId DESC` (OUTER APPLY / index). No delete path exists anywhere (grep verified, doc 11 §9).
5. **Two independent pagination models**: employees grid = full query + in-memory slice (server *and* client), lifecycle lists = SQL OFFSET/FETCH with count-only-when-full, RBAC matrices = proper server paging with COUNT up front. Behavior of pagination UI therefore differs per screen (matches doc 11 findings).
6. **AllowAnonymous overrides the class-level [Authorize] on EmployeesController** for five reads (controller lines 23-24, 41-42, 112-113, 131-132, 162-163) — plus two more in `DepartmentsController` (`:15-16, :30-31`) — so directory/detail/hierarchy data is publicly readable on the deployed origin (verified attributes; consequence flagged in doc 14 security list). Writes on the same controller are now code-gated (`people.create`/`people.update`), no longer role-gated.
7. **Workflow state machine duplicated frontend/backend** (§7 intro): consistent today, two sources of truth by construction.
8. **MediatR is used for exactly one query** (`GetEmployeeHierarchyQuery`); every other controller→service call is direct DI injection. (Verified by grep across controllers.)
9. **One permission vocabulary, five readers**: the same codes are checked by backend policies (doc 08 §9), by route guards (`auth.guard.ts`), inline in the matrices (`*:53`), by the directory/lifecycle actions (`employee-directory.component.ts:74-78`, `employee-lifecycle-list.ts:48-56, :64-80`) and — since 2026-10-23 — by the navigation config (`menu.config.ts`). Nothing generates them, so a rename must be applied in all five places; `auth.guard.spec.ts` and `menu.service.spec.ts` pin the menu↔guard half of that contract.
