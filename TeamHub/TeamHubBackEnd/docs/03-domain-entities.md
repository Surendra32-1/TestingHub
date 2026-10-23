# 03 · Domain Entities

`src/TeamHub.Domain` contains **26 `.cs` files**: 2 abstractions in `Abstract/` and 24 entity classes
in `Entities/`. All are in the single namespace `TeamHub.Domain.Entities` (file-scoped namespaces).
The project has **zero package references** and no behavior — properties only, so Dapper can populate
them by column name.

## Abstractions (`src/TeamHub.Domain/Abstract/`)

```csharp
// BaseEntity.cs
public abstract class BaseEntity<TKey>
{
    public abstract TKey Id { get; set; }
}

// AuditableEntity.cs
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
{
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
```

Every entity derives from `AuditableEntity<int>` and implements `Id` as an override that forwards to
its table-specific key property, e.g. `AddressType.Id` ↔ `AddressTypeId`
(`Entities/AddressType.cs:7-12`). **Verified purpose:** the generic abstraction lets repository and
service code talk about "the id" without knowing each table's PK name — the matrix/lookup services use
`entity.Id` when mapping to DTOs. **Inferred:** the `TKey` generic exists for future non-`int` keys;
today **all 24 entities use `<int>`**, including the tables whose SQL PKs are `BIGINT`
(`UserLoginHistory`, `RefreshTokens`), which have no entity at all.

`CreatedBy`/`LastModifiedBy` are `string?`, matching the `VARCHAR(100)` audit columns after
`employees-audit-columns.sql` normalized `Employees` (see `02-database.md`).

## The 24 entities

Properties below are exactly as declared; `= string.Empty` initializers mean the C# default for a
missing column is `""`, not `null`.

### Lookup / master entities (uniform shape)

Each has: its own PK property + `Id` override, one name string, `bool IsActive`, audit inherited.

| Entity | File | PK | Name property | Extra properties |
| --- | --- | --- | --- | --- |
| `AddressType` | `AddressType.cs` | `AddressTypeId` | `AddressTypeName` | — |
| `BloodGroup` | `BloodGroup.cs` | `BloodGroupId` | `BloodGroupName` | — |
| `BusinessUnit` | `BusinessUnit.cs` | `BusinessUnitId` | `BusinessUnitName` | — |
| `CotivityDepartment` | `CotivityDepartment.cs` | `CotivityDepartmentId` | `CotivityDepartmentName` | — |
| `CotivityDesignation` | `CotivityDesignation.cs` | `CotivityDesignationId` | `CotivityDesignationName` | `int CotivityRank` |
| `Department` | `Department.cs` | `DepartmentId` | `DepartmentName` | — |
| `Designation` | `Designation.cs` | `DesignationId` | `DesignationName` | `int Rank` (SQL `[Rank]`) |
| `DressSize` | `DressSize.cs` | `DressId` | `DressName` | `string Size`, class is `sealed` (only sealed entity) |
| `Gender` | `Gender.cs` | `GenderId` | `GenderName` | — |
| `JobType` | `JobType.cs` | `JobTypeId` | `JobTypeName` | — |
| `Recruitment` | `Recruitment.cs` | `RecruitmentId` | `RecruitmentName` | — |
| `Role` | `Role.cs` | `RoleId` | `RoleName` | — |
| `Skill` | `Skill.cs` | `SkillId` | `SkillName` | — |
| `Team` | `Team.cs` | `TeamId` | `TeamName` | — |
| `Permission` | `Permission.cs` | `PermissionId` | `PermissionCode`, `PermissionName` (both `= string.Empty`) | `string? Module`, `string? Description`, `bool IsActive` |

### Aggregate / junction entities

| Entity | File | Properties |
| --- | --- | --- |
| `Employee` | `Employee.cs` | see expansion below |
| `CotivityEmployee` | `CotivityEmployee.cs` | `CotivityEmployeeId`, `CotivityEmployeeName`, `CotivityDepartmentId`, `CotivityDepartmentDesignationId`, `int? ReportsToCotivityEmployeeId`, `IsActive` |
| `EmployeeAddress` | `EmployeeAddress.cs` | `AddressId`, `EmployeeId`, `AddressTypeId`, `AddressLine1`, `string? AddressLine2`, `City`, `State`, `decimal Latitude`, `decimal Longitude`, `bool IsPrimary`, `Employee? Employee` |
| `EmployeeSkill` | `EmployeeSkill.cs` | `EmployeeSkillId`, `EmployeeId`, `SkillId`, `int SkillLevel`, `bool IsPrimary`, `Employee? Employee`, `Skill? Skill` |
| `EmployeeDress` | `EmployeeDress.cs` | `EmployeeDressId`, `EmployeeId`, `DressId`, `Employee? Employee`, `DressSize? DressSize` |
| `EmployeeImage` | `EmployeeImage.cs` | `ImageId`, `EmployeeId`, `ImagePath` |
| `CotivityEmployeeImage` | `CotivityEmployeeImage.cs` | `CotivityImageId`, `CotivityEmployeeId`, `CotivityImagePath` |
| `UserRole` | `UserRole.cs` | `UserRoleId`, `UserId`, `RoleId` |
| `RolePermission` | `RolePermission.cs` | `RolePermissionId`, `RoleId`, `PermissionId` |

### `Employee` in full (`Entities/Employee.cs`)

```csharp
public class Employee : AuditableEntity<int>
{
    public int EmployeeId { get; set; }
    public override int Id { get => EmployeeId; set => EmployeeId = value; }

    public int ENumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly JoinedDate { get; set; }
    public string JoinedDateBS { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AlternateEmail { get; set; }
    public int DesignationId { get; set; }
    public int DepartmentId { get; set; }
    public int JobTypeId { get; set; }
    public int BloodGroupId { get; set; }
    public int GenderId { get; set; }
    public int? ReportsToEmployeeId { get; set; }
    public int? LeadEmployeeId { get; set; }
    public string? StationNumber { get; set; }
    public int? RecruitmentId { get; set; }
    public int ExtensionNumber { get; set; }
    public string CitizenshipNumber { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? DateOfBirthBS { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string EmergencyContactPerson { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string? MotherName { get; set; }
    public string? SpouseName { get; set; }
    public string PANNumber { get; set; } = string.Empty;
    public string? LockerNumber { get; set; }
    public int TeamId { get; set; }
    public int? CotivityEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public EmployeeImage? Image { get; set; } = new EmployeeImage();
    public ICollection<EmployeeAddress> Addresses { get; set; } = new List<EmployeeAddress>();
    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();
    public ICollection<EmployeeDress> DressSizes { get; set; } = new List<EmployeeDress>();
}
```

Verified mapping notes:

- Scalar properties map 1:1 to `dbo.Employees` columns (`databasesesign.sql:277-383`); there is **no
  `RoleId`** property, matching the removal migration.
- `DateOnly`/`DateOnly?` are used for the two `DATE` columns (`JoinedDate`, `DateOfBirth`), while audit
  `DATETIME2` columns stay `DateTime?` via `AuditableEntity` — so the domain models time-of-day-free
  business dates separately from timestamps. **Verified** through `Microsoft.Data.SqlClient`/Dapper
  support for `DateOnly`.
- `IsActive` defaults to `true` in C#, matching `DEFAULT(1)` in SQL.
- `Image` is a *navigation* property, not a column, and is initialized to a non-null
  `new EmployeeImage()` — so an employee without a stored image still yields an object whose
  `ImagePath` is `""`. The `EmployeeRepository` overwrites it during reads
  (`Infrastructure/Employees/EmployeeRepository.cs:933`). **Verified** quirk; see
  [14-known-issues.md](14-known-issues.md).
- `DressSizes` is the collection of `EmployeeDress` rows (the property name does not match the table
  name `EmployeeDresses`); **`EmployeeAddresses`/`Skills`/`DressSizes` are not loaded by the same query
  as `Employee`** — the repository fills them separately (see `07-repositories.md`).
- The `EmployeeAddress.Employee`, `EmployeeSkill.Skill`, `EmployeeDress.DressSize`/`.Employee`
  back-references are declared but **never populated** (there is no ORM; Dapper does not traverse
  navigation properties). **Inferred** they were written as documentation of the relationships.

## Tables with no entity class (verified absence)

`EmployeeReleases`, `EmployeeTerminates`, `Users`, `UserLoginHistory`, `RefreshTokens` have
**no** Domain entity. Their rows are mapped straight to DTOs by Dapper
(`DTOs/EmployeeRelease/*`, `DTOs/EmployeeTerminate/*`) and to auth records inside
`TeamHub.Auth/Contracts` + `TeamHub.Auth/Models`. **Inferred rationale:** those paths never need a
persistence-agnostic domain model, so the repository returns DTOs directly. This is a real
inconsistency in the layering, documented rather than glossed over.

## Unused entities (verified by symbol search)

Searching every `.cs` under `TeamHub.Application`, `TeamHub.Infrastructure`, `TeamHub.Auth`,
`TeamHub.API` for whole-word entity names:

| Entity | References outside Domain |
| --- | --- |
| `RolePermission` | **0** |
| `UserRole` | **0** |
| `EmployeeAddress` | 2 (`EmployeeService.cs:68,218`) |
| `EmployeeImage` | 4 |
| `CotivityEmployeeImage` | 4 |
| `EmployeeDress` | 5 |
| `EmployeeSkill` | 9 |
| `Employee` | 270 |
| `Department` | 106 |
| `Role` 82 · `Permission` 74 · `Skill` 66 · `Designation` 84 · `Gender` 42 · `Recruitment` 31 · `Team` 32 · `AddressType` 28 · `JobType` 24 · `BusinessUnit` 18 · `CotivityEmployee` 18 · `DressSize` 18 · `CotivityDepartment`/`CotivityDesignation` 16 · `BloodGroup` 33 | |

**Verified:** the two RBAC junction entities are dead code — the matrix repositories work with
`UserRoleMatrixDto`/`RolePermissionMatrixDto` row DTOs and raw SQL instead, so `UserRoles` and
`RolePermissions` tables are reached without their domain types.

## Cosmetic finding (verified by byte inspection)

`Entities/EmployeeImage.cs:13` and `Entities/CotivityEmployeeImage.cs:13` each contain a line holding
only a **U+00A0 non-breaking space** (byte-verified: codepoint `U+00A0`, no other content). C# treats
`U+00A0` as whitespace, so both files compile — a solution build reported no errors for
`TeamHub.Domain` — but they break consistent formatting. **Recommended:** replace with a normal space.
