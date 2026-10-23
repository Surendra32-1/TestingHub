# 04 · DTO Reference

`src/TeamHub.Application/DTOs/` holds **59 files in 21 folders**, all `namespace
TeamHub.Application.DTOs.<Folder>` (file-scoped). Shape convention: a `CreateOrUpdate…Request` for
writes and a `Get…Dto` for reads per entity, plus composite response types for the employee page, the
hierarchy chart, the two lifecycle workflows, and the two RBAC matrices.

Response envelopes live outside this folder: `TeamHub.Shared/Responses/` (`ApiResponse<T>`,
`BaseResponse`, `PagedResult<T>`) and `TeamHub.Auth/DTOs/`. They are included here because they are
part of the wire contract; see [10-error-handling.md](10-error-handling.md) for the error envelope.

## Conventions verified in the source

- Reference DTOs are plain `class` with `{ get; set; }` mutable properties; the newest ones
  (`BusinessUnit`, `DressSize`, hierarchy, tree, `EmployeeDressSizesDto`, `AddressTypeDto`,
  `BusinessUnitDto`) are `sealed class`.
- The lifecycle DTOs are `sealed record` with `{ get; init; }` and positional parameters for responses.
- Every non-nullable string is initialized `= string.Empty`; every collection is initialized `= []`
  (C# 12 collection expression), so **no DTO property ever defaults to `null`** unless declared `?`.
- `bool IsActive` is **not** defaulted on most requests (so a missing field deserializes to `false`),
  **except** `CreateOrUpdateBusinessUnitRequest.cs:7`, `CreateOrUpdateDressSizeRequest.cs:9` and
  `CreateOrUpdateEmployeeRequest.cs:42`, which use `= true`. This is a real behavioral difference for a
  POST that omits `isActive` — see [14-known-issues.md](14-known-issues.md).
- `DateOnly`/`DateOnly?` is used for business dates; `DateTimeOffset?` for timestamps in lifecycle
  responses. Dapper/`System.Text.Json` handle both.
- Lookup requests carry **no id**: the route supplies it (`PUT /api/departments/{id}`), which is why
  `CreateOrUpdateDepartmentRequest` has only name + `IsActive`.
- Validation attributes (`[Required]`, `[Range]`, `[RegularExpression]`, `[MaxLength]`,
  `IValidatableObject`) appear **only on the lifecycle request records**; the master-data and employee
  DTOs are attribute-free and rely on FluentValidation
  ([05-validators.md](05-validators.md)). **Verified** — `EmployeeReleaseRequest.cs:1`,
  `EmployeeTerminationRequest.cs:1`, `EmployeeReleaseWorkflowRequests.cs:1`,
  `EmployeeTerminationWorkflowRequests.cs:1` are the only DTO files with
  `using System.ComponentModel.DataAnnotations;` in the DTO tree.

## Master-data request/response pairs

Requests are `CreateOrUpdate<T>Request`, responses `Get<T>Dto`. "PK-only-in-response" applies to every
row below.

| Folder | Request fields | Response fields |
| --- | --- | --- |
| `AddressType` | `AddressTypeName`, `IsActive` | `AddressTypeId` + both |
| `BloodGroup` | `BloodGroupName`, `IsActive` | `BloodGroupId` + both |
| `BusinessUnit` (sealed) | `BusinessUnitName`, `IsActive = true` | `BusinessUnitId` + both |
| `CotivityDepartment` | `CotivityDepartmentName`, `IsActive` | `CotivityDepartmentId` + both |
| `CotivityDesignation` | `CotivityDesignationName`, `CotivityRank`, `IsActive` | `CotivityDesignationId` + all three |
| `CotivityEmployee` | `CotivityEmployeeName`, `CotivityDepartmentId`, `CotivityDepartmentDesignationId`, `int? ReportsToCotivityEmployeeId`, `string? ImageBase64`, `IsActive` | `CotivityEmployeeId`, name, **both ids and both names** (`CotivityDepartmentName`, `CotivityDepartmentDesignationName`), `int? ReportsToCotivityEmployeeId`, `string? ReportsToCotivityEmployeeName`, `IsActive`, `string? ImageBase64` |
| `Department` | `DepartmentName`, `IsActive` | `DepartmentId` + both |
| `Designation` | `DesignationName`, `Rank`, `IsActive` | `DesignationId` + all three |
| `DressSize` (sealed) | `DressName`, `Size`, `IsActive = true` | `DressId` + all three |
| `Gender` | `GenderName`, `IsActive` | `GenderId` + both |
| `JobType` | `JobTypeName`, `IsActive` | `JobTypeId` + both |
| `Permission` | `PermissionCode`, `PermissionName`, `string? Module`, `string? Description`, `IsActive` | `PermissionId` + all five |
| `Recruitment` | `RecruitmentName`, `IsActive` | `RecruitmentId` + both |
| `Role` | `RoleName`, `IsActive` | `RoleId` + both |
| `Skill` | `SkillName`, `IsActive` | `SkillId` + both |
| `Team` | `TeamName`, `IsActive` | `TeamId` + both |

Only `CotivityEmployee` and `Permission` responses join in display names/extra text; every other
response is id + the request's own fields.

## `Employee` folder (13 files)

### `CreateOrUpdateEmployeeRequest` (`Employee\CreateOrUpdateEmployeeRequest.cs`, 46 lines)

All `Employee` scalars except `EmployeeId`, plus three write-only concerns:

```csharp
// Roles to assign to the employee's sign-in account. Stored in the UserRoles junction
// (per user), not on the Employees row - the legacy Employees.RoleId column is removed.
// An empty list clears all roles for the account.
public List<int> RoleIds { get; set; } = [];   // :35-38
public int? CotivityEmployeeId { get; set; }
public string? ImageBase64 { get; set; }       // :41
public bool IsActive { get; set; } = true;     // :42
public IReadOnlyList<EmployeeAddressDto> Addresses { get; set; } = [];      // :43
public IReadOnlyList<EmployeeSkillDto> Skills { get; set; } = [];          // :44
public IReadOnlyList<EmployeeDressSizesDto> DressSizes { get; set; } = []; // :45
```

Field list (lines 5-45): `ENumber`, `FirstName`, `MiddleName?`, `LastName`, `JoinedDate`,
`JoinedDateBS`, `Email`, `AlternateEmail?`, `DesignationId`, `DepartmentId`, `JobTypeId`,
`BloodGroupId`, `GenderId`, `ReportsToEmployeeId?`, `LeadEmployeeId?`, `StationNumber?`,
`RecruitmentId?`, `ExtensionNumber`, `CitizenshipNumber`, `DateOfBirth?`, `DateOfBirthBS?`,
`PhoneNumber?`, `MobileNumber?`, `EmergencyContactNumber?`, `EmergencyContactPerson`, `FatherName`,
`MotherName?`, `SpouseName?`, `PANNumber`, `LockerNumber?`, `RoleIds`, `TeamId`,
`CotivityEmployeeId?`, `ImageBase64?`, `IsActive`, `Addresses`, `Skills`, `DressSizes`.

**Verified:** there is **no `RoleId` property** — only `RoleIds`. Therefore
`tests/TeamHub.UnitTests/Validation/EmployeeRequestValidatorTests.cs:30,68,96,127` assigning
`RoleId = 1` does not compile; the error is currently masked by earlier declaration-phase failures
(see [13-testing.md](13-testing.md)).

**Verified:** the write model has no `CreatedBy`/`LastModifiedBy` — the service derives audit values
from the authenticated username.

### `GetEmployeeDto` (`Employee\GetEmployeeDto.cs`, 55 lines) — full read model

Same scalars as the request **plus denormalized display names joined from the lookups**, all nullable:
`DesignationName?`, `DepartmentName?`, `JobTypeName?`, `BloodGroupName?`, `GenderName?`,
`ReportsToEmployeeName?`, `LeadEmployeeName?`, `RecruitmentName?`, `RoleName?`, `TeamName?`,
`CotivityEmployeeName?`, and `ImageBase64?`; plus `EmployeeId`, `RoleIds` (`List<int>`), `IsActive`,
and the three `IReadOnlyList<…>` child collections (`Addresses`, `Skills`, `DressSizes`).
Nullability of the `*Name` properties is the consequence of `LEFT JOIN`s in the repository.

### `GetGridEmployeeDto` (`Employee\GetGridEmployeeDto.cs`, 27 lines) — list projection

Deliberately flat and narrow for the directory grid:
`Id`, `Name`, `Email`, `Designation?`, `DesignationRank` (`int`, used for department-tree ordering),
`Department?`, `Gender?`, `ReportsToEmployee?`, `LeadEmployee?`, `StationNumber?`, `RecruitmentId?`,
`ExtensionNumber`, `CitizenshipNumber`, `DateOfBirth?`, `PANNumber`, `LockerNumber?`, `RoleName?`,
`TeamName?`, `ReportToCotivityEmployee?`, `ImageBase64?`, and **string projections**
`IReadOnlyList<string> Addresses`, `IReadOnlyList<string> Skills`.

**Verified:** `Addresses`/`Skills` here are already-formatted strings (the repository aggregates them),
not child DTOs — the grid never needs structure. `Id`/`Name` replace `EmployeeId`/`FirstName`+…

### Child-collection DTOs

| Type | File | Fields |
| --- | --- | --- |
| `EmployeeAddressDto` | `EmployeeAddressDto.cs` | `AddressId`, `AddressTypeId`, `AddressTypeName`, `AddressLine1`, `AddressLine2?`, `City`, `State`, `decimal Latitude`, `decimal Longitude`, `IsPrimary` |
| `EmployeeSkillDto` | `EmployeeSkillDto.cs` | `EmployeeSkillId`, `SkillId`, `SkillName`, `SkillLevel`, `IsPrimary` |
| `EmployeeDressSizesDto` (sealed) | `EmployeeDressSizesDto.cs` | `DressId`, `DressName`, `Size`, `IsActive` |

`EmployeeAddressDto` and `EmployeeSkillDto` are **bidirectional** — the same type is used for write
(input lists in `CreateOrUpdateEmployeeRequest`) and read (output lists in `GetEmployeeDto`), so an
incoming payload must include `AddressId`/`EmployeeSkillId` or `0` for a new row. **Verified** from the
property sets; the repository distinguishes them (`07-repositories.md`).

### Hierarchy DTOs (`Employee\EmployeeHierarchyResponse.cs`, 48 lines)

```csharp
public sealed class EmployeeHierarchyResponse
{
    public IReadOnlyList<EmployeeHierarchyNodeDto> EmployeeManagers { get; set; } = [];
    public IReadOnlyList<CotivityHierarchyNodeDto> CotivityHierarchy { get; set; } = [];
    public SelectedEmployeeDto SelectedEmployee { get; set; } = new();
    public IReadOnlyList<EmployeeHierarchyNodeDto> ReportingHierarchy { get; set; } = [];
}
```

Four properties ↔ the four result sets of `sp_GetEmployeeOrganizationHierarchy`, in order
(`EmployeeManagers` ← #1, `SelectedEmployee` ← #2, `CotivityHierarchy` ← #3, `ReportingHierarchy` ← #4).

- `EmployeeHierarchyNodeDto` and `SelectedEmployeeDto` are **structurally identical**
  (`EmployeeId`, `int? ParentId`, `EmployeeName`, `DesignationName?`, `DepartmentName?`, `ImagePath?`,
  `EmployeeNumber`, `Source = "Employee"`, `Level`) — two types, one shape. **Unknown** why they are not
  one type.
- `CotivityHierarchyNodeDto`: `CotivityEmployeeId`, `int? ParentId`, `CotivityEmployeeName`,
  `CotivityDesignationName?`, `CotivityDepartmentName?`, `ImagePath?`, `Source = "Cotivity"`, `Level` —
  matches result set #3 exactly (which has no `EmployeeNumber`), **verified** against
  `EmployeeHierarchy.sql:147-155`.
- `EmployeeNodeDto` (`EmployeeNodeDto.cs:3-7`) is the tree leaf: `int Id`, `string Name`.
- `EmployeeReportingManagerDto` (`:9-13`): `ReportingManagerId`, `ReportingManagerName`.
- `DesignationTreeDto` (`DesignationTreeDto.cs`): `Designation`, `IReadOnlyList<EmployeeNodeDto> Employees`.
- `DepartmentTreeDto` (`DepartmentTreeDto.cs`): `DepartmentName`, `IReadOnlyList<DesignationTreeDto> Designations`.
  Together these two form the department view returned by `GET /api/employees/departments`.

### `EmployeeMastersResponse` (`Employee\EmployeeMasterResponse.cs`)

**Verified namespace quirk:** the file sits in the `Employee\` folder but declares
`namespace TeamHub.Application.DTOs.Employees` (plural) — the only DTO file whose namespace does not
match its folder, and it needs 10 `using …DTOs.<name>;` lines (`:1-10`).

One aggregated bundle for the add/edit form, all `IEnumerable<T> = []`:
`Departments` (`GetDepartmentDto`), `Designations`, `JobTypes`, `BloodGroups`, `Genders`, `Roles`,
`Teams`, `Skills`, `Recruitments`, `Employees` (`EmployeeReportingManagerDto`),
`CotivityEmployees` (`EmployeeReportingManagerDto`), `DressSizes` (`EmployeeDressSizesDto`),
`Businesses` (`BusinessUnitDto`), `AddressTypes` (`AddressTypeDto`).

**Verified:** `EmployeeMasterResponse.cs` is the only DTO file in the Application DTO tree that has
`using` statements other than `System.ComponentModel.DataAnnotations` — i.e. it is the single place
where the per-folder DTO namespaces must be combined.

## Lifecycle DTOs

### Release (`EmployeeRelease\`, 3 files)

`EmployeeReleaseRequest : IValidatableObject` (`:8-90`) — `sealed record`, all `init`:

| Property | Attributes |
| --- | --- |
| `required string ReleaseType` | `[Required]`, `[RegularExpression("^(Resignation|Retirement|Contract End|Internal Transfer|Mutual Separation)$")]` |
| `DateOnly? ResignationDate` | `[Required(ErrorMessage = "Resignation Date is required.")]` |
| `DateOnly? NoticeDate` | — |
| `DateOnly? LastWorkingDate` | `[Required]` |
| `int? NoticePeriodDays` | `[Required(… "so early release can be detected")]`, `[Range(0,180)]` |
| `string? Reason` / `EarlyReleaseReason` | `[MaxLength(500)]` each |
| `string? Remarks` | `[MaxLength(2000)]` |
| `bool? KnowledgeTransferCompleted`, `AssetsReturned`, `ExitInterviewCompleted` | — |
| `decimal? SettlementAmount` | `[Range(typeof(decimal),"0","9999999999999999.99")]` |
| `bool? RelievingLetterIssued`, `ExperienceLetterIssued` | — |
| `string Status` | `[Required]`, `[RegularExpression("^(Draft|Submitted|PendingApproval|Approved|Rejected|Released|Cancelled)$")]`, default `"Draft"` |

`IValidatableObject.Validate` yields three cross-field rules: `LastWorkingDate >= ResignationDate`;
`NoticeDate >= ResignationDate`; and an either-or rule —
`hasReason = !string.IsNullOrWhiteSpace(Reason) && Reason.Trim().Length >= 10`,
`hasEarlyReleaseReason = !string.IsNullOrWhiteSpace(EarlyReleaseReason)`; failing both produces a
message attached to **both** `Reason` and `EarlyReleaseReason`.

The nullable-with-`[Required]` pattern (e.g. `DateOnly? ResignationDate`) is deliberate:
**Verified** it keeps the DTO able to represent "absent" while still producing a required-field error,
and lets the workflow service distinguish "not supplied" from "supplied null" during partial updates.

`EmployeeReleaseResponse` (`:4-35`) is a positional record of 23 parameters —
`ReleasedId, EmployeeId, ReleaseType, ResignationDate?, NoticeDate?, LastWorkingDate?, NoticePeriodDays?,
Reason?, Remarks?, KnowledgeTransferCompleted?, AssetsReturned?, ExitInterviewCompleted?,
SettlementAmount?, RelievingLetterIssued?, ExperienceLetterIssued?, Status, ApprovedBy?, ApprovedDate?,
CreatedBy?, CreatedDate?, LastModifiedBy?, LastModifiedDate?` (timestamps `DateTimeOffset?`) — with six
`init` properties appended: `EmployeeName = string.Empty`, `EmployeeCode`, `Department?`,
`Designation?`, `ManagerName?`, `ServedNoticeDays?`, `EarlyReleaseReason?`.
**Verified:** `ServedNoticeDays` and `EarlyReleaseReason` are *not* positional parameters even though
both are real columns (`databasesesign.sql:426,429`) — they were added later as init properties, which
is why they cannot break existing construction call sites.

Workflow requests (`EmployeeReleaseWorkflowRequests.cs`):
`UpdateReleaseStatusRequest { required string Status }` with the same 7-value status regex; and
`UpdateReleaseClearanceRequest` with 5 `bool?` checklist items + `decimal? SettlementAmount` — its
docblock states "All boolean items must be true before the employee can be moved to 'Released'".

### Termination (`EmployeeTermination\`, 3 files)

`EmployeeTerminationRequest : IValidatableObject` mirrors the release DTO:
`required string TerminationType` regex
`^(Performance|Policy Violation|PolicyViolation|Misconduct|Layoff|Contract End|Attendance|Other|Fraud|Absenteeism|Restructuring)$`
(note **both** `"Policy Violation"` and `"PolicyViolation"` are accepted — a legacy-spelling tolerance,
verified at `:8`), `EffectiveDate?`, `InvestigationRequired?`, `InvestigationSummary?`,
`WarningCount?` `[Range(0, int.MaxValue)]`, `LastWarningDate?`, `Reason?` `[MaxLength(500)]`,
`Remarks?` (no max length), `AssetRecovered?`, `SystemAccessRevoked?`, `ExitInterviewCompleted?`,
`SettlementAmount?` `[Range(…)]`, `EligibleForRehire?`, `TerminationLetterIssued?`,
`TerminationLetterDate?`, and `Status` default `"Draft"` with regex
`^(Draft|Submitted|PendingApproval|Pending Approval|UnderInvestigation|Approved|Completed|Terminated|Rejected|Cancelled)$`.
Its `Validate` yields exactly one rule: `EffectiveDate` may not be later than
`DateOnly.FromDateTime(DateTime.Today)`.

**Verified:** the termination status regex accepts `"Pending Approval"` (with a space) and
`"UnderInvestigation"`/`"Completed"`, which the workflow service does **not** use as transitions — the
DTO is looser than `TerminationStatusWorkflow`. The release regex has no space variant. The frontend
`statusLabel` maps `PendingApproval` → `'Pending Approval'`
(`employee-lifecycle-list.ts`), which is the likely origin of the tolerance.

`EmployeeTerminationResponse` — 24 positional parameters ending with the same four audit timestamp
pairs, plus init properties `EmployeeName`, `EmployeeCode`, `Department?`, `Designation?`,
`ManagerName?`, `JoinedDate?` (needed to compute tenure on the termination form).

`EmployeeTerminationWorkflowRequests.cs` — `UpdateTerminationStatusRequest` uses a **different,
narrower** regex `^(Draft|Submitted|PendingApproval|Approved|Terminated|Rejected|Cancelled)$` than
`EmployeeTerminationRequest.Status`; `UpdateTerminationClearanceRequest` has 4 `bool?` items
(`AssetRecovered`, `SystemAccessRevoked`, `ExitInterviewCompleted`, `TerminationLetterIssued`) +
`SettlementAmount?`.

## RBAC DTOs (`Rbac\RbacDtos.cs`, 98 lines — one file, 9 types)

| Type | Line | Members |
| --- | --- | --- |
| `RoleOptionDto` | 4 | `RoleId`, `RoleName` — "A role column shared by both assignment matrices" |
| `EmployeeRoleMatrixRowDto` | 12 | `UserId`, `EmployeeId`, `Username`, `FullName`, `List<int> RoleIds = []` |
| `EmployeeRoleMatrixDto` | 26 | `Roles`, `Rows`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages` |
| `UserRoleAssignmentDto` | 42 | `UserId`, `List<int> RoleIds` — "Desired role set for a single user in a batch save" |
| `SaveUserRolesRequest` | 50 | `List<UserRoleAssignmentDto> Assignments` |
| `RolePermissionMatrixRowDto` | 56 | `PermissionId`, `PermissionCode`, `PermissionName`, `Module?`, `List<int> RoleIds` |
| `RolePermissionMatrixDto` | 70 | `Roles`, `Rows`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages` |
| `RolePermissionAssignmentDto` | 87 | `PermissionId`, `List<int> RoleIds` |
| `SaveRolePermissionsRequest` | 95 | `List<RolePermissionAssignmentDto> Assignments` |

The matrices are **role-major in one direction and permission-major in the other**: the employee-role
matrix is keyed by `UserId` (rows = users, columns = roles); the role-permission matrix is keyed by
`PermissionId` (rows = permissions, columns = roles). The docblock at `:85-86` states the reason for
keying by permission: *"Keyed by permission (not role) so a page of the matrix can be saved without
knowing the other pages."* Each save request therefore carries a **complete desired set** per row, which
is what makes full-replacement SQL safe under pagination (verified in `07-repositories.md`).

Both matrix DTOs embed paging fields directly rather than reusing `PagedResult<T>` — **Verified**
difference; **Inferred** reason: they must ship the `Roles` column headers alongside the page.

## Shared envelopes (`src/TeamHub.Shared/Responses/`)

```csharp
public abstract class BaseResponse { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class ApiResponse<T> : BaseResponse { public T? Data { get; set; } }

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize) { … }
}
```

`PagedResult.Create` (`PagedResult.cs:19-38`) clamps `pageNumber >= 1` and
**`pageSize` into `1..100`** (`Math.Clamp`), computes `totalPages = Ceiling(total/pageSize)`, then
re-clamps `pageNumber` to `totalPages` when it is out of range (`totalPages == 0 ? 1 :
Min(pageNumber, totalPages)`) and pages the **in-memory** sequence with `Skip/Take`.
**Verified consequence:** a request for `pageSize=500` silently returns 100 items, and any repository
that materializes rows before calling `Create` is paging in memory rather than in SQL.

The success envelope `ApiResponse<T>` is `{ success, message, data }`. Error responses are
`ProblemDetails` whose `extensions` are filled with `success:false`, `message`, `data:null`, `error`,
`errors:{}` by `ApiProblemDetailsFactory.AddResponseFields` — **verified** that this makes error JSON
shape-compatible with the frontend `ApiResponse<T>` interface, which also declares `error`/`errors`.

## Auth DTOs (`src/TeamHub.Auth/DTOs/AuthRequests.cs`)

```csharp
public sealed record DevSignInRequest { [Range(1, int.MaxValue)] public required int UserId { get; init; } }
public sealed record AuthResponse(string AccessToken, int ExpiresIn,
                                  IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
public sealed record CurrentUserResponse(int UserId, int EmployeeId, string Username, string Email,
                                  int DepartmentId, int DesignationId,
                                  IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
```

**Verified security statement in the docblock (`:5-9`):** "roles and permissions are always resolved
from the database on the server and are never taken from the request body" — `DevSignInRequest` accepts
nothing but an id. The refresh token is **not** in `AuthResponse`; it is returned separately inside
`AuthenticatedSession(AuthResponse Response, string RefreshToken)`
(`TeamHub.Auth/Contracts/IAuthService.cs:27`) so the controller can put it in an HttpOnly cookie.

Internal auth models (not sent to clients, `TeamHub.Auth/Models/AuthUser.cs`):
`AuthUser(UserId, EmployeeId, Username, Email, IsActive, IsEmployeeActive, DepartmentId, DesignationId,
Roles, Permissions)` — note both user-level and employee-level active flags;
`DevSignInUser(UserId, Username, DisplayName, Roles)`;
`internal RefreshSession(RefreshTokenId, UserId, TokenHash, ExpiryDate, RevokedDate, LoginHistoryId,
Username, Email, IsActive, IsEmployeeActive, EmployeeId, DepartmentId, DesignationId, Roles,
Permissions)`.
