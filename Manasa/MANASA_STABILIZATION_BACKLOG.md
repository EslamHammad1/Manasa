# Manasa Stabilization Backlog

Review date: 2026-10-24

This is a source-based stabilization review. No application code, configuration, migrations, or database objects were changed. Runtime/database evidence is called out separately; the development LocalDB was not accessible in this environment, so database behavior beyond the previously reported sequence exception was not re-exercised.

## Prioritized findings

| ID | Title | Status | Severity / priority |
|---|---|---|---|
| STAB-01 | Public registration and login have no app-level rate limits | Confirmed code gap; deployment mitigations unverified | High / P1 conditional |
| STAB-02 | Demo administrator provisioning depends on environment isolation | Unconfirmed deployment risk; code is development-gated | High / P1 conditional |
| STAB-03 | Admin-created student username generation still composes `NEXT VALUE FOR` | Confirmed code defect | High / P1 for admin student creation |
| STAB-04 | Teacher inactive state is not consistently enforced for sign-in | Confirmed behavior inconsistency | Medium / P2 |
| STAB-05 | Student deactivation reuses Identity failed-login lockout state | Confirmed behavior; policy/impact needs decision | Medium / P2 |
| STAB-06 | Admin accounts can be committed before setup-link generation | Confirmed failure mode; occurrence unverified | Medium / P2 |
| STAB-07 | Terms acceptance is validated but not retained | Confirmed implementation gap; obligation depends on policy | Medium / P2 conditional |
| STAB-08 | Admin catalog lists materialize unbounded result sets | Confirmed scale risk; current impact unmeasured | Medium / P2 at expected growth |
| STAB-09 | Subject list loads two sibling collections in one query | Confirmed query shape; impact depends on fan-out | Medium / P2 at scale |
| STAB-10 | Password rules differ between public registration and setup | Confirmed mismatch; policy is unspecified | Medium / P2 policy decision |
| STAB-11 | Setup links have no reissue/recovery path | Confirmed workflow gap | Medium / P2 |
| STAB-12 | Student, teacher and catalog searches use substring matching | Confirmed query pattern; performance impact unmeasured | Low / P3 until measured |
| STAB-13 | Terms of sign-in identity are not verified by email | Confirmed behavior; requirement unconfirmed | Medium / P2 policy decision |
| STAB-14 | Password-setup completion text is stale | Confirmed UX defect | Low / P3 |
| STAB-15 | Student-facing dashboard is absent | Confirmed product gap; feature work, not a stabilization fix | Product backlog |
| STAB-17 | Setup-token provider scope and lifetime | Resolved in source; URL-handling risk remains | Closed in code / residual operational check |
| STAB-18 | Public-registration sequence SQL error | Fixed in source; runtime DB verification remains | Resolved in source |
| STAB-20 | Setup-link host and Data Protection key persistence depend on deployment | Unconfirmed deployment risk | Medium / P2 conditional |

## Findings

### STAB-01 — Public registration and login have no app-level rate limits

- **Status:** Confirmed code gap; whether a reverse proxy/WAF supplies equivalent controls is unverified.
- **Priority:** P1 conditional. Public registration can be automated for account spam; login is also public, though Identity lockout-on-failure provides a separate control.
- **Evidence:** `Program.cs` has no rate-limiter services or middleware/policies. `Controllers/AccountController.cs` exposes anonymous registration and login GET/POST actions. The login POST calls `PasswordSignInAsync(... lockoutOnFailure: true)`; that does not rate-limit registration or all login traffic.
- **Reproduction/verification:** Inspect application middleware and deployment edge controls; verify request counts and behavior on a staging endpoint before rollout.
- **Minimal fix:** Define policies for registration and login, add ASP.NET Core rate limiting, and configure proxy/client-IP handling correctly. Keep account lockout and generic login errors.
- **Dependencies/order:** Decide registration openness and abuse thresholds; coordinate with deployment owners. First security workstream after environment controls are verified.

### STAB-02 — Demo administrator provisioning depends on environment isolation

- **Status:** Unconfirmed deployment risk. No unsafe deployment was verified.
- **Priority:** P1 conditional because a public deployment classified as Development could run migrations and seed configured privileged accounts.
- **Evidence:** `Program.cs` runs migration, role setup, and `DevelopmentDataSeeder.SeedAccountsAsync` only inside `app.Environment.IsDevelopment()`. `Data/DevelopmentDataSeeder.cs` creates two Administrator identities if the corresponding `DemoSeed:*Password` values exist. No demo passwords are in checked-in `appsettings.json` or `appsettings.Development.json`; local launch profiles use Development. Production environment variables, secret stores, and environment classification are outside the repository.
- **Reproduction/verification:** Confirm the actual deployment environment value and whether any `DemoSeed:*Password` secret is present in that deployment without printing it.
- **Minimal fix:** If production isolation cannot be guaranteed operationally, require an explicit separate opt-in for demo seeding or fail startup when Development is used in a public deployment. Do not add real credentials to config.
- **Dependencies/order:** Deployment owner verification first; do not change source based only on unknown hosting settings.

### STAB-03 — Admin-created student username generation still composes `NEXT VALUE FOR`

- **Status:** Confirmed code defect.
- **Priority:** P1 for the Admin student-create workflow: SQL Server rejects the composed sequence expression, preventing this path from creating students.
- **Evidence:** `Areas/Admin/Controllers/StudentsController.cs`, `GetNextStudentUserNameAsync()`, calls `.SqlQueryRaw<long>("SELECT NEXT VALUE FOR dbo.StudentUsernameSequence AS [Value]").SingleAsync()`. EF Core composes `SingleAsync` over raw SQL; SQL Server then sees `NEXT VALUE FOR` inside a derived query/subquery, which its error explicitly disallows. The sequence itself is valid: `Data/ApplicationDbContext.cs` declares `HasSequence<long>("StudentUsernameSequence")`, and migration `Data/Migrations/20261021134452_StudentUsernameSequence.cs` creates it. The same old query in public registration was changed to direct async enumeration in `Controllers/AccountController.cs`; this Admin copy was not updated.
- **Reproduction/verification:** On a test database with the sequence present, invoke Admin student creation and confirm the original SQL exception occurs before the user is created. Do not test against production data.
- **Minimal fix:** Use direct async enumeration of the raw SQL query, as in `AccountController.GetNextStudentUserNameAsync()`, without composing `SingleAsync`. Preserve username formatting and the existing uniqueness check.
- **Dependencies/order:** Can be fixed independently; high priority because it blocks an existing admin workflow. Verify no account remains after failure.

### STAB-04 — Teacher inactive state is not consistently enforced for sign-in

- **Status:** Confirmed behavior inconsistency; enforcement intent is evident from the toggle action, but there is no teacher workspace in this checkout.
- **Priority:** P2. A teacher marked inactive through create/edit can still authenticate; practical access impact is currently limited because no teacher-only workspace exists.
- **Evidence:** `Areas/Admin/Controllers/TeachersController.cs`, `Create(TeacherFormViewModel)`, sets `TeacherProfile.IsActive = model.IsActive`, but creates the Identity user with `LockoutEnabled = true` and no future `LockoutEnd` when `IsActive` is false. `Edit(int, TeacherFormViewModel)` updates `profile.IsActive` without synchronizing Identity lockout. `ToggleActive(int)` does synchronize both. `AccountController.Login` checks Identity sign-in state; no code checks `TeacherProfile.IsActive` during sign-in.
- **Reproduction/verification:** Create or edit a teacher with Active profile unchecked and attempt sign-in. Confirm Identity accepts credentials if no other lockout applies. This was not run in this review.
- **Minimal fix:** Define one source of truth for inactive access. Either synchronize lockout on every create/edit/toggle path or implement a consistent sign-in authorization check. Avoid duplicating state without synchronization.
- **Dependencies/order:** Decide teacher inactivity semantics before teacher-facing features are implemented. Pair with STAB-05 state-model decision.

### STAB-05 — Student deactivation reuses Identity failed-login lockout state

- **Status:** Confirmed behavior; whether this coupling is acceptable is a product/security policy decision.
- **Priority:** P2. Admin activation can clear an Identity lockout caused by failed sign-in attempts; temporary lockout can also make the admin list show a student as inactive.
- **Evidence:** `Areas/Admin/Controllers/StudentsController.cs`, `Index`, derives `IsActive` from `LockoutEnd`. `ToggleActive` uses that same field to infer current state, sets it about 100 years ahead to deactivate, and clears it to activate. `Program.cs` enables Identity lockout-on-failure through `PasswordSignInAsync` in `AccountController.Login`.
- **Reproduction/verification:** Lock a test student through failed login attempts, inspect the Admin list, then invoke Activate. Observe that `LockoutEnd` is cleared. Not executed here.
- **Minimal fix:** Separate administrative active status from transient Identity lockout, or formally define that administrator activation is allowed to clear a lockout and adjust list labels/actions accordingly.
- **Dependencies/order:** Requires account-state policy; coordinate with teacher active-state behavior (STAB-04).

### STAB-06 — Admin accounts can be committed before setup-link generation

- **Status:** Confirmed failure mode; no occurrence was observed.
- **Priority:** P2. An exception after the transaction commits can leave a passwordless account with no delivered setup link.
- **Evidence:** Student creation in `Areas/Admin/Controllers/StudentsController.cs` commits the account/role transaction before calling `GenerateUserTokenAsync` and building the TempData link. Teacher creation in `Areas/Admin/Controllers/TeachersController.cs` similarly commits user, role, profile and subject links before token generation. There is no reissue action.
- **Reproduction/verification:** Inject a controlled token-provider failure in a test environment after commit, then verify the account remains and no link is available. Do not induce this on live data.
- **Minimal fix:** Provide a safe administrator reissue path for passwordless accounts and clear failure feedback. Do not hold a database transaction open across external token/HTTP delivery work.
- **Dependencies/order:** Account-lifecycle requirements and admin UX; related to STAB-11.

### STAB-07 — Terms acceptance is validated but not retained

- **Status:** Confirmed implementation gap; legal/product retention requirement is unconfirmed.
- **Priority:** P2 conditional. The app cannot later establish which terms/version a user accepted.
- **Evidence:** `ViewModels/Account/RegisterViewModel.cs` requires `AcceptTerms` via `MustBeTrueAttribute`; `Controllers/AccountController.cs` does not copy that value to `ApplicationUser` or another entity. `Models/ApplicationUser.cs` has no acceptance fields; current migration snapshot has no consent record.
- **Reproduction/verification:** Register and inspect persisted user data; no acceptance evidence will exist. Runtime DB check not performed.
- **Minimal fix:** If required, persist accepted timestamp and terms version during the same registration transaction.
- **Dependencies/order:** Legal/product confirmation; schema change only if retention is required.

### STAB-08 — Admin catalog lists materialize unbounded result sets

- **Status:** Confirmed scale risk; current performance impact is unmeasured.
- **Priority:** P2 once catalog/person counts grow; P3 for a small seed dataset.
- **Evidence:** `Areas/Admin/Controllers/TeachersController.cs`, `SubjectsController.cs`, `CoursesController.cs`, and `CoursePackagesController.cs` Index actions call `ToListAsync()` on all matching rows. Only `StudentsController.Index` bounds results to 25; Admin dashboard recent courses are bounded to 6.
- **Reproduction/verification:** Load representative large datasets in a non-production benchmark and measure SQL, memory, response time, and rendered HTML size.
- **Minimal fix:** Add consistent server-side pagination and project only displayed fields; preserve filters/sort in page links.
- **Dependencies/order:** Expected data volume and admin UX; implement before catalog sizes become large.

### STAB-09 — Subject list loads two sibling collections in one query

- **Status:** Confirmed query shape; impact depends on how many courses and teacher assignments each subject has.
- **Priority:** P2 at scale.
- **Evidence:** `Areas/Admin/Controllers/SubjectsController.cs`, `Index`, includes both `Subject.Courses` and `Subject.SubjectTeachers`, then materializes full entities. EF Core single-query collection includes can multiply joined rows for sibling collections. View only displays the two counts, not the related entity data.
- **Reproduction/verification:** Inspect generated SQL and query plans or run a representative high-fan-out benchmark; runtime profiling was not available.
- **Minimal fix:** Project subject rows with count subqueries or use `AsSplitQuery`; combine with pagination from STAB-08.
- **Dependencies/order:** Query-plan/data-volume measurement; can be implemented with list pagination.

### STAB-10 — Password rules differ between public registration and setup

- **Status:** Confirmed model/UI mismatch; desired security policy is unspecified.
- **Priority:** P2 policy decision. Public registration allows a six-character minimum at the view-model layer; administrator-created accounts require eight. Identity’s password policy is not explicitly configured in `Program.cs`, so effective server requirements rely on framework defaults.
- **Evidence:** `ViewModels/Account/RegisterViewModel.cs` has `[StringLength(100, MinimumLength = 6)]`; `ViewModels/Account/SetPasswordViewModel.cs` has minimum 8. `Program.cs` configures unique email but no `IdentityOptions.Password` policy. `CreateAsync(user,password)` and `AddPasswordAsync` still apply Identity validators, so UI attributes are not the only checks.
- **Reproduction/verification:** Compare client/server validation with passwords of lengths 6–7 and the effective Identity options in a no-database unit test. Not run.
- **Minimal fix:** Set an explicit product-approved Identity password policy and align both view models/client validation to it; do not weaken policy to eliminate validation errors.
- **Dependencies/order:** Security/product policy decision before implementation.

### STAB-11 — Setup links have no reissue/recovery path

- **Status:** Confirmed workflow gap.
- **Priority:** P2. Setup links expire after one hour or may be lost; passwordless admin-created accounts then have no current setup link and there is no reissue action.
- **Evidence:** `Data/AccountSetupTokenProvider.cs` sets one-hour lifetime. `Areas/Admin/Controllers/StudentsController.cs` and `TeachersController.cs` generate links only during account creation. No resend/reissue action or email service exists. `AccountController.SetPassword` rejects already-passworded users and invalid tokens, but does not issue replacements.
- **Reproduction/verification:** Let a setup token expire or discard it; inspect Admin actions for recovery. No runtime token tests were executed.
- **Minimal fix:** Add an Administrator-authorized action that verifies the account is still passwordless and issues a replacement setup link through the same one-hour provider.
- **Dependencies/order:** Account lifecycle and safe link-sharing UX; pair with STAB-06.

### STAB-12 — Substring searches can become expensive

- **Status:** Confirmed query pattern; performance impact unmeasured.
- **Priority:** P3 until representative query plans/data sizes show impact.
- **Evidence:** Student, teacher, subject, course and package Index actions use `.Contains(term)` over usernames/names/emails/titles. This commonly produces leading-wildcard searches that ordinary B-tree indexes cannot seek efficiently.
- **Reproduction/verification:** Inspect generated SQL/query plans on representative data and measure common search terms.
- **Minimal fix:** Paginate first; only introduce prefix/full-text search or dedicated indexes after query-plan evidence and product search requirements.
- **Dependencies/order:** Scale and search UX; measurement before schema changes.

### STAB-13 — Registration does not verify email ownership

- **Status:** Confirmed behavior; whether email verification is a requirement is unconfirmed.
- **Priority:** P2 policy decision. Users may register an address they do not control; account recovery/contact workflows based on email would be unreliable.
- **Evidence:** `AccountController.Register` creates users without setting `EmailConfirmed`; `Program.cs` does not require confirmed accounts. No email sender or confirmation action was found. Admin-created accounts are explicitly marked confirmed in `DevelopmentDataSeeder`, but use supplied demo values only.
- **Reproduction/verification:** Register with an address not controlled by the registrant; current flow can still proceed to login if Identity password validation succeeds. Not run.
- **Minimal fix:** If required by product, implement confirmation delivery and configure Identity to require confirmation before sign-in. Do not treat this as a defect until policy is decided.
- **Dependencies/order:** Email service, account lifecycle, and product policy.

### STAB-14 — Account setup completion message is stale

- **Status:** Confirmed UX defect.
- **Priority:** P3.
- **Evidence:** `Views/Account/SetPasswordComplete.cshtml` says the user can sign in “when the login page is available”, but `AccountController.Login` and `Views/Account/Login.cshtml` already exist.
- **Reproduction/verification:** Complete a valid setup link and read the completion page; not run in this review.
- **Minimal fix:** Replace the text with current sign-in guidance and a sign-in link.
- **Dependencies/order:** None; small copy fix.

### STAB-15 — Student-facing dashboard is absent

- **Status:** Confirmed missing feature, not a code-rendering defect.
- **Priority:** Product backlog; student login currently redirects to the generic Home page unless a local ReturnUrl is supplied.
- **Evidence:** `AccountController.Login` has no Student-role destination. There is no Student area/controller/view. `Views/Home/Index.cshtml` is non-empty starter content. The only dashboard is `[Authorize(Roles = "Administrator")]` in `Areas/Admin/Controllers/DashboardController.cs`.
- **Reproduction/verification:** Sign in without ReturnUrl and inspect destination; database/browser runtime was not available in this review.
- **Minimal fix:** Product-defined Student landing endpoint/view and role-aware post-login default, preserving valid local ReturnUrl handling. This is feature work and was not implemented.
- **Dependencies/order:** Student dashboard scope/content; schedule after stabilization decisions.

## Reviewed items that are not open code defects

### STAB-17 — Dedicated account setup token provider

- **Status:** Resolved in source; residual bearer-link exposure remains.
- **Evidence:** `Program.cs` registers `AccountSetupTokenProvider` in addition to default providers. Admin student/teacher creation generates with that provider and purpose; `AccountController.SetPassword` verifies using the same name/purpose/user and uses `AddPasswordAsync`. `Data/AccountSetupTokenProvider.cs` sets one-hour lifetime. The ordinary default reset provider is not reconfigured.
- **Residual risk:** The manually shared token remains in a URL and can be used until expiry or successful setup; proxy logs, host logging, and link-sharing practices need deployment review. `Program.cs` does not configure a shared/durable Data Protection key ring, so multi-instance or ephemeral-host token validation depends on deployment defaults. Do not call it risk-free or fully eliminated.

### STAB-20 — Setup-link host and Data Protection key persistence depend on deployment

- **Status:** Unconfirmed deployment risk; no deployment topology was available.
- **Priority:** P2 conditional. A malformed/untrusted Host reaching the application could affect the absolute setup URL generated for manual sharing; non-shared or ephemeral Data Protection keys can make generated links fail on another instance or after restart.
- **Evidence:** `appsettings.json` sets `AllowedHosts` to `*`. `StudentsController.Create` and `TeachersController.Create` use `Url.ActionLink` without an explicit canonical host. `Program.cs` does not configure Data Protection key persistence or sharing. The framework/deployment may configure host filtering and key storage outside this repository.
- **Reproduction/verification:** Verify proxy host validation and public host routing; verify key-ring persistence and cross-instance sharing using a non-production setup account. No production/runtime test was performed.
- **Minimal fix:** If ingress does not already enforce a trusted host, set an allowlist or configure a canonical public origin for setup links. Configure durable shared Data Protection keys only if hosting topology requires them.
- **Dependencies/order:** Deployment topology and ingress configuration; coordinate with STAB-02 and STAB-17 operational checks.

### STAB-18 — Public registration sequence SQL error

- **Status:** Resolved in source; runtime database verification remains outstanding.
- **Evidence:** `Controllers/AccountController.GetNextStudentUserNameAsync()` now uses `AsAsyncEnumerable()` to execute raw `SELECT NEXT VALUE FOR` without composing `SingleAsync` into a derived table. The sequence declaration/migration are valid and unchanged. The earlier reported SQL exception matches the previous composed-query shape.
- **Verification:** Code path inspected and project has compiled in a prior review using a separate output path. LocalDB was unavailable, so successful registration against SQL Server was not runtime-verified.
- **Related open defect:** The distinct Admin student-create method in `Areas/Admin/Controllers/StudentsController.GetNextStudentUserNameAsync()` still calls `.SingleAsync()` on the raw sequence query; track under STAB-03, not as an unresolved registration issue.

## Suggested implementation order

1. **Deployment/security verification:** STAB-02 environment isolation and STAB-01 edge/application throttling.
2. **Existing workflow blockers/state integrity:** STAB-03 Admin sequence query; STAB-04 teacher inactive enforcement; STAB-05 student lockout/deactivation semantics.
3. **Account lifecycle:** STAB-06 and STAB-11 setup reissue/recovery, then STAB-10 password policy and STAB-13 email verification decisions.
4. **Consent and UX:** STAB-07 retention decision and STAB-14 completion copy.
5. **Scale/performance:** STAB-08 pagination, STAB-09 subject projection/split query, and measure STAB-12 before schema/search changes.
6. **Separate product scope:** STAB-15 Student dashboard after requirements are defined.

## Verification performed and limits

- Static inspection covered `Program.cs`, account controllers/view models, setup provider, development seeder/configuration, admin controllers/views, entity model configuration, migrations/snapshot, and earlier investigation reports.
- No migrations, database queries, or destructive operations were run.
- No automated test project was found in the repository.
- A build was not run during this review because the application has been running and this workspace previously encountered a locked executable during build. Runtime sign-in, setup-token, data-integrity and performance scenarios remain unverified.
- The list of open items distinguishes source-confirmed behavior from deployment/policy-dependent risk and unmeasured performance impact; it is not a claim that every conditional risk is exploitable in production.
