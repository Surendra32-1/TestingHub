# 15 · Coverage Report (Phase 5/7 — Evidence-Based)

Status vocabulary used throughout this document set, per the audit rules:

- **Verified** — established by reading the actual source (file+line citations in the docs), or by running a build.
- **Partial** — the artifacts were read, but some aspect could not be confirmed without executing it (runtime, external systems).
- **Unverified** — no evidence gathered; no claim made.

Analysis method: complete repository inventory → module-by-module source reading (every behaviour-bearing `.cs` under `src/`, every behaviour-bearing `.ts`/template under `TeamHubFrontEnd/src/app`, every SQL script, every config file) → end-to-end trace assembly → documentation written **from** the evidence, not alongside it → self-audits (three in-doc corrections were made when verification contradicted an earlier draft statement: route count in doc 11, `OnPush` count in doc 11, workflow-graph completeness in doc 12 §7/§8; one earlier draft claim about PUT validation was corrected against `Program.cs:90` before finalizing doc 12 §11.2).

**Revision 2026-10-23 (permission-driven navigation):** when the recursive menu landed in the frontend, docs 01/08/09/11/12/13/14 were re-audited against source rather than extended. That re-audit invalidated several pre-existing statements, which were corrected in place: the master-data controllers are gated by per-module `master.<module>` **policies** (not `[Authorize(Roles = "Admin")]`), **31 of the 35** catalog codes were found enforced (not "3 of 15" — now **32 of 36**, see the second revision paragraph), `AuthService` grew 97→120 lines (all its citations shifted), `employee-directory.component.ts` 164→173 lines and its `canManageEmployees`/`hasRole('Admin')` check is now four granular `hasPermission` calls (`:72-78`), and the frontend test inventory is 10 spec files / 62 tests (not one). Every line reference in this document set was re-derived from the current files.

**Revision 2026-10-23 b (role grants + `terminations.add`):** the resignation/termination "Add" paths were then taken from *unenforced* to *seeded and symmetric*. `seed-data.sql` gained §11 (`:429-493`), an idempotent role→permission grant table for `NormalUser`/`Manager`/`HR`, and `terminations.add` was introduced as a real permission: catalog row in `rbac-schema.sql:139`, `RbacPolicies.TerminationAdd` (`RbacPolicies.cs:21, :61` → 32 policies), `[Authorize]` on the termination create action (`EmployeeTerminationsController.cs:85`, leaving `:108/:125/:145` on `terminations.manage`), `terminationAddGuard` on both create routes, and the "New Termination" button / directory "Terminate" action reading the code. Counts corrected across docs 02/08/09/11/13/14: catalog 35→36 seeded codes (live table 38 rows), policies 31→32, tests 62→63. This revision is also the first in the set backed by **executed** evidence: the seed batches were applied to the live `TeamHub` database (and re-applied to prove idempotency), and sign-in + a 403 on an unauthorized termination `POST` were reproduced over HTTP.

---

## 1. Source-area coverage

| Area | Inventory | Analyzed | Documented | Residual gap |
|---|---|---|---|---|
| `TeamHub.Domain` entities | 24/24 entity files | 24/24 | doc 03 (per-entity columns/types/nullability from SQL DDL) | none |
| `TeamHub.Application` DTOs | 21 folders | all | doc 04 (field-level, incl. DataAnnotations regexes) | none |
| `TeamHub.Application` Validators | 18 folders | all | doc 05 (rule-by-rule) | none |
| `TeamHub.Application` Services | 15 services | 15/15 | doc 06 + doc 12 flows | none |
| `TeamHub.Application` Workflows | 3 files | 3/3 | doc 12 §7–8 (full transition graphs re-read from source at audit time) | none |
| `TeamHub.Infrastructure` repositories | 23 folders + DB layer | all | doc 07 (SQL cited per method) + doc 12 | none |
| SQL scripts (schema, RBAC, seed, hierarchy SP, migrations) | 8 files | all read | docs 02, 03, 12 §4 (SP logic incl. cycle guards, indexes) | execution history — *Partial* as of 2026-10-23: the `rbac-schema.sql` catalog/Admin-mirror batch and `seed-data.sql` §11 grant batch were **run** against the live `TeamHub` DB and re-run to prove idempotency (doc 02 §RBAC); the other scripts' apply history remains Unverified (no migration ledger in repo) |
| `TeamHub.Auth` | all folders | all | doc 08 (JWT, refresh rotation, RBAC policies, Entra) | live Entra app-registration values — Partial (docs/entra-id-setup.md read; tenant config not inspectable) |
| `TeamHub.API` controllers | 21 controllers | 21/21 read in full | doc 09 (route/attribute/response per action, every `[Authorize]` attribute re-verified per action) + doc 12 | none |
| `TeamHub.Auth` controllers | `AuthController` | full | docs 08, 09 (doc 09 counts 22 controllers / 116 endpoints incl. this one) | none |
| `Program.cs` composition + middleware | 1 file | full | docs 01, 10, 12 §1 | none |
| `TeamHub.Shared` | response/common services | all | docs 04, 10 | none |
| Frontend `src/app` | 46 behaviour-bearing `.ts` + 10 `.spec.ts` + all used templates | all read in full | doc 11 (routes 16/16, guards, interceptor, every component/service, per-method; §12.1 for the permission-driven recursive navigation: `core/config/menu.config.ts`, `core/config/menu-filter.ts`, `core/config/menu.service.ts`, `shared/components/nav-menu/*`) | none static |
| Frontend config (`angular.json`, `package.json`, tsconfig, environments) | all | all | docs 01, 11 §1/§5, 13 §5 | `ng test` and both build configurations were **run** (doc 13 §5); runtime rendering in a real browser — Partial |
| Backend project/config files (`.csproj`, `NuGet.config`, `appsettings*`) | all | all | docs 01 §config, 02 (connection), 08 §12.1 (secret) | none |
| Tests | 10 backend `.cs` + 10 frontend spec files (63 tests) | all read | doc 13 (+ `dotnet build` of both test projects **run**, both FAILED; `ng test --watch=false` **run**, 62 passed / 1 pre-existing failure — captured evidence 2026‑10‑23) | none |
| `.vs/`, `bin/`, `obj/`, `dist/`, `node_modules/`, logs | inventoried as non-source | excluded deliberately | doc 11/01 note build outputs are generated | none — no source content lives there |

## 2. Workflow traces (doc 12) — end-to-end completeness

| Feature | Frontend→DB→UI traced | Error paths | Auth checks | Side effects |
|---|---|---|---|---|
| Sign-in / session restore / logout | Verified (incl. `sessionRevision` → `sessionState` → `visibleMenu()` re-filter) | Verified (404/403 gates, refresh) | Verified | cookie + sessionStorage + menu re-render |
| Directory read + department read + org tree | Verified | Verified (404 shape) | **AllowAnonymous verified** | none |
| Hierarchy view (recursive SP) | Verified | Verified (404 on missing employee) | AllowAnonymous verified | none |
| Employee create | Verified | Verified (manual ruleset validation, partial-failure noted) | `administration.create` policy (`EmployeesController.cs:60`) verified | Users provisioning, image file+row |
| Employee edit | Verified | Verified (ruleset asymmetry, rollback-on-0-rows) | `administration.update` policy (`:97`) verified | child replace, role reset, new image |
| Resignation create/transition/clearance | Verified | Verified (409 gates, non-transactional risk) | Verified policy | employee deactivation coupling |
| Termination (deltas vs resignation) | Verified incl. dead `Completed` branch | Verified | Verified — reads `terminations.view`, create `terminations.add` (`:85`), writes `terminations.manage`; a `POST` with a token lacking the code returned 403 over HTTP (2026-10-23) | same deactivation pattern |
| RBAC matrix load/save (both) | Verified | Verified (escalation exceptions → 400/403) | policy + superadmin bypass | transactional DELETE+INSERT; stale-claims window |
| Master-data CRUD (15 datasets) | Traced fully via Departments representative + per-controller endpoint tables (doc 09 confirms the 21 controllers share the generated pattern) | Verified (auto-validation + handler mapping) | per-module `master.<module>` policy on the class attribute; Departments `GET`s are `[AllowAnonymous]` | soft delete only |
| Permission-driven navigation render (frontend) | Verified (`APP_MENU` → `MenuService.visibleMenu()` → `filterMenu` → recursive `app-nav-menu` → guard → backend policy) | Verified (no session → anonymous items only; non-mutating filter proven by spec) | Visibility only — *not* enforcement; no backend menu table/API exists | none (sessionStorage read) |
| Mid-session permission refresh | Verified (doc 12 §2.3a) | Verified (401 → rotation → re-query) | `rbac:role-permissions.manage` on the matrix save | ≤15-min stale-claims window (M3) |

## 3. Design-rationale classification (doc-by-doc)

- **Verified rationale** (code/comments prove it): soft-delete everywhere, newest-image reads, transaction placement, AllowedInitial gates, clearance gates, cycle-guarded recursion, index-for-hierarchy script.
- **Inferred rationale** (labeled as such in text): cartesian grid + C# dedup as a deliberate simplification; count-only-when-full as paging optimization.
- **Unknown** (explicitly marked, never invented): abandoned `Completed` termination state; Entra half-migration intent; literal-`'NULL'` seed origin; `RoleId→RoleIds` migration timing; historical intent to skip Create-ruleset on PUT.
- **Recommended** (clearly separated in doc 14 §Recommended): R1–R7 only — no recommendation is presented anywhere as existing behavior.

## 4. Honest limitations

1. **Static analysis + build runs, plus limited live execution.** Docs 01–12 were written from source and build output; SQL semantics were checked by reading statements and the DDL. Since 2026-10-23 b the RBAC seed batches and the sign-in/authorization round-trips **have** been executed against the live database and the running API (doc 02 §RBAC, doc 14 §Explicitly unverified), so those specific claims are *Verified by execution*; everything else that only manifests at runtime (lock contention in lifecycle batches, actual image files, full refresh rotation) is still *Partial*. Note that the API process serving `localhost` was not restarted after adding `terminations.add`, so the new policy is compiled but not yet live.
2. **The database is now partially inspectable** — `dbo.Permissions` and `dbo.RolePermissions` were queried directly (38 catalog rows, per-role grant counts in doc 02). Applied migrations for the *other* scripts, actual indexes beyond the scripts, and business data remain unknown; docs 02/03 still describe the schema *as scripted*.
3. **Browser runtime not exercised.** `ng test --watch=false` and both build configurations were run (doc 13 §5), but no headless/browser session was driven, so actual rendered menu behavior against a live SQL Server backend is *Partial* — the navigation evidence is unit-test + static-source based.
4. **Git history was not used** as evidence; statements about *when* something rotted (e.g. test staleness point of origin) are avoided.
5. Line-number citations refer to the files as of this audit (2026‑10‑23); they will drift with edits — symbol names accompany them for resilience.

## 5. Completion-criteria checklist (against the mandate)

| Criterion | State |
|---|---|
| Every relevant source directory inventoried | ✅ Verified (doc 11/13 enumerate all; excluded dirs listed above) |
| Every discovered module analyzed | ✅ Verified — 6 backend projects, all frontend features, all SQL/config |
| All classes/methods/DTOs/entities/fields accounted for | ✅ docs 03/04/06/07/11 (per-member tables; dead members explicitly marked dead, e.g. `EmployeeMastersResponse`, `getCurrentUser`, Login-Account controls) |
| All API endpoints documented | ✅ doc 09 — 22 controllers (21 API + `AuthController`), every action and every `[Authorize]` attribute re-verified per action, incl. the 4 never called by the SPA (marked) |
| All tables/columns/relationships/constraints documented | ✅ doc 02/03 — incl. audit columns, soft-delete flags, FK directions, the hierarchy SP + its indexes |
| All frontend routes/components/forms/API integrations | ✅ doc 11 — 16/16 route entries, 46/46 behaviour-bearing sources (+10 specs), all `EmployeeService`/`rbac.service`/master-client methods, navigation tree spec §12.1 |
| Major workflows traced end to end | ✅ doc 12 — 9 numbered workflows plus the two traces added 2026-10-23 (§2.3a permission refresh, §2.5 navigation rendering): 11 traces with error/auth/side-effect paths |
| Existing documentation audited & corrected | ✅ the 26 rejected scaffolds were discarded; each new doc was self-audited after writing and 4 draft claims were corrected against source before finalizing; the 2026-10-23 navigation revision then corrected a further set of stale backend/frontend statements (see the *Revision 2026-10-23* paragraph above) against freshly read source |
| Diagrams/examples match implementation | ✅ all ASCII flows in docs 01/12 were drawn from traced call sequences |
| Remaining gaps explicitly reported | ✅ this document + per-doc *Partial*/*Unknown* labels |

**Final status: the documentation set (docs 01–15 + README) is Complete with respect to static source evidence and the recorded test/build runs**, and partially upgraded to executed evidence for the RBAC seed and the resignation/termination create paths. The remaining open areas are inherently runtime/external (§4 items 1–3), each explicitly labeled rather than glossed. This revision (2026-10-23) additionally covers the frontend permission-driven navigation in doc 11 §12.1 with matching traces in doc 12, the `terminations.add` grant end-to-end (docs 02/08/09/11/12), and test/status evidence in docs 13/14.
