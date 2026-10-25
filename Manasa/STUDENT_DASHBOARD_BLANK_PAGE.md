# Student Dashboard Blank Page Investigation

## Finding

The checked-in project does not implement a student dashboard. There is no Student area, student-facing dashboard controller/action, or student dashboard view. The only dashboard route/view is in the Admin area and its controller is protected for Administrators.

On successful login, `AccountController.Login` redirects to a validated local `ReturnUrl` when one is supplied; otherwise it redirects every user, including students, to `Home/Index` (`Controllers/AccountController.cs`, login POST around lines 86–114). `Views/Home/Index.cshtml` is the starter page and contains a visible “Welcome” heading and Learn link. The shared `_Layout.cshtml` renders view content with `@RenderBody()` and has no student-specific content condition.

Therefore, the confirmed source-level cause is a missing student dashboard/landing page. The application does not route students to an implemented dashboard. If the intended destination is `/Student/Dashboard`, there is no matching Student controller or area endpoint; the conventional routes would not render a Student dashboard there. If the login has no return URL, it should render the non-empty generic Home page instead. The exact URL shown during the reported blank page was not provided, so the repository alone cannot establish which of those paths the student reached.

## Flow and relevant code

1. `Views/Account/Login.cshtml` includes the `ReturnUrl` in a hidden form field; the GET accepts it in `AccountController.Login(string? returnUrl)`.
2. The login POST calls `SignInManager.PasswordSignInAsync` (`Controllers/AccountController.cs`, around line 103). On success, it calls `LocalRedirect(model.ReturnUrl!)` for a local return URL or `RedirectToAction("Index", "Home")` otherwise (around lines 109–112). It does not branch on the Student role.
3. `Program.cs` maps an area route for existing areas and a default `Home/Index` conventional route (around lines 75–82). The repository contains `Areas/Admin/Controllers` and `Areas/Admin/Views`, but no `Areas/Student` or student dashboard controller/view.
4. `Areas/Admin/Controllers/DashboardController.cs` and `Areas/Admin/Views/Dashboard/Index.cshtml` implement only the administrator dashboard. The controller has `[Authorize(Roles = "Administrator")]`; it is not a student landing page.
5. `Views/Home/Index.cshtml` contains visible starter content. `Views/_ViewStart.cshtml` selects `Views/Shared/_Layout.cshtml`, whose main element renders the page body. There is no student authorization condition that omits its content.
6. `wwwroot/js/site.js` contains only the template comment. `wwwroot/css/site.css` has no rules hiding the normal page body or home content; its responsive `display: none` declarations apply to Admin sidebar elements only. The standard layout loads local Bootstrap and app CSS/JS assets.

## Rendering, runtime, and verification limits

- Static source inspection shows a non-empty Home view and no student dashboard view to render. It does not prove the exact HTML delivered by the running instance.
- Read-only GETs were attempted at `http://localhost:5113/` and `http://localhost:5113/Student/Dashboard`; the configured local endpoint was unavailable from this environment. No HTTP response was received.
- Browser developer-console output and the active IDE/console log buffer are not available here. No archived application logs were found in the project. There is no relevant student dashboard action in which repository-side view/runtime exceptions could be traced.
- A build was not run because the application is already running and the earlier build attempt in this workspace showed its executable can be locked by the running process. No files or database objects were changed.

## Root cause and recommended fix

The student dashboard is absent from the current application, and successful login has no student-specific redirect. The smallest product-level fix is to add a Student-authorized landing endpoint and view, then route successful Student logins there when no valid return URL is supplied. Preserve the current local-return-URL handling. Dashboard content requirements should determine the view contents; there is not enough evidence in this investigation to invent them.

If the user is seeing a blank page at a route other than `/Student/Dashboard`, capture the exact URL and browser network/console result to distinguish an unsupported route from a runtime or asset-loading issue. No code changes were made as this request was investigation-only.
