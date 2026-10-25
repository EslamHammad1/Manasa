# R-02: SetPassword Token Security Review

## Scope and conclusion

This review covers only the administrator-created student/teacher password setup workflow and `AccountController.SetPassword`. The setup link remains a bearer credential carried in its query string. A dedicated Identity Data Protection token provider now limits setup-token validity to one hour without changing ordinary password-reset token behavior. Production request logging, browser/device handling, and link-sharing practices are outside this repository and remain unverified.

## Workflow and code locations

1. `Areas/Admin/Controllers/StudentsController.cs` creates a passwordless student with `UserManager.CreateAsync(student)`, assigns the Student role, commits, then calls `GenerateUserTokenAsync` with the dedicated account-setup provider and purpose. It places the generated link in the TempData key `AccountSetupUrl` and the created username in TempData.
2. `Areas/Admin/Controllers/TeachersController.cs` creates a passwordless teacher, role, profile, and subject links, commits, then generates a token with the same dedicated provider and purpose and stores the setup link in TempData.
3. `Areas/Admin/Views/Students/Index.cshtml` and `Areas/Admin/Views/Teachers/Index.cshtml` render the link for an authenticated administrator to open and manually share. Both state it is not sent by email.
4. `Controllers/AccountController.cs` exposes an anonymous GET at `SetPassword(userId, token)`. It looks up the specified user and returns 404 if the account does not exist or already has a password. Otherwise it places the user ID and token into `SetPasswordViewModel`.
5. `Views/Account/SetPassword.cshtml` carries both fields in hidden inputs on an antiforgery-protected POST. The POST looks up that user again, rejects a missing/already-passworded account, verifies the token with the same provider and purpose for that user, then calls `AddPasswordAsync` to add a password to the passwordless account. Successful setup renders `SetPasswordComplete`.

## Token behavior and findings

| Question | Finding |
|---|---|
| Provider | `Program.cs` registers `AccountSetupTokenProvider` separately from `.AddDefaultTokenProviders()`. Student and teacher creation use `GenerateUserTokenAsync` with provider name `AccountSetupTokenProvider` and purpose `AccountSetup`; `AccountController.SetPassword` calls `VerifyUserTokenAsync` with those same values. The default password-reset provider remains configured for ordinary reset workflows. |
| Effective lifetime | Before this change, setup used the default password-reset provider, whose default Data Protection token lifetime was one day. Now `Data/AccountSetupTokenProvider.cs` configures only the new setup provider to one hour. Ordinary password-reset tokens continue to use the default one-day provider behavior. The default is documented in [Microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-identity/account-confirmation-and-password-recovery?view=aspnetcore-6.0) and the [Identity API reference](https://learn.microsoft.com/pt-br/dotnet/api/microsoft.aspnetcore.identity.dataprotectiontokenprovideroptions.tokenlifespan?view=aspnetcore-9.0). |
| User and purpose binding | `VerifyUserTokenAsync` validates the token with the setup provider for the `user` instance found by submitted user ID and for purpose `AccountSetup`. The Data Protection token includes user ID, purpose, issue time, and (when supported) security stamp. Tokens for another user or purpose do not validate. See [Identity's provider implementation](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/DataProtectorTokenProvider.cs) and [UserManager implementation](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserManager.cs). |
| Reuse after success | The POST rejects a user with a non-empty `PasswordHash`. `AddPasswordAsync` only adds a password to a passwordless account and updates the password/security stamp through Identity, making the setup token unusable afterwards. |
| Invalid/expired token | The setup provider verification returns false; the controller shows a generic invalid-or-expired message and does not call `AddPasswordAsync`. Identity handles password validation errors from `AddPasswordAsync` as before. |
| Missing user | Both GET and POST return 404 for a missing user; already-passworded users also receive 404. |
| App-level disclosure | The repository contains no explicit log statement that writes the setup URL or token. The controller displays Identity error descriptions, not the submitted token. The setup view references local CSS/JavaScript and has no external resource URL. The token is intentionally present in the generated URL, the administrator page, and the setup form's hidden field. |
| Resend/recovery | No regeneration or resend mechanism exists. A newly generated link can be made only by an administrator workflow that creates a new account; there is no implemented recovery action for an existing passwordless account. Adding that workflow would exceed the requested scope and is not needed to establish a token-validation defect. |

## Confirmed weaknesses and unverified risks

**Confirmed design exposure:** the token is a bearer credential in a query-string URL and is displayed to an administrator for manual sharing. Anyone who obtains a still-valid link can attempt to set the intended account's password. The one-hour lifetime reduces the window in which a copied or stolen link can be used; it does not prevent use while the token remains valid. Tokens are user/purpose-bound and invalidated after successful password setup.

**Not confirmed from source:** whether production proxy/web-server/APM logs retain query strings, whether the link is copied to an unintended recipient, or how endpoint/browser history is protected. The app source has no request-logging configuration specific to this route. No deployment or infrastructure configuration was available to inspect. Do not infer that these external systems leak tokens from the mere fact that a query parameter is used.

**No additional confirmed code defect:** the GET is anonymous by design so the invited user can reach the setup page, while the POST is antiforgery-protected and Identity validates the token for the specified account. The public GET by itself does not set a password. Token validation is delegated to Identity, and both actions check the account state.

## Root cause and fix

Previously, the setup link used the same password-reset token provider and one-day lifetime as ordinary password reset. The minimal compatible improvement was to register a separate Data Protection token provider with a one-hour lifespan and use it only for account setup. Generation and verification use matching provider name and purpose; after verification, `AddPasswordAsync` applies the password only to a still-passwordless account. Ordinary Identity reset behavior is unchanged. The link format and administrator manual-sharing workflow are preserved.

## Files changed

- `Data/AccountSetupTokenProvider.cs` — defines the distinct setup token provider and its one-hour lifetime.
- `Program.cs` — registers that provider alongside the unchanged default providers.
- `Areas/Admin/Controllers/StudentsController.cs` and `Areas/Admin/Controllers/TeachersController.cs` — generate setup links with the dedicated provider and purpose.
- `Controllers/AccountController.cs` — verifies setup tokens with that same provider and purpose, then uses Identity's `AddPasswordAsync` for the passwordless account.
- `R02_SETPASSWORD_TOKEN_SECURITY_FIX.md` — records the revised findings, implementation, and verification.

## Verification

- Source inspection traced both admin account creation flows, provider registration, generation and verification calls, `SetPasswordViewModel`, setup views, and relevant cookie configuration.
- Framework token lifetime/provider behavior was checked against Microsoft documentation and ASP.NET Core Identity source.
- `dotnet build Manasa.csproj --no-restore` — succeeded after the code change with 0 warnings and 0 errors.
- No test project or existing focused tests were found in this checkout. Runtime tests for successful setup, invalid/expired tokens, wrong-user tokens, and ordinary password-reset lifetime were not executed. Provider names/purpose and lifetimes were verified by code inspection. No database migration or destructive operation was run.

## Remaining concerns and next steps

- Treat setup links as secrets: share only with the intended user over an approved channel and avoid recording or forwarding the full URL. A stolen link remains usable until its one-hour expiry or successful setup.
- Verify that production reverse proxies, web servers, and observability tools redact query strings for the setup endpoint, and review their retention/access controls.
- If policy requires shorter validity or reliable recovery, define those requirements before changing token lifetime or adding an administrator reissue flow.
