# Post-Redesign Review

Review date: 2026-10-25. Investigation only; no application, configuration, or database files were changed. The only new file is this report.

## Executive summary

The localization resource files are currently under `Resources/Localization/`, matching the `Manasa.Localization.SharedResource` marker and `ResourcesPath = "Resources"`. The build succeeds. Static inspection found all 289 neutral resource keys represented in the Arabic catalog, no duplicate keys, and no missing resource keys among the scanned Razor/controller/ViewModel references. The user reports Arabic is working in the running application; this review did not independently exercise the running application or browser.

No broken MVC destinations were identified in the inspected navigation links. Student course routes filter unpublished courses, and Student/Admin controller role attributes remain present. No automated test project exists in the checkout. The main confirmed concerns are unbounded Admin catalog lists, an inaccurate empty-state message referring to lesson creation, and forward arrows that remain pointed right in RTL. Additional account-state and setup-link workflow risks are supported by source but need policy/runtime context.

## Repository and verification state

- Git history has only the original project commits dated 2026-10-21; it does not provide a committed redesign baseline.
- Before creating this report, Git showed 41 modified tracked files, no staged changes, no deleted tracked files, and untracked Student/localization/assets/report files. Most untracked documentation and account-setup artifacts predate this review. `Properties/launchSettings.json` is also modified in the working tree; its change is not attributed to this redesign review.
- The English and Arabic resource catalogs are under `Resources/Localization/`. Static checks found 289 keys in each, no key differences or duplicates, no missing keys referenced in scanned views/controllers/ViewModels, and no suspicious encoding markers.
- Build: `dotnet build Manasa.csproj --no-restore -p:UseAppHost=false -p:OutputPath=.\obj\PostRedesignReviewVerify\` succeeded with 0 warnings and 0 errors.
- No test project or test source files were found; only `Manasa.csproj` is present.
- No app startup, browser/device inspection, database query, migration, or runtime authentication/CRUD test was run. Development startup executes `Database.MigrateAsync()` in `Program.cs`; avoiding that path was necessary to honor the no-database-change restriction.

### Working-tree inventory before this report was created

The tracked modified files were: `Areas/Admin/Controllers/{CoursePackages,Courses,Students,Subjects,Teachers}Controller.cs`; `Areas/Admin/Views/CoursePackages/{Form,Index}.cshtml`; `Areas/Admin/Views/Courses/{Form,Index}.cshtml`; `Areas/Admin/Views/Dashboard/Index.cshtml`; `Areas/Admin/Views/Shared/_AdminLayout.cshtml`; `Areas/Admin/Views/Students/{Form,Index}.cshtml`; `Areas/Admin/Views/Subjects/{Form,Index}.cshtml`; `Areas/Admin/Views/Teachers/{Form,Index}.cshtml`; `Areas/Admin/Views/_ViewImports.cshtml`; `Controllers/AccountController.cs`; `Program.cs`; `Properties/launchSettings.json`; `ViewModels/Account/{LoginViewModel,RegisterViewModel,SetPasswordViewModel}.cs`; `ViewModels/Admin/{CourseFormViewModel,CoursePackageFormViewModel,StudentViewModels,SubjectFormViewModel,TeacherFormViewModel}.cs`; `Views/Account/{AccessDenied,Login,Register,SetPassword,SetPasswordComplete}.cshtml`; `Views/Home/{Index,Privacy}.cshtml`; `Views/Shared/{Error,_Layout}.cshtml`; `Views/Shared/_Layout.cshtml.css`; `Views/_ViewImports.cshtml`; and `wwwroot/css/site.css`.

Untracked application/source files were: `.objStudentDashboardVerify/` (build artifacts); `Areas/Student/Controllers/DashboardController.cs`; `Areas/Student/Views/Dashboard/{Details,Index}.cshtml`; `Areas/Student/Views/{_ViewImports,_ViewStart}.cshtml`; `Controllers/LocalizationController.cs`; `Data/AccountSetupTokenProvider.cs`; `Localization/SharedResource.cs`; `Resources/Localization/{SharedResource.resx,SharedResource.ar.resx}`; `ViewModels/Student/DashboardViewModel.cs`; and `wwwroot/images/learning-illustration.svg`. Untracked reports/artifacts were `MANASA_STABILIZATION_BACKLOG.md`, `PROJECT_FULL_INVESTIGATION.md/.pdf`, `R02_SETPASSWORD_TOKEN_SECURITY_FIX.md/.pdf`, `RECENT_CHANGES_LAST_5_HOURS.md`, `REGISTRATION_FAILURE_FIX.md/.pdf`, `STUDENT_DASHBOARD_BLANK_PAGE.md`, and `STUDENT_DASHBOARD_READINESS.md`. The current report is also a new untracked file.

That status snapshot had no staged changes and no deleted tracked files. It is a working-tree snapshot, not a statement that all items were created in the same session.

## Prioritized findings

| ID | Status | Severity | Finding |
|---|---|---|---|
| PR-01 | Confirmed scale risk; impact unmeasured | P2, conditional on data growth | Several Admin catalog Index actions load every matching row. |
| PR-02 | Confirmed UX defect | P3 | Course empty state promises lesson creation, but no lesson feature exists in this checkout. |
| PR-03 | Confirmed RTL visual inconsistency | P3 | Forward arrow glyphs remain right-facing in Arabic layouts. |
| PR-04 | Confirmed workflow gap; failure occurrence unverified | P2 | Initial account setup links expire and there is no resend/reissue action; account creation commits before token generation. |
| PR-05 | Confirmed state coupling; intended policy unverified | P2, policy-dependent | Student administrative activation uses Identity lockout state. Teacher profile active state is not consistently tied to sign-in state. |
| PR-06 | Potential responsive issue; browser verification required | P3, unverified | Admin topbar has a fixed mobile height and no wrapping rule while containing several controls. |
| PR-07 | Potential display ambiguity; currency policy unknown | P3, conditional | Package prices use the current culture's currency format, but the model has no currency field or configured currency policy. |

### PR-01 — Admin catalog lists are unbounded

- **Evidence:** `Areas/Admin/Controllers/TeachersController.cs`, `Index`, materializes all matching teacher profiles with `ToListAsync()` around line 38. `SubjectsController.Index` similarly materializes all subjects around line 28 and includes both `Courses` and `SubjectTeachers` around lines 18–20. `CoursesController.Index` loads all matching courses around line 42. `CoursePackagesController.Index` loads all matching packages around line 35. By comparison, `StudentsController.Index` uses a paged list query.
- **Impact:** Query result size, memory use, and HTML response size grow with catalog size. The subject query's two collection includes also create a joined row multiplication risk at high fan-out.
- **Status boundary:** The unbounded query shape is confirmed; no production-scale dataset or query-plan measurement was available, so current user impact is unknown.
- **Smallest safe fix:** Add consistent server-side pagination to the four unbounded lists. Consider projecting the subject count fields or split querying when addressing the same list; preserve existing search/filter state.

### PR-02 — Course empty-state text advertises unavailable lesson creation

- **Evidence:** `Areas/Admin/Views/Courses/Index.cshtml`, around line 16, says “Add a course to start creating lessons and packages.” Repository search found no lesson entity, controller, route, or UI workflow; packages are implemented.
- **Impact:** The empty state promises an Admin capability that is not available.
- **Smallest safe fix:** Change the message to refer only to building the course catalog and packages, or another currently supported action.

### PR-03 — Forward arrows are not mirrored for RTL

- **Evidence:** `Areas/Student/Views/Dashboard/Index.cshtml` uses a literal `→` for the “View course details” action. `Areas/Admin/Views/Dashboard/Index.cshtml` uses `→` on quick-action links and management links. In `wwwroot/css/site.css`, RTL mirroring exists for `.student-direction-arrow`, but not these forward-arrow spans.
- **Impact:** The arrow direction conflicts with the RTL reading direction in Arabic, even when translations and document direction are correct.
- **Smallest safe fix:** Add a shared directional-icon class and mirror it under `[dir="rtl"]`, or use an appropriate direction-aware icon. Keep decorative arrows hidden from assistive technology.

### PR-04 — Setup-link expiration has no recovery action

- **Evidence:** `Data/AccountSetupTokenProvider.cs` sets `TokenLifespan` to one hour. `StudentsController.Create` and `TeachersController.Create` generate links only after committing the user/account transaction. `AccountController.SetPassword` tells users to request a new link from an administrator when it is invalid or expired. No reissue action was found in the Admin controllers.
- **Impact:** If token generation fails after commit, or if the manually shared link expires/is lost, the account can remain passwordless without an in-app recovery path. No occurrence was observed in this review.
- **Smallest safe fix:** Define an authorized, passwordless-account-only reissue flow. Do not broaden this review into implementation; confirm account lifecycle requirements first.

### PR-05 — Administrative account state shares Identity lockout

- **Evidence:** `StudentsController.Index` derives active state from `LockoutEnd` around line 55, and `ToggleActive` sets that field far into the future or clears it around lines 183–187. Login uses `PasswordSignInAsync(... lockoutOnFailure: true)` in `AccountController.Login` around lines 106–110, so the same field participates in failed-login lockout. In `TeachersController`, create/edit set `TeacherProfile.IsActive`, while the explicit `ToggleActive` action also changes Identity `LockoutEnd` around lines 188–192. Login does not inspect `TeacherProfile.IsActive`.
- **Impact:** Administrative activation can clear a security lockout, and teacher profile activity is not consistently enforced at sign-in. Whether these states are intended to be coupled is a product/security policy decision. The current checkout has no teacher workspace, so the impact on teacher-only features cannot be tested.
- **Smallest safe fix:** Define account deactivation versus transient lockout semantics before changing behavior; if they differ, store/enforce them independently.

### PR-06 — Admin topbar may overflow at narrow widths

- **Evidence:** `wwwroot/css/site.css` sets `.admin-topbar` to a fixed 54px height under 992px and leaves it as a single-line flex container. At widths below 721px, padding/gaps change, but no wrapping or alternate compact layout is specified. The Admin topbar contains context/site links, avatar, and a two-button language switcher in `_AdminLayout.cshtml`.
- **Status boundary:** This is a source-based risk, not a confirmed visual failure; no mobile browser/device test was run.
- **Smallest safe fix:** Verify at narrow phone widths in a browser, then add a compact/wrapping layout only if overflow is reproduced.

### PR-07 — Currency display depends on culture without a currency policy

- **Evidence:** `Areas/Admin/Views/CoursePackages/Index.cshtml` formats `Price` with `ToString("C")`. `CoursePackage` stores a price but no currency code; localization supports generic `en` and `ar` cultures.
- **Status boundary:** Culture-sensitive formatting is confirmed, but the intended product currency is unspecified. This is not classified as a confirmed defect.
- **Smallest safe fix:** Confirm one product currency and configure it explicitly, or store currency with the price if packages can have multiple currencies. Do not infer currency from the UI language.

## Localization, RTL/LTR, and navigation review

- `Program.cs` registers resource-based localization, supported cultures `en` and `ar`, and request localization before routing. The cookie provider is the only configured provider; `LocalizationController.SetLanguage` creates the framework culture cookie and only redirects to a local `returnUrl`.
- Public and Admin layouts derive `lang` and `dir` from `CurrentUICulture` and select the existing local Bootstrap RTL/LTR stylesheet. The Student area inherits the public layout. Static CSS inspection found logical properties for directional spacing and no hard-coded `left`/`right` positioning declarations in the inspected site styles.
- Both shared layouts preserve the current path and query in the language-switch form. POST endpoints use antiforgery validation. Role links are conditional in the public layout; endpoint authorization remains on Student/Admin controllers.
- Static link inspection found the Home, Account, Student Dashboard/details, and Admin navigation destinations represented by corresponding controllers/actions. No broken route was confirmed from source.
- The app contains user-entered course, subject, teacher, and profile content that is not translated. The resources localize interface text only; this is expected with the current data model.
- The user reports Arabic is working in the running application. This review found no static resource-key mismatch, but did not independently verify English↔Arabic switching, persistence, HTML attributes, or actual RTL layout in a browser.

## Authentication, authorization, Student, and Admin review

- Login retains local `ReturnUrl` validation. Without a local return URL, a Student who is not also an Administrator is directed to the Student Dashboard; other accounts retain the Home destination.
- Student Dashboard actions use `[Authorize(Roles = "Student")]`; course queries are `AsNoTracking()` and filter on `IsPublished`. Details use course ID plus `IsPublished` and return `NotFound` for missing/unpublished rows.
- Admin Dashboard and management controllers retain `[Authorize(Roles = "Administrator")]`; inspected mutating actions use POST and antiforgery validation.
- Account setup generation/validation continues using the dedicated provider and the same `AccountSetup` purpose; no password reset provider behavior change was found in this review.
- These checks are source-level only. No authentication, database-backed dashboard, Admin CRUD, or role-denial scenario was executed.

## Recommended order

1. Correct the misleading lesson wording and RTL forward-arrow presentation.
2. Decide the account deactivation/lockout policy and setup-link recovery requirement; address them in a separate approved change.
3. Add pagination before Admin catalog sizes make unbounded materialization costly; profile the subject query when representative data is available.
4. Browser-check the Admin topbar at phone widths and confirm currency presentation with product requirements.
