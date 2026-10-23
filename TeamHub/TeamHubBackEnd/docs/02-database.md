# 02 · Database

SQL Server database named `TeamHub`. There is **no EF Core and no migration framework** — the schema
is created and evolved by hand-written, mostly idempotent `.sql` scripts that live in
`src/TeamHub.Infrastructure/`. Application code executes plain SQL through Dapper
(see [07-repositories.md](07-repositories.md)).

## Scripts and execution order

| Script | Lines | Purpose |
| --- | --- | --- |
| `databasesesign.sql` | 855 | Core tables: master/lookup, Employees, lifecycle, addresses/skills/images/dresses |
| `authentication-schema.sql` | 136 | `Users`, `UserLoginHistory`, `RefreshTokens`, indexes, guarded Admin/NormalUser user seed |
| `rbac-schema.sql` | 227 | `Permissions`, `RolePermissions`, `UserRoles`, 36-code permission catalog seed + full-catalog `Admin` mirror, `Employees.RoleId` → `UserRoles` migration |
| `EmployeeHierarchy.sql` | 217 | 4 covering indexes + `sp_GetEmployeeOrganizationHierarchy` |
| `seed-data.sql` | 497 | Reference data and a demo dataset (10 employees, addresses, skills, releases, terminations, 2 users) + role-permission grants for `NormalUser`/`Manager`/`HR` (§11) |
| `employees-audit-columns.sql` | 69 | Renames/widens `Employees` audit columns to match every other table |
| `employees-roleid-removal.sql` | 74 | Preserves roles into `UserRoles`, then drops `Employees.RoleId` |
| `authentication-entra-schema.sql` | 75 | Adds `Users.EntraTenantId`/`EntraObjectId`, drops all password storage |

Order that the scripts themselves declare (`rbac-schema.sql:5-7`,
`authentication-schema.sql:3`, `authentication-entra-schema.sql:3`, `seed-data.sql:8`):

```
databasesesign.sql
  → authentication-schema.sql
  → rbac-schema.sql
  → seed-data.sql              (header says after databasesesign + authentication-schema)
  → EmployeeHierarchy.sql      (needs Employees/EmployeeImages/CotivityEmployeeImages)
  → employees-audit-columns.sql
  → employees-roleid-removal.sql
  → authentication-entra-schema.sql
```

**Verified ordering constraint:** `rbac-schema.sql:164-173` selects `e.RoleId` from `Employees`, so it
must run **before** `employees-roleid-removal.sql` drops that column. **Verified conflict:**
`seed-data.sql:181-187` also inserts into the `RoleId` column of `dbo.Employees`, which (a) does not
exist on a fresh database because `databasesesign.sql:324` has `--RoleId INT NOT NULL` commented out,
and (b) is dropped by `employees-roleid-removal.sql`. Running `seed-data.sql` as written therefore
fails on the employee block. See [14-known-issues.md](14-known-issues.md).

**Verified:** every script relies on `GO` batch separators, so it must be run with `sqlcmd`/SSMS —
a plain `SqlCommand` cannot execute them (stated in `employees-audit-columns.sql:14` and
`employees-roleid-removal.sql:15`).

## Convention patterns (verified across the schema)

- Surrogate key: `<Name>Id INT IDENTITY(1,1) PRIMARY KEY`, except the auth tables which use
  `BIGINT IDENTITY` for `UserLoginHistory.LoginHistoryId` and `RefreshTokens.RefreshTokenId`
  (`authentication-schema.sql:31,47`).
- Audit columns on almost every table: `CreatedBy VARCHAR(100)`, `CreatedDate DATETIME(2)`,
  `LastModifiedBy VARCHAR(100)`, `LastModifiedDate DATETIME(2)`.
- **Soft delete**: lookup tables and `Employees` carry `IsActive BIT NOT NULL DEFAULT(1)`; deleting
  through the API sets `IsActive = 0` rather than removing the row. Child rows
  (`EmployeeAddresses`, `EmployeeSkills`, `EmployeeDresses`, `EmployeeImages`) have **no** `IsActive`
  — they are physically replaced by the repository.
- Guarded creation: `IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'X') BEGIN CREATE TABLE … END GO`.
- Date duality: every Nepali-calendar date is stored as a `DATE` column plus a `VARCHAR(10)` BS
  column (`JoinedDate`/`JoinedDateBS`, `DateOfBirth`/`DateOfBirthBS`).
- Time defaults are inconsistent: `Employees.CreatedDate`, `EmployeeReleases.CreatedDate`,
  `EmployeeTerminates.CreatedDate` use `DEFAULT GETDATE()` (server-local,
  `databasesesign.sql:332,447,511`), while auth/RBAC/seed rows use `SYSUTCDATETIME()`.
  **Verified** difference; **Unknown** whether intentional.

## Core tables (`databasesesign.sql`)

### Lookup / master tables

All have the same shape (id, name, audit, `IsActive`) unless extra columns are listed.

| Table | Line | Key | Name column | Extra columns / constraints |
| --- | --- | --- | --- | --- |
| `Departments` | 4 | `DepartmentId` | `DepartmentName VARCHAR(100) NOT NULL UNIQUE` | — |
| `CotivityDepartments` | 22 | `CotivityDepartmentId` | `CotivityDepartmentName VARCHAR(100) NOT NULL UNIQUE` | — |
| `Designations` | 40 | `DesignationId` | `DesignationName VARCHAR(100) NOT NULL UNIQUE` | `[Rank] INT NOT NULL` |
| `CotivityDesignations` | 59 | `CotivityDesignationId` | `CotivityDesignationName VARCHAR(100) NOT NULL UNIQUE` | `CotivityRank INT NOT NULL` |
| `BloodGroups` | 78 | `BloodGroupId` | `BloodGroupName VARCHAR(5) NOT NULL UNIQUE` | — |
| `Gender` | 96 | `GenderId` | `GenderName VARCHAR(50) NOT NULL UNIQUE` | table name is singular |
| `AddressType` | 114 | `AddressTypeId` | `AddressTypeName VARCHAR(100) NOT NULL UNIQUE` | table name is singular |
| `Roles` | 132 | `RoleId` | `RoleName VARCHAR(100) NOT NULL UNIQUE` | seeded: Admin, NormalUser, Manager, HR |
| `Teams` | 150 | `TeamId` | `TeamName VARCHAR(100) NOT NULL UNIQUE` | — |
| `Skills` | 168 | `SkillId` | `SkillName VARCHAR(100) NOT NULL UNIQUE` | — |
| `Recruitments` | 186 | `RecruitmentId` | `RecruitmentName VARCHAR(100) NOT NULL UNIQUE` | — |
| `JobTypes` | 204 | `JobTypeId` | `JobTypeName VARCHAR(100) NOT NULL UNIQUE` | — |
| `DressSizes` | 608 | `DressId` | `DressName VARCHAR(100) NOT NULL` | `[Size] VARCHAR(10) NOT NULL`; `UQ_DressSizes_DressName_Size UNIQUE (DressName, Size)` — **see defect below** |
| `BusinessUnits` | 626 | `BusinessUnitId` | `BusinessUnitName VARCHAR(150) NOT NULL` | `UQ_BusinessUnits_BusinessUnitName UNIQUE`; a second guarded block (`:644-683`) creates a matching unique index only if one is absent, throwing `51001` when duplicate names exist |

**Verified defect — `DressSizes` (`databasesesign.sql:608-624`):** the batch opens `BEGIN` at line 609,
creates the table through line 623, and then hits `GO` at line 624 **without a matching `END`**. In
T-SQL that is a syntax error, so the `CREATE TABLE DressSizes` statement never completes. Because
`EmployeeDresses` (`:685-704`) declares
`FK_EmployeeDresses_DressSizes FOREIGN KEY (DressId) REFERENCES DressSizes(DressId)`, that batch fails
as well, and `seed-data.sql:132-139` / `:323-335` also fail. Every other `IF NOT EXISTS … BEGIN`
block in this file is closed correctly (`Departments` `:16`, `BusinessUnits` `:641`,
`EmployeeDresses` `:705`).

### `CotivityEmployees` (`databasesesign.sql:222-271`)

**Verified destructive behavior:** unlike every other table, this one is dropped first —
`IF OBJECT_ID('dbo.CotivityEmployees','U') IS NOT NULL BEGIN DROP TABLE dbo.CotivityEmployees; END GO`
(`:222-226`) — then recreated unconditionally. Re-running the script erases Cotivity employee data
(and `CotivityEmployeeImages`, which references it, is *not* dropped, so it is dropped-orphaned and
its FK re-creation at `:587-605` only succeeds if the table was also removed).

| Column | Type | Notes |
| --- | --- | --- |
| `CotivityEmployeeId` | INT IDENTITY PK | |
| `CotivityEmployeeName` | VARCHAR(150) NOT NULL | not unique on its own |
| `CotivityDepartmentId` | INT NOT NULL | FK → `CotivityDepartments` |
| `CotivityDepartmentDesignationId` | INT NOT NULL | FK → `CotivityDesignations` |
| `ReportsToCotivityEmployeeId` | INT NULL | self-FK → `CotivityEmployees` |
| audit + `IsActive` | | |
| `CK_CotivityEmployees_NoSelfReporting` | CHECK | `ReportsTo IS NULL OR ReportsTo <> CotivityEmployeeId` |
| `UQ_CotivityEmployees` | UNIQUE | `(Name, DepartmentId, DesignationId, ReportsTo)` — NULL `ReportsTo` makes rows non-comparable under SQL Server's UNIQUE semantics for that combination |

### `Employees` (`databasesesign.sql:275-407`)

| Column | Type / nullability | Constraints |
| --- | --- | --- |
| `EmployeeId` | INT IDENTITY NOT NULL | PK |
| `ENumber` | INT NOT NULL | UNIQUE |
| `FirstName` / `MiddleName` / `LastName` | VARCHAR(100) NOT NULL / NULL / NOT NULL | |
| `JoinedDate` | DATE NOT NULL | Gregorian |
| `JoinedDateBS` | VARCHAR(10) NULL | Nepali calendar text |
| `Email` | VARCHAR(150) NOT NULL | UNIQUE + `CK_Employees_Email CHECK (Email LIKE '%@%.%')` |
| `AlternateEmail` | VARCHAR(150) NULL | |
| `DesignationId`, `DepartmentId`, `JobTypeId`, `BloodGroupId`, `GenderId` | INT NOT NULL | FKs to their lookups |
| `ReportsToEmployeeId`, `LeadEmployeeId` | INT NULL | self-FKs → `Employees(EmployeeId)` |
| `StationNumber` | VARCHAR(20) NULL | |
| `RecruitmentId` | INT NULL | FK → `Recruitments` |
| `ExtensionNumber` | INT NOT NULL | |
| `CitizenshipNumber` | VARCHAR(50) NOT NULL | |
| `DateOfBirth` / `DateOfBirthBS` | DATE NULL / VARCHAR(10) NULL | |
| `PhoneNumber`, `MobileNumber` | VARCHAR(20) NULL | |
| `EmergencyContactNumber` | VARCHAR(20) NULL | |
| `EmergencyContactPerson` | VARCHAR(150) NOT NULL | |
| `FatherName` | VARCHAR(150) NOT NULL | |
| `MotherName`, `SpouseName` | VARCHAR(150) NULL | |
| `PANNumber` | VARCHAR(50) NOT NULL | |
| `LockerNumber` | VARCHAR(20) NULL | |
| ~~`RoleId`~~ | commented out at `:324` and `:364-366` | role authority moved to `UserRoles` |
| `TeamId` | INT NOT NULL | FK → `Teams` |
| `CotivityEmployeeId` | INT NULL | FK → `CotivityEmployees` |
| `IsActive` | BIT NOT NULL DEFAULT 1 | soft delete; set to 0 by release/termination terminal states |
| `CreatedBy`, `CreatedDate`, `LastModifiedBy`, `LastModifiedDate` | VARCHAR(100) NULL / DATETIME2 NOT NULL DEFAULT GETDATE() / VARCHAR(100) NULL / DATETIME2 NULL | |

Follow-up `ALTER` block (`:386-407`) makes an existing table catch up: adds `JoinedDateBS`,
`DateOfBirthBS`, `PhoneNumber`, `MobileNumber`, `EmergencyContactNumber`,
`EmergencyContactPerson`, `FatherName`, `MotherName`, `SpouseName` when missing (all as **NULL** —
note this diverges from the NOT NULL versions in the `CREATE` for the last four), and **drops**
`ReleaseDate` and `ReasonForRelease` when present, because that data now lives in
`EmployeeReleases`/`EmployeeTerminates`.

`employees-audit-columns.sql:17-59` exists because `Employees` previously had
`CreatedBy INT / ModifiedBy INT / ModifiedDate`; it widens to `VARCHAR(100)` and renames to
`LastModifiedBy` / `LastModifiedDate` so the table matches `AuditableEntity<string>` in
`src/TeamHub.Domain/Abstract/`.

### Lifecycle tables

`EmployeeReleases` (`databasesesign.sql:409-470`) — PK `ReleasedId`:

`EmployeeId` INT NOT NULL FK→Employees · `ReleaseType VARCHAR(100) NOT NULL` · `ResignationDate`,
`NoticeDate`, `LastWorkingDate` DATE NULL · `NoticePeriodDays`, `ServedNoticeDays` INT NULL ·
`Reason NVARCHAR(500)`, `EarlyReleaseReason NVARCHAR(500)` (also re-added idempotently at `:460-469`),
`Remarks NVARCHAR(MAX)` NULL · clearance bits `KnowledgeTransferCompleted`, `AssetsReturned`,
`ExitInterviewCompleted` · `SettlementAmount DECIMAL(18,2)` · letter bits
`RelievingLetterIssued`, `ExperienceLetterIssued` · `Status VARCHAR(50) NOT NULL` (workflow state,
no CHECK constraint — states are enforced in C#, see `06-application-services.md`) ·
`ApprovedBy VARCHAR(100)`, `ApprovedDate DATETIME2` · audit columns, `CreatedBy NOT NULL`.

`EmployeeTerminates` (`databasesesign.sql:471-521`) — PK `TerminationId` (table name is
`EmployeeTerminates`, not `EmployeeTerminations`; `seed-data.sql:370` calls this out explicitly):

`EmployeeId` FK→Employees · `TerminationType VARCHAR(100) NOT NULL` · `EffectiveDate DATE` ·
`InvestigationRequired BIT`, `InvestigationSummary NVARCHAR(MAX)` · `WarningCount INT`,
`LastWarningDate DATE` · `Reason NVARCHAR(500)`, `Remarks NVARCHAR(MAX)` · clearance bits
`AssetRecovered`, `SystemAccessRevoked`, `ExitInterviewCompleted` · `SettlementAmount DECIMAL(18,2)` ·
`EligibleForRehire BIT` · `TerminationLetterIssued BIT`, `TerminationLetterDate DATE` ·
`Status VARCHAR(50) NOT NULL` · `ApprovedBy`/`ApprovedDate` · audit columns (`CreatedBy NULL` here,
unlike `EmployeeReleases` where it is NOT NULL).

### Employee child collections

| Table | Line | Columns | Notes |
| --- | --- | --- | --- |
| `EmployeeAddresses` | 523 | `AddressId` PK, `EmployeeId` FK, `AddressTypeId` FK, `AddressLine1 VARCHAR(160) NOT NULL`, `AddressLine2 VARCHAR(160) NULL`, `City VARCHAR(100) NOT NULL`, `[State] VARCHAR(100) NOT NULL`, `Latitude DECIMAL(10,8) NOT NULL`, `Longitude DECIMAL(11,8) NOT NULL`, `IsPrimary BIT DEFAULT 0`, audit | no unique constraint; multiple rows per employee |
| `EmployeeSkills` | 547 | `EmployeeSkillId` PK, `EmployeeId` FK, `SkillId` FK, `SkillLevel INT NOT NULL CHECK(SkillLevel BETWEEN 1 AND 5)`, `IsPrimary BIT DEFAULT 0`, audit | |
| `EmployeeImages` | 566 | `ImageId` PK, `EmployeeId` FK, `ImagePath VARCHAR(500) NOT NULL`, audit | multiple images allowed; hierarchy reads `TOP 1 … ORDER BY ImageId DESC` |
| `CotivityEmployeeImages` | 587 | `CotivityImageId` PK, `CotivityEmployeeId` FK, `CotivityImagePath VARCHAR(500) NOT NULL`, audit | `DATETIME` (not `DATETIME2`) for its audit dates |
| `EmployeeDresses` | 685 | `EmployeeDressId` PK, `EmployeeId` FK, `DressId` FK → `DressSizes(DressId)`, audit | depends on the broken `DressSizes` batch |

## Authentication tables (`authentication-schema.sql`)

**`Users`** (`:8-24`): `UserId INT IDENTITY PK`, `EmployeeId INT NOT NULL` with
`UQ_Users_EmployeeId UNIQUE` and FK → `Employees` (one login per employee),
`Username NVARCHAR(100)` `UQ_Users_Username UNIQUE`, `Email NVARCHAR(255) NULL`,
`IsActive BIT DEFAULT 1`, `LastLoginDate DATETIME2 NULL`, `CreatedDate DATETIME2 NOT NULL DEFAULT
SYSUTCDATETIME()`, `UpdatedDate DATETIME2 NULL`. There is **no password column** after
`authentication-entra-schema.sql:45-55` drops `PasswordHash`, `PasswordSalt`, `IsFirstLogin`.
Entra linking columns `EntraTenantId NVARCHAR(64) NULL` and `EntraObjectId NVARCHAR(64) NULL` are
added with a filtered unique index `UQ_Users_EntraIdentity` (`:18-31`).

**`UserLoginHistory`** (`:27-40`): `LoginHistoryId BIGINT IDENTITY PK`, `UserId` FK,
`LoginDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()`, `LogoutDate NULL`,
`IpAddress NVARCHAR(50)`, `UserAgent NVARCHAR(500)`, `IsSuccessful BIT NOT NULL`.
Index `IX_UserLoginHistory_UserId(UserId, LoginDate)`.

**`RefreshTokens`** (`:43-71`): `RefreshTokenId BIGINT IDENTITY PK`, `UserId` FK,
`LoginHistoryId BIGINT` FK → `UserLoginHistory`, `Token NVARCHAR(500) NOT NULL`,
`ExpiryDate DATETIME2 NOT NULL`, `CreatedDate`, `RevokedDate NULL`,
`ReplacedByToken NVARCHAR(500) NULL`, `CreatedByIp`, `RevokedByIp`, `UserAgent`.
`IX_RefreshTokens_Token` is **UNIQUE** (`:65-67`); `IX_RefreshTokens_UserId_Active(UserId,
RevokedDate, ExpiryDate)` supports active-token lookup.
**Verified:** `Token NVARCHAR(500)` stores the SHA-256 hash of the raw token (the raw value only ever
appears in the HttpOnly cookie) — see [08-auth-and-rbac.md](08-auth-and-rbac.md).

Seed blocks (`:77-135`) run two precondition guards before inserting
`(287,'Admin')` and `(10,'NormalUser')`: `THROW 51000` if the referenced employee or active role is
missing/inactive, `THROW 51001` if a username or employee is already bound to a different account.
The insert itself is `WHERE NOT EXISTS`-guarded, and a final block seeds `UserRoles` only
`IF OBJECT_ID(N'dbo.UserRoles','U') IS NOT NULL` — so this script is safe before `rbac-schema.sql`.
**Verified note:** these fixed employee ids (287, 10) do not come from `seed-data.sql`, which creates
10 employees; the guard is written for an existing production database.

## RBAC tables (`rbac-schema.sql`)

| Table | Line | Columns / constraints |
| --- | --- | --- |
| `Permissions` | 33 | `PermissionId INT IDENTITY PK`, `PermissionCode VARCHAR(100) NOT NULL` `UQ_Permissions_PermissionCode`, `PermissionName VARCHAR(150) NOT NULL`, `Module VARCHAR(100) NULL`, `Description NVARCHAR(500) NULL`, `IsActive BIT DEFAULT 1`, audit |
| `RolePermissions` | 55 | `RolePermissionId` PK, `RoleId` FK→Roles, `PermissionId` FK→Permissions, `UQ_RolePermissions_Role_Permission UNIQUE (RoleId, PermissionId)`, audit, `IX_RolePermissions_RoleId`, `IX_RolePermissions_PermissionId` |
| `UserRoles` | 85 | `UserRoleId` PK, `UserId` FK→Users, `RoleId` FK→Roles, `UQ_UserRoles_User_Role UNIQUE (UserId, RoleId)`, audit, `IX_UserRoles_RoleId` |

Permission catalog seed (`:117-161`, one transaction) inserts **36 codes** with a
`WHERE NOT EXISTS (… PermissionCode …)` guard, so re-running adds only codes introduced since the last
apply. Coverage: 6 `People`, 10 `Resignations` (`view`, `manage`, `add` + 7 `resignations.action.*`),
3 `Terminations` (`view`, `add`, `manage`), 14 `master.<module>` (`Settings`) and the 3 stable RBAC codes
(`permissions.manage`, `user-roles.assign`, `role-permissions.manage`) — the codes
`RbacPolicies.PolicyPermissions` enforces (doc 08 §9). `:164-172` then mirrors the **entire** catalog
onto the `Admin` role with an idempotent `CROSS JOIN`, which is why the seed must be re-run after any
code is added or Admin loses the corresponding button.

**Verified live-DB state (re-queried 2026-10-23):** `dbo.Permissions` has **38** rows — the 36 seeded codes
plus the legacy `master.view` / `master.manage`, which were removed from the seed but still exist in the
table (they are no longer granted to anything new and are not used by the SPA); `Admin` holds all 38.

Data migration (`:185-194`) copies `Employees.RoleId` into `UserRoles` for existing accounts — **unguarded**,
so after `employees-roleid-removal.sql` drops that column the last batch of a re-run fails with
*Invalid column name 'RoleId'* (catalog + Admin grants are in earlier `GO` batches and still apply;
`14-known-issues.md` M10). A verification `SELECT` (`:204-213`) lists users with zero active role
assignments, and a commented rollback block (`:219-226`) drops the three tables in reverse dependency order.

### Role-permission grants for the demo roles (`seed-data.sql:429-493`)

`seed-data.sql` §11 grants enforced codes to the three non-admin roles, so resignation self-service and
termination/offboarding are reachable without hand-ticking the Role Permission Matrix. Idempotent
(`RoleName` + `PermissionCode` natural keys, `WHERE NOT EXISTS`, `r.IsActive = 1 AND p.IsActive = 1`):

| Role | Granted codes | Intent |
|---|---|---|
| `NormalUser` | `resignations.add` | Self-service filing; "People" needs no code (`authenticatedGuard` only), so the menu shows People + Apply Resignation |
| `Manager` | `resignations.view`, `resignations.add`, `resignations.action.submit`/`.sendforapproval`/`.approve`/`.reject`/`.cancel`, `master.team` | Runs the resignation queue; no termination writes |
| `HR` | `people.create`, `people.update`, `people.release`, `resignations.view`, `resignations.add`, `resignations.action.submit`/`.sendforapproval`/`.cancel`/`.exitchecklist`/`.release`, `terminations.view`, `terminations.add`, `terminations.manage` | People master data + both termination capabilities — `terminations.add` files the record (`EmployeeTerminationsController.cs:85`, mirrored by `terminationAddGuard`), `terminations.manage` edits and transitions it — + operational resignation steps |

The six inert codes (`people.view`, `people.terminate`, `people.hierarchy.view`, `resignations.manage`,
`master.view`, `master.manage`) are deliberately **not** granted — a tick that changes nothing is worse
than no tick. *Verified* applied to the live DB 2026-10-23: the §11 batch first inserted 20 rows, then 1
more when `terminations.add` was added to the HR list; final grant totals `NormalUser` = 1, `Manager` = 8,
`HR` = 13, `Admin` = 38 (the pre-existing hand-granted `people.create` was not duplicated, and a re-run of
the batch inserts 0 rows). Remaining gap: only `Admin`/`NormalUser` have `Users` rows and
`DevSignIn.AllowedRoles` is `["Admin","NormalUser"]`, so `Manager`/`HR` grants cannot be signed in as
yet (`14-known-issues.md` M9).

## Indexes added by `EmployeeHierarchy.sql:1-51`

| Index | Table | Key / INCLUDE |
| --- | --- | --- |
| `IX_Employees_ReportsToEmployeeId` | Employees | `(ReportsToEmployeeId)` INCLUDE `EmployeeId, FirstName, MiddleName, LastName, DesignationId, DepartmentId, ENumber, CotivityEmployeeId` |
| `IX_CotivityEmployees_ReportsToCotivityEmployeeId` | CotivityEmployees | `(ReportsToCotivityEmployeeId)` INCLUDE `CotivityEmployeeId, CotivityEmployeeName, CotivityDepartmentId, CotivityDepartmentDesignationId` |
| `IX_EmployeeImages_EmployeeId_ImageId` | EmployeeImages | `(EmployeeId, ImageId DESC)` INCLUDE `ImagePath` |
| `IX_CotivityEmployeeImages_EmployeeId_ImageId` | CotivityEmployeeImages | `(CotivityEmployeeId, CotivityImageId DESC)` INCLUDE `CotivityImagePath` |

The 20-plus index statements at the bottom of `databasesesign.sql` (`:708-855`) are **commented out**;
the live index set is the four above plus those in `authentication-schema.sql`, `rbac-schema.sql`,
and the unique constraints.

## `sp_GetEmployeeOrganizationHierarchy(@EmployeeId)` (`EmployeeHierarchy.sql:53-216`)

One procedure, **four result sets**, all shaped as
`(EmployeeId/CotivityEmployeeId, ParentId, Name, DesignationName, DepartmentName, ImagePath,
[EmployeeNumber,] Source, Level)`:

1. **Managers above the employee** (`:59-102`) — recursive CTE walking
   `ReportsToEmployeeId` upward. Anchor is the employee at `Level 0`; each step subtracts 1, and the
   final `WHERE managerIds.Level < 0` drops the anchor so the result is strictly ancestors,
   `ORDER BY Level`.
2. **The employee itself** (`:104-123`) — plain `SELECT` with `Level = 0`.
3. **Cotivity managers** (`:125-170`) — same upward walk over `CotivityEmployees`, entered through
   `Employees.CotivityEmployeeId`. No `Level < 0` filter, so the Cotivity anchor row is included;
   `Source` is `'Cotivity'`; columns are `CotivityEmployeeId`, `CotivityEmployeeName`,
   `CotivityDesignationName`, `CotivityDepartmentName`, `CotivityImagePath AS ImagePath`, and there is
   **no `EmployeeNumber`** column in this result set.
4. **Reportees below the employee** (`:172-214`) — recursive CTE walking downward
   (`report.ReportsToEmployeeId = chain.EmployeeId`), `Level + 1` per child, ordered by
   `Level, EmployeeName, EmployeeId`.

Cycle protection in all three recursive CTEs is a materialized `VisitedPath` of the form
`/id/id/…/` compared with
`chain.VisitedPath NOT LIKE '%/' + CAST(candidate.Id AS VARCHAR(20)) + '/%'`. Each recursive statement
uses `OPTION (MAXRECURSION 32767)`. Images come from `OUTER APPLY (SELECT TOP 1 … ORDER BY <ImageId> DESC)`,
i.e. the newest image wins. Lookups are joined with `LEFT JOIN`, so a missing designation or
department yields NULL names rather than dropping the node.

**Verified:** this procedure is consumed by `EmployeeHierarchyRepository` through Dapper
`QueryMultipleAsync` and surfaces as the `people.hierarchy.view` feature — traced in
[12-workflows.md](12-workflows.md).

## Logical relationships (from the FKs above)

```
Departments ─┐
Designations ─┤
JobTypes ─────┼──► Employees ◄── Teams ──► Recruitments ─┘
BloodGroups ──┤        │  ▲
Gender ───────┘        │  └── self: ReportsToEmployeeId, LeadEmployeeId
                       │
        ┌──────────────┼──────────────┬───────────────┐
        ▼              ▼              ▼               ▼
 EmployeeAddresses EmployeeSkills EmployeeImages EmployeeDresses ──► DressSizes
   └ AddressType    └ Skills
                       │
        ┌──────────────┴──────────────┐
        ▼                             ▼
 EmployeeReleases              EmployeeTerminates

CotivityDepartments ─┐
CotivityDesignations ─┴──► CotivityEmployees (self: ReportsToCotivityEmployeeId)
                                  ▲
                                  └── Employees.CotivityEmployeeId
                                  └── CotivityEmployeeImages

Users (1─1 Employees) ──► UserLoginHistory ──► RefreshTokens
Users ──► UserRoles ◄── Roles ◄── RolePermissions ►── Permissions
```
