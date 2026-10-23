# 13 · Testing Inventory & Build Status

Everything in this document was produced by reading all ten backend test `.cs` files, the frontend
spec files, `package.json`, both `.csproj` files, and by **actually running `dotnet build` on both test
projects on 2026‑10‑23**. No claim below is based on test names alone. *Updated 2026‑10‑23:* the
frontend suite was also **executed** (`ng test --watch=false`) after the permission-driven navigation
landed — §5 now reports run results rather than a file count.

---

## 1. Test projects that exist

| Project | Framework | Location | Membership |
|---|---|---|---|
| `TeamHub.UnitTests` | net10.0, xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4 | `TeamHubBackEnd/tests/TeamHub.UnitTests` | referenced projects: TeamHub.Application, TeamHub.Domain (csproj lines 17‑20) |
| `TeamHub.IntegrationTests` | xunit | `TeamHubBackEnd/tests/TeamHub.IntegrationTests` | real‑SQL‑Server tests, opt‑in via env var |
| Frontend unit tests | Angular 22, `ng test` → **vitest** ^4.0.8 (package.json) | `TeamHubFrontEnd/src/app/**/*.spec.ts` | **10 spec files / 63 tests** (was 1 file / 2 tests before 2026-10-23) |

Verified: `TeamHubBackEnd.slnx` does **not** list the two test projects (the solution builds without them). Verified: **no CI at all** — a repository-wide glob for `.github/**`, `*.yml`, `*.yaml`, `azure-pipelines*`, `.gitlab-ci*`, `Jenkinsfile` returned zero matches. Tests have only ever been runnable manually.

---

## 2. Build status — VERIFIED BROKEN (both backend test projects)

Commands run from a PowerShell session with .NET SDK 10.0.203:

```
dotnet build tests\TeamHub.UnitTests\TeamHub.UnitTests.csproj        → BUILD FAILED (2 errors reported, exit 1)
dotnet build tests\TeamHub.IntegrationTests\...csproj                → BUILD FAILED (2 errors reported, exit 1)
```

Reported errors (verbatim):

- UnitTests `RbacServiceTests.cs(159,51)`: **CS0535** — `FakeUserRoleRepository` does not implement `IUserRoleRepository.GetMatrixAsync(int, int, string?, CancellationToken)`.
- UnitTests `RbacServiceTests.cs(175,57)`: **CS0535** — same for `FakeRolePermissionRepository`/`IRolePermissionRepository`.
- IntegrationTests `RbacRepositoryTests.cs(34,39)` and `(51,39)`: **CS7036** — call `GetMatrixAsync()` with no arguments; required parameter `pageNumber` missing.

**The true breakage is wider than the error count shows.** Verified stale references that the capped compiler log does not list (because both fakes fail to bind, the compiler suppresses cascading member-binding diagnostics):

1. `CreateOrUpdateEmployeeRequest` has **no `RoleId` property** — the full class was read (46 lines, `DTOs/Employee/CreateOrUpdateEmployeeRequest.cs`); it exposes `RoleIds` only, and a source-wide regex for `public int RoleId {` finds the property on `Role`, `UserRole`, `RolePermission`, `GetRoleDto`, `RoleOptionDto` and two row classes — never on the employee request. Yet `EmployeeRequestValidatorTests.cs` sets `RoleId = 1,` in **four** places (lines 30, 68, 96, 127 — each also mis-indented to column 0, evidence of a hurried edit against a failing build).
2. `RolePermissionAssignmentDto` is keyed by **permission** (`PermissionId` + `RoleIds`, verified in `RbacDtos.cs:87-92`). Three test sites still use the removed role-keyed shape (`RoleId = …, PermissionIds = […]`): `RbacServiceTests.cs:93` and `:109`, `PermissionAndRbacValidatorTests.cs:102` and `:114`.
3. Both matrix fakes implement the old parameterless `GetMatrixAsync(CancellationToken)` signature (`RbacServiceTests.cs:164, 179`) instead of the current `(int pageNumber, int pageSize, string? search, CancellationToken)`.

Consequence: **`dotnet test` cannot execute anything** in either project until these files are updated. Because the test projects are not in the `.slnx`, normal solution builds never surfaced this — the suite has silently rotted. (Fix recommendation is in doc 14; marked *Recommended*, not existing behavior.)

---

## 3. Unit-test inventory (what each file intends to cover)

All tests are xunit `[Fact]`s; no theories, no mocking library — hand-written fakes.

### 3.1 `Services/DepartmentServiceTests.cs` (51 lines) — 1 test, matches the current API
`CreateAsync_MapsRequestAndReturnsCreatedDepartment`: fake repository captures the entity; asserts id mapping (42), name, IsActive, and that `CreatedBy == "System"` — this test *enshrines* the hardcoded audit actor that doc 12 §5/§10 flags.

### 3.2 `Services/RbacServiceTests.cs` (226 lines) — 8 tests, 6 stale
Working against current service APIs: `UserRoleMatrix_SuperAdmin_PersistsWithoutHoldingRoles`, `UserRoleMatrix_NonSuperAdminCannotGrantRoleTheyDoNotHold`, `UserRoleMatrix_RejectsUnknownOrInactiveRoleId`, `UserRoleMatrix_EmptyBatchIsNoOp` — these four genuinely exercise `UserRoleMatrixService.SaveAsync` escalation/validation gates (doc 12 §9.1). Broken by §2 items 1‑3: the two `RolePermissionMatrix_*` tests use the removed DTO shape, and both fakes' `GetMatrixAsync` predates paging. Also present: `PermissionService_CreateAsync_TrimsFieldsAndStampsSystemActor` and `..._RejectsDuplicateCode` (verify trim + `CreatedBy="System"` + `RbacValidationException` on duplicate code).

### 3.3 `Validation/EmployeeLifecycleRequestValidationTests.cs` (165 lines)
Tests `EmployeeReleaseRequest`/`EmployeeTerminationRequest` **DataAnnotations** (`Validator.TryValidateObject`): required Resignation/LastWorking/Reason, last‑working‑date ≥ resignation‑date, reason ≥ 10 non-whitespace chars, etc. These mirror the DTO attributes verified in docs 04/05 — no reference to the broken shapes, so they would compile if the project built.

### 3.4 `Validation/EmployeeRequestValidatorTests.cs` (135 lines) — 4× broken
`CreateOrUpdateEmployeeRequestValidator` tests (email length = DB column length, required fields). Cannot compile until `RoleId` → `RoleIds = [1]` (§2 item 1).

### 3.5 `Validation/LookupRequestValidatorTests.cs` (87 lines)
Master-data (Department/Designation/etc.) validator coverage: name-required/length. No stale shapes observed.

### 3.6 `Validation/PermissionAndRbacValidatorTests.cs` (119 lines) — partially broken
`SaveUserRolesRequestValidator` tests are current (line 90 uses `UserRoleAssignmentDto { UserId, RoleIds }`); the two `SaveRolePermissionsRequestValidator` tests (lines 102, 114) use the removed role-keyed shape (§2 item 2).

### 3.7 `UnitTest1.cs` (10 lines) and the IntegrationTests twin
Empty template placeholders (`public void Test1() {}`) — zero assertions.

### Coverage gaps (verified by absence — searched all test sources)
No tests exist for: `EmployeeService.Create/UpdateAsync` orchestration (§12 §5‑6), `EmployeeReleaseService`/`EmployeeTerminationService` `TransitionStatusAsync` gates, the `ReleaseStatusWorkflow`/`TerminationStatusWorkflow` state machines themselves, any API controller (no WebApplicationFactory/`IClassFixture` anywhere), the auth module (JWT/refresh/RBAC policies), and every repository except the optional RBAC ones. The workflow classes are the most behavior-dense untested code in the repo.

---

## 4. Integration tests (real database, opt-in)

Pattern verified in both files: each test first reads `Environment.GetEnvironmentVariable("TEAMHUB_TEST_CONNECTION_STRING")` and **returns silently when unset** (`DatabaseConnectionTests.cs:12-15`, `RbacRepositoryTests.cs:28-31` etc.) — so a green `dotnet test` run without the variable means *zero integration coverage actually executed*.

- `DatabaseConnectionTests.TestConnectionAsync`: opens the connection, `SELECT 1`, asserts 1.
- `RbacRepositoryTests` (4 tests): matrix load smoke tests (currently broken, §2), `UserRoleSaveAsync_RejectsUnknownUserAndCommitsNothing` (transaction rollback proof — asserts `UserRoles` stays empty after `RbacValidationException`, doc 12 §9.1), `PermissionRepository_CreateReadUpdateSoftDelete_RoundTrips` (create → CodeExists → read → update → delete → gone, with `finally` cleanup deleting RolePermissions then Permissions).

These are the only tests that touch the repository transaction semantics documented in doc 12.

---

## 5. Frontend tests

**Executed 2026-10-23** with `node node_modules\@angular\cli\bin\ng.js test --watch=false` (the local
CLI is used because the global one is not on PATH in this environment; `\.node_modules\.bin\ng.cmd test
--watch=false` is equivalent): **10 files, 63 tests, 62
passed, 1 failed**. There is **no lint target** — `package.json` defines no `lint` script and no
`angular.json` lint section, so `ng lint` cannot run.

| Spec file | Tests | Covers |
| --- | --- | --- |
| `core/config/menu-filter.spec.ts` | 11 | nested gating, ancestor retention at 4 levels, empty-group removal, ordering, `requiresAll`/`requiresAny`/`hideIfPermission`, multi-role union, **non-mutation** of `APP_MENU` (JSON snapshot + identity checks), one-`master.*`-grant on the real tree |
| `core/config/menu.service.spec.ts` | 11 | full-catalog admin tree with exact child labels, signed-out entries, self-service mutual exclusion, zero-permission account, case-insensitivity, logout staleness, refresh-changed permissions, account switching |
| `shared/components/nav-menu/nav-menu.component.spec.ts` | 6 | leaf vs group rendering, 3-level recursion in one component, real `Router` navigation on click, active leaf **and** every ancestor group, `alignEnd`, empty tree |
| `shared/components/topbar/topbar.component.spec.ts` | 4 | root-level entries rendered from the filtered tree, Sign-out visibility, collapse after `clearSession()` |
| `core/auth/auth.guard.spec.ts` | 8 | unauthorized direct navigation → `/home` `UrlTree`, guard re-check after logout, anonymous → `/login?returnUrl`, expired-session restore, `masterGuard` per-module codes, `signedInLandingGuard`, and that the termination create routes need `terminations.add` — holding `terminations.manage` alone is bounced |
| `features/master-settings/master-settings.spec.ts` | 9 | pre-existing — metadata-driven form/config behaviour |
| `features/employees/pages/employee-add-form/employee-add-form.spec.ts` | 7 | pre-existing — form validation paths |
| `features/employees/components/nepali-date-picker/nepali-date-picker.component.spec.ts` | 3 | pre-existing |
| `features/employees/pages/employee-lifecycle-form/employee-lifecycle-form.spec.ts` | 2 | pre-existing — **1 of these 2 fails** (see below) |
| `app.spec.ts` | 2 | pre-existing — bootstrap smoke |

**The only failure is pre-existing and unrelated to the navigation work:**
`employee-lifecycle-form.spec.ts:56` asserts a label `Reason for Not Serving Full Notice Period`, which
appears nowhere in `src` — the template renders `Early Release Reason *`. Left untouched (fixing it
means deciding which is authoritative: the spec text or the shipped label). *Verified* by reading both
files; the suite passed 62/63 with the assertion removed from consideration.

Two operational notes for whoever extends the suite: (a) the session lives in `sessionStorage`, which
is **shared across spec files in one jsdom worker**, so every test that writes a session must clear it
(`afterEach(() => sessionStorage.clear())` — present in `topbar`, `menu.service` and `auth.guard`
specs) or it leaks into unrelated files; (b) functional guards must be invoked inside
`TestBed.runInInjectionContext`, and their result may be an `Observable`, a `boolean` or a `UrlTree`, so
assertions need to await it (`auth.guard.spec.ts` `resolveGuard` helper).

**Build status (frontend, same date):** `ng build --configuration development` completes; `tsc --noEmit
-p tsconfig.app.json` and the spec project are clean. The **production** build fails only on two
pre-existing budgets — initial 1.25 MB vs the 1 MB error limit, and `employee-lifecycle-list.css`
9.40 kB vs the 8 kB `anyComponentStyle` error limit (`angular.json`). No menu/nav file appears in any
budget warning (`nav-menu.component.css` is 2.55 kB, under the 4 kB warn threshold).

---

## 6. Honest summary

| Layer | Tests written | Currently executable? | Real coverage today |
|---|---|---|---|
| Master-data service mapping | 1 | **No** — file itself is current, but the whole project fails to build (§2) | Department create only |
| RBAC service gates | 8 (6 stale) | **No** — project fails to build | Design intent: escalation + validation gates |
| Validators / DataAnnotations | ~15 | **No** — same project | Employee + lifecycle + lookup + RBAC request rules |
| Repositories (integration) | 5 | **No** — project fails to build; opt-in env var even when fixed | RBAC transaction rollback, permission round-trip |
| Controllers / auth / workflows / employees CRUD | 0 | n/a | None |
| Frontend | 63 (10 files) | **Yes — executed 2026-10-23: 62 pass, 1 pre-existing failure** | Navigation/menu filter + recursion + guards + master settings + employee form + date picker; pages, services and HTTP layer still largely untested |

**Bottom line (verified)**: neither backend test project compiles against the current source; the test suite predates the matrix-paging and permission-keyed-DTO refactors and the `RoleId → RoleIds` removal, and no CI exists to have caught any of it. Documentation elsewhere in this set references this page rather than claiming test health anywhere else.
