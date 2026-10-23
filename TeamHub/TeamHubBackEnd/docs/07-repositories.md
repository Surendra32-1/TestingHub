# 07 · Repositories and Data Access

Scope: `src/TeamHub.Infrastructure/**` — 21 repository classes, 2 database plumbing types, 1 DI file.
All 21 repository files were read in full; every SQL statement quoted below is verbatim from the current
source.

## Data-access technology (verified)

* **Dapper 2.1.66** with hand-written T-SQL strings. **There is no Entity Framework anywhere in the
  solution** (`TeamHub.Infrastructure.csproj` references `Dapper`, `Microsoft.Data.SqlClient` 6.1.3; no
  `Microsoft.EntityFrameworkCore` package, no `DbContext` type — verified by searching all `.cs` and
  `.csproj` files).
* Schema is managed by the `.sql` scripts in the same project, not by migrations
  (see [02-database.md](02-database.md)).
* Every command goes through `new CommandDefinition(sql, params, [transaction, ]cancellationToken: …)` —
  so **cancellation is honoured on every read and write**, with one exception noted below
  (`EmployeeService.GetEmployeeMastersAsync` drops the token before the call ever reaches here).

### `Database/`

```csharp
// Database/IDbConnectionFactory.cs:5-8
public interface IDbConnectionFactory { IDbConnection CreateConnection(); }

// Database/SqlConnectionFactory.cs:6-8
public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
```

Registered as a **singleton** (`DependencyInjection.cs:32`); the 21 repositories are **scoped**
(`:33-53`). Every repository method does `using var connection = connectionFactory.CreateConnection();`
— a new `SqlConnection` per operation, disposed at the end of the method. A connection is only opened
explicitly when a transaction is needed (`EmployeeRepository.cs:451, :667`;
`UserRoleRepository.cs:105`; `RolePermissionRepository.cs:101`); Dapper opens it otherwise and the real
work is absorbed by ADO.NET connection pooling. **Verified** pattern; **Inferred** rationale: singleton
factory + short-lived connections is the canonical Dapper setup.

**Consequence (verified):** an operation that touches two repositories (e.g. employee insert + role
assignment) uses **two connections and cannot share a transaction** — see `EmployeeService` ordering at
`Services/EmployeeService.cs:97-98`.

### Shape shared by all 21 repositories

1. `public sealed class XRepository(IDbConnectionFactory connectionFactory) : IXRepository`.
2. **SQL is always a `const string` (or `private const string BaseSelect`) literal** — no query builder, no
   interpolation of user values. User input reaches SQL only as parameters; the one whitelist is
   `searchField is "name" or "designation" or "department" ? searchField : "all"`
   (`EmployeeRepository.cs:155`).
3. **Reads map straight onto Application DTOs or onto a `private sealed class …Row`** nested in the same
   repository. There is **no mapper class, no AutoMapper, no mapping extension** in the solution — verified:
   the only mapping code is the `Map(...)`/`ToDto(...)` private helpers inside repositories and the inline
   object-initialisers in services.
4. Type conversion is done by two private helpers repeated in the lifecycle and employee repositories:
   `ToDateOnly(DateTime?) => DateOnly.FromDateTime(v)` and
   `ToOffset(DateTime?) => new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))`; writes go
   the other way with `DateOnly.ToDateTime(TimeOnly.MinValue)`. **Verified:** because services always write
   `DateTime.UtcNow`, treating the `DATETIME` columns as UTC is internally consistent (the DB-side
   `DEFAULT GETDATE()` on a few tables is the one exception — see [02-database.md](02-database.md) and
   [14-known-issues.md](14-known-issues.md)).
5. **Writes return either the new identity (`ExecuteScalarAsync<int>` after `OUTPUT INSERTED.…`) or a
   `bool` from `affected > 0`.** No repository throws its own exceptions; they let `SqlException` bubble to
   the API exception handler (unique-constraint → 409, FK → 409).

## Soft-delete filtering is inconsistent (verified table)

`Delete` is `UPDATE … SET IsActive = 0` in **all 21 repositories** — no `DELETE` statement exists for any
master table (the only hard `DELETE`s are for child/mapping rows: `UserRoles`, `RolePermissions`,
`EmployeeAddresses/Skills/Dresses`). But the `IsActive = 1` predicate is applied unevenly:

| Repository | `GetAll` filters `IsActive=1` | `GetById` filters | `Update` guards | `Delete` guards | Extra |
| --- | --- | --- | --- | --- | --- |
| `AddressTypeRepository` | ✅ `:13` | ✅ `:19` | ✅ `:31` | ✅ `:37` | |
| `GenderRepository` | ✅ | ✅ | ✅ | ✅ | table name is singular `Gender` |
| `JobTypeRepository` | ✅ | ✅ | ✅ | ✅ | |
| `RecruitmentRepository` | ✅ | ✅ | ✅ | ✅ | |
| `RoleRepository` | ✅ | ✅ | ✅ | ✅ | |
| `SkillRepository` | ✅ | ✅ | ✅ | ✅ | |
| `TeamRepository` | ✅ | ✅ | ✅ | ✅ | |
| `BloodGroupRepository` | ❌ `:13-17` | ❌ `:28-32` | ❌ `:54-60` | ❌ `:72-76` | |
| `DepartmentRepository` | ❌ `:13-17` | ❌ | ❌ | ❌ | |
| `DesignationRepository` | ❌ `:13-17` | ❌ | ❌ | ❌ | orders by `[Rank]` |
| `CotivityDepartmentRepository` | ❌ `:13-17` | ❌ | ❌ | ❌ | |
| `CotivityDesignationRepository` | ❌ `:13-17` | ❌ | ❌ | ❌ | orders by `CotivityRank` |
| `BusinessUnitRepository` | ❌ `:13` | ❌ `:21` | ❌ `:39-45` | ❌ `:53-56` | reads use `COALESCE(IsActive, 1)` |
| `DressSizeRepository` | ❌ `:13` | ❌ `:21` | ❌ `:39-46` | ❌ `:54-57` | |
| `PermissionRepository` | ❌ `:13-16` | ✅ `:26` | ✅ `:68` | ✅ `:75` | |
| `EmployeeRepository` | ✅ (grid `:193`, tree `:34`) | ❌ (`:289`) | ❌ (`:603`) | — (no delete) | |
| `CotivityEmployeeRepository` | ❌ `:20-30` | ❌ `:39-55` | ❌ `:93-101` | ❌ `:110-111` | |

**Verified consequences:**

* A deleted **blood group / department / designation / Cotivity record / business unit / dress size** still
  appears in the standalone `GET /api/{entity}` list endpoints (used by the master-settings grid), while the
  *employee form* dropdowns — which come from `EmployeeRepository.GetEmployeeMastersAsync`
  (`:736-877`, every result set filtered `WHERE IsActive = 1`) — hide them. The same table therefore looks
  different depending on which endpoint the UI calls.
* `Delete` on an already-deleted row returns `true` (unfiltered `UPDATE … SET IsActive = 0`), so repeated
  deletes are reported as successful.
* `UpdateAsync` without `AND IsActive = 1` can silently "revive" nothing but can edit a hidden row.
* `BusinessUnitRepository` is the only one defensively reading `COALESCE(IsActive, 1)`
  (`:13, :21`), matching `BusinessUnits.IsActive` being added by a later `ALTER` script
  (see [02-database.md](02-database.md)).
* **Verified asymmetry inside `GetEmployeeMastersAsync`:** the `CotivityEmployees` result set has **no
  `IsActive` filter** (`:810-814`) while the 13 other result sets do. Soft-deleted Cotivity employees
  therefore still appear in the employee form's "Cotivity reporting manager" dropdown.

## Master-data repositories (14 nearly identical classes)

Method set: `GetAllAsync`, `GetByIdAsync`, `CreateAsync(entity)`, `UpdateAsync(entity)`, `DeleteAsync(id)`.

| Aspect | Verified implementation |
| --- | --- |
| Identity on insert | `OUTPUT INSERTED.<Pk>` + `ExecuteScalarAsync<int>` (`CotivityDesignationRepository.cs:37-43`) |
| `CreateAsync` parameter object | the **Domain entity itself** (`new CommandDefinition(sql, designation, …)`) — Dapper reflects over `CotivityDesignation` and binds matching names |
| `UpdateAsync` | sets every mapped column + `LastModifiedDate`/`LastModifiedBy`, `WHERE <pk> = @<pk>`, returns `affected > 0` |
| `DeleteAsync` | soft delete, parameter object is anonymous `new { <Pk> = id }` |
| Ordering | by name; `[Rank]`/`CotivityRank` first for designations; `DressName, Size` for dress sizes |
| Transaction | **none** — single statement |

**Verified:** the `[Rank]` bracketing appears in `DesignationRepository` (`:14, :29, :43, :58`) but the raw
`Rank` is used in `EmployeeRepository.GetEmployeeMastersAsync` (`:749, :752`). Both are valid T-SQL
(`RANK` is only reserved as a function call), so this is a style inconsistency, not a defect.

## `CotivityEmployeeRepository` (164 lines)

* `GetAllAsync`/`GetByIdAsync` (`:14-30`, `:39-55`) share one `SELECT` with three `LEFT JOIN`s
  (department, designation, self-referential `CotivityEmployees reporting`) plus

  ```sql
  OUTER APPLY (SELECT TOP (1) CotivityImagePath FROM CotivityEmployeeImages
               WHERE CotivityEmployeeId = ce.CotivityEmployeeId
               ORDER BY CotivityImageId DESC) image
  ```

  — **the newest image wins**, matching the append-only image strategy.
* `CreateImageAsync` (`:77-88`) inserts into `CotivityEmployeeImages` with `OUTPUT INSERTED.CotivityImageId`.
* `Map` (`:117-129`) converts `ImagePath` → `ImageBase64` through `ToBase64DataUrl` (`:131-149`), which
  **reads the file from disk** (`FileHelper.GetBase64StringFromFile(relativePath)`) and prefixes a
  `data:{mime};base64,` URL chosen by extension (`image/jpeg | image/png | image/gif |
  application/octet-stream`).
  **Verified performance/behavior consequence:** `GET /api/cotivity-employees` performs **one file read per
  employee** and embeds every photo in the JSON list. Same for the employee grid (below).
  `ImagePath` is normalised with `Replace('\\','/').TrimStart('/')` (`:138`) — a web-root-relative path,
  which is also what the frontend uses for `<img src>` when base64 is absent.
* `private sealed class CotivityEmployeeRow` (`:151-162`) is the SQL-only shape (`ImagePath` instead of
  `ImageBase64`) — the Row/DTO split is how the repository keeps `Domain` out of the read path.

## `EmployeeRepository` (1076 lines — the largest file in the solution)

Interface `Contracts/Repositories/IEmployeeRepository.cs:7-24`, **10 methods, and deliberately no
`DeleteAsync`**: employees are never soft-deleted through this repository (only `IsActive` via
`UpdateAsync`, and deactivation as a side effect of release/termination).

### `GetEmployeesAsync` (private, `:148-253`) — backs both `GetAllAsync` and `GetActiveByDepartmentIdAsync`

* One flat `SELECT` with **10 `LEFT JOIN`s** (`:181-192`) producing a cartesian fan-out across
  `EmployeeAddresses`, `EmployeeSkills`+`Skills`, and `EmployeeImages`, then de-duplicated in memory by
  `rows.GroupBy(row => row.Id)` (`:212`) with `first = group.First()` (`:215`).
* `Addresses`/`Skills` are rebuilt as distinct **strings**, not child DTOs
  (`:242-249`) — `Address` is a pre-joined `CONCAT_WS(', ', AddressLine1, AddressLine2, City, State)`
  (`:178`), so the grid DTO carries display text only.
* `RoleName` comes from a correlated `STRING_AGG` sub-select over `UserRoles`→`Users`→`Roles`
  (`:175`), i.e. **the role names are reached through `Users`, not `Employees`** — consistent with
  `RoleId` having been removed from `Employees`.
* Search (`:195-201`): `@SearchTerm IS NULL OR` four `LIKE` branches; `TeamName` is searched **only** when
  `@SearchField = 'all'`, so a field-scoped search can never find an employee by team. **Verified.**
* Ordering (`:202-204`): `CASE WHEN @DepartmentId IS NOT NULL THEN dsg.[Rank] END ASC` — designation rank
  only matters inside a department; otherwise alphabetical. **Verified** intent from the code shape.
* `WHERE e.IsActive = 1` (`:193`) — deleted/inactive employees never appear in the grid.
* **No `OFFSET/FETCH`: the grid is unpaginated.** Combined with per-row `ToBase64DataUrl` (`:239-241`) this
  means the employees list reads every stored image file on every request. See
  [14-known-issues.md](14-known-issues.md).
* **Verified image inconsistency:** this grid uses a plain `LEFT JOIN EmployeeImages ei` (`:192`) and then
  `first.ImagePath` from the arbitrary join order, whereas `GetByIdAsync` uses
  `OUTER APPLY … ORDER BY ImageId DESC`. For an employee with multiple stored images, **the list and the
  detail can show different photos.**
* `ImageBase64` is `null` when `ImagePath` is blank, and there is no cache.

### `GetByIdAsync` (`:255-404`)

* Main `SELECT` with 10 `LEFT JOIN`s + `OUTER APPLY` newest image (`:258-289`); **no `IsActive` filter**
  (`:289`) — inactive employees are readable by id, which is what makes `GET` + `[AllowAnonymous]`
  (see [09-api-endpoints.md](09-api-endpoints.md)) able to expose former employees' data.
* Then **four more round trips on the same connection** (`:346-349`): addresses, skills, dresses, role ids.
  Not a `QueryMultiple` — five sequential network calls for one employee.
* `addressSql` (`:300-323`) is the only defensive read in the layer: a `ROW_NUMBER()` CTE partitions by
  `AddressTypeId` + upper-cased/`LTRIM(RTRIM())`-ed `AddressLine1/2/City/State` and keeps
  `DuplicateRank = 1`, ordered `IsPrimary DESC, AddressId DESC`. **Verified:** it hides exact duplicate
  address rows at read time instead of preventing them at write time (no unique index on
  `EmployeeAddresses`, and all indexes in `databasesesign.sql:708-855` are commented out).
* `roleIdSql` (`:337-344`) reads ids back from `UserRoles`→`Users`→`Roles`, ordered by `role.RoleName`.
* Null-coalescing to empty strings for non-nullable DTO members: `JoinedDateBS ?? string.Empty` (`:359`),
  `EmergencyContactPerson ?? string.Empty` (`:386`), `FatherName ?? string.Empty` (`:387`).
* `ImageBase64 = ToBase64DataUrl(row.ImagePath)` (`:399`) — detail view embeds the photo.

### `CreateAsync` (`:406-562`) — the only true multi-table transaction for employees

```
connection.Open(); using var transaction = connection.BeginTransaction();
try {
  ExecuteScalarAsync<int>(employeeSql, employeeParams, transaction)   // :497  OUTPUT INSERTED.EmployeeId
  foreach address  → ExecuteScalarAsync<int>(addressSql,  …, transaction)   // :491
  foreach skill    → ExecuteScalarAsync<int>(skillSql,    …, transaction)   // :535
  foreach dress    → ExecuteScalarAsync<int>(dressSql,    …, transaction)   // :550
  transaction.Commit();  return employee.EmployeeId;                          // :554-555
} catch { transaction.Rollback(); throw; }                                    // :557-561
```

* **Verified:** one round trip **per child row** (N+M inserts, no TVP, no bulk). With the validator allowing
  several addresses/skills/dress sizes this is bounded but chatty.
* Each insert's returned identity is written **back onto the Domain entity**
  (`address.AddressId = …`, `:518`; `skill.EmployeeSkillId`, `:535`; `dress.EmployeeDressId`, `:550`) —
  **but the service never re-reads those values** when building its response
  (`Services/EmployeeService.cs:157, :169` map from the same objects… which it re-creates, so the response
  still shows `0`). Verified interaction defect documented in [06-application-services.md](06-application-services.md).
* `JoinedDate`/`DateOfBirth` converted with `ToDateTime(TimeOnly.MinValue)` (`:462, :477-479`);
  `DateOfBirth` needs an explicit `(DateTime?)` cast to keep `null` typed (`:478`).

### `UpdateAsync` (`:564-734`) — delete-and-reinsert children, all in one transaction

1. `UPDATE Employees SET …34 columns… WHERE EmployeeId = @EmployeeId` (`:566-603`).
2. `if (affectedRows == 0) { transaction.Rollback(); return false; }` (`:673-677`) — the controller maps
   `false` to 404, and the rollback means **no child table is touched when the employee row is missing**.
3. `DELETE FROM EmployeeAddresses / EmployeeSkills / EmployeeDresses WHERE EmployeeId` (`:680-682`).
4. Re-insert each child (`:684-724`), **without** `OUTPUT` this time (`:608-624`) — ids are not returned.
5. `transaction.Commit(); return true;` (`:726-727`), same `catch { Rollback; throw; }` (`:729-733`).

**Verified:** the child rows are **hard-deleted** (they are not soft-delete tables), so address/skill/dress
history is lost on every PUT, and the new rows get fresh identity values. Combined with step 3 running
whenever the payload has empty collections, an update that omits `Addresses` wipes them — the frontend must
always resend the full child arrays (see [11-frontend.md](11-frontend.md)).

### `GetEmployeeMastersAsync` (`:736-877`)

One `CommandDefinition` holding **14 `SELECT` batches**, read back with 14 ordered
`await multi.ReadAsync<…>()` calls (`:852-875`) into `EmployeeMastersResponse`:

```
Departments → Designations → JobTypes → BloodGroups → Gender → Roles → Teams → Skills
→ Recruitments → Employees(as ReportingManagerId/Name) → CotivityEmployees(as same DTO)
→ BusinessUnits → DressSizes → AddressTypes
```

* **Verified fragility:** the mapping is **positional** — inserting, removing or reordering any `SELECT`
  silently shifts every following result set onto the wrong DTO. There is no test covering this method
  (see [13-testing.md](13-testing.md)).
* `Employees` and `CotivityEmployees` are both projected onto `EmployeeReportingManagerDto` with
  `ReportingManagerId`/`ReportingManagerName` aliases (`:804-805, :811-812`) — one DTO reused for two
  sources. The employee list contains **every active employee including the one being edited**; the
  self-report exclusion is done in the Angular form only (see [05-validators.md](05-validators.md)).
* `AddressTypes` selects only `AddressTypeId, AddressTypeName` (`:833-838`) — no `IsActive` column, because
  `AddressTypeDto` needs only the label.
* Filters: all `WHERE IsActive = 1` except `CotivityEmployees` (none, `:813-814`) and `BusinessUnits` which
  uses `WHERE COALESCE(IsActive, 1) = 1` plus `COALESCE(IsActive, 1) AS IsActive` (`:819, :821`).

### `AssignEmployeeRolesAsync` (`:878-931`)

```
if (string.IsNullOrWhiteSpace(email)) return;                     // :880-884
distinctRoleIds = roleIds.Where(id => id > 0).Distinct().ToArray // :885-889
-- ensureAndClearSql                                              // :894-906
DECLARE @UserId INT;
SELECT @UserId = UserId FROM dbo.Users WHERE EmployeeId = @EmployeeId;
IF @UserId IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
BEGIN
  INSERT INTO dbo.Users (EmployeeId, Username, Email, IsActive, CreatedDate)
  VALUES (@EmployeeId, @Username, @Email, 1, SYSUTCDATETIME());
  SET @UserId = SCOPE_IDENTITY();
END
IF @UserId IS NOT NULL
DELETE FROM dbo.UserRoles WHERE UserId = @UserId;
-- then, if ids remain, insertSql                                // :918-926
DECLARE @UserId INT; SELECT @UserId = UserId FROM dbo.Users WHERE EmployeeId = @EmployeeId;
IF @UserId IS NOT NULL
INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
SELECT @UserId, r.RoleId, @ModifiedBy, SYSUTCDATETIME()
FROM dbo.Roles r WHERE r.IsActive = 1 AND r.RoleId IN @RoleIds;
```

Verified properties:

* `Username` **is** the email — both bound from the same value (`:909`:
  `new { EmployeeId = employeeId, Username = email, Email = email }`). This is the mechanism that links an
  `Employees` row to a `Users` row.
* **Clear-then-insert**, so the submitted role list is authoritative; an empty list means "remove all roles"
  (the method returns after the DELETE at `:912-915`).
* Roles are filtered by `r.IsActive = 1` in SQL, so an inactive/unknown role id is **silently dropped** — no
  error, unlike `UserRoleRepository.SaveAsync` which throws `RbacValidationException` for the same mistake.
  **Verified inconsistency** between the two paths that write `UserRoles`.
* **No transaction and no `connection.Open()`**: two independent auto-committed batches on one connection
  (`:907` and `:927`). A failure in the second batch leaves the user with zero roles.
* `dbo.Users` is created with `IsActive = 1` and **no password/Entra fields** — a user created this way can
  only sign in through the paths the auth module supports (see [08-auth-and-rbac.md](08-auth-and-rbac.md)).
* If `@UserId IS NULL` *and* a `Users` row with that `Username` already exists for a **different** employee,
  nothing is created or linked and the role selection is dropped silently (`:898`). **Verified** branch
  analysis; **Unknown** whether reachable in practice (the unique `Username` constraint would have to predate
  the employee record).
* Called **after** `CreateAsync`/`UpdateAsync` have committed (`Services/EmployeeService.cs:98, :250`), so it
  is outside the employee transaction by design.

### `GetHierarchyAsync` (`:65-107`)

`connection.QueryMultipleAsync` with `commandType: CommandType.StoredProcedure` on
`dbo.sp_GetEmployeeOrganizationHierarchy`, then four ordered reads
(managers → `ReadSingleOrDefault` selected employee → Cotivity rows → reports). Returns `null` when result
set 2 has no row (`:78-81`), which the controller turns into 404. Row types
`EmployeeHierarchyEmployeeRow` (`:1051-1062`) and `EmployeeHierarchyCotivityRow` (`:1064-1074`) mirror the
proc's columns; **verified** they match result set #3's narrower column list (no `EmployeeNumber`), which is
why the Cotivity DTO has no employee number (see [12-workflows.md](12-workflows.md)).
`MapEmployeeHierarchyNode` and `MapSelectedEmployee` (`:109-135`) are two near-identical private mappers for
two structurally identical DTOs.

### `CreateImageAsync` (`:933-943`)

Single insert into `EmployeeImages` with `OUTPUT INSERTED.ImageId`; **no transaction, no deletion of previous
images** — the append-only pattern again.

## Lifecycle repositories

`EmployeeReleaseRepository` (320 lines) and `EmployeeTerminationRepository` (291 lines) are the other two
multi-statement writers.

* **`private const string BaseSelect`** shared by every read (`:14-24` / `:13-24`):
  `SELECT r.*` (`t.*`) plus `CONCAT_WS` employee name, `e.ENumber AS EmployeeCode`, department, designation,
  manager name via `LEFT JOIN Employees manager ON manager.EmployeeId = e.ReportsToEmployeeId`. The
  termination variant also selects `e.JoinedDate` (`:18`). **Verified:** `SELECT r.*` means the `…Row`
  classes must keep up with any column added to the table — there is no explicit column list to protect the
  mapping.
* Reads: `GetAllAsync` (`:26-33`), `GetByEmployeeIdAsync`, `GetByIdAsync`, all `ORDER BY CreatedDate DESC,
  <pk> DESC`.
* **`GetPagedAsync` (`:35-87`)** builds a dynamic `WHERE` list from the four filters
  (`search` → `CONCAT_WS(…) LIKE @Search OR CAST(e.ENumber AS CHAR(20)) LIKE @Search` (`:41`), department
  equality, status equality, release-type equality), then `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS
  ONLY`. The `LIKE` wildcards are built in C#: `Search = $"%{search?.Trim()}%"` (`:61`).
  * **Verified total-count heuristic** (`:69-78`): `totalItems = items.Count`, and the `COUNT(1)` query only
    runs `if (items.Count == pageSize)`. Because the fallback is the *page* count and not
    `(page-1)*pageSize + items.Count`, **`TotalCount`/`TotalPages` are wrong for any page after the first
    whose result is not full** (e.g. page 3 of 3 returning 5 rows reports `TotalCount = 5`,
    `TotalPages = 1`). **Recommended** fix; see [14-known-issues.md](14-known-issues.md).
  * `PageNumber = page` is reported **unclamped** while `Offset` uses `Math.Max(page, 1)` (`:65`) — a request
    for `page=0` returns page-1 data labelled `pageNumber: 0`.
  * The `COUNT` query re-uses `whereSql` but with a shorter `FROM` (only `Employees` + `Departments`,
    `:74-75`) — correct today because no filter references the other joins; fragile if a designation filter
    is added.
  * No `e.IsActive` filter, so releases/terminations of deactivated employees are listed.
* **`CreateAsync` (`:107-129`)** guards the FK in SQL instead of relying on it:
  `IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeId = @EmployeeId) RETURN;` (`:110`) then the insert —
  so a bad employee id yields `null` from the repository (→ service `null` → controller 404) rather than a
  500 from an FK violation. Same trick in the termination repository.
* **`UpdateAsync` (`:131-154`)** — one batch: full column UPDATE (`:134-145`) **including `Status`**, followed
  by

  ```sql
  IF @Status = 'Released'
  UPDATE Employees SET IsActive = 0
  WHERE EmployeeId = (SELECT EmployeeId FROM EmployeeReleases WHERE ReleasedId = @ReleasedId);
  ```

  (`:146-149`); the termination twin is `IF @Status IN ('Completed', 'Terminated') UPDATE Employees SET
  IsActive = 0 …` (`EmployeeTerminationRepository.cs:144-147`). **Verified:** this is the concrete mechanism
  behind the "PUT bypasses the workflow" finding in [06-application-services.md](06-application-services.md),
  and `'Completed'` is a status the termination workflow does not know.
* **`TransitionStatusAsync` (`:156-174` / `:154-172`)** — UPDATE status (+ `ApprovedBy`/`ApprovedDate` via
  `CASE WHEN @Status IN ('Approved','Released'|'Approved','Terminated') THEN … ELSE column END`), then
  `IF @Status = 'Released'|'Terminated' UPDATE Employees SET IsActive = 0`; finally
  `affected > 0 ? await GetByIdAsync(…) : null` (`:174`).
* **`UpdateClearanceAsync` (`:177-205` / `:175-201`)** — sets only the checklist booleans and
  `SettlementAmount = COALESCE(@SettlementAmount, SettlementAmount)` (`:187`), i.e. **sending `null` keeps the
  stored amount** while sending a number overwrites it; then re-reads.
* `Parameters(int employeeId, …, bool created, int <pk> = 0)` (release `:208-265`, termination `:204-235`) is a
  shared create/update parameter factory that
  - converts every `DateOnly?` with `?.ToDateTime(TimeOnly.MinValue)`,
  - **derives `ServedNoticeDays = (lastWorking.DayNumber - resignation.DayNumber)`** in C# (`:217-219`) —
    server-side computed, may be negative for early release, and is not validated anywhere;
  - sets `IsApproved = request.Status is "Approved" or "Released"` (`:249`; termination adds `"Completed"`,
    `:227`) and mirrors that into `ApprovedBy`/`ApprovedDate`;
  - switches audit columns on the `created` flag (`CreatedBy`/`CreatedDate` vs
    `LastModifiedBy`/`LastModifiedDate`, `:259-263`).
* **Verified:** none of these multi-statement batches uses a transaction — the status update and the employee
  deactivation are two auto-committed statements in one `ExecuteAsync`, so a failure between them (or a
  trigger error on `Employees`) can leave a `Released` record with an active employee.
* Both files end with a `Map` + `ToDateOnly`/`ToOffset` + a private `…Row` class carrying
  `EmployeeName`, `EmployeeCode` (`int`, non-nullable), `Department`, `Designation`, `ManagerName`
  (release `:288-319`, termination `:258-290`).

## RBAC repositories

### `PermissionRepository` (79 lines)

Plain master CRUD **plus `CodeExistsAsync(permissionCode, excludePermissionId, ct)`** (`:32-42`):

```sql
SELECT COUNT(1) FROM dbo.Permissions
WHERE PermissionCode = @PermissionCode
  AND (@ExcludeId IS NULL OR PermissionId <> @ExcludeId);
```

— one statement serving both create (exclude = null) and update (exclude = self). The service converts
`count > 0` into `RbacValidationException`. Reads/updates/deletes filter `IsActive = 1` except `GetAllAsync`
(`:13-16`), which deliberately returns **inactive** permissions too — required by the permissions admin grid
(that is where `SetActiveAsync` gets its meaning).

### `UserRoleRepository` (163 lines)

* `GetMatrixAsync` (`:11-95`) — **four** statements on one connection: active roles (`:17-21`), a
  `COUNT(1)` over `Users ⋈ Employees` with an optional name `LIKE` (`:23-28`), the paged user rows
  (`:30-42`, `ORDER BY e.FirstName, e.LastName, u.Username OFFSET/FETCH`), then the assignments for exactly
  those ids (`:44-48`, `WHERE ur.UserId IN @UserIds`). Paging arithmetic is computed **in C#**:
  `Math.Max(1, pageNumber)` + `Math.Clamp(pageSize, 1, 100)` (`:13-14`),
  `totalPages = Ceiling(totalCount / (double)pageSize)`, `pageNumber = totalPages == 0 ? 1 :
  Math.Min(pageNumber, totalPages)` (`:56-57`), `offset = (pageNumber - 1) * pageSize` (`:58`).
  Rows are assembled by grouping the assignment read into `roleIdsByUser` (`:70-75`) and defaulting to `[]`
  (`:83`), so **a user with no roles still produces a matrix row** — required by "clear all roles".
  Both the count and the page filter `u.IsActive = 1 AND e.IsActive = 1` (`:27, :39`), and the assignment
  join requires `r.IsActive = 1` (`:47`) — inactive roles are invisible in the matrix.
* `SaveAsync(assignments, modifiedBy, ct)` (`:97-144`) — `connection.Open()` + `BeginTransaction()`, then per
  assignment: verify the user exists/active (`:110-116`), verify **all** distinct role ids are active
  (`:117-127`, comparing `validRoleCount != distinctRoleIds.Count`), `DELETE FROM dbo.UserRoles WHERE UserId`
  (`:130-132`), then one `INSERT … VALUES (@UserId, @RoleId, @ModifiedBy, SYSUTCDATETIME())` per role
  (`:134-140`), `Commit()` (`:143`). Throws `RbacValidationException` (from
  `TeamHub.Application.Common`, which `TeamHub.Infrastructure` references) — **verified** the exception type
  is validated twice: once in the service (`UserRoleMatrixService.cs:34`) and again here
  (`:115, :126`), a deliberate defense-in-depth since the repository is the transaction boundary.
* `BuildFullName` (`:146-151`) joins non-blank name parts — the C# counterpart of SQL's `CONCAT_WS`.
* **Verified:** the repository's own `DELETE`-then-`INSERT` means the payload must be the *complete* role set
  per user; partial payloads erase the rest. Same semantics as `RolePermissionRepository`.

### `RolePermissionRepository` (150 lines)

Structurally identical to `UserRoleRepository`, keyed by **permission** instead of user:
`rolesSql` (`:17-21`) + permission `COUNT` (`:23-27`) + paged permissions (`:29-38`) +
`SELECT rp.PermissionId, rp.RoleId … WHERE rp.PermissionId IN @PermissionIds` (`:40-44`), grouped into
`roleIdsByPermission` (`:66-71`). `SaveAsync` (`:93-140`) validates permission existence and role activeness,
`DELETE FROM dbo.RolePermissions WHERE PermissionId = @PermissionId` (`:126-128`) and re-inserts per role
(`:130-136`), all in one transaction. **Verified:** `DELETE` is scoped by permission, so saving one page of
the matrix only rewrites the grants of the permissions on that page — which is exactly the design note in
`DTOs/Rbac/RbacDtos.cs:85-86` ("Keyed by permission (not role) so a page of the matrix can be saved without
knowing the other pages."). Rows are filtered `p.IsActive = 1` and joined `r.IsActive = 1`, and search
matches `PermissionName` only — **not** `PermissionCode` or `Module` (`:36`).

## Cross-cutting verified findings for this layer

| # | Finding | Evidence | Status |
| --- | --- | --- | --- |
| 1 | Soft-delete filtering differs per repository → the same record can be visible or hidden depending on endpoint | table above | Verified |
| 2 | Every employee/cotivity list call reads image files from disk and returns base64 | `EmployeeRepository.cs:239-241`, `CotivityEmployeeRepository.cs:128` | Verified |
| 3 | Employee grid is unpaginated | `EmployeeRepository.cs:157-204` (no `OFFSET`) | Verified |
| 4 | Grid uses `LEFT JOIN EmployeeImages`, detail uses `OUTER APPLY … ImageId DESC` → possibly different photos | `:192` vs `:283-288` | Verified |
| 5 | `GetEmployeeMastersAsync` maps 14 result sets positionally | `:845-876` | Verified (fragile) |
| 6 | Release/termination `TotalCount` understated on non-full pages after the first | `EmployeeReleaseRepository.cs:69-78` | Verified (bug) |
| 7 | `Status` is written by PUT, bypassing the state machine, and can deactivate the employee | release `:141-149`, termination `:139-147` | Verified |
| 8 | Multi-statement write batches have no transaction (except `CreateAsync`/`UpdateAsync`/`SaveAsync`) | release `:151`, termination `:151`, `AssignEmployeeRolesAsync` `:907/:927` | Verified |
| 9 | `SELECT r.*` couples the private Row classes to the full table shape | `:15`, `:14` | Verified |
| 10 | `AssignEmployeeRolesAsync` silently drops inactive role ids while `UserRoleRepository.SaveAsync` throws | `:926` vs `:117-127` | Verified |
| 11 | Duplicate addresses are filtered at read time, not prevented at write time | `:300-323` | Verified |
| 12 | No repository exposes a hard delete for any master table | all `DeleteAsync` | Verified |
| 13 | `EmployeeRepository` has no `DeleteAsync` at all | `IEmployeeRepository.cs:7-24` | Verified |
| 14 | `CotivityEmployees` result set in the masters batch is the only unfiltered one | `:810-814` | Verified |
| 15 | `searchField = "all"` never matches a search scoped to team names | `:200` | Verified |
| 16 | Ids returned by child inserts are assigned onto entities the service then ignores | `:518/:535/:550` + `EmployeeService.cs:157/:169` | Verified |
| 17 | `ServedNoticeDays` is computed server-side and can be negative, never validated | `EmployeeReleaseRepository.cs:217-219` | Verified |
