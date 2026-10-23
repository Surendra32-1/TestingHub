# 10 · Error Handling and Logging

Scope: `src/TeamHub.API/Responses/**` (3 files, read in full), the pipeline registration in
`src/TeamHub.API/Program.cs`, the four Application-layer exception types, the throw sites that
reach them, and the file/image helpers in `src/TeamHub.Shared/Common/Services/**` (3 files, read in
full) because they are the largest remaining source of runtime exceptions.

All statements are **Verified** against the cited source unless labelled *Inferred*, *Unknown*, or
*Recommended*.

---

## 1. Where errors are produced and where they are converted

| Stage | What can fail | Who handles it |
| --- | --- | --- |
| Routing / model binding | bad route value, unparseable JSON, `[FromQuery]` type mismatch | MVC → `InvalidModelStateResponseFactory` (`ApiErrorHandlingExtensions.cs:13-46`) |
| Validation | FluentValidation (`default` ruleset) + DataAnnotations + `IValidatableObject` | same ModelState factory (`Program.cs:90` enables it) |
| Action body (manual validation) | `EmployeesController.Create` `"Create"` ruleset | the action builds its own ProblemDetails (`EmployeesController.cs:74-84`) |
| Authorization | missing/invalid token, unmet policy/role | ASP.NET → `AddProblemDetails` customiser (`ApiErrorHandlingExtensions.cs:49-53`) |
| Service / repository | workflow, RBAC, `SqlException`, file I/O | `ApiExceptionHandler` (`Program.cs:92`, `:113`) |
| Unhandled | anything else | `ApiExceptionHandler` default arm → 500 |

Pipeline (`Program.cs:113-118`): `app.UseExceptionHandler()` is first, so the handler wraps every
later stage, including `UseAuthentication`/`UseAuthorization`.

```csharp
// Program.cs:90-92
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApiErrorHandling();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
```

`AddApiErrorHandling` (`ApiErrorHandlingExtensions.cs:8-56`) calls `AddControllers()` again (see
[09-api-endpoints.md](09-api-endpoints.md) §11 finding 13) and installs the two customisers.

**Verified:** there is **no** `IExceptionFilter`, no `UseStatusCodePages`, no
`MapHealthChecks`, and no `AddLogging` call (searched all `*.cs` in `src/`; the only `ILogger`
injection in the solution is `ApiExceptionHandler.cs:9`). Logging configuration is therefore the
host default driven by `appsettings.json:2-7` (`Default: Information`,
`Microsoft.AspNetCore: Warning`) — identical in `appsettings.Development.json:2-7`.

---

## 2. The error envelope

```csharp
// ApiProblemDetailsFactory.cs:22-31
public static void AddResponseFields(ProblemDetails problem, string? message = null)
{
    var responseMessage = message ?? problem.Detail ?? problem.Title ?? "An error occurred.";
    problem.Extensions["success"] = false;
    problem.Extensions["message"] = responseMessage;
    problem.Extensions["data"] = null;
    problem.Extensions["error"] = responseMessage;
    problem.Extensions.TryAdd("errors", new Dictionary<string, string[]>());
}
```

A failure therefore serialises as (camelCase, RFC 9110 members first):

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Duplicate value",
  "status": 409,
  "detail": "A record with this value already exists. Please use a unique value.",
  "instance": "/api/departments",
  "success": false,
  "message": "A record with this value already exists. Please use a unique value.",
  "data": null,
  "error": "A record with this value already exists. Please use a unique value.",
  "errors": {}
}
```

**Verified properties of the contract:**

* `message` == `error` == `detail` in every path except the validation ones, where `message` is the
  space-joined **distinct** error list and `detail` is either `"One or more validation errors
  occurred."` (auto path, `ApiErrorHandlingExtensions.cs:30`) or the same joined list (manual path,
  `EmployeesController.cs:73, :79`).
* `errors` is `Dictionary<string, string[]>` keyed by **property name**; `TryAdd` (`:30`) means a
  caller that already populated `errors` keeps its values — the manual employee path relies on this
  (`EmployeesController.cs:83` assigns `problem.Extensions["errors"]` *after*
  `AddResponseFields`, so the overwriting assignment, not `TryAdd`, wins). **Verified ordering.**
* `data` is always `null` — the frontend types it as the DTO and reads `response.data` only on
  success (`rbac.service.ts:27`).
* The envelope is **not** applied to `AuthController`'s hand-built problems
  (`AuthController.cs:230-236`) — see `08-auth-and-rbac.md` §9.
* `Type` URIs are `https://httpstatuses.com/{status}` in the two factory paths
  (`ApiProblemDetailsFactory.cs:11`, `ApiExceptionHandler.cs:53`) and the manual employee path
  (`EmployeesController.cs:76`); framework-generated 401/403/404 problems keep ASP.NET's own
  `https://tools.ietf.org/html/rfc9110…` types while still receiving the five extensions via
  `CustomizeProblemDetails` (`ApiErrorHandlingExtensions.cs:51-52`). **Verified** mismatch in `type`
  URIs between controller-authored and framework-authored errors.

### The 404 helper

Every business controller has a private one-line builder (named `P`, `NotFoundProblem`,
`NotFoundDetails`, or `CreateNotFoundProblemDetails` — see `09-api-endpoints.md` §4):

```csharp
// JobTypesController.cs:31-34 (representative)
private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
    "Job type not found", $"Job type with ID {id} was not found.", HttpContext.Request.Path);
```

`NotFound` (`ApiProblemDetailsFactory.cs:7-20`) sets `status = 404` and passes `detail` through as
the message. **Verified:** these 404s are *returned from the action*, not thrown, so they never
reach `ApiExceptionHandler` and are never logged.

---

## 3. Exception → status mapping (`ApiExceptionHandler.cs:21-47`)

```csharp
var (status, title, detail) = exception switch
{
    SqlException { Number: 2601 or 2627 } => (409, "Duplicate value",      "A record with this value already exists. Please use a unique value."),
    SqlException { Number: 547 }          => (409, "Related record conflict", "This record is referenced by other records and cannot be changed or deleted."),
    ReleaseWorkflowException workflowEx   => (409, "Invalid status transition", workflowEx.Message),
    RbacValidationException rbacValidationEx => (400, "Invalid assignment",  rbacValidationEx.Message),
    PrivilegeEscalationException privilegeEx => (403, "Insufficient privileges", privilegeEx.Message),
    _ => (500, "Request failed", "An unexpected error occurred while processing your request.")
};
```

| Status | Trigger | Verified throw sites |
| --- | --- | --- |
| 409 Duplicate | SQL error 2601 (unique index) / 2627 (unique constraint) | any repository INSERT/UPDATE hitting `UQ_*` — e.g. `UQ_Users_Username`, `UQ_BusinessUnits_BusinessUnitName` (`databasesign.sql:680`), `UQ_Permissions_PermissionCode` (`rbac-schema.sql:47`) |
| 409 Related record | SQL error 547 (FK) | master-data `DELETE` of a referenced row (`07-repositories.md`), `RefreshTokens` FKs, `Users.EmployeeId` |
| 409 Invalid transition | `ReleaseWorkflowException` | `ReleaseStatusWorkflow.EnsureTransition` / `EnsureInitial` (`Workflows/ReleaseStatusWorkflow.cs`) — thrown by `EmployeeReleaseService` **and** `EmployeeTerminationService` (there is no separate termination exception type; see `06-application-services.md`) |
| 400 Invalid assignment | `RbacValidationException` | `RolePermissionMatrixService` + `UserRoleRepository.SaveAsync`/`RolePermissionRepository.SaveAsync` (`07-repositories.md`) |
| 403 Insufficient privileges | `PrivilegeEscalationException` | `UserRoleMatrixService` escalation guard (`06-application-services.md`) |
| 500 Request failed | everything else | see §5 |

**Verified characteristics:**

* Message text for the three custom exceptions is **passed through to the client verbatim**. The
  `SqlException` arms use fixed, safe wording — no driver message leakage. *Recommended*: keep it
  that way; nothing in the code logs the SQL error text to the response, but `logger.LogError`
  (`:51`) writes the whole exception (including SqlException details) to the log, which is correct.
* `TerminationStatusWorkflow` reuses `ReleaseWorkflowException`, so a bad termination transition is
  titled **"Invalid status transition"** with the message
  `…release/termination…` depending on the workflow's own string. **Verified** (single type, two
  workflows) — *Recommended*: rename to `StatusWorkflowException`.
* Nothing maps `OperationCanceledException`. If the client aborts, Dapper surfaces a cancellation
  that falls into the 500 arm — except that `TryHandleAsync` bails first:

  ```csharp
  // ApiExceptionHandler.cs:16-19
  if (httpContext.RequestAborted.IsCancellationRequested) { return false; }
  ```
  Returning `false` means "not handled", so the exception propagates to `UseExceptionHandler`,
  which detects the aborted connection and writes nothing. **Verified** correct behaviour for the
  common abort case; an `OperationCanceledException` *not* caused by the client (e.g. a command
  timeout with `RequestAborted` unset) still becomes a logged 500.
* There is **no** 404 mapping for "not found" exceptions — every not-found case is a controller
  `return NotFound(...)` (§2). A service that returned `null` where the controller forgot to check
  would produce a 200 with `data: null` rather than a 404. **Verified** that all read actions do
  check, except `GetAll`-style actions where an empty list is legitimate.

---

## 4. Validation failure paths in detail

### 4a. Automatic (14 master-data + `CotivityEmployee` + Permissions DTOs)

FluentValidation auto-validation feeds `ModelState`; the custom factory builds a 400:

* `title` = `"Validation failed"`, `status` = 400, `detail` = `"One or more validation errors
  occurred."` (`ApiErrorHandlingExtensions.cs:27-31`)
* `message` = the distinct error strings joined with a space (`:34-42`), empty-list fallback
  `"One or more validation errors occurred."` (`:41`)
* `errors` = `ModelState` grouped by key → array of messages, with blank messages normalised to
  `"The supplied value is invalid."` (`:20-22`)

Route-constraint/binding failures land in the **same** factory because they are `ModelState` errors.
**Verified** consequence: `GET /api/job-types?pageSize=abc` returns 400 with the extended envelope
rather than the framework's default `ValidationProblemDetails`.

### 4b. Manual (`EmployeesController.Create` only)

`EmployeesController.cs:63-85`: builds a `ValidationContext` with
`new RulesetValidatorSelector(new[] { "default", "Create" })`, validates, and on failure groups
`ValidationResult.Errors` by `PropertyName` (`:71-72`). Note this runs **in addition to** the
automatic `default`-ruleset validation that already passed, so a create request is validated twice
and the auto pass reports first if a `default` rule fails (`:87` never runs). **Verified** from the
two independent entry points (`Program.cs:90` + `:63-67`); *Inferred* reason: the MVC filter cannot
select a second ruleset. See [05-validators.md](05-validators.md).

### 4c. Controller-level business rule (one case)

`CotivityEmployeesController.cs:52-69` builds a 400 ProblemDetails inline for
"An employee cannot report to themselves." with a hand-written `errors` dictionary. It calls
`AddResponseFields(problem)` **without** a message argument (`:63`), so `message`/`error` fall back
to `problem.Detail` — a subtle difference from the other paths, **verified**.

### 4d. What validation does *not* cover

* Lifecycle **state transitions** (enforced at service level → 409, `06-application-services.md`).
* `PUT /api/employee-releases/{id}` and `PUT /api/employee-terminations/{id}` bypass the workflow
  entirely and can set an arbitrary status string that the POST path rejects
  (`07-repositories.md`, `09-api-endpoints.md` §6/§7). Severity: **high**.
* No error taxonomy/versioning constant exists — the strings are literals scattered over 22
  controllers. **Verified.** *Recommended*: a shared `ErrorMessages`/`ProblemType` constant set if
  clients begin keying off `title`.

---

## 5. Verified non-mapped exceptions (all become 500)

The default arm hides the real cause from the client (`ApiExceptionHandler.cs:43-46`), which is the
correct posture, but the underlying failures are worth listing because they are reachable:

| Exception | Origin | Effect |
| --- | --- | --- |
| `Exception("Error Saving File.", ex)` | `FileExtension.ToFileUrl` `:70-73` — wraps **everything**: bad base64, `NotSupportedException("Unsupported image format.")` `:104/:112`, ImageSharp decode failure, disk/write errors, `ArgumentNullException` from `_env` | 500 on `POST`/`PUT /api/Employees` and `POST/PUT /api/CotivityEmployees` after the row may already have been inserted (the image write happens in the service **after** `repository.CreateAsync`, `EmployeeService.cs:97-115` — **Verified** orphaned employee with no image and a 500 response) |
| `Exception("Error reading file and converting to base64.", ex)` (wraps `FileNotFoundException`) | `FileHelper.GetBase64StringFromFile` `:47-58`, called by `EmployeeRepository.ToBase64DataUrl` (`:946-965`) and `CotivityEmployeeRepository` `:131-149` | **500 on reads.** `ToBase64DataUrl` has no `try`/`catch` (`:954`), so one missing image file on disk fails the *entire* unpaginated employee grid request, not just that row. Severity: **high**. *Recommended*: degrade to `null`/`imagePath` per row |
| `Exception("Error Deleting Files. Check Logs", ex)` | `FileExtension.DeleteFile` `:87-90` | 500; **note there is no delete-file call in any current employee/release/termination write path** — verified: `DeleteFile` is never called from `src/` outside its own definition, so image rows/files are never removed (append-only) |
| `InvalidOperationException("IWebHostEnvironment has not been initialized.")` | `FileExtension.cs:44-47`, `FileHelper.cs:39-42` | only if `Program.cs:110-111` (`FileExtension.Initialize`, `FileHelper.Initialize`) did not run; **Verified** they run at startup before `app.Run()`, so this is unreachable through HTTP — but it makes both helpers **static-with-state**, untestable without a host |
| `NotSupportedException("Unsupported file extension.")` | `FileExtension.GetEncoder` `:127-136` | 500 (mapped by nothing) |
| `TaskCanceledException` / `SqlException` (timeout 1205, 2587, …) | Dapper / SQL Server | 500. **Verified:** only 2601/2627/547 are mapped, so a deadlock (1205) surfaces as `500 "Request failed"` rather than a retryable 409/503 |

### Swallowed errors (silent failures)

| Site | Behavior | Verified consequence |
| --- | --- | --- |
| `FileHelper.ProcessImagePaths` `:91-94` | `catch (Exception ex) { Console.WriteLine(...); }` per path, continues | unreadable images vanish from the response with no log entry beyond stdout; **method has no caller in `src/`** (verified) so this is currently dead code |
| `FileHelper.ProcessImagePaths` `:97-100` | outer `Console.WriteLine` | same |
| `EmployeeRepository.AssignEmployeeRolesAsync` `:926` | `WHERE r.IsActive = 1` drops inactive role ids silently | request succeeds, roles missing (`07-repositories.md`) |
| `EmployeeReleaseRepository.CreateAsync` `:110` | `IF NOT EXISTS (…) RETURN;` | no exception, no row, service reports success (`07-repositories.md`) |
| `EmployeeService.GetEmployeeMastersAsync` `:271-274` | drops the `CancellationToken` | client abort cannot cancel the 14-result-set query |

*Recommended*: replace the `Console.WriteLine` swallows with injected logging; the only `ILogger`
in the whole solution is in `ApiExceptionHandler`.

---

## 6. Frontend error consumption (verified)

```csharp
// master-settings.ts:279 (representative)
this.toastService.error(getApiErrorMessage(error, `Could not save ${this.config?.singular?.toLowerCase()}.`));
```

The SPA has one error-message reader, `getApiErrorMessage(error, fallback)` in
`shared/services/api-error-message.ts` (47 lines), used by **26 call sites in 10 files** across the
employee, master-settings and RBAC features (counted by enumerating the imports and call sites; the
remaining hits are the import lines themselves). Its precedence order,
read from `:35-46`, is:

1. `error.error.errors` — the per-field dictionary, flattened recursively
   (`messagesFrom` `:7-26` walks strings, arrays and object values), de-duplicated and joined with
   spaces (`:35-38`); each value is a `Dictionary<string, string[]>` serialised as
   `{ "departmentName": ["…"] }`, so the **field messages win over everything else**.
2. `error.error.error` — the `error` extension string (`:40-43`).
3. `error.error.message`, else `error.error.detail` — first element only (`:45-46`).
4. The caller's own `fallback` string.

`error` here is the Axios-style `HttpErrorResponse`; `:30` unwraps `outer['error']` and falls back to
the response object itself, so a non-ProblemDetails body (HTML, proxy error) still yields the
fallback rather than `undefined`.

Consequences, **Verified**:

* `message` is what users normally see for non-validation failures, and the **field** errors are what
  users see for 400s — the `detail` constant `"One or more validation errors occurred."` never
  reaches the toast when `errors` is populated.
* Because step 1 flattens *all* object values, an `errors` dictionary that contained nested
  ProblemDetails-like objects would leak unrelated strings; today it only ever holds
  `string[]` per key, so the behavior is as intended.
* 401 on any non-auth call is intercepted, refreshed once, and only then redirected
  (`auth.interceptor.ts:19-35`) — so a 401 never reaches a component toast.
* 403 reaches the component and shows the framework-generated problem (which *does* carry the
  extensions, `ApiErrorHandlingExtensions.cs:51-52`).
* The employee form does not map `errors` keys onto individual controls — it passes the whole
  dictionary through `getApiErrorMessage` as one sentence
  (`employee-add-form.ts:370`). **Verified**; *Recommended*: index `error.error.errors` per control
  so messages appear under the offending field.

**Verified gap:** the SPA does not branch on `status` for the 409 duplicate/conflict cases —
`master-settings.ts:279` shows whatever `message` the server produced, which is the intended UX, but
there is no special handling for "related record conflict" versus "duplicate value" beyond the text.
**Unknown** whether richer handling was planned. See [11-frontend.md](11-frontend.md).

---

## 7. Logging and diagnostics summary (verified)

| Concern | State |
| --- | --- |
| Failure log | one call: `logger.LogError(exception, "API request failed with status {StatusCode}.", status)` (`ApiExceptionHandler.cs:51`) — full exception incl. stack trace, structured status code |
| Success/request logging | none (no middleware, no `AddRequestLogging`) |
| Correlation id / `TraceIdentifier` | never written into the problem (`ApiProblemDetailsFactory` has no `traceId`) — **Verified**, so a client-reported error cannot be tied to a log line except by timestamp + path (`instance`) |
| Serilog / file sink / external sink | **none** — no such package in any `.csproj` (verified against `TeamHub.API.csproj` and `obj/project.assets.json`) |
| Health check endpoint | none (`MapHealthChecks` absent) |
| Response caching headers | only `Cache-Control: no-store` on token responses (`AuthController.cs:62, :119`) |
| Swagger errors | `[ProducesResponseType]` declared only on `AuthController` (`09-api-endpoints.md` §10) |
| Manual error probes | `TeamHub.API/auth.http` and `TeamHub.API.http` request collections (`13-testing.md`) |

*Recommended* (not current behavior): add `ProblemDetails.Extensions["traceid"] =
HttpContext.TraceIdentifier` inside `AddResponseFields` so 500s are investigable; add a health
endpoint since the DB connection is the app's single hard dependency
(`DatabaseConnectionTests.cs` proves the string but nothing checks it at runtime).

---

## 8. Verified findings (error handling)

1. **A missing image file breaks whole read endpoints** (`EmployeeRepository.cs:954` has no
   per-row guard; grid is unpaginated). Severity: high.
2. **Image-write failure after a successful INSERT** leaves an orphaned row plus a 500
   (`EmployeeService.cs:97-115`, no transaction spanning DB + disk). Severity: medium-high.
3. **Only 3 of the SQL error families are mapped**; deadlocks, timeouts, permission errors all
   become opaque 500s. Severity: medium.
4. **No `traceId` in the error envelope** while logs contain the full exception — correlation is by
   timestamp/path only. Severity: medium (operational).
5. **`TerminationStatusWorkflow` throws `ReleaseWorkflowException`**, so termination errors are
   titled with release wording. Severity: low.
6. **Four different names for the same 404 helper** and four ProblemDetails construction styles
   (factory / manual / inline / framework). Severity: low (consistency).
7. **Employee create validates twice** (auto `default` + manual `default,Create`) — cost only, no
   behavioral difference. Severity: cosmetic.
8. **`Console.WriteLine` used for error reporting** in `FileHelper` (`:93, :99`) and its exceptions
   are swallowed. Severity: low (currently unreached code).
9. **`AddApiErrorHandling` calls `AddControllers()` a second time** (`:10`) while
   `AddApplicationPart` was attached elsewhere (`TeamHubAuthServiceCollectionExtensions.cs:129`).
   Works today; fragile ordering. Severity: low.
10. **Silent-success write paths** (FK guard `RETURN`, dropped inactive roles, dropped cancellation
    token) produce no error at all — the hardest class of failure to see from the client.
    Severity: medium. See `07-repositories.md`.

---

Related: [01-architecture.md](01-architecture.md) · [05-validators.md](05-validators.md) ·
[06-application-services.md](06-application-services.md) · [07-repositories.md](07-repositories.md) ·
[08-auth-and-rbac.md](08-auth-and-rbac.md) · [09-api-endpoints.md](09-api-endpoints.md) ·
[11-frontend.md](11-frontend.md) · [14-known-issues.md](14-known-issues.md)
