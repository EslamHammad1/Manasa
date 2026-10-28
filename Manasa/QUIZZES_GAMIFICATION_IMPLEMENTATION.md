# Quizzes and Gamification Implementation

## Implemented

- Added course quizzes with administrator creation/editing, question authoring, ordering, preview, publish/unpublish, and attempt reporting.
- Added Student quiz discovery, details, attempts, server-side grading, results, and per-quiz history.
- Added persisted quiz XP awards, badge definitions/awards, and daily learning activity records. The Student dashboard displays XP, badges, streaks, recent quiz results, and progress toward eligible badge milestones.
- Added complete English and Arabic resource entries for the new user interface.
- Kept all quiz access available to authenticated Students for published courses. No enrollment, package, payment, or purchase gate was introduced.

## Data model and rules

- `Quiz` belongs to a `Course`; `QuizQuestion` belongs to a quiz; `QuizOption` belongs to a question.
- `QuizAttempt` belongs to an Identity student and quiz. It stores attempt number, UTC timestamps, status, score, percentage, pass result, a row version, and immutable quiz/rule/question snapshots. `QuizAttemptAnswer` stores selected answer text and scored result snapshots so editing questions does not alter historical results.
- `StudentXpAward` is an append-only ledger with a unique `(StudentId, EventType, EventKey)` key. A student earns 25 XP the first time they pass each quiz. Refreshing pages and repeating a pass cannot award more XP for that quiz.
- `BadgeDefinition` stores localizable resource keys; `StudentBadge` stores unique student awards. Initial criteria: First Quiz after a valid submission; Quiz Master after passing quizzes in five distinct courses; Perfect Score after a 100% quiz; Learning Milestone at 100 XP.
- `StudentLearningDay` stores one row per student per activity date. A successfully submitted quiz counts as meaningful activity. Streak dates use UTC calendar days; current streak accepts activity today or yesterday, and longest streak is derived from persisted days.
- A quiz requires at least one question, positive points, supported question types, at least two options per question, exactly one correct option, and exactly two options for True/False. Percentages are rounded to two decimal places using midpoint-away-from-zero before pass comparison.
- Attempt identity is taken from the authenticated Identity claim. Scores and pass results are calculated server-side; attempt reads are owner-scoped; start and submit requests use antiforgery validation. Server-side time expiry and attempt-limit checks do not rely on client JavaScript.

## Routes

- Admin: `/Admin/Quizzes` (list), `Create`, `Edit/{id}`, `Questions/{id}`, `Question`, `Preview/{id}`, `Attempts/{id}`, and POST `Publish`/`DeleteQuestion`.
- Student: `/Student/Quizzes` (available), `Details/{id}`, `Take/{id}`, `Result/{id}`, `History?quizId={id}`, and POST `Start`/`Submit`.
- Student dashboard: `/Student/Dashboard` now includes persisted gamification data and recent quiz results.

All administrator quiz actions are protected by the Administrator role. Student quiz actions are protected by the Student role. Answer keys are only rendered on the administrator preview; student take pages receive option text without correctness flags.

## Migration and safety review

- Migration: `20261027132154_QuizAndGamification`.
- Migration adds quiz, attempt/answer, XP, badge, and activity-day tables, badge seed rows, foreign keys, and indexes. It does not drop or alter existing user, course, lesson, progress, or Identity columns. Ownership/course deletes are restricted; quiz child questions/options and attempt child answers cascade. Unique indexes prevent duplicate attempt numbers, XP events, badge awards, and student activity dates.
- The migration has **not** been applied. EF model check reports no pending model changes.
- LocalDB could not start in this environment, so applied migration state and actual SQL execution could not be verified. Review/apply in development with:

  ```powershell
  dotnet ef migrations script 20261021140535_StudentAccountCreatedAt 20261027132154_QuizAndGamification
  dotnet ef database update
  ```

  Inspect the generated SQL before applying it. Do not apply this migration to production as part of this change.

## Verification

- `dotnet test Manasa.Tests/Manasa.Tests.csproj -c Release --no-restore`: passed, 5 tests. Coverage includes correct/incorrect MC and True/False grading, missing answers, score/percentage and pass boundary, invalid configuration, and unknown question/option rejection.
- `dotnet build -c Release --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet ef migrations has-pending-model-changes --no-build`: no pending model changes.
- Resource parity check found no missing Arabic counterparts or duplicate resource names.
- NuGet vulnerability metadata was unreachable during test restore (`NU1900`); the test run used restored/cached packages and passed.
- No database migration application, browser walkthrough, or end-to-end authorization/concurrency test was possible here. Concurrency handling uses serializable attempt transactions, row-version checks, and unique indexes, but should still be exercised against SQL Server before production rollout.

## Scope limits

- The current repository has no Lesson entity or lesson-completion event, so lesson XP and a First Lesson badge were not added. Course completion is also undefined; Course Finisher was not added.
- Availability dates and course-level quizzes were not added; this implementation anchors quizzes to the existing Course model.
- Leaderboard is deferred to avoid adding public student ranking/profile exposure before privacy rules are defined.
- Attempt time expiry is enforced by the server at submission and when reopening an expired in-progress attempt; the page does not run a client-side countdown.
