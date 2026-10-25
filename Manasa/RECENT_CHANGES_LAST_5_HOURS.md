# Manasa Recent Changes — Last Five Hours

Review time: 2026-10-25 16:27 (+03:00). This is an evidence-based reconstruction from the checkout, Git metadata, file timestamps, and source diffs. It does not imply that every edit was made by one person or within the exact five-hour window.

## 1. Executive Summary

The checkout contains a substantial platform UI redesign, a Student Dashboard and published-course details page, and English/Arabic localization with RTL/LTR layout selection. Admin management pages have been restyled and localized, account and form validation/status text has been localized, and a local learning illustration was added. The Student Dashboard uses existing published course data; no enrollment, payment, lesson, or progress features were added in this work.

Git history contains only the original project commits from 2026-10-21. There are no staged changes. The tracked working-tree diff contains 41 modified files and no deleted files. Numerous new files are untracked, including the Student area, localization infrastructure and resources, illustration, and investigation reports. Most tracked UI/localization files have 2026-10-25 timestamps between 15:32 and 16:02, which places them within the approximate review window. `Properties/launchSettings.json` is timestamped 2026-10-24 and is outside it. Earlier report files and the account-setup token provider also predate the window. Timestamps and the absence of intervening commits prevent reliable attribution of every change to the exact five-hour period.

## 2. Changed Files Inventory

### Tracked working-tree modifications

| File paths | Type | Change evidenced by the current diff |
|---|---|---|
| `Program.cs` | Modified | Adds localization services, view and DataAnnotations localization, culture configuration/middleware, and registration of the dedicated account-setup token provider. Existing development startup and Identity configuration remain present. |
| `Controllers/AccountController.cs` | Modified | Localizes account feedback and Identity errors; includes the Student default post-login destination, dedicated setup-token validation, and direct asynchronous sequence execution. The diff combines changes from prior account-related tasks and the recent localization pass. |
| `Controllers/LocalizationController.cs` | New | POST language-selection action, culture cookie, antiforgery validation, and safe local return URL redirect. |
| `Areas/Admin/Controllers/{CoursePackages,Courses,Students,Subjects,Teachers}Controller.cs` | Modified | Localizes validation and status feedback. CRUD behavior is retained in the inspected diff. |
| `Areas/Student/Controllers/DashboardController.cs` | New | Student-role-protected dashboard and course-details actions query published courses without tracking. Details are limited to published courses. |
| `Data/AccountSetupTokenProvider.cs` | New/untracked | Dedicated one-hour account setup token provider from the earlier R-02 work; outside the five-hour timestamp window. |
| `ViewModels/Account/{LoginViewModel,RegisterViewModel,SetPasswordViewModel}.cs` | Modified | Adds localized display names to account fields. |
| `ViewModels/Admin/{CourseFormViewModel,CoursePackageFormViewModel,StudentViewModels,SubjectFormViewModel,TeacherFormViewModel}.cs` | Modified | Adds localized display names for Admin form fields. |
| `ViewModels/Student/DashboardViewModel.cs` | New | View models for the greeting, published-course cards, and course details. |
| `Localization/SharedResource.cs` | New | Shared marker type for resource-based localization. Its namespace/resource-path alignment is discussed under Known Issues. |
| `Resources/SharedResource.resx`, `Resources/SharedResource.ar.resx` | New | Neutral English and Arabic resource catalogs. The catalogs currently have matching keys, but file placement versus the marker type namespace requires correction. |
| `Views/_ViewImports.cshtml`, `Areas/Admin/Views/_ViewImports.cshtml`, `Areas/Student/Views/_ViewImports.cshtml` | Modified/new | Makes the shared localizer available to the corresponding Razor views. |
| `Views/Shared/_Layout.cshtml`, `Views/Shared/_Layout.cshtml.css` | Modified | Public navigation/footer, language switcher, culture-driven `lang`/`dir` and Bootstrap CSS selection, and public shell styling. |
| `Areas/Admin/Views/Shared/_AdminLayout.cshtml` | Modified | Admin workspace shell, navigation, culture-driven direction/framework stylesheet, and language switcher. |
| `Views/Home/{Index,Privacy}.cshtml` | Modified | Landing and privacy-placeholder pages restyled and localized. |
| `Views/Account/{AccessDenied,Login,Register,SetPassword,SetPasswordComplete}.cshtml` | Modified | Authentication/account pages restyled and UI text localized. Registration’s existing form behavior is retained. |
| `Views/Shared/Error.cshtml` | Modified | Restyles and localizes the generic error page. |
| `Areas/Student/Views/Dashboard/{Index,Details}.cshtml` | New | Student catalog cards, empty state, and published-course details page. |
| `Areas/Admin/Views/Dashboard/Index.cshtml` | Modified | Restyles the existing Admin metrics, recent courses, and quick actions; data remains sourced from the existing model. |
| `Areas/Admin/Views/{Students,Teachers,Subjects,Courses,CoursePackages}/{Index,Form}.cshtml` | Modified | Restyles lists and forms, localizes labels/status/empty-state text, and uses existing data and routes. Teacher and course images have visual fallbacks. |
| `wwwroot/css/site.css` | Modified | Shared visual system, responsive public/auth/Student/Admin layouts, focus and reduced-motion styling, and RTL-aware rules. |
| `wwwroot/images/learning-illustration.svg` | New | Local learning illustration used on the landing and account pages. |
| `Properties/launchSettings.json` | Modified | Working-tree diff adds/reorders launch profiles and IIS Express settings. Its 2026-10-24 timestamp indicates it is outside the approximate five-hour window; the diff does not establish who made it or why. |

### Other untracked files and artifacts

- `MANASA_STABILIZATION_BACKLOG.md`, `PROJECT_FULL_INVESTIGATION.md` and `.pdf`, `R02_SETPASSWORD_TOKEN_SECURITY_FIX.md` and `.pdf`, `REGISTRATION_FAILURE_FIX.md` and `.pdf`, `STUDENT_DASHBOARD_BLANK_PAGE.md`, and `STUDENT_DASHBOARD_READINESS.md` are investigation/planning artifacts from earlier tasks, not application UI changes in this window.
- `.objStudentDashboardVerify/` is an untracked build-output directory; it is not source code.
- Git reports no deleted paths and no staged changes. The resources, localization controller, Student area, Student view model, and SVG are untracked rather than staged additions.

## 3. UI/UX Changes

- Public pages: home, privacy placeholder, shared navigation/footer, and generic error page.
- Authentication: access denied, login, registration, and initial password setup/completion pages. The account pages use a split visual panel and local illustration.
- Student: role-protected dashboard with a published-course list, empty state, and details page with existing subject/teacher/description/thumbnail fields.
- Admin: existing dashboard and Student, Teacher, Subject, Course, and Course Package list/form pages. The existing CRUD forms and routes remain visible in the source.
- Shared styling: navy/blue palette, responsive cards/forms/tables, focus styles, reduced-motion handling, local Bootstrap assets, and a local SVG. Course data and Admin dashboard counts are not fabricated.

## 4. Localization Changes

- `Program.cs` registers localization with `ResourcesPath = "Resources"`, view localization, and DataAnnotations localization using `SharedResource`.
- Request localization sets English as default, supports `en` and `ar`, and uses `CookieRequestCultureProvider` before routing/endpoints.
- `LocalizationController.SetLanguage` accepts `en`/`ar`, writes the framework culture cookie via `MakeCookieValue`, and redirects only to a local `returnUrl` (otherwise Home). Both shared layouts submit the current path and query string.
- The Razor import files inject `IStringLocalizer<SharedResource>`. Account/Admin controller feedback also uses the shared localizer. Resource key comparison found 289 keys per file, no key differences, and no duplicate keys.
- User-entered course titles, descriptions, and profile text are displayed as entered; the existing schema does not provide translated content.

## 5. RTL/LTR Changes

- Both shared layouts derive `lang` from the current UI culture’s two-letter language code and `dir` from `TextInfo.IsRightToLeft`.
- They load the existing local Bootstrap RTL CSS for RTL cultures and LTR CSS otherwise.
- `site.css` uses logical spacing in most layout rules, includes RTL handling for the Admin shell and directional course navigation, and defines responsive breakpoints. `Properties/launchSettings.json` does not configure culture; the cookie/request-localization path controls culture.

## 6. Navigation and Functional Changes

- The Student navigation link is shown for the Student role; the Admin navigation link remains Administrator-only.
- Login preserves a validated local `ReturnUrl`; absent one, Student-only accounts route to the Student Dashboard and other accounts retain the existing Home destination.
- Student course cards route to details; both dashboard and details source only `IsPublished` courses. Existing Admin navigation routes to its dashboard and management sections remain in place.
- Language forms post to the localization action and preserve the current local path/query on redirect.

## 7. Known Issues

- Arabic selection was reported as not working correctly. Phase 1 identified a resource-base-name mismatch: `Program.cs` sets `ResourcesPath` to `Resources`, the marker type is `Manasa.Localization.SharedResource`, but the resource files were initially at `Resources/SharedResource*.resx`. The initial build embedded `Manasa.Resources.SharedResource.resources` and `Manasa.Resources.SharedResource.ar.resources`, which do not include the marker namespace segment. Phase 2 below records the correction and verification.
- The code-derived language switcher/cookie path appears internally consistent, but an HTTP/browser test of culture persistence and the rendered Arabic pages has not yet been performed.
- Static reference scanning found no missing string keys in the current English or Arabic resource catalogs. This does not prove that every user-facing string is localized or that translations render correctly.

## 8. Potential Risks and Dependencies

- Actual browser behavior, server logs, and database-backed pages depend on a running application and database. They are not established by the working tree alone.
- The language cookie is essential, HttpOnly, SameSite Lax, and marked Secure only on HTTPS requests. The current development launch profiles include HTTP; whether that transport is used is environment-dependent.
- Bootstrap RTL assets are already present locally; no new package or external image dependency is introduced by these changes.
- `Properties/launchSettings.json` is a working-tree change outside the estimated time window. It should not be attributed to the UI/localization work without further evidence.

## 9. Verification Status

- Inspected Git history, tracked diffs, untracked paths, current project configuration, localization controller, marker type, Student controller, and compiled resource manifest names.
- The most recent prior build in this checkout used a separate output path and succeeded with zero warnings and zero errors. No automated test project was found.
- No browser/device inspection, language-switch HTTP test, login test, Admin CRUD test, database query, or runtime log check was performed for this report.
- No application code was modified while preparing this Phase 1 report.

## Phase 2 Addendum — Arabic Resource Lookup Fix

Phase 1 was completed, saved, and checked for required sections before application files were changed.

### Confirmed root cause and fix

`IStringLocalizer<SharedResource>` derives its resource base name from the marker class namespace and the configured `ResourcesPath`. The marker is `Manasa.Localization.SharedResource` and `Program.cs` configures `Resources`; therefore the culture files need to be under `Resources/Localization/`. Before the fix they were at the root of `Resources/`, and the compiled resource manifest confirmed the omitted `Localization` segment. The localizer consequently could not find the Arabic resource set and silently returned the neutral resource values/resource keys.

The two resource files were moved without changing their contents:

- `Resources/SharedResource.resx` → `Resources/Localization/SharedResource.resx`
- `Resources/SharedResource.ar.resx` → `Resources/Localization/SharedResource.ar.resx`

No controller, culture-cookie, authentication, authorization, business, or database code was changed in Phase 2.

### Verification after the fix

- `dotnet build Manasa.csproj --no-restore -p:UseAppHost=false -p:OutputPath=.\\obj\\ArabicLocalizationVerify\\` succeeded with 0 warnings and 0 errors.
- The compiled neutral manifest is now `Manasa.Resources.Localization.SharedResource.resources`; the Arabic satellite manifest is `Manasa.Resources.Localization.SharedResource.ar.resources`. Both match the marker namespace and localization resource path.
- Rechecked the resource catalogs: 289 keys in each, no key differences, and no missing localizer references in the scanned views/controllers/view models.
- A local Production-mode HTTP smoke test could not complete. Startup reported that existing Data Protection keys could not be decrypted in this environment and that Windows Event Log writes were denied; the Home request then failed in the exception/logging path. The process was stopped. No Development startup, migration, or database operation was run.
- Consequently, actual browser language switching, cookie persistence/refresh, and rendered Arabic pages were not runtime-verified. Culture cookie formatting, local redirect validation, middleware order, `lang`/`dir`, and RTL Bootstrap selection were reviewed in source.
