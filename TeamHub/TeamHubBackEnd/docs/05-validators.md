# 05 · Validators

`src/TeamHub.Application/Validators/` holds **20 files declaring 21 FluentValidation classes**
(18 folders; `Employee/` has 3 files, `Rbac/` has 1 file with 2 classes). All are `sealed class … :
AbstractValidator<T>` with every rule built in the constructor, and every message is explicit
(`.WithMessage(...)`) so the API returns Nepali-market-friendly plain English rather than
FluentValidation's default text.

Packages: `FluentValidation` 11.11.0 + `FluentValidation.DependencyInjectionExtensions` 11.11.0
(`src/TeamHub.Application/TeamHub.Application.csproj`).

## How validators are registered and invoked

1. **Registration** — `src/TeamHub.API/Program.cs:74-89` calls
   `AddValidatorsFromAssemblyContaining<T>` **16 separate times** (Department, BloodGroup, Designation,
   Gender, AddressType, Role, Team, Skill, Recruitment, JobType, CotivityDepartment,
   CotivityDesignation, CotivityEmployee, Employee, BusinessUnit, DressSize).
   **Verified behavior:** each call registers *every* validator in the containing
   (`TeamHub.Application`) assembly, so 16 calls register the same 21 classes 16 times over — the list
   is redundant, and it also explains why `CreateOrUpdatePermissionRequestValidator` and the two RBAC
   batch validators work even though they are never named in `Program.cs`.
2. **Automatic path** — `AddFluentValidationAutoValidation()` (`Program.cs:90`) makes MVC validate
   every `[FromBody]` DTO on model binding, using the **`default` ruleset only**. This covers all
   master-data and `CotivityEmployee` controllers.
3. **Manual path with a ruleset** — `EmployeesController.Create` injects
   `IValidator<CreateOrUpdateEmployeeRequest>` (`Controllers/EmployeesController.cs:20`) and validates by
   hand so that the `"Create"` ruleset is included:
   `new RulesetValidatorSelector(new[] { "default", "Create" })` (`:63-67`). On failure it builds the
   same ProblemDetails envelope itself (`:70-84`) instead of relying on the MVC factory. That is the
   only ruleset in the codebase (**Verified**: `RuleSet(` appears exactly once, in
   `Employee/CreateOrUpdateEmployeeRequestValidator.cs:27`). Because auto-validation would only run
   `default`, the controller performs the extra check — see [09-api-endpoints.md](09-api-endpoints.md).
4. **Lifecycle DTOs are not FluentValidation-validated.** There is no validator for
   `EmployeeReleaseRequest`/`EmployeeTerminationRequest`; their `[Required]`/`[Range]`/
   `[RegularExpression]` attributes and `IValidatableObject.Validate` are enforced by MVC's own model
   validation, and the results arrive through the same `InvalidModelStateResponseFactory` ProblemDetails
   path (`src/TeamHub.API/Responses/ApiErrorHandlingExtensions.cs:10-46`).

## Style shared by the master-data validators

Two recurring constructions:

```csharp
RuleFor(request => request.XyzName)
    .Cascade(CascadeMode.Stop)                                             // stop after first failure
    .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("… is required.")
    .MinimumLength(2).WithMessage("… must be at least 2 characters.")       // present on some only
    .MaximumLength(100).WithMessage("… must be 100 characters or fewer.");
```

- **`Must(!string.IsNullOrWhiteSpace(...))` instead of `.NotEmpty()`** everywhere a "required" string is
  checked — this rejects whitespace-only input, which `.NotEmpty()` would accept. **Verified** in all
  18 name-style validators. `.NotEmpty()` is used only in the employee validator
  (`CreateOrUpdateEmployeeRequestValidator.cs:25,32,70,73`).
- **`CascadeMode.Stop` is per-rule, not global** (`AbstractValidator` default is
  `CascadeMode.StopOnFirstFailure` for a single rule chain anyway; the explicit call documents intent).
  **Inferred:** written explicitly so the "required" error is the only one the UI shows for an empty name.
- `IsActive` has **no validation rule anywhere** — it is a plain `bool` accepted as sent.

## Every master-data validator (verified rule-by-rule)

| Validator | File | Rules |
| --- | --- | --- |
| `CreateOrUpdateAddressTypeRequestValidator` | `AddressType/…:10-13` | `AddressTypeName`: required, ≤100 |
| `CreateOrUpdateBloodGroupRequestValidator` | `BloodGroup/…:11-14` | `BloodGroupName`: required, **≤5** (mirrors `VARCHAR(5)`) |
| `CreateOrUpdateBusinessUnitRequestValidator` | `BusinessUnit/…:10-13` | `BusinessUnitName`: required, ≤150 (mirrors `VARCHAR(150)`) |
| `CreateOrUpdateCotivityDepartmentRequestValidator` | `CotivityDepartment/…:10-14` | `CotivityDepartmentName`: required, **≥2**, ≤100 |
| `CreateOrUpdateCotivityDesignationRequestValidator` | `CotivityDesignation/…:10-17` | name required, ≥2, ≤100; `CotivityRank ≥ 0` |
| `CreateOrUpdateCotivityEmployeeRequestValidator` | `CotivityEmployee/…:10-25` | `CotivityEmployeeName` required, ≥2, ≤150; `CotivityDepartmentId > 0`; `CotivityDepartmentDesignationId > 0`; `ReportsToCotivityEmployeeId > 0` **`.When(…HasValue)`** |
| `CreateOrUpdateDepartmentRequestValidator` | `Department/…:11-15` | `DepartmentName` required, ≥2, ≤100 |
| `CreateOrUpdateDesignationRequestValidator` | `Designation/…:11-18` | `DesignationName` required, ≥2, ≤100; `Rank ≥ 0` |
| `CreateOrUpdateDressSizeRequestValidator` | `DressSize/…:10-18` | `DressName` required, ≤100; `Size` required, ≤10 — two independent `CascadeMode.Stop` chains |
| `CreateOrUpdateGenderRequestValidator` | `Gender/…:10-13` | `GenderName` required, ≤50 |
| `CreateOrUpdateJobTypeRequestValidator` | `JobType/…:10-13` | `JobTypeName` required, ≤100 |
| `CreateOrUpdatePermissionRequestValidator` | `Permission/…:10-25` | `PermissionCode` required, ≤100, `.Matches(@"^[A-Za-z0-9._:-]+$")`; `PermissionName` required, ≤150; `Module?` ≤100; `Description?` ≤500 |
| `CreateOrUpdateRecruitmentRequestValidator` | `Recruitment/…:10-13` | `RecruitmentName` required, ≤100 |
| `CreateOrUpdateRoleRequestValidator` | `Role/…:10-13` | `RoleName` required, ≤100 |
| `CreateOrUpdateSkillRequestValidator` | `Skill/…:10-13` | `SkillName` required, ≤100 |
| `CreateOrUpdateTeamRequestValidator` | `Team/…:10-13` | `TeamName` required, ≤100 |

**Verified:** max-lengths equal the SQL column widths in `databasesesign.sql`, so a value that passes
validation also fits the column. The `≥2` minimum exists on `Department`, `Designation`,
`CotivityDepartment`, `CotivityDesignation`, `CotivityEmployee` but **not** on `AddressType`,
`BloodGroup`, `BusinessUnit`, `Gender`, `JobType`, `Permission`, `Recruitment`, `Role`, `Skill`, `Team`,
`DressSize` — an inconsistency, not a rule. **Unknown** reason.

`PermissionCode`'s character whitelist is the only regex in a master validator, and it exists so codes
stay usable as policy names (`rbac:permissions.manage` style) — the three seeded RBAC codes contain
`.`, `-` and `:`.

## `CreateOrUpdateEmployeeRequestValidator` (106 lines, the largest)

Default-set rules, in source order (`Employee/CreateOrUpdateEmployeeRequestValidator.cs`):

| Lines | Property | Rules |
| --- | --- | --- |
| 10-11 | `ENumber` | `> 0` |
| 12-15 | `FirstName` | required, ≤100 |
| 16-17 | `MiddleName` | ≤100 (no required check — nullable) |
| 18-21 | `LastName` | required, ≤100 |
| 22-23 | `JoinedDate` | `.NotEqual(default(DateOnly))` — i.e. "a `DateOnly` must really be supplied", since the non-nullable struct otherwise silently validates as `0001-01-01` |
| 24-26 | `JoinedDateBS` | `.NotEmpty()`, `.Matches(@"^\d{4}-\d{2}-\d{2}$")` |
| **27-29** | `JoinedDate` | **`RuleSet("Create", …)`**: `.GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))` — "Date of joining must be today or later" |
| 30-34 | `Email` | `.NotEmpty()`, `.EmailAddress()`, ≤150 |
| 35-38 | `AlternateEmail` | `.EmailAddress()`, ≤150, `.When(!IsNullOrWhiteSpace)` |
| 39-43 | `DesignationId`, `DepartmentId`, `JobTypeId`, `BloodGroupId`, `GenderId` | each `> 0` |
| 44-52 | `ReportsToEmployeeId`, `LeadEmployeeId`, `RecruitmentId` | each `> 0` `.When(…HasValue)` |
| 53-54 | `ExtensionNumber` | `≥ 0` |
| 55-58 | `CitizenshipNumber` | required, ≤50 |
| 59-62 | `DateOfBirth` | `.LessThan(today)` `.When(…HasValue)` |
| 63-65 | `DateOfBirthBS` | `YYYY-MM-DD` regex `.When(!IsNullOrWhiteSpace)` |
| 66-68 | `PhoneNumber`, `MobileNumber`, `EmergencyContactNumber` | ≤20 each, **no message override** |
| 69-71 | `EmergencyContactPerson` | `.NotEmpty()`, ≤150 |
| 72-74 | `FatherName` | `.NotEmpty()`, ≤150 |
| 75-76 | `MotherName`, `SpouseName` | ≤150 each |
| 77-80 | `PANNumber` | required, ≤50 |
| 81-84 | `StationNumber`, `LockerNumber` | ≤20 each |
| 85-86 | `RoleIds` | `RuleForEach(…).GreaterThan(0)` |
| 87 | `TeamId` | `> 0` |
| 88-90 | `CotivityEmployeeId` | `> 0` `.When(…HasValue)` |
| 91-92 | `Addresses`, `Skills` | `RuleForEach(…).SetValidator(new EmployeeAddressDtoValidator() / new EmployeeSkillDtoValidator())` — child validators instantiated inline in the constructor |
| 93-98 | `DressSizes` | `RuleForEach(…).ChildRules(dress => dress.RuleFor(item => item.DressId).GreaterThan(0))`; then "A dress size can only be selected once" (distinct `DressId`s) |
| 99-104 | `Addresses`, `Skills` | at most one `IsPrimary` in each collection |

Verified observations about this validator:

- **The `"Create"` ruleset inverts the usual meaning.** Joining date must be **today or later** on
  create, while the default set (used by PUT) omits that rule so historic records remain editable.
  **Verified** the rule text; **Inferred** the reason (editing an old employee must not fail).
- **No rule ties `JoinedDate` to `JoinedDateBS`**, and no rule validates that the BS string is a real
  Nepali date — it is stored as free text `VARCHAR(10)`. The BS↔AD conversion happens only in the
  Angular `NepaliDatePickerComponent` ([11-frontend.md](11-frontend.md)).
- **No self-report or cycle guard.** `ReportsToEmployeeId` is only checked `> 0`. There is no rule,
  service check, or DB constraint preventing `ReportsToEmployeeId = the employee's own id` or a manager
  cycle on `dbo.Employees` — verified by searching all validators, `EmployeeService`, and
  `EmployeeRepository` for such a comparison. The only related protection is at *read* time: the
  hierarchy CTE's `VisitedPath` cycle guard (`EmployeeHierarchy.sql:78,145,191`), and the SQL `CHECK`
  constraint that exists **only** for Cotivity (`CK_CotivityEmployees_NoSelfReporting`,
  `databasesesign.sql:245-249`). The Angular master-settings form excludes the current row from its own
  reports-to dropdown (`master-settings.ts` `optionsFor()`), which is a **client-side-only** rule for
  employees. See [14-known-issues.md](14-known-issues.md).
- **`ImageBase64` has no validator rule.** The JPEG/PNG/GIF + ≤5 MB limits live in the Angular form
  (`employee-add-form.ts`) and in `FileExtension.GetFileExtension`/`FileHelper`
  (`TeamHub.Shared/Common/Services/`); nothing rejects other content types at the API boundary.
- Four rules (`PhoneNumber`, `MobileNumber`, `EmergencyContactNumber`, and the trailing `≤150`
  follow-ups on `EmergencyContactPerson`/`FatherName`/`MotherName`/`SpouseName`) rely on
  FluentValidation's default messages because `.MaximumLength(n)` is not followed by `.WithMessage`.
  **Verified** inconsistency with the doc-style used elsewhere.

## Child validators

- `EmployeeAddressDtoValidator` (`Employee/EmployeeAddressDtoValidator.cs:10-35`):
  `AddressTypeId > 0`; `AddressLine1` required ≤160; `AddressLine2` ≤160; `City` required ≤100;
  `State` required ≤100; `Latitude` `.InclusiveBetween(-90, 90)`; `Longitude`
  `.InclusiveBetween(-180, 180)`. **Verified:** the lat/long bounds match `DECIMAL(10,8)` /
  `DECIMAL(11,8)`; no `AddressTypeName` rule (it is a read-only join field). `AddressId` is unvalidated
  because it is the identity key.
- `EmployeeSkillDtoValidator` (`Employee/EmployeeSkillDtoValidator.cs:10-14`): `SkillId > 0`;
  `SkillLevel` `.InclusiveBetween(1, 5)` — the exact mirror of
  `CHECK(SkillLevel BETWEEN 1 AND 5)` in `databasesesign.sql:554`, so the rule exists twice by design
  (fail fast in the API, guarantee in the DB).

## RBAC batch validators (`Rbac/RbacBatchRequestValidators.cs`, 2 classes)

`SaveUserRolesRequestValidator` (`:6-34`):
`Assignments` `.NotNull()`; then per item — `UserId > 0`, `RoleIds` `.NotNull()`, every id `> 0`
(`ids.All(id => id > 0)`), and no duplicates within a user
(`ids.Distinct().Count() == ids.Count`); finally, collection-level
`items.DistinctBy(item => item.UserId).Count() == items.Count` — "Each user may only appear once in the
batch".

`SaveRolePermissionsRequestValidator` (`:36-64`) is the same structure keyed by `PermissionId`, with
messages "A permission cannot be granted to the same role more than once" and "Each permission may only
appear once in the batch".

**Verified design detail:** the three `Must(ids => ids is null || …)` rules tolerate `null` even though
`.NotNull()` runs first on the same property — belt-and-braces so a `null` list produces one message
("Role ids cannot be null.") instead of two, because a `Must` on a null list would otherwise be a
`NullReferenceException`.

**Verified:** an *empty* `RoleIds` list is legal in both validators — that is the mechanism by which
"clear all roles / all grants for this row" is expressed. Server-side privilege checks are **not** in
these validators; they are in the matrix services (`RbacValidationException`,
`PrivilegeEscalationException`), see [06-application-services.md](06-application-services.md).

## What is validated where — summary

| Concern | Validated by |
| --- | --- |
| Master-data field shape | FluentValidation (auto) |
| Employee create-only rule (joining date not in past) | FluentValidation `"Create"` ruleset, invoked manually by `EmployeesController` |
| Address geometry, skill level 1-5, duplicate dress sizes, single primary address/skill | FluentValidation child rules |
| Release/termination field shape, enums, cross-field date ordering | DataAnnotations + `IValidatableObject` (MVC) |
| Lifecycle **state transitions** and clearance gating | `ReleaseStatusWorkflow` / `TerminationStatusWorkflow` services, not validators |
| Role/permission assignment legality, privilege escalation | Matrix services |
| Uniqueness of names/emails/ENumber | **Database** unique constraints → `SqlException 2601/2627` → 409 (no "does this already exist" validator) |
| Self-reporting manager, cycles (Employees) | **Nothing at write time** (client-side dropdown only) |
| Image MIME type / size | Frontend form + `FileExtension`/`FileHelper`; not the validator layer |
