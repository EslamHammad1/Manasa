# Student Dashboard Readiness

Review date: 2026-10-25

## Scope and conclusion

This is a source-only review for planning a Student Dashboard. No application code or database schema was changed, and no database or runtime behavior was tested. Findings below distinguish code-confirmed behavior from requirements that need product or deployment decisions.

The project has Identity login, Student role assignment, an Administrator-only management area, and an administrative course catalog. It does not currently have a student landing area, student-owned learning records, enrollment, lessons, assignments, or progress tracking. The smallest useful dashboard can welcome the signed-in student and list published courses from the existing catalog, but “my courses” or progress features cannot be implemented from existing data.

## Existing functionality and code locations

### Authentication, authorization, and routing

- `Program.cs` registers ASP.NET Core Identity with EF Core storage and default token providers, configures the application cookie, then runs authentication and authorization middleware. The cookie uses `.Manasa.Auth`, is HttpOnly, SameSite Lax, lasts eight hours with sliding expiration, and is Secure in non-development environments.
- `Controllers/AccountController.cs`, `Register(POST)`, creates an `ApplicationUser` and assigns the `Student` role. The login POST accepts username or email and calls `PasswordSignInAsync(..., lockoutOnFailure: true)`.
- After successful login, `AccountController.Login(POST)` redirects to a local `ReturnUrl` when supplied; otherwise it redirects every role to `Home/Index`. It has no role-specific landing destination.
- The default route in `Program.cs` is `/{controller=Home}/{action=Index}/{id?}`. The area route defaults to `/Admin/Dashboard/Index` for area routes.
- `Areas/Admin/Controllers/DashboardController.cs` and the other Admin controllers require `[Authorize(Roles = "Administrator")]`. `Views/Shared/_Layout.cshtml` only displays the Administration link to users in that role.
- No Student area/controller or Student-authorized dashboard route exists. `Views/Home/Index.cshtml` is the generic welcome page and is not role-restricted.
- `Areas/Admin/Controllers/StudentsController.cs` is Administrator-only; its queries identify student accounts through the Identity role join. It supports account listing, editing, activation/deactivation, and creation, not student learning activity.

### Student-related data and relationships

- `Models/ApplicationUser.cs` extends Identity and stores optional `DisplayName` and `CreatedAtUtc`. Student membership is represented through Identity's user-role relationship, not a Student profile entity.
- `Data/ApplicationDbContext.cs` exposes `Subjects`, `TeacherProfiles`, `SubjectTeachers`, `Courses`, and `CoursePackages`. It configures course-to-subject, optional course-to-teacher, and package-to-course relationships. It defines no enrollment, lesson, assignment, submission, or progress DbSet/entity/relationship.
- `Models/Course.cs` stores title, description, optional thumbnail, subject, optional teacher profile, publication flag, and creation time.
- `Models/CoursePackage.cs` stores a package's course, description, price, access duration, and active flag. No purchase, entitlement, or enrollment record links a user to a package/course.
- `Models/Subject.cs`, `Models/TeacherProfile.cs`, and `Models/SubjectTeacher.cs` provide catalog organization and teacher associations. They do not reference students.
- `Data/DevelopmentDataSeeder.cs` may provision a demo Student when a local development password setting is provided; this is development seed behavior, not a student learning-data source.
- The checked-in EF model/migrations define the catalog and Identity structures visible in the current model. This review did not inspect a live database, so it does not assert that any deployed database is at a particular migration level or contains particular records.

### Course and learning functionality

- `Areas/Admin/Controllers/CoursesController.cs` lets Administrators create and edit courses, including `IsPublished`, and constrain course subject/teacher assignments. Its catalog list is also Admin-only.
- `Areas/Admin/Controllers/CoursePackagesController.cs` manages course packages. Packages describe price and access duration but are not connected to purchases or student access.
- `Areas/Admin/Controllers/DashboardController.cs` shows aggregate student/teacher/catalog counts and recent courses to Administrators. It is not student-facing and does not show enrollment or activity.
- Repository searches excluding generated assets and migrations found no application code for enrollment, lesson delivery, assignment, submission, or learning progress.
- The model contains a sample course seeded with `IsPublished = false`; therefore published-course list content is not guaranteed to exist in a fresh development database.

## What student data is available

Available per student: Identity user ID, username, email, optional display name and phone number, account creation time, confirmation/lockout fields, and Student role membership. Admin student list also derives a status from `LockoutEnd`.

Available globally for a catalog view: courses and their subject, optional teacher profile, publication state and description; subjects; and course packages with active/price/access-duration fields.

Not available: a student's enrollment/access entitlement, purchased package, course completion, lesson state, assignments, submissions, grades, activity history, or progress. Do not infer any of these from role membership, package existence, or account lockout state.

## Minimum viable dashboard using existing functionality

The narrow MVP can be a Student-role-protected landing page that:

1. Greets the current account using claims/Identity user data (prefer a server-side lookup keyed by the authenticated user ID if the display name is not present as a claim).
2. Lists only courses explicitly marked `IsPublished`, with title, subject and existing description/thumbnail where appropriate.
3. Provides an empty state when no courses are published.
4. Does not label courses as enrolled, purchased, assigned, or in progress; no such relationship exists.

This requires a small Student controller/route, a view model, a Razor view, and a query against existing catalog entities. It does not require a schema change. Whether the catalog should show inactive subjects, inactive teacher profiles, or packages/prices is not settled by current student-facing requirements; the safest MVP is to filter on `Course.IsPublished` and avoid promising package access. A published course currently does not imply that its content or access system exists.

## Confirmed concerns and open requirements

### Confirmed from source

- Students have no role-specific login destination and no Student dashboard route; successful default login goes to generic Home.
- There is no Student authorization policy/controller boundary for a dashboard because no such endpoint currently exists.
- There is no student-owned course/enrollment or learning-progress data to display.
- Catalog management is Administrator-only. There is no current public/student course listing to reuse directly.
- `StudentsController.ToggleActive` uses Identity `LockoutEnd` as administrative activation state, and the student list derives status from that field. This can conflate account deactivation with failed-login lockout. A dashboard must not use this field as a substitute for enrollment or progress. Changing its semantics is outside dashboard readiness and needs a separate policy decision.

### Requirements or deployment evidence not established

- Whether the dashboard should be the default destination for all students or only linked from navigation.
- Whether students may browse all published courses, only courses they have purchased, or only courses assigned by an administrator. Current data supports only a global published catalog.
- Whether course details/content, payments, enrollments, assignments, or progress are planned and their domain rules.
- Whether production role provisioning and account activation policies match development settings. This source review does not establish deployed configuration or database contents.

## Authorization and student-data isolation requirements

- Protect the Student dashboard endpoint with `[Authorize(Roles = "Student")]` (or an equivalent named policy) and verify role membership server-side; hiding a link in Razor is not authorization.
- Do not grant Student access to `/Admin/*`; the existing Admin role attributes should remain in place. If login becomes role-aware, retain the `Url.IsLocalUrl` check and ensure a student-supplied local `ReturnUrl` does not bypass endpoint authorization.
- Use the authenticated Identity user ID from server-side claims as the owner key for any future student-specific query. Never take a target student ID from a query/form parameter as the authorization boundary.
- Keep global published catalog data separate from private student records. If enrollment/progress is later added, constrain every read and mutation to both the signed-in user's ID and the relevant record, with appropriate database foreign keys and tests.
- Do not expose email, phone, lockout details, or other account data on a shared/student page unless a clear need exists. Encode output through Razor and use antiforgery on state-changing forms.

## Recommended implementation plan

1. **Confirm product boundaries:** decide default student landing behavior and whether MVP is a global published catalog or requires student-specific enrollment. If enrollment is required now, define the enrollment/access lifecycle before building UI; existing schema cannot support it safely.
2. **Add the smallest protected endpoint:** create a Student controller/action guarded for the Student role and an explicit route/link. Keep Admin controller authorization unchanged.
3. **Build a narrowly scoped query/view model:** query no-tracking published courses and their subject; select only fields required by the page. Use the current authenticated user ID for personalized greeting/account data. Avoid package-access claims.
4. **Add view and navigation:** show greeting, published course cards/list, and an empty state. Add student navigation conditionally by role; do not alter Admin links.
5. **Make login routing decision:** if approved, redirect a Student to the new dashboard only when no valid local `ReturnUrl` is present; retain normal local redirect and rely on endpoint authorization for cross-role URLs.
6. **Verify authorization and isolation:** test unauthenticated redirect to login, Student success, Administrator/Teacher denial, local ReturnUrl behavior, and that any student-specific data query cannot select another user's records. Also verify zero published courses and normal catalog rendering.
7. **Defer learning features:** implement enrollments, course content, lessons, assignments, or progress only after the corresponding models, lifecycle rules, access policies, and migrations are separately specified and approved.

## Verification limits

- Static inspection only; no build was run because this task requested investigation/planning only and no implementation verification was needed.
- No database query, migration, browser test, or runtime log inspection was performed. The report describes source-level functionality and does not assert live data availability.
- No code, configuration, or database schema was changed.
