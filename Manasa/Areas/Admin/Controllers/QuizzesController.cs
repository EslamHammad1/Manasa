using Manasa.Data;
using Manasa.Localization;
using Manasa.Models;
using Manasa.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Manasa.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class QuizzesController(ApplicationDbContext db, TimeProvider clock, IStringLocalizer<SharedResource> text) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rows = await db.Quizzes.AsNoTracking().OrderBy(q => q.Title).Select(q => new AdminQuizRowViewModel
        {
            Id = q.Id, Title = q.Title, CourseTitle = q.Course.Title, IsPublished = q.IsPublished,
            QuestionCount = q.Questions.Count, AttemptCount = q.Attempts.Count
        }).ToListAsync();
        return View(new AdminQuizListViewModel { Quizzes = rows });
    }

    public async Task<IActionResult> Create() => View("Form", await BuildFormAsync(new QuizFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuizFormViewModel model)
    {
        if (!await CourseExistsAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), text["Select a valid course."]);
        if (!ModelState.IsValid) return View("Form", await BuildFormAsync(model));
        var now = clock.GetUtcNow().UtcDateTime;
        var quiz = new Quiz
        {
            CourseId = model.CourseId, Title = model.Title.Trim(), Description = model.Description?.Trim(),
            PassingScorePercent = model.PassingScorePercent, TimeLimitMinutes = model.TimeLimitMinutes,
            MaximumAttempts = model.MaximumAttempts, ShowExplanationsAfterSubmission = model.ShowExplanationsAfterSubmission,
            IsPublished = false, CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = text["Quiz created."].Value;
        return RedirectToAction(nameof(Questions), new { id = quiz.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await db.Quizzes.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();
        return View("Form", await BuildFormAsync(new QuizFormViewModel
        {
            Id = quiz.Id, CourseId = quiz.CourseId, Title = quiz.Title, Description = quiz.Description,
            PassingScorePercent = quiz.PassingScorePercent, TimeLimitMinutes = quiz.TimeLimitMinutes,
            MaximumAttempts = quiz.MaximumAttempts, ShowExplanationsAfterSubmission = quiz.ShowExplanationsAfterSubmission,
            IsPublished = quiz.IsPublished
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuizFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var quiz = await db.Quizzes.SingleOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();
        if (model.IsPublished && !await IsPublishableAsync(id, model))
            ModelState.AddModelError(string.Empty, text["This quiz needs valid questions and a published course before publication."]);
        if (!await CourseExistsAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), text["Select a valid course."]);
        if (!ModelState.IsValid) return View("Form", await BuildFormAsync(model));
        quiz.CourseId = model.CourseId; quiz.Title = model.Title.Trim(); quiz.Description = model.Description?.Trim();
        quiz.PassingScorePercent = model.PassingScorePercent; quiz.TimeLimitMinutes = model.TimeLimitMinutes;
        quiz.MaximumAttempts = model.MaximumAttempts; quiz.ShowExplanationsAfterSubmission = model.ShowExplanationsAfterSubmission;
        quiz.IsPublished = model.IsPublished; quiz.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = text["Quiz updated."].Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Questions(int id)
    {
        var quiz = await db.Quizzes.AsNoTracking().Include(q => q.Questions).SingleOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();
        return View(new AdminQuizQuestionsViewModel
        {
            QuizId = id, QuizTitle = quiz.Title, IsPublished = quiz.IsPublished,
            Questions = quiz.Questions.OrderBy(q => q.DisplayOrder).Select(q => new AdminQuestionRowViewModel
            { Id = q.Id, Text = q.Text, Type = q.Type.ToString(), Points = q.Points, DisplayOrder = q.DisplayOrder }).ToList()
        });
    }

    public async Task<IActionResult> Preview(int id)
    {
        var preview = await db.Quizzes.AsNoTracking().Where(q => q.Id == id).Select(q => new AdminQuizPreviewViewModel
        {
            Title = q.Title, CourseTitle = q.Course.Title, Description = q.Description,
            PassingScorePercent = q.PassingScorePercent, TimeLimitMinutes = q.TimeLimitMinutes,
            Questions = q.Questions.OrderBy(question => question.DisplayOrder).Select(question => new AdminQuestionPreviewViewModel
            {
                Text = question.Text, Type = question.Type, Points = question.Points,
                Options = question.Options.OrderBy(option => option.DisplayOrder).Select(option => new AdminOptionPreviewViewModel
                { Text = option.Text, IsCorrect = option.IsCorrect }).ToList()
            }).ToList()
        }).SingleOrDefaultAsync();
        return preview is null ? NotFound() : View(preview);
    }

    public async Task<IActionResult> Question(int quizId, int? id)
    {
        var quiz = await db.Quizzes.AsNoTracking().SingleOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();
        if (id is null) return View(new QuizQuestionFormViewModel { QuizId = quizId, DisplayOrder = await db.QuizQuestions.CountAsync(q => q.QuizId == quizId) + 1 });
        var question = await db.QuizQuestions.Include(q => q.Options).SingleOrDefaultAsync(q => q.Id == id && q.QuizId == quizId);
        if (question is null) return NotFound();
        return View(new QuizQuestionFormViewModel
        {
            QuizId = quizId, Id = question.Id, Text = question.Text, Type = question.Type, Points = question.Points,
            DisplayOrder = question.DisplayOrder, Explanation = question.Explanation,
            OptionsText = string.Join(Environment.NewLine, question.Options.OrderBy(o => o.DisplayOrder).Select(o => o.Text)),
            CorrectOptionNumber = question.Options.OrderBy(o => o.DisplayOrder).ToList().FindIndex(o => o.IsCorrect) + 1
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Question(QuizQuestionFormViewModel model)
    {
        var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Options).SingleOrDefaultAsync(q => q.Id == model.QuizId);
        if (quiz is null) return NotFound();
        var options = model.OptionsText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r')).ToList();
        if (options.Count < 2 || options.Count > 20 || model.CorrectOptionNumber > options.Count)
            ModelState.AddModelError(nameof(model.OptionsText), text["Provide 2 to 20 answer options and select an existing correct option."]);
        if (!Enum.IsDefined(model.Type)) ModelState.AddModelError(nameof(model.Type), text["Select a valid question type."]);
        if (model.Type == QuizQuestionType.TrueFalse && options.Count != 2)
            ModelState.AddModelError(nameof(model.OptionsText), text["True/False questions require exactly two options."]);
        if (!ModelState.IsValid) return View(model);
        QuizQuestion question;
        if (model.Id is int questionId)
        {
            question = await db.QuizQuestions.Include(q => q.Options).SingleOrDefaultAsync(q => q.Id == questionId && q.QuizId == model.QuizId)
                ?? throw new InvalidOperationException("Question not found.");
            db.QuizOptions.RemoveRange(question.Options);
            question.Options.Clear();
        }
        else
        {
            question = new QuizQuestion { QuizId = quiz.Id };
            db.QuizQuestions.Add(question);
        }
        question.Text = model.Text.Trim(); question.Type = model.Type; question.Points = model.Points;
        question.DisplayOrder = model.DisplayOrder; question.Explanation = model.Explanation?.Trim();
        question.Options = options.Select((value, index) => new QuizOption
        { Text = value, DisplayOrder = index + 1, IsCorrect = index + 1 == model.CorrectOptionNumber }).ToList();
        quiz.IsPublished = false;
        quiz.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = text["Question saved. The quiz is unpublished until reviewed and published again."].Value;
        return RedirectToAction(nameof(Questions), new { id = quiz.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int quizId, int questionId)
    {
        var question = await db.QuizQuestions.SingleOrDefaultAsync(q => q.Id == questionId && q.QuizId == quizId);
        if (question is null) return NotFound();
        db.QuizQuestions.Remove(question);
        var quiz = await db.Quizzes.SingleAsync(q => q.Id == quizId);
        quiz.IsPublished = false; quiz.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = text["Question deleted. The quiz is now unpublished."].Value;
        return RedirectToAction(nameof(Questions), new { id = quizId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish)
    {
        var quiz = await db.Quizzes.SingleOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();
        if (publish && !await IsPublishableAsync(id, new QuizFormViewModel { CourseId = quiz.CourseId }))
        {
            TempData["ErrorMessage"] = text["This quiz needs valid questions and a published course before publication."].Value;
            return RedirectToAction(nameof(Questions), new { id });
        }
        quiz.IsPublished = publish; quiz.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = publish ? text["Quiz published."].Value : text["Quiz unpublished."].Value;
        return RedirectToAction(nameof(Questions), new { id });
    }

    public async Task<IActionResult> Attempts(int id)
    {
        var quiz = await db.Quizzes.AsNoTracking().Where(q => q.Id == id).Select(q => new { q.Id, q.Title }).SingleOrDefaultAsync();
        if (quiz is null) return NotFound();
        var attempts = await db.QuizAttempts.AsNoTracking().Where(a => a.QuizId == id)
            .OrderByDescending(a => a.StartedAtUtc).Select(a => new AdminQuizAttemptRowViewModel
            {
                Id = a.Id, StudentName = a.Student.DisplayName ?? a.Student.UserName ?? "Student",
                AttemptNumber = a.AttemptNumber, Percentage = a.Percentage, Passed = a.Passed, Status = a.Status,
                StartedAtUtc = a.StartedAtUtc, SubmittedAtUtc = a.SubmittedAtUtc
            }).ToListAsync();
        return View(new AdminQuizAttemptsViewModel { QuizId = id, QuizTitle = quiz.Title, Attempts = attempts });
    }

    private async Task<QuizFormViewModel> BuildFormAsync(QuizFormViewModel model)
    {
        model.Courses = await db.Courses.AsNoTracking().OrderBy(c => c.Title)
            .Select(c => new SelectListItem(c.Title, c.Id.ToString())).ToListAsync();
        return model;
    }
    private Task<bool> CourseExistsAsync(int id) => db.Courses.AnyAsync(c => c.Id == id);
    private async Task<bool> IsPublishableAsync(int? quizId, QuizFormViewModel form)
    {
        if (quizId is null) return false;
        var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Options).SingleOrDefaultAsync(q => q.Id == quizId);
        return quiz is not null && quiz.PassingScorePercent is >= 0 and <= 100 && quiz.TimeLimitMinutes is null or > 0 &&
            quiz.MaximumAttempts is null or > 0 && await db.Courses.AnyAsync(c => c.Id == (form.CourseId > 0 ? form.CourseId : quiz.CourseId) && c.IsPublished) &&
            quiz.Questions.Count > 0 && quiz.Questions.All(q => Enum.IsDefined(q.Type) && q.Points > 0 && q.Options.Count >= 2 && q.Options.Count(o => o.IsCorrect) == 1 &&
                (q.Type != QuizQuestionType.TrueFalse || q.Options.Count == 2));
    }
}
