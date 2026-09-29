using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class UserChoiceValidationTests
{
    [Fact]
    public void ValidateStep_AcceptsTwoUniqueConfiguredAnswers()
    {
        var step = ValidStep();
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_AllowsEmptyOptionalDialogTexts()
    {
        var step = ValidStep();
        step.Settings.Title = string.Empty;
        step.Settings.Question = string.Empty;
        step.Settings.Description = string.Empty;
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_RejectsTooFewOrDuplicateAnswers()
    {
        var step = ValidStep();
        step.Settings.Options.RemoveAt(1);
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);

        step = ValidStep();
        step.Settings.Options[1].Id = step.Settings.Options[0].Id;
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);

        step = ValidStep();
        step.Settings.Options[1].Label = " one ";
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_RejectsMoreThanEighteenAnswers()
    {
        var step = ValidStep();
        step.Settings.Options = Enumerable.Range(1, 19)
            .Select(index => new UserChoiceOption
            {
                Id = $"option-{index}",
                Label = $"Option {index}"
            })
            .ToList();

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_RejectsNegativeDesktopIndex()
    {
        var step = ValidStep();
        step.Settings.DesktopIndex = -1;

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_DisabledInvalidChoiceIsSkippedForBackwardCompatibility()
    {
        var step = new UserChoiceStep { IsEnabled = false };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_RejectsConditionAfterReferencedAnswerWasDeleted()
    {
        var source = ValidStep();
        source.Id = "choice";
        var conditionStep = new IfStep
        {
            Settings = new()
            {
                Conditions =
                [
                    new()
                    {
                        SourceStepId = source.Id,
                        PropertyId = "selected_option_id",
                        PropertyPath = nameof(TaskAutomation.Steps.UserChoiceResult.SelectedOptionId),
                        Operator = ConditionOperator.Equals,
                        Comparison = new() { Value = "deleted-id" }
                    }
                ]
            }
        };

        Assert.False(JobValidation.ValidateStep([source, conditionStep], conditionStep).IsValid);
    }

    private static UserChoiceStep ValidStep() => new()
    {
        Settings = new()
        {
            Title = "Title",
            Question = "Question",
            Options =
            [
                new() { Id = "one", Label = "One" },
                new() { Id = "two", Label = "Two" }
            ]
        }
    };
}
