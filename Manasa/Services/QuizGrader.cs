using Manasa.Models;

namespace Manasa.Services;

public sealed record GradedAnswer(int QuestionId, int? SelectedOptionId, bool IsCorrect, decimal EarnedPoints);
public sealed record QuizGrade(decimal EarnedScore, decimal MaximumScore, decimal Percentage, bool Passed,
    IReadOnlyList<GradedAnswer> Answers);

public static class QuizGrader
{
    public static QuizGrade Grade(IReadOnlyList<QuizSnapshot> questions, IReadOnlyDictionary<int, int> selectedOptions,
        decimal passingScorePercent)
    {
        if (passingScorePercent is < 0 or > 100 || questions.Count == 0 ||
            questions.Select(q => q.QuestionId).Distinct().Count() != questions.Count ||
            questions.Any(q => !Enum.IsDefined(q.Type) || q.Points <= 0 || q.Options.Count < 2 || q.Options.Count(o => o.IsCorrect) != 1 ||
                (q.Type == QuizQuestionType.TrueFalse && q.Options.Count != 2)))
            throw new ArgumentException("Quiz configuration is invalid.", nameof(questions));
        if (selectedOptions.Keys.Any(id => questions.All(q => q.QuestionId != id)))
            throw new ArgumentException("Submitted answers contain an unknown question.", nameof(selectedOptions));

        var answers = new List<GradedAnswer>(questions.Count);
        decimal earned = 0;
        decimal maximum = 0;
        foreach (var question in questions)
        {
            maximum += question.Points;
            int? optionId = selectedOptions.TryGetValue(question.QuestionId, out var selected) ? selected : null;
            var option = optionId is null ? null : question.Options.SingleOrDefault(o => o.OptionId == optionId);
            if (optionId is not null && option is null)
                throw new ArgumentException("Submitted answers contain an unknown option.", nameof(selectedOptions));
            var correct = option?.IsCorrect == true;
            var points = correct ? question.Points : 0m;
            earned += points;
            answers.Add(new GradedAnswer(question.QuestionId, optionId, correct, points));
        }
        var percentage = maximum == 0 ? 0m : Math.Round(earned / maximum * 100m, 2, MidpointRounding.AwayFromZero);
        return new QuizGrade(earned, maximum, percentage, percentage >= passingScorePercent, answers);
    }
}
