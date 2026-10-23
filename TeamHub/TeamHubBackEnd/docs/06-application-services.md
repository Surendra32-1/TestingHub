# 06 · Application Services, Workflows and MediatR

Scope: `src/TeamHub.Application/{Services, Workflows, Features, Common, Contracts}`.
Every class and method below was read in full; line citations are from the current files.

## Inventory (verified counts)

| Folder | Files | Types | Note |
| --- | --- | --- | --- |
| `Contracts/Services/` | 21 | 21 interfaces | exactly one interface per file |
| `Contracts/Repositories/` | 21 | 21 interfaces | exactly one interface per file |
| `Services/` | **15** | **21 service classes** | `LookupServices.cs` holds 7 classes |
| `Workflows/` | 3 | 2 static workflow classes + 1 exception | |
| `Features/Employees/` | 2 | 1 MediatR query record + 1 handler | |
| `Common/` | 2 | 2 exception types | |

**21 service interfaces are implemented by 21 classes in 15 files** — the gap is entirely
`Services/LookupServices.cs`, which declares `GenderService`, `AddressTypeService`, `RoleService`,
`TeamService`, `SkillService`, `RecruitmentService`, `JobTypeService` (`:14, :29, :44, :59, :74, :89,
:104`).

### Registration

* Services: `AddScoped<TInterface, TImpl>` × **21**, written directly in
  `src/TeamHub.API/Program.cs:51-66` and `:69-73` — the `AddMediatR(…)` call (`:67-68`) sits *between*
  the two blocks, which is why the list looks split. There is **no** `AddApplication()` extension
  method; `TeamHub.Application` has no `DependencyInjection.cs` (**Verified**: only
  `TeamHub.Infrastructure/DependencyInjection.cs` exists).
* Repositories: `AddScoped` × 21 in `src/TeamHub.Infrastructure/DependencyInjection.cs:33-53`, plus
  `AddSingleton<IDbConnectionFactory>` at `:32`.
* MediatR: `options.RegisterServicesFromAssembly(typeof(GetEmployeeHierarchyQuery).Assembly)`
  (`Program.cs:67-68`).

**Verified design consequence:** services are registered manually and individually, so a new service
requires three edits (interface, class, `Program.cs`). Adding a validator or a handler needs none,
because those two are assembly-scanned.

## Conventions shared by all 21 services

1. `public sealed class XService(IXRepository repository) : IXService` — primary constructor injection,
   no fields, no property injection.
2. Read methods are expression-bodied pass-throughs: the service adds nothing but the interface
   boundary. (`DepartmentService.cs:10-14`, all masters.)
3. `CancellationToken cancellationToken = default` on master-data/RBAC/employee contracts;
   **no default** on `IEmployeeReleaseService`/`IEmployeeTerminationService`
   (`IEmployeeReleaseService.cs:8-22`) — those force controllers to thread the token explicitly.
4. `createdBy`/`lastModifiedBy` are always the literal `"System"`, timestamps always
   `DateTime.UtcNow`. See the audit finding below.
5. No service catches an exception. All DB errors propagate to `ApiExceptionHandler`
   (see [10-error-handling.md](10-error-handling.md)).
6. No service uses `IUnitOfWork`/transactions — transaction scope lives inside repositories
   (`QueryMultipleAsync` + explicit transaction), see [07-repositories.md](07-repositories.md).

## The `"System"` audit actor (verified, project-wide)

Every write path hard-codes the actor:

| Service | Evidence |
| --- | --- |
| All 14 master/lookup/Cotivity services | `CreatedBy = "System"` / `LastModifiedBy = "System"` inline in each method |
| `EmployeeService` | `:67, :79, :87, :93, :112, :217, :229, :237, :243, :264` + `"System"` passed to `AssignEmployeeRolesAsync` (`:98, :250`) |
| `EmployeeReleaseService`, `EmployeeTerminationService` | `private const string ActorName = "System"` (`:11`) |
| `PermissionService` | `:33, :81` |
| `RolePermissionMatrixService` | `repository.SaveAsync(request.Assignments, "System", cancellationToken)` (`:50`) |
| `UserRoleMatrixService` | `actingRoleNames.FirstOrDefault() ?? "System"` (`:45`) |

`AddTeamHubAuth` puts the authenticated user's id/email into claims, and controllers have `User`
available, but **no service receives it** — `CreatedBy`/`LastModifiedBy` therefore never identify who
made a change. **Verified** by reading all 15 service files; **Inferred** reason: the services predate
the auth module (the audit columns and the RBAC layer are separate additions, see
[14-known-issues.md](14-known-issues.md)).

`UserRoleMatrixService:45` is the one place an identity-like value leaks in — and it leaks the wrong
value: the *first role name of the acting admin* is written into the audit `CreatedBy` column of
`UserRoles`, not a user identity. **Verified** from source; **Recommended** to replace with the caller's
user id/email.

## Master-data services (14 classes, one pattern)

`BloodGroupService`, `BusinessUnitService`, `DepartmentService`, `DesignationService`, `DressSizeService`,
`GenderService`, `AddressTypeService`, `RoleService`, `TeamService`, `SkillService`, `RecruitmentService`,
`JobTypeService`, `CotivityDepartmentService`, `CotivityDesignationService`

Shape (from `DepartmentService.cs:10-51`): `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`,
`DeleteAsync`. `CreateAsync` builds the entity, calls `repository.CreateAsync`, then hand-maps the entity
back into the `Get*Dto` — it does **not** re-read the row, so DB-side defaults/computed values never reach
the response. `UpdateAsync`/`DeleteAsync` return `bool` straight from the repository.

Verified differences between them:

| Service | Deviation | Evidence |
| --- | --- | --- |
| `BusinessUnitService` | `.Trim()` on name (create **and** update) | `:20, :38` |
| `DressSizeService` | `.Trim()` on both `DressName` and `Size` | `:20-21, :40-41` |
| all other masters | no `.Trim()` — leading/trailing spaces are stored | `BloodGroupService.cs:20`, `DepartmentService.cs:20`, `LookupServices.cs` (all 7), `CotivityDepartmentService.cs:20`, `CotivityDesignationService.cs:20` |
| `DesignationService`, `CotivityDesignationService` | carry `Rank`/`CotivityRank` through | `:21/:44`, `:21/:43` |
| others | single name field only | |

**Inferred:** `.Trim()` was added where the column has a `UNIQUE` constraint and stray whitespace would
silently create a duplicate-looking row; **Unknown** for the rest, since several non-trimming tables are
also unique-constrained (see [02-database.md](02-database.md)). Documented as an inconsistency, not a rule.

## `CotivityEmployeeService` (`CotivityEmployeeService.cs`, 97 lines)

Adds the image branch that the plain masters lack:

* `CreateAsync` (`:17-58`): insert row → `repository.CreateAsync` returns the new id → if
  `!string.IsNullOrWhiteSpace(request.ImageBase64)`, build
  `$"cotivity-employee-{id}-{Guid.NewGuid():N}"`, call `request.ImageBase64.ToFileUrl(FolderName.Cotivity_Employee_FOLDER, imageFileName)`
  (`:34-35`), and — only when the returned path is non-blank — `repository.CreateImageAsync(new
  CotivityEmployeeImage { … CreatedBy = "System" })` (`:39-45`).
* `UpdateAsync` (`:60-93`): updates first; `if (!updated || string.IsNullOrWhiteSpace(ImageBase64))
  return updated;` (`:74-77`) then writes a **new** image row with a fresh GUID file name.
* **Verified behavior:** images are append-only. The old `CotivityEmployeeImage` row is never deleted and
  the previous file is never removed from disk, which is why the hierarchy read uses
  `OUTER APPLY (SELECT TOP 1 … ORDER BY ImageId DESC)` (`EmployeeHierarchy.sql`). Same pattern for
  `EmployeeService` with `FolderName.Infinite_Employee_FOLDER`.
* **Verified:** a failed image write is silent — `ToFileUrl` returning blank (or throwing inside the
  helper) never rolls back or fails the create/update; the row is saved and the caller sees success.
* `DeleteAsync` (`:95-96`) is a soft delete via the repository (see [02-database.md](02-database.md)).

## `EmployeeService` (`EmployeeService.cs`, 275 lines — the largest class in the solution)

Interface (`Contracts/Services/IEmployeeService.cs:7-21`) — 7 methods:

| Method | Notes |
| --- | --- |
| `GetAllAsync(CancellationToken cancellationToken = default, string? searchTerm = null, string searchField = "all")` | **`CancellationToken` is the first parameter** (`IEmployeeService.cs:9-12`), unlike every other service in the project — callers must pass it positionally or use named arguments. Verified in source; **Unknown** why; **Recommended** to normalise. |
| `GetActiveByDepartmentIdAsync(int departmentId, ct)` | pass-through (`:18-21`) |
| `GetByIdAsync(int employeeId, ct)` | pass-through (`:23-24`) |
| `GetOrganizationTreeAsync(ct)` | pass-through → `DepartmentTreeDto` list (`:26-27`) |
| `CreateAsync(CreateOrUpdateEmployeeRequest, ct)` | `:29-176` |
| `UpdateAsync(int employeeId, CreateOrUpdateEmployeeRequest, ct)` | `:178-270` |
| `GetEmployeeMastersAsync(ct)` | `:271-274` |

### `CreateAsync` — exact sequence

1. Build `Employee` from **33 scalar properties** (`:31-65`), plus `IsActive`, `CreatedDate =
   DateTime.UtcNow`, `CreatedBy = "System"` (`:66-67`).
2. Project `request.Addresses` → `List<EmployeeAddress>` (`:68-80`), `request.Skills` →
   `List<EmployeeSkill>` (`:81-88`), `request.DressSizes` → `List<EmployeeDress>` (`:89-94`) — each child
   gets its own `CreatedDate`/`CreatedBy`.
3. `employee.EmployeeId = await repository.CreateAsync(employee, ct)` (`:97`) — the repository returns the
   identity and persists the three child collections (one round-trip; see
   [07-repositories.md](07-repositories.md)).
4. `await repository.AssignEmployeeRolesAsync(employee.EmployeeId, request.RoleIds, request.Email, "System", ct)`
   (`:98`) — roles are **not** on the `Employees` row; they land in `UserRoles`, matched/created by email
   (that is why `request.Email` is passed).
5. Image: `:100-115`, identical pattern to Cotivity but with `FolderName.Infinite_Employee_FOLDER` and
   file name `employee-{id}-{guid:N}`.
6. Hand-build and return `GetEmployeeDto` (`:117-175`).

Verified properties of step 6 — the returned DTO is a **projection of the request, not of the database**:

* `RoleIds = request.RoleIds` (`:150`) and `ImageBase64 = request.ImageBase64` (`:154`) echo the input
  (the base64 is returned to the client even though only a file path was stored).
* `Addresses` maps `AddressId = address.AddressId` (`:157`) from objects that were just constructed and
  never assigned an id → always `0` in the response. Same for `Skills`' `EmployeeSkillId` (`:169`).
* `DressSizes = request.DressSizes` (`:174`) echoes the request DTO list directly.
* Consequence: the client cannot rely on the create response for generated ids. `EmployeesController.Create`
  ignores the DTO anyway and returns `ApiResponse<int>` with `Data = result.EmployeeId`
  (`Controllers/EmployeesController.cs:88-93`).

### `UpdateAsync` (`:178-270`)

Same mapping as create, but with `LastModifiedDate`/`LastModifiedBy` (`:216-217`) on the parent while the
**children still get `CreatedDate`/`CreatedBy`** (`:228-229, :236-237, :243`) — consistent with
replace-and-reinsert child rows, which the repository performs. Order of effects, verified:

1. `updated = await repository.UpdateAsync(employee, ct)` (`:247`).
2. `if (updated)` → re-assign roles (`:248-251`).
3. `if (updated && ImageBase64 non-blank)` → insert new image row (`:253-267`).
4. `return updated` (`:269`) — the controller maps `false` to 404.

**Verified:** `UpdateAsync` writes child collections unconditionally, so an update payload with empty
`Addresses`/`Skills`/`DressSizes` deletes those child rows. There is no guard, and the `Create`-only
ruleset does not protect this path.

### `GetEmployeeMastersAsync` (`:271-274`)

```csharp
public async Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken = default)
{
  return await repository.GetEmployeeMastersAsync();
}
```

**Verified:** the `cancellationToken` is accepted and **discarded** — the repository call receives none,
so a client disconnect cannot cancel the multi-result-set read. Same file also has no `null` check because
the repository returns a non-nullable aggregate.

## Lifecycle services

`EmployeeReleaseService` (93 lines) and `EmployeeTerminationService` (85 lines) are near-identical, and
each is thin: all SQL and paging lives in the repository.

| Method | Behavior (verified) |
| --- | --- |
| `GetAllAsync` | pass-through, no paging (`:13-14` / `:13-14`) |
| `GetPagedAsync` | **normalises input before the repository call**: `Math.Max(page, 1)`, `Math.Clamp(pageSize, 1, 100)`, and each filter becomes `null` when whitespace-only, else `.Trim()` (`:16-25` / `:16-24`). Termination has no `releaseType` filter. |
| `GetByEmployeeIdAsync`, `GetByIdAsync` | pass-throughs |
| `CreateAsync` | **workflow gate**: unless `AllowedInitial.Contains(request.Status)` throw `ReleaseWorkflowException("A new resignation can only be saved as 'Draft' or 'Submitted'. …")` (`:32-47` / `:31-43`), then delegate to the repository with `ActorName`. Returns nullable (`null` when the employee id does not exist — decided in the repository). |
| `UpdateAsync` | **pure pass-through, no workflow check** (`:49-53` / `:45-46`) — see the finding below |
| `TransitionStatusAsync` | read current → `null` short-circuit → `EnsureTransition(current.Status, newStatus)` → clearance gate → repository transition (`:55-78` / `:48-71`) |
| `UpdateClearanceAsync` | pass-through to repository with `ActorName` (`:80-84` / `:73-77`) |
| `IsFullyCleared` (private) | Release: **5** bits must be `== true` — `KnowledgeTransferCompleted`, `AssetsReturned`, `ExitInterviewCompleted`, `RelievingLetterIssued`, `ExperienceLetterIssued` (`:86-91`). Termination: **4** bits — `AssetRecovered`, `SystemAccessRevoked`, `ExitInterviewCompleted`, `TerminationLetterIssued` (`:79-83`). All are `bool?` compared to `true`, so `null` fails. |

### Verified findings on the lifecycle paths

1. **PUT bypasses the state machine.** `UpdateAsync` performs no `EnsureTransition`, and the repository's
   UPDATE writes `Status = @Status` unconditionally
   (`Infrastructure/EmployeeTerminations/EmployeeTerminationRepository.cs:139`). A client can therefore set
   any status that satisfies the DTO regex — including the terminal `Terminated` — **without** the
   clearance check. Worse, the same batch then deactivates the employee
   (`IF @Status IN ('Completed','Terminated') UPDATE Employees SET IsActive = 0`, `:144-147`), so PUT can
   disable an account **and** skip the checklist in one call. Only the dedicated transition endpoint enforces
   the workflow. **Verified** (service + repository source). **Recommended:** route status changes through
   `TransitionStatusAsync` only, or add `EnsureTransition` to the update path.
   See [14-known-issues.md](14-known-issues.md).
2. **The `EmployeeTerminations` request regex is wider than the workflow.**
   `DTOs/EmployeeTermination/EmployeeTerminationRequest.cs:27` allows
   `Draft|Submitted|PendingApproval|Pending Approval|UnderInvestigation|Approved|Completed|Terminated|Rejected|Cancelled`,
   while `TerminationStatusWorkflow.All` contains only 7 values (`:23-25`). `UnderInvestigation`,
   `Completed` and `Pending Approval` (with a space) pass model validation but are *unknown* to the
   workflow, so they are rejected on POST by `AllowedInitial` with **409** (not 400), and accepted silently
   on PUT (per finding 1). **Verified** by comparing both sources.
3. **`ReleaseWorkflowException` is shared by both lifecycles.** `TerminationStatusWorkflow` throws
   `ReleaseWorkflowException` (`:67, :71, :76, :86`) and `EmployeeTerminationService.CreateAsync` throws it
   too (`:37`). There is no `TerminationWorkflowException`. The message text is termination-specific, so only
   the type name is misleading. **Verified.**
4. **Paging is normalised in three different layers, inconsistently.** Release/termination clamp in the
   *service* (`Math.Max(page, 1)` / `Math.Clamp(pageSize, 1, 100)`, `:19-20` / `:19-20`); the two matrix
   services pass `pageNumber`/`pageSize` through **raw** (`RolePermissionMatrixService.cs:12-13`,
   `UserRoleMatrixService.cs:12-13`) and the clamping happens in the *repository* instead
   (`UserRoleRepository.cs:13-14`, `RolePermissionRepository.cs:13-14`); everything else that needs paging
   relies on `PagedResult.Create` (`TeamHub.Shared/Responses/PagedResult.cs`). **Verified** asymmetry — the
   behaviour is equivalent, the location is not.
5. **A successful transition/deactivation is two auto-committed statements, not one transaction.**
   `TransitionStatusAsync`'s SQL batch ends with
   `IF @Status = 'Released' UPDATE Employees SET IsActive = 0 …` (`EmployeeReleaseRepository.cs:166-168`),
   and the service simply awaits it (`:77`). See [07-repositories.md](07-repositories.md).

## Workflow state machines

`Workflows/ReleaseStatusWorkflow.cs` (89 lines) and `Workflows/TerminationStatusWorkflow.cs` (90 lines) are
static classes; the class doc comment states the intent
(`ReleaseStatusWorkflow.cs:3-9`): *"The persisted `Status` string is the single source of truth shared by the
API validation regex, the database and the UI. … the workflow cannot be bypassed by sending an arbitrary
status."* (**Verified** quote; the bypass claim is contradicted by lifecycle finding 1 above.)

| Member | Definition |
| --- | --- |
| 7 `public const string` | Release `Draft, Submitted, PendingApproval, Approved, Released, Rejected, Cancelled` (`:12-18`); Termination identical except `Terminated` replaces `Released` (`:13-19`) |
| `All` | `HashSet<string>(StringComparer.Ordinal)` of the 7 (`:21-24` / `:22-25`) |
| `AllowedInitial` | `{ Draft, Submitted }` (`:27-30` / `:28-31`) |
| `Graph` (private) | `[Draft] = [Submitted, Cancelled]`, `[Submitted] = [PendingApproval, Approved, Rejected, Cancelled]`, `[PendingApproval] = [Approved, Rejected, Cancelled]`, `[Approved] = [Released\|Terminated, Cancelled]`, `[Released/Terminated] = []`, `[Rejected] = []`, `[Cancelled] = []` (`:33-42` / `:34-43`) |
| `IsKnown`, `IsTerminal`, `NextStates`, `CanTransition` | set/dictionary lookups; `IsTerminal` = graph entry with empty next list (`:46-54` / `:47-55`) |
| `RequiresClearance(status)` | `status == Released` (`:56`) / `status == Terminated` (`:57`) — exactly one gated state each |
| `EnsureTransition(from, to)` | 4 guards in order (`:62-88` / `:63-89`) |

`EnsureTransition` messages, verbatim from source:

| Guard | Message |
| --- | --- |
| unknown current | `Unknown current resignation status '{from}'.` / `Unknown current termination status '{from}'.` |
| unknown target | `Unknown target resignation status '{to}'.` / `…termination…` |
| `from == to` (`StringComparison.Ordinal`) | `The resignation is already in status '{to}'.` / `The termination is already in status '{to}'.` |
| not in graph | `Cannot move a resignation from '{from}' to '{to}' - {hint}.` where `hint` is `"it is a final state and can no longer change"` when there are no next states, else `"allowed next: A, B"` |

**Verified observations:** `Rejected` and `Cancelled` are terminal with no re-entry, and `Graph` uses
`StringComparer.Ordinal` everywhere, so `pendingapproval` ≠ `PendingApproval`. The graph never allows a
transition *out of* `Released`/`Terminated`, matching the DB-side soft-delete independence.

`Workflows/ReleaseWorkflowException.cs:8` — `public sealed class ReleaseWorkflowException(string message) :
Exception(message);` with a doc comment stating it is "Mapped to HTTP 409 Conflict by the API exception
handler" — **Verified** in `API/Responses/ApiExceptionHandler.cs`.

## RBAC services

### `PermissionService` (`PermissionService.cs`, 95 lines)

The only master-data-like service with server-side uniqueness pre-checks:

* `CreateAsync` (`:17-37`): `code = request.PermissionCode.Trim()` → `repository.CodeExistsAsync(code,
  null, ct)` → `throw new RbacValidationException($"Permission code '{code}' already exists.")` → build
  entity, `Module`/`Description` collapse to `null` when whitespace-only (`:29-30`) → insert → `ToDto`.
* `UpdateAsync` (`:39-59`): same, with `CodeExistsAsync(code, permissionId, ct)` — the id argument excludes
  the row being updated, so a self-referential save is allowed.
* `SetActiveAsync` (`:64-84`): **re-reads** the row (`GetByIdAsync`), returns `false` when absent, then
  rebuilds the whole entity with `IsActive = isActive` and calls `UpdateAsync`. This is read-modify-write
  with no transaction, so a concurrent edit to another column can be lost. **Verified** from source;
  **Inferred** that the risk is acceptable because permission rows are edited only by admins.
* `DeleteAsync` (`:61-62`) pass-through; `ToDto` (`:86-94`) is the only private mapper in the folder besides
  the two lifecycle `Map`-style helpers.

### `UserRoleMatrixService` (`:8-46`) and `RolePermissionMatrixService` (`:8-51`)

Both take two repositories and implement the same **self-escalation guard**:

```csharp
if (request.Assignments is null || request.Assignments.Count == 0) return;   // no-op save
if (!isSuperAdmin)
{
    var codesById = (await permissionRepository.GetAllAsync(ct))
        .ToDictionary(p => p.PermissionId, p => p.PermissionCode);           // or rolesById by RoleId→RoleName
    var held = new HashSet<string>(actingPermissionCodes, StringComparer.OrdinalIgnoreCase);
    foreach (var assignment in request.Assignments)
    {
        if (assignment.RoleIds.Count == 0) continue;                          // revokes are free
        if (!codesById.TryGetValue(assignment.PermissionId, out var code))
            throw new RbacValidationException($"Permission id {assignment.PermissionId} does not exist or is inactive.");
        if (!held.Contains(code))
            throw new PrivilegeEscalationException(
                $"You cannot grant the permission '{code}' because you do not currently hold it.");
    }
}
await repository.SaveAsync(request.Assignments, /* actor */, ct);
```

Verified details and differences:

| | `RolePermissionMatrixService` | `UserRoleMatrixService` |
| --- | --- | --- |
| Guard set | per assignment, skips `RoleIds.Count == 0` (`:32-35`) | flattens `SelectMany(RoleIds).Distinct()` first (`:29-30`) |
| Unknown id | `Permission id {id} does not exist or is inactive.` (`:39`) | `Role id {roleId} does not exist or is inactive.` (`:34`) |
| Escalation | `You cannot grant the permission '{code}' …` (`:44-45`) | `You cannot grant the role '{roleName}' …` (`:39-40`) |
| Comparison | `OrdinalIgnoreCase` on **permission codes** | `OrdinalIgnoreCase` on **role names** |
| Actor written | literal `"System"` (`:50`) | `actingRoleNames.FirstOrDefault() ?? "System"` (`:45`) |

**Verified semantics:** the guard is *grant-only* — revoking is always allowed, and a non-empty role set is
treated as "(re)granting", which the in-code comment states (`:28-29`: *"Assignments are keyed by permission;
a non-empty role set means the permission is being (re)granted, so the acting admin must already hold it.
Revokes are free."*). **Consequence:** because assignments are keyed per permission, granting a permission to
a role *including* the acting admin's own role is the practical escalation route only if the admin lacks the
code — which the check forbids. It also means an admin **cannot grant `Admin`-only permissions at all**
unless they already hold them, so delegation is limited to what the caller already has. **Inferred** intent.

Callers: `API/Controllers/RolePermissionsController.cs:31`
`service.SaveAsync(request, User.PermissionCodes(), User.IsSuperAdmin(), ct)` and
`API/Controllers/UserRolesController.cs:31` `service.SaveAsync(request, User.RoleNames(), User.IsSuperAdmin(), ct)`
— the three `User.*` extension methods come from `TeamHub.Auth/Authorization`
(see [08-auth-and-rbac.md](08-auth-and-rbac.md)). Both endpoints are protected by
`[Authorize(Policy = RbacPolicies.ManageRolePermissions)]` / `AssignUserRoles`
(`:13` each), so the policy check and the data check are separate layers.

## `Common/` exception types

* `PrivilegeEscalationException.cs:7` — `sealed class … : Exception(message)`, doc comment: "Thrown when an
  administrator attempts to grant a role or permission they do not themselves hold. Mapped to HTTP 403
  Forbidden by the API exception handler."
* `RbacValidationException.cs:8` — "…references invalid or inactive identifiers, or otherwise fails
  server-side validation. Mapped to HTTP 400 Bad Request…"

Both are parameter-cctor exceptions with no extra properties, no error codes and no `Data` payload — the
message string is the entire contract, and it is surfaced verbatim to the client through ProblemDetails
`detail`/`message`. **Verified.**

## MediatR feature (the only one)

* `Features/Employees/GetEmployeeHierarchyQuery.cs:6` —
  `public sealed record GetEmployeeHierarchyQuery(int EmployeeId) : IRequest<EmployeeHierarchyResponse?>;`
* `GetEmployeeHierarchyQueryHandler.cs:7-13` — handler takes `IEmployeeRepository` and its `Handle` is a
  single pass-through to `repository.GetHierarchyAsync(request.EmployeeId, cancellationToken)`.

**Verified:** `IMediator` appears in exactly one controller (`EmployeesController.cs:21, :168`) and this is
the **only** query/handler pair in the solution (`MediatR` 12.4.1 is a package dependency of
`TeamHub.Application`). The same result is reachable without MediatR (`IEmployeeService` has no hierarchy
method, so the mediator is genuinely the only route to `GetHierarchyAsync`). **Inferred:** the CQRS wrapper
was introduced for this one read (probably as a pattern to extend later); **Recommended:** either adopt
handlers for other reads or drop the package — see [14-known-issues.md](14-known-issues.md).

## Dependency direction (verified)

```
TeamHub.API controllers
   └─> Contracts/Services (interfaces)            ← Program.cs wires concrete services
         └─> Services/*.cs (this layer)
               ├─> DTOs/* (input + output records)
               ├─> Domain/Entities (built inline; never returned outward)
               ├─> Workflows/* (transition rules)
               ├─> Common/* (RBAC exceptions)
               ├─> TeamHub.Shared/Common/Services (ToFileUrl, FolderName)
               └─> Contracts/Repositories (interfaces) → implemented in Infrastructure
```

`TeamHub.Application` references no ASP.NET type inside `Services/` — the only web-facing knowledge is the
exception→status mapping, which lives in the API project. **Verified** by reading all 15 service files: the
`using` sets are limited to `TeamHub.Application.*`, `TeamHub.Domain.Entities`, `TeamHub.Shared.*`.

## Module summary

| Concern | Owned by |
| --- | ---|
| Field shape | Validators (doc 05) + DataAnnotations |
| Entity construction + audit columns | Services (this doc) |
| Multi-table writes, transactions, paging SQL | Repositories (doc 07) |
| Status legality | `ReleaseStatusWorkflow` / `TerminationStatusWorkflow` |
| Grant legality / self-escalation | The two matrix services |
| Exception → HTTP status | `ApiExceptionHandler` (doc 10) |
