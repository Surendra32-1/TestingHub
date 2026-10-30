# 09 · API Endpoints

Scope: `src/TeamHub.API/Controllers/**` (21 controllers) plus `AuthController` from
`src/TeamHub.Auth/Controllers/**` — **22 controllers, 116 endpoints total**, every one read in full.
Response envelopes and paging come from `TeamHub.Shared/Responses/**` and
`src/TeamHub.API/Responses/**`, also read in full.

All statements are **Verified** against the cited source unless labelled *Inferred*, *Unknown*, or
*Recommended*.

---

## 1. Conventions shared by every endpoint

### Response envelope

```csharp
// TeamHub.Shared/Responses/BaseResponse.cs:3-8
public abstract class BaseResponse { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
// TeamHub.Shared/Responses/ApiResponse.cs:3-6
public class ApiResponse<T> : BaseResponse { public T? Data { get; set; } }
```

Every successful business endpoint returns **HTTP 200/201 wrapped in `ApiResponse<T>`**:

```json
{ "success": true, "message": "…retrieved successfully.", "data": … }
```

Serialisation is camelCase (default ASP.NET Core `JsonSerializerDefaults.Web`); the C# properties
are PascalCase. **Verified** by reading no `[JsonPropertyName]` attributes anywhere in
`TeamHub.Shared`/`TeamHub.API` and the frontend interfaces in
`TeamHubFrontEnd/src/app/features/employees/services/employee.service.ts:35, 77` which use
`recruitmentId`, `recruitmentName` (camelCase).

Errors **do not** use `ApiResponse`; they use RFC 9110 `ProblemDetails` extended with
`success/message/data/error/errors` — see [10-error-handling.md](10-error-handling.md).
**Verified**: `ApiProblemDetailsFactory.AddResponseFields`
(`src/TeamHub.API/Responses/ApiProblemDetailsFactory.cs:22-31`) is the single place that adds those
extensions, and it is called from the model-state factory (`ApiErrorHandlingExtensions.cs:38`), the
global problem configurator (`:52`), the exception handler (`ApiExceptionHandler.cs:59`), the
`NotFound` helper (`ApiProblemDetailsFactory.cs:18`), and `CotivityEmployeesController.cs:63`.

### `Message` strings are hand-written per action

Each action supplies its own literal (`"Job types retrieved successfully."`,
`"Employee release records retrieved successfully."`, …). There is no localisation and no message
constant — verified by reading all 22 controllers. *Inferred*: the SPA shows these strings via
`toastService.success(...)` using its **own** text instead
(`master-settings.ts:269` builds `"{Singular} created."`), so the server `message` is mostly
ignored by the UI. **Verified** the SPA does not read `response.message` in `master-settings.ts` or
`rbac.service.ts:26` (both use `response.data`).

### Paging is applied **in memory**, not in SQL (most endpoints)

```csharp
// TeamHub.Shared/Responses/PagedResult.cs:19-38
public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
{
    pageNumber = Math.Max(1, pageNumber);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var rows = source as IReadOnlyCollection<T> ?? source.ToArray();
    var totalCount = rows.Count;
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    pageNumber = totalPages == 0 ? 1 : Math.Min(pageNumber, totalPages);
    var offset = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
    return new PagedResult<T> { Items = rows.Skip(offset).Take(pageSize).ToArray(), … };
}
```

Properties: `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, plus computed
`HasPreviousPage => PageNumber > 1` (`:15`) and `HasNextPage => PageNumber < TotalPages` (`:17`).

**Verified consequence:** the 16 `PagedResult`-returning master-data `GET` actions (15 CRUD
controllers + `PermissionsController`) and the 2 `EmployeesController` list actions
(`GetAll`, `GetActiveByDepartmentId`) all call `service.GetAllAsync(ct)` — which loads the **entire
table** — and only then slice it in memory. `pageSize` is clamped to `1..100`, `pageNumber` to
`1..totalPages`, so out-of-range page requests silently return the first/last page with a corrected
`pageNumber` rather than an empty page or a 400.

Only these endpoints page **in the database** (real `OFFSET/FETCH`):
`GET /api/employee-releases/paged` (`EmployeeReleasesController.cs:29-46`),
`GET /api/employee-terminations/paged` (`EmployeeTerminationsController.cs:29-45`),
`GET /api/user-roles/matrix` (`UserRolesController.cs:16-26`),
`GET /api/role-permissions/matrix` (`RolePermissionsController.cs:16-26`) — see
[07-repositories.md](07-repositories.md) for their SQL and the total-count heuristic defect.

### Validation entry point

`Program.cs:90` `builder.Services.AddFluentValidationAutoValidation();` → MVC validates every
`[FromBody]` DTO against its FluentValidation validator using the **`default` ruleset** before the
action body runs; failures are converted by the custom
`InvalidModelStateResponseFactory` (`ApiErrorHandlingExtensions.cs:13-46`) into the extended
ProblemDetails with an `errors` dictionary keyed by DTO field name
(the empty-message fallback is `"The supplied value is invalid."`, `:21`).
DataAnnotations/`IValidatableObject` run in the same ModelState pass, which is how the
release/termination DTOs are validated (see [05-validators.md](05-validators.md)).

`[FromRoute]`/`[FromQuery]` parameters are validated only by their route constraints
(`{id:int}`, `{id:int:min(1)}`). **Verified:** `min(1)` is used on the lifecycle endpoints
(`EmployeeReleasesController.cs:61, :101, :117, :136`;
`EmployeeTerminationsController.cs:60, :100, :116, :135`) but **not** on the master-data or employee
routes (`EmployeesController.cs:23, :41, :97, :131`, `JobTypesController.cs:27-30`, …), so
`GET /api/employees/0` reaches the service and returns 404 rather than a routing miss.
*Inferred*: the lifecycle controllers were written later with the constraint convention.

### Two route styles coexist

| Style | Controllers | Effective route |
| --- | --- | --- |
| Explicit kebab-case | `address-types`, `business-units`, `dress-sizes`, `genders`, `job-types`, `recruitments`, `roles`, `skills`, `teams`, `permissions`, `user-roles`, `role-permissions`, `employee-releases`, `employee-terminations`, `auth` | as written |
| `[Route("api/[controller]")]` token | `BloodGroups`, `CotivityDepartments`, `CotivityDesignations`, `CotivityEmployees`, `Departments`, `Designations`, `Employees` | `api/BloodGroups`, `api/CotivityDepartments`, `api/CotivityDesignations`, `api/CotivityEmployees`, `api/Departments`, `api/Designations`, `api/Employees` |

**Verified:** ASP.NET attribute routing is case-insensitive, which is exactly why the SPA's
lower-case calls work: `master-settings.ts:72, :76, :96, :100, :108, :145` use `departments`,
`designations`, `cotivitydepartments`, `cotivitydesignations`, `cotivityemployees`, `bloodgroups`,
and `employee.service.ts:293` uses `api/employees`. The tokens also mean **no plural-to-kebab
transformation exists** — `api/CotivityEmployees` (one word) is the real route, not
`api/cotivity-employees`; a client that guessed the kebab form would 404. *Recommended*: replace
`[controller]` with explicit kebab strings for one convention.

### Path-traversal / case note on `Create`

`EmployeesController.Create` returns `Created($"…/api/employees/{id}")` with a **hard-coded lower-case
path** (`EmployeesController.cs:88`), while every other controller uses
`CreatedAtAction(nameof(GetById), new { … })` which generates from the route token
(`/api/Employees/{id}` for this controller, `/api/BloodGroups/{id}` for blood groups). **Verified**
inconsistency in the `Location` header casing between the two styles; harmless for HTTP but visible
in Swagger.

---

## 2. Authorization matrix (verified attribute by attribute, re-audited 2026-10-23)

| Controller | Class attribute | Extra action attributes |
| --- | --- | --- |
| `EmployeesController` | `[Authorize] ` (`:18`) | `[AllowAnonymous]` on `GetById :24`, `GetDetails :42`, `GetAll :113`, `GetActiveByDepartmentId :132`, `GetHierarchy :163` · `[Authorize(Policy = RbacPolicies.AdministrationCreate)]` on `Create :60`, `AdministrationUpdate` on `Update :97` |
| `DepartmentsController` | `[Authorize(Policy = RbacPolicies.MasterDepartment)]` (`:13`) | `[AllowAnonymous]` on `GetAll :16`, `GetById :31` |
| `DesignationsController` | `[Authorize(Policy = RbacPolicies.MasterDesignation)]` `:13` | — |
| `BusinessUnitsController` | `[Authorize(Policy = RbacPolicies.MasterBusinessUnit)]` `:13` | — |
| `DressSizesController` | `[Authorize(Policy = RbacPolicies.MasterDressSize)]` `:13` | — |
| `CotivityDepartmentsController` | `[Authorize(Policy = RbacPolicies.MasterCotivityDepartment)]` `:13` | — |
| `CotivityDesignationsController` | `[Authorize(Policy = RbacPolicies.MasterCotivityDesignation)]` `:13` | — |
| `CotivityEmployeesController` | `[Authorize(Policy = RbacPolicies.MasterCotivityEmployee)]` `:13` | — |
| `RolesController` | `[Authorize(Policy = RbacPolicies.MasterRole)]` `:13` | — |
| `AddressTypesController` | `[Authorize(Policy = RbacPolicies.MasterAddressType)]` `:13` | — |
| `BloodGroupsController` | `[Authorize(Policy = RbacPolicies.MasterBloodGroup)]` `:13` | — |
| `SkillsController` | `[Authorize(Policy = RbacPolicies.MasterSkills)]` `:13` | — |
| `JobTypesController` | `[Authorize(Policy = RbacPolicies.MasterJobType)]` `:13` | — |
| `TeamsController` | `[Authorize(Policy = RbacPolicies.MasterTeam)]` `:13` | — |
| `GendersController` | `[Authorize(Policy = RbacPolicies.MasterGender)]` `:13` | — |
| `EmployeeReleasesController` | `[Authorize]` `:14` | `ResignationView` on `:33, :48, :68, :82` · `Create :103` `[Authorize]` + in-code `ResignationAdd` `:110` / `AdministrationRelease` `:129` (self-service vs. HR offboarding) · `ResignationAdd` on `Update :157` · status transitions `:174-190` resolve the per-target policy in code · `ResignationExitChecklist` on `:211` |
| `EmployeeTerminationsController` | `[Authorize]` `:12` | `TerminationView` on `:16, :31, :50, :64` · `TerminationAddOrAdministrationTerminate` on employee-specific `Create :85` · `TerminationManage` on `:108, :125, :145` |
| `PermissionsController` | `[Authorize(Policy = RbacPolicies.ManagePermissions)]` `:13` | — |
| `UserRolesController` | `[Authorize(Policy = RbacPolicies.AssignUserRoles)]` `:13` | — |
| `RolePermissionsController` | `[Authorize(Policy = RbacPolicies.ManageRolePermissions)]` `:13` | — |
| `RecruitmentsController` | `[Authorize(Roles = "Admin")]` `:12` | the only role-only controller left, and the only surface with **no** permission code |
| `AuthController` | none | `[AllowAnonymous]` on `dev-sign-in :29`, `dev-users :67`, `refresh-token :83`; `[Authorize]` on `logout :124`, `logout-all :148`, `me :171` |

Consequences, **verified against `TeamHub.Auth` (see [08-auth-and-rbac.md](08-auth-and-rbac.md) §3, §9)**:

* `[Authorize(Roles = "Admin")]` is satisfied by a `Role` claim equal to `Admin`, because
  `RoleClaimType = "Role"` (`TeamHubAuthServiceCollectionExtensions.cs:73, :102`) and
  `MapInboundClaims = false` (`:61, :88`) — but after the per-module policy rollout **only
  `RecruitmentsController` relies on it**. The other 21 controllers are either code-gated by a
  `PermissionRequirement` policy, plain `[Authorize]`, or anonymous reads.
* Every policy also succeeds for the `Admin` role via the super-admin bypass
  (`PermissionAuthorizationHandler.cs:25-29`), independent of grants — the asymmetry noted against the
  frontend menu in `11-frontend.md` §12.1/§13 finding 16.
* A NormalUser token can now reach, **provided it carries the matching code**: each of the 14 master
  modules, the resignation list/create/update/clearance/transition actions, the termination
  read/add/manage actions, and the three RBAC matrix endpoints. Without any codes it reaches only
  `GET /api/Employees/tree`, `GET /api/Employees/employee-master`, the six anonymous endpoints below,
  and the auth endpoints.
* **Verified security finding:** these 7 endpoints answer **without any token at all**, in every
  environment:

  | Endpoint | Leaks |
  | --- | --- |
  | `GET /api/Employees` (`:113`) | the **entire unpaginated employee grid** — names, emails, phones, designations, departments, teams, role names, and a base64 photo per row (see `07-repositories.md` §`GetEmployeesAsync`) |
  | `GET /api/Employees/{id}` (`:24`) | full employee profile incl. addresses, skills, dress sizes, base64 image |
  | `GET /api/Employees/{id}/details` (`:42`) | **identical** to the above — same `service.GetByIdAsync` call (`:45`) |
  | `GET /api/Employees/department/{id}` (`:132`) | active employees of one department, same DTO |
  | `GET /api/Employees/{id}/hierarchy` (`:163`) | reporting chain for any employee id |
  | `GET /api/departments` (`DepartmentsController.cs:16`) | every department row **including soft-deleted ones** (see `07-repositories.md` filter matrix) |
  | `GET /api/departments/{id}` (`:31`) | one department row |

  No comment or commit-time artefact in the repository explains the choice; the pattern (read-only
  directory + org chart public, writes Admin-only) suggests an intentionally public
  "employee directory" feature. **Unknown** — the reason is not recorded in the source.
  *Recommended*: at minimum require authentication on the grid and detail endpoints; PII plus
  embedded photos in an anonymous response is a data-exposure risk.

* **Verified redundancy:** `GET /api/Employees/{id}` and `GET /api/Employees/{id}/details` are the
  same action body with a different `Message` string (`:36` vs `:54`), and the SPA calls **both**
  (`employee.service.ts:348` and `:352`). Only `/details` is used by the edit form
  (`employee-edit-form`), `/​{id}` by the profile view. *Recommended*: collapse to one.

---

## 3. `EmployeesController` — `api/[controller]` → `/api/Employees` (`:16`)

Constructor dependencies: `IEmployeeService service`, `IValidator<CreateOrUpdateEmployeeRequest>
employeeValidator`, `IMediator mediator` (`:19-21`). This is the **only** place `IMediator` is
injected in the API (`:168`), and the only manual ruleset validation (`:63-67`).

| # | Verb + route | Action | Auth | Request | Response |
| --- | --- | --- | --- | --- | --- |
| 1 | `GET {employeeId:int}` | `GetById` `:25` | **anonymous** | route int | `ApiResponse<GetEmployeeDto>`; 404 ProblemDetails `"Employee not found"` `:29-32` |
| 2 | `GET {employeeId:int}/details` | `GetDetails` `:43` | **anonymous** | route int | same DTO + same service call; message `"Employee details retrieved successfully."` `:54` |
| 3 | `POST` | `Create` `:61` | `Roles="Admin"` | `CreateOrUpdateEmployeeRequest` (body) | `ApiResponse<int>` = new `EmployeeId`; **201** with `Location: /api/employees/{id}` `:88`; manual `default`+`Create` ruleset validation `:63-85` |
| 4 | `PUT {employeeId:int}` | `Update` `:98` | `Roles="Admin"` | `CreateOrUpdateEmployeeRequest` | **204** `NoContent`; 404 when the service returns `false` `:104-109` |
| 5 | `GET` | `GetAll` `:114` | **anonymous** | `?pageNumber=1&pageSize=10&searchTerm=&searchField=all` | `ApiResponse<PagedResult<GetGridEmployeeDto>>`; in-memory slice of the **full** unfiltered-by-page grid `:121-127` |
| 6 | `GET department/{departmentId:int}` | `GetActiveByDepartmentId` `:133` | **anonymous** | route int + `pageNumber`, `pageSize` | `ApiResponse<PagedResult<GetGridEmployeeDto>>` `:139-146` |
| 7 | `GET tree` | `GetOrganizationTree` `:150` | `[Authorize]` (any user) | — | `ApiResponse<IEnumerable<DepartmentTreeDto>>` `:154-159` |
| 8 | `GET {employeeId:int}/hierarchy` | `GetHierarchy` `:164` | **anonymous** | route int | `ApiResponse<EmployeeHierarchyResponse>`; **MediatR** `GetEmployeeHierarchyQuery` `:168`; 404 on `null` `:170-173` |
| 9 | `GET employee-master` | `GetEmployeeMastersAsync` `:183` | `[Authorize]` (any user) | — | `ApiResponse<EmployeeMastersResponse>` — the 14 result sets the employee form uses (see `07-repositories.md`) |

Query-parameter naming is **verified inconsistent**: this action uses `searchTerm` (`:117`), the
lifecycle endpoints use `search` (`EmployeeReleasesController.cs:33`), and the SPA maps its own
control state onto each spelling (`employee.service.ts:324, 385, 402`).

`GET employee-master` is also the **only** endpoint whose action method is named in
`…Async` form (`:183`) — style inconsistency, no functional effect. **Verified.**

---

## 4. Master-data CRUD (15 controllers × 5 endpoints)

`AddressTypes`, `BloodGroups`, `BusinessUnits`, `CotivityDepartments`, `CotivityDesignations`,
`CotivityEmployees`, `Departments`, `Designations`, `DressSizes`, `Genders`, `JobTypes`,
`Recruitments`, `Roles`, `Skills`, `Teams`.

Uniform shape (verified across all 15; only the route-token style, id parameter name, DTO name and
message strings differ):

| Verb + route | Action | Returns |
| --- | --- | --- |
| `GET ?pageNumber=1&pageSize=10` | `GetAll` | `200 ApiResponse<PagedResult<Get…Dto>>` — full table loaded, sliced in memory |
| `GET {id:int}` | `GetById` | `200 ApiResponse<Get…Dto>` \| `404` ProblemDetails from the controller's private `NotFound*`/`P` helper |
| `POST` | `Create` | `201 ApiResponse<Get…Dto>` + `Location` from `CreatedAtAction`; `400` on validation failure |
| `PUT {id:int}` | `Update` | `204 NoContent` \| `404` when the service returns `false` |
| `DELETE {id:int}` | `Delete` | `204 NoContent` \| `404` when the service returns `false` |

Formatting variants **Verified** (cosmetic only): `JobTypes/Recruitments/Roles/Skills/Teams` write
`GetById`/`Create`/`Update`/`Delete` as single-line expression bodies and name the helper `P(int)`;
`AddressTypes/BusinessUnits/DressSizes/Cotivity*` split them over lines and call the helper
`NotFoundProblem`/`NotFoundDetails`; `BloodGroups/Departments/Designations` (the oldest-looking
files) use the long form `CreateNotFoundProblemDetails`, explicit `if (x is null) return NotFound(…)`,
and even omit spaces after commas in signatures (`BloodGroupsController.cs:30, :48`;
`DesignationsController.cs:30, :48`; `DepartmentsController.cs:32, :50`).

Differences that are **not** cosmetic:

* **`CotivityEmployeesController.Update` adds a controller-level business rule** (`:49-75`):
  `if (request.ReportsToCotivityEmployeeId == id)` → hand-built 400 ProblemDetails with
  `Detail = "An employee cannot report to themselves."` and
  `extensions["errors"]["reportsToCotivityEmployeeId"]` (`:54-69`). This is the only self-reference
  guard in the API, it exists **only on PUT** (`Create` at `:37` has no such check), and it does not
  detect cycles longer than one hop. **Verified.** *Recommended*: move into the validator/`IValidatableObject`
  and apply on create too. **Unknown** why only the update path is guarded.
* **`PermissionsController` has a 6th endpoint** — see §5.
* **`DepartmentsController` read side is `[AllowAnonymous]`** while its siblings are not (§2).
* The id route parameter name differs per controller (`addressTypeId`, `bloodGroupId`,
  `businessUnitId`, `cotivityDepartmentId`/`id`, `dressId`, `genderId`, `jobTypeId`,
  `recruitmentId`, `roleId`, `skillId`, `teamId`, `departmentId`, `designationId`, `permissionId`) —
  **Verified** from each `[Http*(…)]` string. It affects only `CreatedAtAction` link generation.

### `RecruitmentsController` has no SPA consumer

**Verified:** no string `api/recruitments` exists anywhere under `TeamHubFrontEnd/src/app`
(searched all `.ts`), and `masterConfigs` in `master-settings.ts:70-164` defines **15** keys that
cover departments, designations, business units, dress sizes, the three Cotivity entities, roles,
permissions, address types, blood groups, skills, job types, teams and genders — recruitment is
absent. The recruitment list the employee form uses comes from
`GET /api/Employees/employee-master` instead (`employee-add-form.ts:246` reads
`data.recruitments`; model at `employee-master.model.ts:42-43, :61`). `RecruitmentsController` is
therefore reachable only by direct API clients (documented in `TeamHub.API/auth.http` / Swagger).
**Unknown** whether it is kept for external consumers or is simply orphaned.

---

## 5. `PermissionsController` — `/api/permissions` (`:12`), policy `rbac:permissions.manage`

| Verb + route | Action | Notes |
| --- | --- | --- |
| `GET ?pageNumber&pageSize` | `GetAll` `:17` | `ApiResponse<PagedResult<GetPermissionDto>>`; the repository's `GetAll` does **not** filter `IsActive` (`07-repositories.md`) so inactive permissions are listed |
| `GET {permissionId:int}` | `GetById` `:30` | filters `IsActive` in SQL |
| `POST` | `Create` `:37` | 201 + `Location` |
| `PUT {permissionId:int}` | `Update` `:44` | 204 \| 404 |
| `PATCH {permissionId:int}/status?isActive={bool}` | `SetStatus` `:48` | **the only `HttpPatch` in the solution**; query-string flag, 204 \| 404 |
| `DELETE {permissionId:int}` | `Delete` `:52` | 204 \| 404 (soft delete) |

`SetStatus` and `Delete` overlap in effect (`DELETE` sets `IsActive = 0`); both call different
service methods (`SetActiveAsync` vs `DeleteAsync`). **Verified**; *Recommended*: keep one.

---

## 6. `EmployeeReleasesController` — `/api/employee-releases` (`:12`), Admin-only

| # | Verb + route | Action | Request | Response / behavior |
| --- | --- | --- | --- | --- |
| 1 | `GET` | `GetAll` `:16` | — | `ApiResponse<IReadOnlyList<EmployeeReleaseResponse>>` — **no paging at all** (`:19-25`) |
| 2 | `GET paged?pageNumber=1&pageSize=5&search=&department=&status=&releaseType=` | `GetPaged` `:30` | 6 query params | `ApiResponse<PagedResult<EmployeeReleaseResponse>>`, filters applied **in SQL** (`:39`) |
| 3 | `GET employee/{employeeId:int}` | `GetByEmployeeId` `:49` | route int | `ApiResponse<IReadOnlyList<…>>` |
| 4 | `GET {releasedId:int:min(1)}` | `GetById` `:62` | route int | 200 \| 404 `"Release record not found"` |
| 5 | `POST employee/{employeeId:int}` | `Create` `:80` | `EmployeeReleaseRequest` | 201 `ApiResponse<EmployeeReleaseResponse>`; `null` → **404 "Employee not found"** `:84-90` (the FK-missing case is reported as a missing employee) |
| 6 | `PUT {releasedId:int:min(1)}` | `Update` `:102` | `EmployeeReleaseRequest` | 204 \| 404. **Verified defect:** the repository writes `Status = @Status` with no workflow check (`EmployeeReleaseRepository.cs:141`), so this endpoint bypasses `ReleaseStatusWorkflow` and deactivates the employee when `Status = "Released"` (`:146-149`) |
| 7 | `POST {releasedId:int:min(1)}/status` | `TransitionStatus` `:118` | `UpdateReleaseStatusRequest { Status }` | 200 with the updated record and message `"Resignation status updated to '{status}'."` `:130`; the **only** workflow-enforcing path (`EmployeeReleaseService.TransitionStatusAsync`); invalid transition → 409 via `ReleaseWorkflowException` |
| 8 | `PUT {releasedId:int:min(1)}/clearance` | `UpdateClearance` `:137` | `UpdateReleaseClearanceRequest` | 200 `ApiResponse<EmployeeReleaseResponse>` \| 404 |

## 7. `EmployeeTerminationsController` — `/api/employee-terminations` (`:12`), code-gated

Structurally identical to §6, verified against `EmployeeTerminationsController.cs`:
`GET` (`:17`, `terminations.view`), `GET paged?pageNumber&pageSize&search&department&status` (`:32` —
**no `releaseType` filter**), `GET employee/{employeeId:int}` (`:51`), `GET {terminationId:int:min(1)}`
(`:65`) — all three still `terminations.view` — `POST employee/{employeeId:int}` (`:86`,
**`administration.terminate` or `terminations.add`**: employee-specific filing accepts either grant, separate from rewriting),
`PUT {terminationId:int:min(1)}` (`:109`, `terminations.manage`, same workflow
bypass — `EmployeeTerminationRepository.cs:139, :144-147`),
`POST {terminationId:int:min(1)}/status` (`:126`), `PUT {terminationId:int:min(1)}/clearance`
(`:146`) — both `terminations.manage`. Messages use "Termination record …" (`:133-134, :153`).

**Verified:** the create-path split is deliberate and mirrored end-to-end — the SPA's
"New Termination" button and its route require `terminations.add`; the employee-specific "Terminate"
action accepts `administration.terminate` or `terminations.add` in both the SPA guard and API policy. The
list's transition menu keeps checking `terminations.manage`
(`employee-lifecycle-list.ts:77-80`). A role granted only `terminations.add` can file a record and
nothing more; `Admin` bypasses all of it via the role claim (`08-auth-and-rbac.md` §3).

**Verified difference:** `UpdateReleaseStatusRequest`/`UpdateTerminationStatusRequest` are validated
by DataAnnotations + `IValidatableObject` only (`05-validators.md`), and the termination status
regex (`EmployeeTerminationRequest.cs:27`) admits `UnderInvestigation`, `Completed`,
`Pending Approval` — three values outside `TerminationStatusWorkflow.All`. POST then fails at the
workflow (409) while PUT accepts them silently via the bypass.

---

## 8. RBAC matrix controllers

| Controller | Route | Verb + route | Action | Notes |
| --- | --- | --- | --- | --- |
| `UserRolesController` | `api/user-roles` `:12` | `GET matrix?pageNumber=1&pageSize=10&search=` | `GetMatrix` `:17` | `ApiResponse<EmployeeRoleMatrixDto>` (repository-paged, `07-repositories.md`) |
| | | `PUT matrix` | `SaveMatrix` `:29` | passes `User.RoleNames()` + `User.IsSuperAdmin()` into the service (`:31`) — the escalation guard reads the **caller's roles**; `Data` is `null`, message `"Role assignments saved successfully."` `:35` |
| `RolePermissionsController` | `api/role-permissions` `:12` | `GET matrix?pageNumber&pageSize&search` | `GetMatrix` `:17` | `ApiResponse<RolePermissionMatrixDto>` |
| | | `PUT matrix` | `SaveMatrix` `:29` | passes `User.PermissionCodes()` + `IsSuperAdmin()` (`:31`) — the guard reads the caller's **permission claims** |
| `PermissionsController` | see §5 | | | |

**Verified:** both `SaveMatrix` actions return **200 with `ApiResponse<object?>` whose `Data` is
never set** (`:32-36`) rather than 204, even though the mutation is complete. The SPA reads only
`response.data` on the GET paths (`rbac.service.ts:26-27, 41-42`) and ignores the save response
body (`rbac.service.ts:31, 45`). *Recommended*: return 204 for consistency with the rest of the API.
The two guards deliberately use **different** caller inputs (roles vs permissions), matching the
different trust models of the two matrices — see `06-application-services.md`.

---

## 9. `AuthController` — `/api/auth`

Six endpoints; full request/response/cookie semantics, the origin gate and the development-bypass
chain are documented in [08-auth-and-rbac.md](08-auth-and-rbac.md) §8. Summary:

| Verb + route | Auth | Success |
| --- | --- | --- |
| `POST dev-sign-in` | `[AllowAnonymous]` + Development-only | 200 `AuthResponse` + refresh cookie |
| `GET dev-users` | `[AllowAnonymous]` + Development-only | 200 `DevSignInUser[]` |
| `POST refresh-token` | `[AllowAnonymous]`, cookie-driven | 200 `AuthResponse` + rotated cookie |
| `POST logout` | `[Authorize]` | 204 |
| `POST logout-all` | `[Authorize]` | 204 |
| `GET me` | `[Authorize]` | 200 `CurrentUserResponse` |

**Verified inconsistency:** the auth endpoints return the **raw DTO** (`AuthResponse`,
`CurrentUserResponse`, `DevSignInUser[]`) with **no `ApiResponse` envelope** — the only endpoints in
the solution that break the envelope convention. Their failures also use a locally built
`ProblemDetails` (`AuthController.cs:230-236`) that carries only `status/title/detail` and **none of
the `success/message/data/error/errors` extensions** — unlike business errors, which always go
through `ApiProblemDetailsFactory`. Exception: a 401/403 from `[Authorize]`/`FallbackPolicy` is
produced by ASP.NET's own problem-details pipeline, which **does** apply
`CustomizeProblemDetails` (`ApiErrorHandlingExtensions.cs:49-53`), so those responses do get the
extensions. *Inferred*: the auth controller was written before/with the envelope convention in mind
but never conformed; **Unknown** why.

---

## 10. Swagger / OpenAPI surface

`Program.cs:93-106` registers `AddEndpointsApiExplorer()` + `AddSwaggerGen` (title "TeamHub API",
version `v1`) and `AddOpenApi()`; `:120-129` maps `/swagger` and the OpenAPI document
**only in Development**. Every action carries `[ProducesResponseType]`? — **Verified: no.** Only
`AuthController` declares them (`:31-33, :69-70, :85-86, :126, :150, :173-174`); the 21 business
controllers rely on the inferred signature types, so the generated document omits concrete error
schemas for them. `TeamHub.API.http` and `auth.http` exist as manual request collections
(see [13-testing.md](13-testing.md)).

---

## 11. Verified findings (endpoints)

1. **7 endpoints are anonymous in every environment**, including the whole-organization employee
   grid with embedded base64 photos. Severity: high. See §2.
2. `GET /api/Employees/{id}` and `…/{id}/details` are duplicate implementations. Severity: low.
3. In-memory paging in 18 list actions loads whole tables. Severity: medium (performance),
   confirmed unbounded for the employee grid (10 joins × per-row file reads).
4. `GET /api/employee-releases` and `/api/employee-terminations` have **no paging variant
   consumed by the SPA?** — verified the SPA calls both the unpaged (`employee.service.ts:366, 436`)
   *and* the paged (`:385, :402`) endpoints; the unpaged ones are used by the employee-profile
   dialogs. Severity: low, informational.
5. Query-parameter naming is inconsistent across the API (`searchTerm` vs `search`;
   `pageSize` default 10 vs 5). Severity: low.
6. `[Route("api/[controller]")]` produces PascalCase, single-word routes for 7 controllers while
   the rest are kebab-case. Severity: low (SPA matches because routing is case-insensitive).
7. `min(1)` route constraint applied on only 8 of 30+ id-bearing routes. Severity: low.
8. Self-report guard exists solely on `PUT /api/CotivityEmployees/{id}`, not on `POST`. Severity:
   medium (a cycle of length 1 can be created on insert; the deeper cycle case is unchecked
   everywhere).
9. `PATCH /api/permissions/{id}/status` duplicates the effect of `DELETE`. Severity: low.
10. RBAC matrix saves return 200 with `data: null` instead of 204. Severity: cosmetic.
11. Auth responses skip the `ApiResponse` envelope and their hand-built problems skip the
    extensions. Severity: medium (clients must special-case the shape).
12. `RecruitmentsController` (5 endpoints) has no SPA consumer. Severity: low (dead surface /
    intentional external API — **Unknown**).
13. `Program.cs:91` registers `ApiExceptionHandler` while `:90` enables FluentValidation
    auto-validation and `:10` of `ApiErrorHandlingExtensions` calls `AddControllers()` a **second**
    time (the first is `TeamHubAuthServiceCollectionExtensions.cs:129`, which also attaches the Auth
    application part). **Verified** duplicate `AddControllers` call; harmless because the
    registrations are idempotent and the application part is added before the second call, but the
    ordering is fragile: if `AddApiErrorHandling` had run *after* a controller-config change it
    would still not remove the part. Severity: cosmetic.

---

Related: [04-dtos.md](04-dtos.md) · [05-validators.md](05-validators.md) ·
[06-application-services.md](06-application-services.md) · [07-repositories.md](07-repositories.md) ·
[08-auth-and-rbac.md](08-auth-and-rbac.md) · [10-error-handling.md](10-error-handling.md) ·
[11-frontend.md](11-frontend.md) · [12-workflows.md](12-workflows.md)
