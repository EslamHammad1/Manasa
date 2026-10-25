# Registration Failure Investigation

## Result

The exact message, `We could not complete your registration. Please try again.`, is added in two places in `Controllers/AccountController.cs`:

- After `UserManager.AddToRoleAsync(student, "Student")` returns an unsuccessful `IdentityResult` (around lines 59–65). That branch rolls back the transaction and logs only the returned Identity error codes.
- In the `catch (Exception exception)` around the registration transaction (around lines 70–77). That branch logs the exception, then returns the generic message.

The repository contains no runtime log entry, exception details, or database state from the reported attempt. Therefore it is not possible to determine from available evidence whether the observed message came from the role-assignment failure or an exception, nor to confirm a specific root cause. No code fix was made because selecting one would be speculative.

## Traced registration path

1. `RegisterViewModel` and MVC model validation run first. Required name/email/password fields, email format and length, password length (6–100), matching confirmation, phone format, and accepted terms are checked. Invalid model state returns the form without entering the `try` block or displaying the generic message.
2. The action checks for an existing email. A match returns the specific message `An account with this email already exists.`
3. Inside a database transaction, `GetNextStudentUserNameAsync` executes `SELECT NEXT VALUE FOR dbo.StudentUsernameSequence` and checks for a username collision.
4. `UserManager.CreateAsync(student, model.Password)` validates Identity password and user requirements and persists the user. A failed `IdentityResult` rolls back and maps returned error codes into ModelState; this path does not display the generic message.
5. `UserManager.AddToRoleAsync(student, "Student")` persists the role link. A failed result rolls back, logs the role assignment error codes, and displays the generic message.
6. If role assignment succeeds, the transaction commits, the username is put in TempData, and the action redirects to Login. Registration does not generate or validate account setup tokens.

`Program.cs` configures Identity with the EF Core store and default token providers plus the separate `AccountSetupTokenProvider`. The separate setup provider is used by administrator-created accounts only; it is not called by registration and does not alter this registration path.

## Database and migration evidence

`ApplicationDbContext` defines the `StudentUsernameSequence`; migration `20261021134452_StudentUsernameSequence` creates it. The `AdminRoleSeed` migration inserts the `Student` role. This establishes what the checked-in migrations intend to create, but does not establish which migrations have been applied to the database used by the failed attempt. Startup automatically migrates only when `app.Environment.IsDevelopment()`; non-development deployments must have schema/roles provisioned separately.

User creation and role assignment occur inside the same explicit EF database transaction. The Identity result-failure branches explicitly roll back; exceptions unwind through async transaction disposal. No evidence is available to determine the persistence outcome if the failure happened during transaction commit or whether any earlier account from the reported attempt exists.

## Actual failure and missing evidence

The source establishes the two possible generic-message branches but not which one ran. The exception handler already logs the exception object and stack trace with `logger.LogError(exception, "Student registration failed.")`. The role failure branch logs its error codes. Neither log entry is present in this workspace.

The safest next diagnostic is to retrieve the server/application log entry for the failed attempt, redacting user identifiers and all secrets, and determine whether it is the registration exception log or the role-assignment error-code log. If it is an exception, its exception type and sanitized message/stack are needed. Also verify the applied migration level and presence of the Student role/username sequence on the configured database using read-only inspection. Do not run migrations as part of that diagnostic without approval.

## Follow-up diagnosis (2026-10-24)

- `WebApplication.CreateBuilder(args)` uses the standard ASP.NET Core host logging providers. No file logging provider or file sink is configured. The `Logging:LogLevel` settings in `appsettings.json` and `appsettings.Development.json` set thresholds only; they do not select a persistent log file. The exception log and role-assignment log therefore go to the active host console/debug/event providers (for a local IDE launch, typically the run/console and debugger output windows), subject to how that host was launched.
- No archived application `.log` files were found in the repository. A Manasa process is currently running, but its originating IDE/console output buffer is not accessible through this workspace. The original failed-attempt log entry is unavailable here; no runtime failure is claimed from the process being present.
- A read-only `sqlcmd` check against `(localdb)\\mssqllocaldb` did not connect (`Encryption not supported on the client` / `Client unable to establish connection`). `SqlLocalDB info mssqllocaldb` also reported an unexpected error accessing the LocalDB instance registry configuration. The `DB_ID` query did not reach SQL Server. Consequently, the existence of `ManasaLearning`, the Student role, the username sequence, and applied migration history remain unverified. No database writes or migrations were performed.
- The attempted account cannot be checked without a non-secret lookup identifier (for example, the email address used). No user list was queried, to avoid exposing unrelated personal data.
- Exact next step: retrieve the original run's console/debug/host log around the failed submission and provide the relevant exception type plus sanitized stack/message, or the Student-role error codes. Separately, run read-only role/sequence/`__EFMigrationsHistory` checks from the same Windows account and SQL tooling context that can access the app's configured LocalDB. If account existence must be checked, provide the exact account email privately for a targeted existence-only query; do not provide a password or token.

## Changes and verification

- No application code was changed because the root cause is not confirmed and changing the catch behavior or schema assumptions would conceal or guess at the failure.
- `REGISTRATION_FAILURE_FIX.md` documents this investigation and the exact evidence needed to select a fix.
- `dotnet build Manasa.csproj --no-restore` initially could not finish because the running application held `bin/Debug/net9.0/Manasa.exe` open (MSB3021/MSB3027).
- `dotnet build Manasa.csproj --no-restore -p:UseAppHost=false -p:OutputPath=.\obj\RegistrationVerify\` succeeded with 0 warnings and 0 errors. No test project was identified in the checkout.
- Runtime verification of successful registration, invalid form handling, and partial-account behavior could not be performed: the failed-attempt logs are unavailable and this environment could not connect to the development LocalDB. No migrations or destructive database operations were run.
