# MANASA Learning Progress Investigation

**Scope:** Static investigation and implementation plan for lessons and student learning progress. No application code, project configuration, migration, or database was changed by this investigation.

## 1. Executive summary

This checkout has course catalog administration and a student-facing published-course catalog with a course details page. It does not have course lessons, enrollment records, student lesson progress, completion tracking, resume behavior, lesson navigation, or progress UI.

The existing Student area is protected by the `Student` role. Its course queries include every published course and do not check enrollment or a package purchase. That is the access behavior currently expressed by the code, but the intended product policy is not established. `CoursePackage` stores a price and an access duration, but the student flow does not use either field to grant or deny access.

The safest implementation is to agree on course access and lesson-content rules first, then add ordered lesson authoring and a unique student/lesson progress record. Student identity should come from the authenticated server-side user. Preserve the current published-course access behavior until a product decision explicitly defines enrollment or package entitlements.

This report is based on source inspection. The app was not run and the database was not queried. No test project exists in the checkout, so runtime behavior remains unverified.

## 2. Repository baseline and inspection boundary

- Branch: `Dev`.
- Working tree: clean at inspection time.
- Application: .NET 9 ASP.NET Core MVC with Razor views, ASP.NET Core Identity, EF Core, and SQL Server. `Manasa.csproj` has Identity EF Core, EF Core Design, and SQL Server references; it contains no test project or test package references.
- Data access follows the existing controllers plus injected `ApplicationDbContext` pattern. No repository, service, API, lesson, enrollment, or progress folders or types were found in the application source.
- The Student area uses the shared public layout. English and Arabic cultures are configured, and the shared layout selects Bootstrap RTL for right-to-left cultures.
- Static searches were limited to application source, views, resources, and migrations; vendored Bootstrap and jQuery files were excluded from feature conclusions.

`Program.cs` runs `Database.MigrateAsync()` automatically when the app starts in Development. The app was not started, so this investigation caused no migration or database write.

## 3. Existing feature inventory

“Implemented in code” below means source paths and behavior were traced; it does not mean the behavior was exercised at runtime.

| Capability | Status | Evidence and limits |
|---|---|---|
| Student authentication and role gate | Implemented in code; runtime unverified | `Areas/Student/Controllers/DashboardController.cs:12` applies `[Authorize(Roles = "Student")]`. Identity is configured in `Program.cs:19-31`, with authentication and authorization middleware at `Program.cs:82-83`. |
| Course catalog and publication | Implemented in code | `Models/Course.cs:5-30` has catalog fields and `IsPublished`; admin CRUD is in `Areas/Admin/Controllers/CoursesController.cs:19-165`. No lesson collection or student relationship exists on `Course`. |
| Student course discovery | Implemented in code | `DashboardController.Index` at `Areas/Student/Controllers/DashboardController.cs:17-45` resolves the signed-in user, selects all published courses, and projects catalog fields. `Areas/Student/Views/Dashboard/Index.cshtml:6-63` renders the list and an empty catalog state. |
| Student course details | Implemented in code | `DashboardController.Details` at `Areas/Student/Controllers/DashboardController.cs:48-65` returns metadata for a matching published course and `NotFound` otherwise. `Areas/Student/Views/Dashboard/Details.cshtml:1-37` renders course image, subject, title, teacher, and description only. |
| Enrollment / student-course relationship | Missing | `ApplicationUser` has no course relationship (`Models/ApplicationUser.cs:5-10`); `Course` has no student relationship (`Models/Course.cs:26-30`). No enrollment entity or table appears in `Data/ApplicationDbContext.cs:8-18` or the initial schema migration. |
| Lessons and ordering | Missing | There is no lesson model, DbSet, controller action, or Student lesson view. `Course` has no lesson navigation property. No lesson table exists in the initial migration at `Data/Migrations/20261021133521_InitialLearningPlatform.cs:16-280` or in the model snapshot. |
| Lesson content and content types | Missing | No lesson content field, media/file storage, content-type enum, or lesson rendering path was found. The project dependencies do not include a media player, markdown renderer, or file-storage integration. |
| Student access to lessons | Missing; course-level behavior is observable | The current code permits an authenticated Student to see any published course: the dashboard filters only `IsPublished` (`DashboardController.cs:27`), and details filter only course ID plus `IsPublished` (`DashboardController.cs:52`). There is no enrollment/package check. Whether this is desired is an open product question. |
| Completion recording and persistence | Missing | No progress entity or DbSet exists; there are no completion endpoints, completion timestamps, or completion indicators. |
| Last-accessed lesson and resume | Missing | No persisted lesson activity or resume query exists. The current dashboard view model contains only student name and course catalog cards (`ViewModels/Student/DashboardViewModel.cs:3-35`). |
| Course counts and completion percentage | Missing | No query or view-model fields calculate completed or total lessons. The displayed course count in the dashboard is the number of published course cards, not learning progress (`Index.cshtml:14-19`). |
| Previous/next lesson navigation | Missing | There are no lesson routes or lesson-ordering fields. |
| Empty/unavailable course handling | Partially implemented | The catalog has an empty state when there are no published courses. Details return `NotFound` for missing or unpublished courses. There is no empty-course lesson state because lessons do not exist. |
| Arabic/English localization and RTL/LTR | Infrastructure implemented; progress localization missing | `Program.cs:39-45` supports `en` and `ar`; `Views/Shared/_Layout.cshtml:7-16` sets language, direction, and RTL Bootstrap; `LocalizationController` validates the language and redirects only to local return URLs (`Controllers/LocalizationController.cs:8-31`). No lesson/progress UI resource keys were found in `Resources/Localization/SharedResource.resx` or `.ar.resx`. This is source evidence, not a visual RTL test. |
| Enrollment derived from package purchase | Missing | `Models/CoursePackage.cs:5-25` contains `Price`, `AccessDurationDays`, and `IsActive`, but no order, payment, entitlement, or enrollment relationship. `DashboardController` does not query packages. |

One existing English resource key, “Add a course to start creating lessons and packages,” is used by the admin course-list empty state. It is copy only; it does not correspond to lesson authoring functionality (`Resources/Localization/SharedResource.resx:582-583`, Arabic counterpart `SharedResource.ar.resx:573`).

## 4. Current student flow

1. The user signs in through the existing Identity account flow. Student-area requests require the `Student` role.
2. `/Student/Dashboard` resolves the current Identity user with `UserManager.GetUserAsync(User)`. If resolution fails, it returns `Challenge` (`DashboardController.cs:17-25`).
3. The dashboard queries all published courses ordered by title. It does not filter by student ID, enrollment, package, or access expiry (`DashboardController.cs:27-40`).
4. Selecting a course opens `/Student/Dashboard/Details/{id}`. The details page shows catalog metadata; it does not start a lesson or update persistent activity (`DashboardController.cs:48-65`; `Details.cshtml:6-37`).

The area route is conventional: `Program.cs:87-95` maps `{area:exists}/{controller=Dashboard}/{action=Index}/{id?}`. There are currently no lesson-specific routes.

## 5. Recommended data model and architecture

### Lesson content

Add a `CourseLesson` entity related to `Course`, with at least:

- `Id`, `CourseId`, `Title`, and `DisplayOrder`.
- A publication/availability flag so a published course can contain unavailable drafts.
- A content representation selected after product requirements are agreed. A constrained plain-text or Markdown body is a small first step; video, embeds, downloadable files, and rich HTML require separate hosting, security, and user-experience decisions. Razor encodes values by default; avoid rendering lesson HTML with `Html.Raw` unless it is sanitized by a defined policy.
- UTC creation/update timestamps following the existing `CreatedAtUtc` convention.

Index lessons by `(CourseId, IsPublished, DisplayOrder, Id)` for course lesson lists. Sort by `DisplayOrder` then `Id` so ordering remains deterministic. If the product requires unique positions, define how concurrent/reordered position updates avoid uniqueness conflicts before adding a unique constraint.

### Student progress

Add a `StudentLessonProgress` entity as one row per student and lesson:

- `StudentId` referencing the Identity `ApplicationUser.Id` string key.
- `LessonId` referencing `CourseLesson.Id`.
- `IsCompleted`, nullable `CompletedAtUtc`, and nullable `LastAccessedAtUtc`.

Configure a unique index on `(StudentId, LessonId)` to prevent duplicate rows, plus foreign-key indexes as needed by EF Core. The unique constraint remains the final concurrency guard even if controller logic first queries for an existing row. A repeated completion request should be idempotent: do not insert another row and preserve the original completion time. Use `Restrict` deletion, consistent with relationships in `ApplicationDbContext.cs:73-123`, or define an explicit archive/purge policy so deleting content cannot silently erase a learner’s history.

Use progress rows for both first access and completion. To avoid a write on a GET request, lesson-list and Previous/Next links can POST to an antiforgery-protected “open lesson” action that upserts `LastAccessedAtUtc` and redirects to the read-only lesson GET. A separate POST marks the lesson complete. Derive `StudentId` from `UserManager.GetUserAsync(User)` or the authenticated user ID claim; never accept it from a form, query, or route value.

Calculate a course percentage from the count of completed progress rows for lessons currently available to the student divided by the total available/published lesson count. Return 0% for a course with no available lessons. Decide rounding and what happens when lessons are added, unpublished, or revised after a student has completed them. Resume should select the most recently accessed available lesson for that student and course, with `DisplayOrder`/ID as a deterministic fallback.

### Enrollment/access policy

The code currently allows any authenticated Student to view published courses, regardless of package state. Preserve that behavior for the initial learning feature unless the product owner confirms another policy. If package purchase is intended to gate access, add a separately specified enrollment/entitlement workflow before enforcing it; `Price` and `AccessDurationDays` alone are not proof of purchase or authorization.

### Application structure

Continue using MVC controllers, view models, `ApplicationDbContext`, EF Core migrations, and Razor views. No repository/CQRS/MediatR dependency is justified by the current architecture. Keep database entities out of forms and use view models for lesson authoring and student progress presentation.

## 6. Ordered implementation plan

The following is a proposed future implementation. No step below was performed.

### Phase 0 — Confirm product rules

- Decide whether access stays “any Student may open a published course” or requires an enrollment/entitlement.
- Decide which lesson content types are in scope, what counts as completion, and whether progress survives lesson edits/unpublishing/deletion.
- Define how newly published lessons affect a learner’s percentage and resume target.
- **Files:** none. **DB:** none. **Dependency:** before schema/API work.
- **Acceptance:** written decisions for access, content, completion, and progress lifecycle.

### Phase 1 — Add lesson content and authoring

- Add `Models/CourseLesson.cs`; add a lesson collection to `Models/Course.cs`; configure DbSet, FK deletion, and course-order indexes in `Data/ApplicationDbContext.cs`.
- Add an admin form view model, likely `ViewModels/Admin/CourseLessonFormViewModel.cs`; add an Administrator-authorized `Areas/Admin/Controllers/CourseLessonsController.cs` and `Areas/Admin/Views/CourseLessons/` list/form views.
- Link lesson management from the course list or course form. Preserve existing course routes and validation.
- Generate an EF migration under `Data/Migrations/` and update `ApplicationDbContextModelSnapshot.cs` using EF tooling; review the generated SQL before applying it.
- Add English/Arabic resource keys in `Resources/Localization/SharedResource.resx` and `.ar.resx`.
- **Security:** admin-only authoring; validate that lesson IDs belong to the selected course; encode/sanitize content according to the chosen content type.
- **Verification:** lesson create/edit/order/publish/unpublish and validation; course deletion behavior with lessons; migration script inspection on a disposable SQL Server database.

### Phase 2 — Add persistent student progress

- Add `Models/StudentLessonProgress.cs`; configure the unique `(StudentId, LessonId)` key/index and restrictive foreign keys in `Data/ApplicationDbContext.cs`.
- Generate and review the progress migration and model snapshot updates in `Data/Migrations/`.
- Choose and implement one idempotent upsert path for lesson access and one for explicit completion. Preserve UTC timestamp semantics.
- **Security:** derive student ID from authenticated identity; validate the lesson belongs to a published course and is itself available; protect writes with POST and antiforgery; do not trust client completion state or student IDs.
- **Verification:** repeated completion creates one row; completion timestamp stays stable; two-user isolation; concurrent duplicate requests; unpublished/mismatched course and lesson IDs are denied or not found.

### Phase 3 — Add lesson reading and navigation

- Extend `Areas/Student/Controllers/DashboardController.cs` or introduce `Areas/Student/Controllers/LessonsController.cs` for lesson GET, open/access POST, and completion POST. Keep `[Authorize(Roles = "Student")]` on the relevant controller/actions.
- Extend `ViewModels/Student/DashboardViewModel.cs` or add focused lesson view models.
- Add a lesson Razor view under `Areas/Student/Views/` with ordered lesson content, completed/current states, and Previous/Next navigation.
- Validate course/lesson parent relationships in every route and query. Do not expose unpublished lesson content.
- **Verification:** first/last lesson navigation, empty course, missing/unpublished lesson, cross-course lesson ID, unauthenticated/non-student access, Arabic and English direction.

### Phase 4 — Show progress and resume

- Extend `DashboardController.Index` and `Details` query projections to calculate counts, percentage, and last-accessed lesson without per-card N+1 queries; use grouped projections or bounded correlated subqueries and inspect generated SQL.
- Extend `Areas/Student/Views/Dashboard/Index.cshtml` with progress and Resume Learning; extend `Details.cshtml` with the ordered lesson list and course progress.
- Extend/add student view models in `ViewModels/Student/` and resource keys in both localization resource files.
- **Verification:** 0/partial/100% states, zero-lesson course, correct counts for two students, stable resume selection, progress after logout/login.

### Phase 5 — Tests, accessibility, and regression verification

- Add a test project (none exists now), likely `Manasa.Tests/` with aligned .NET 9 test packages and a SQL Server-backed integration test setup for migrations, Identity, constraints, and controller flows. Avoid relying only on EF InMemory for FK/unique-index tests because it does not enforce relational database behavior.
- Cover authorization and identity isolation, lesson/course validation, idempotency/concurrency, progress math, and route behavior. Add localization/resource and manual keyboard/screen-reader checks for status indicators and controls.
- Run migration creation/script review, build, automated tests, and manual English LTR/Arabic RTL browser checks at desktop and mobile sizes.
- **Regression risks:** existing all-published-course access may be accidentally narrowed; dynamic lesson denominator can change percentages; deleting lessons can orphan or erase progress if delete behavior is misconfigured; rich HTML or third-party embeds can introduce script/privacy risks; concurrent progress upserts can race without DB uniqueness.

## 7. Testing strategy and acceptance criteria

There is one application project (`Manasa.csproj`) and no test project in the checkout. Before release, add coverage for:

- Identity: only Student role can use Student learning endpoints; a student cannot read or update another student’s progress.
- Availability: unpublished courses and lessons are unavailable; a lesson ID from a different course cannot be used with the requested course.
- Progress: first access records one row, repeated access updates the same row, completion is idempotent, completion timestamp is stable, and percentage/counts match available lessons.
- Resume/navigation: latest accessed available lesson resumes; Previous/Next follows stable display order and handles boundaries.
- Lifecycle: zero lessons gives a zero percentage and no invalid resume link; edits, unpublishing, and attempted deletion follow the agreed history policy.
- Persistence: progress remains after a new authenticated session; relational unique/FK constraints hold under concurrent writes.
- UI: resource coverage in English/Arabic, LTR/RTL order and directional controls, keyboard focus, accessible completion state, responsive lesson list and content.

Acceptance should require persistent per-user progress, server-derived identity, no access-policy drift, correct totals and resume behavior, no duplicate records, and verified migration behavior on a disposable SQL Server database. A successful build alone does not establish these outcomes.

## 8. Open questions and verification limits

1. Are published courses intended to remain open to all authenticated students, or should active packages represent purchased enrollment? Current code does not answer this product question.
2. Which lesson types are required for the first release: text, video URL, downloadable file, quiz, or another format? No content-storage or media service exists in this checkout.
3. Is completion an explicit student action, completion of a video threshold, or another rule? Do not mark a lesson complete merely because it was opened.
4. Should adding a lesson reduce an already-completed course percentage, and what happens to completion when a lesson is edited, unpublished, or removed?
5. Should teachers author lessons? The Teacher role exists, but no Teacher area/workspace or authoring workflow exists. The plan uses the current Administrator-only management boundary unless product requirements say otherwise.
6. Runtime authorization, SQL Server behavior, migration execution, generated SQL, browser rendering, and English/Arabic visual behavior were not tested. No database was connected or modified.
