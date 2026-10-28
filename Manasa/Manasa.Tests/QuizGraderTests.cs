using Manasa.Models;
using Manasa.Services;
using Xunit;

namespace Manasa.Tests;

public sealed class QuizGraderTests
{
    [Fact]
    public void Grades_correct_multiple_choice_and_true_false_answers()
    {
        var questions = new[]
        {
            Question(1, QuizQuestionType.MultipleChoice, 2m, (11, true), (12, false), (13, false)),
            Question(2, QuizQuestionType.TrueFalse, 1m, (21, false), (22, true))
        };

        var grade = QuizGrader.Grade(questions, new Dictionary<int, int> { [1] = 11, [2] = 22 }, 70m);

        Assert.Equal(3m, grade.EarnedScore);
        Assert.Equal(3m, grade.MaximumScore);
        Assert.Equal(100m, grade.Percentage);
        Assert.True(grade.Passed);
        Assert.All(grade.Answers, answer => Assert.True(answer.IsCorrect));
    }

    [Fact]
    public void Incorrect_and_missing_answers_receive_zero_points()
    {
        var questions = new[]
        {
            Question(1, QuizQuestionType.MultipleChoice, 2m, (11, true), (12, false)),
            Question(2, QuizQuestionType.TrueFalse, 1m, (21, false), (22, true))
        };

        var grade = QuizGrader.Grade(questions, new Dictionary<int, int> { [1] = 12 }, 70m);

        Assert.Equal(0m, grade.EarnedScore);
        Assert.Equal(3m, grade.MaximumScore);
        Assert.Equal(0m, grade.Percentage);
        Assert.False(grade.Passed);
        Assert.Equal(2, grade.Answers.Count);
        Assert.Null(grade.Answers[1].SelectedOptionId);
    }

    [Theory]
    [InlineData(70, true)]
    [InlineData(70.01, false)]
    public void Applies_pass_boundary_to_rounded_percentage(decimal passingScore, bool expected)
    {
        var questions = new[]
        {
            Question(1, QuizQuestionType.MultipleChoice, 7m, (11, true), (12, false)),
            Question(2, QuizQuestionType.TrueFalse, 3m, (21, false), (22, true))
        };

        var grade = QuizGrader.Grade(questions, new Dictionary<int, int> { [1] = 11 }, passingScore);

        Assert.Equal(70m, grade.Percentage);
        Assert.Equal(expected, grade.Passed);
    }

    [Fact]
    public void Rejects_invalid_question_configuration_and_unknown_answers()
    {
        var noCorrectAnswer = new[] { Question(1, QuizQuestionType.MultipleChoice, 1m, (11, false), (12, false)) };
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(noCorrectAnswer, new Dictionary<int, int>(), 50m));

        var valid = new[] { Question(1, QuizQuestionType.MultipleChoice, 1m, (11, true), (12, false)) };
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(valid, new Dictionary<int, int> { [999] = 11 }, 50m));
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(valid, new Dictionary<int, int> { [1] = 999 }, 50m));
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(valid, new Dictionary<int, int>(), 101m));
    }

    private static QuizSnapshot Question(int id, QuizQuestionType type, decimal points, params (int Id, bool Correct)[] options) =>
        new(id, $"Question {id}", type, points, null,
            options.Select(option => new OptionSnapshot(option.Id, $"Option {option.Id}", option.Correct)).ToList());
}
