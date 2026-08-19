using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobValidationTests
{
    [Fact]
    public void ValidateCandidate_AcceptsDynamicRoiPaddingFromIntegerVariable()
    {
        var padding = new JobVariable
        {
            Name = "ROI padding",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(8)
        };
        var source = new TemplateMatchingStep { Id = "source" };
        var dynamicRoi = new DynamicRoiStep
        {
            Settings = new DynamicRoiSettings
            {
                Padding = -1,
                BoundsSource = new ResultBinding
                {
                    SourceStepId = source.Id,
                    PropertyPath = nameof(TemplateMatchingResult.BoundingBox)
                },
                PaddingSource = new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = padding.Id.ToString("D")
                }
            }
        };

        var result = JobValidation.ValidateCandidate(
            [source], dynamicRoi, [source, dynamicRoi], [padding]);

        Assert.True(result.IsValid, result.Error);
    }

    [Fact]
    public void ValidateJob_AcceptsConditionBackedByCompatibleJobVariable()
    {
        var variable = new JobVariable
        {
            Name = "Enabled",
            ValueKind = ResultValueKind.Boolean,
            Value = System.Text.Json.Nodes.JsonValue.Create(true)
        };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            Operator = ConditionOperator.IsTrue
        };
        var job = new Job
        {
            Variables = [variable],
            Steps = [new IfStep { Settings = new() { Conditions = [condition] } }, new EndIfStep()]
        };

        Assert.True(JobValidation.ValidateJob(job).IsValid);
    }

    [Fact]
    public void ValidateJob_EmptyJob_IsValid() => Assert.True(JobValidation.ValidateJob(new Job()).IsValid);

    [Fact]
    public void ValidateStep_DisabledInvalidStep_IsAllowed()
    {
        var step = new ShowTextStep { IsEnabled = false, Settings = new() { Text = "", FontSize = -1 } };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_CameraCaptureRequiresSelectedDevice()
    {
        var missing = new CameraCaptureStep();
        var configured = new CameraCaptureStep
            { Settings = new() { CameraId = "@device:pnp:camera-id", CameraName = "USB Camera" } };

        Assert.False(JobValidation.ValidateStep([missing], missing).IsValid);
        Assert.True(JobValidation.ValidateStep([configured], configured).IsValid);
    }

    [Fact]
    public void ValidateStep_CameraCaptureRequiresCompleteSpecificQuality()
    {
        var invalid = new CameraCaptureStep
        {
            Settings = new()
            {
                CameraId = "camera",
                QualityMode = CameraQualityMode.Specific,
                Width = 1920,
                Height = 1080,
                FramesPerSecond = 30
            }
        };
        var valid = new CameraCaptureStep
        {
            Settings = new()
            {
                CameraId = "camera",
                QualityMode = CameraQualityMode.Specific,
                Width = 1920,
                Height = 1080,
                FramesPerSecond = 30,
                PixelFormat = "MJPG"
            }
        };

        Assert.False(JobValidation.ValidateStep([invalid], invalid).IsValid);
        Assert.True(JobValidation.ValidateStep([valid], valid).IsValid);
    }

    [Fact]
    public void ValidateStep_FileSystemOperationRequiresConfiguredActivePathSources()
    {
        var source = new WindowsStateQueryStep
            { Id = "source", Settings = new() { QueryType = "filesystem.path", Parameters = new() { ["path"] = "C:\\source" } } };
        var target = new WindowsStateQueryStep
            { Id = "target", Settings = new() { QueryType = "filesystem.path", Parameters = new() { ["path"] = "C:\\target" } } };
        var operation = new FileSystemOperationStep
        {
            Settings = new()
            {
                Operation = FileSystemOperation.Copy,
                SourceMode = FileSystemPathSource.TaskResult,
                SourceResult = new() { SourceStepId = "source", PropertyId = "path", PropertyPath = "Path" },
                TargetMode = FileSystemPathSource.TaskResult,
                TargetResult = new() { SourceStepId = "target", PropertyId = "path", PropertyPath = "Path" }
            }
        };

        Assert.True(JobValidation.ValidateStep([source, target, operation], operation).IsValid);
        operation.Settings.TargetResult = new();
        Assert.False(JobValidation.ValidateStep([source, target, operation], operation).IsValid);
    }

    [Fact]
    public void ValidateStep_SaveImageRequiresImageSourceAndSupportedExtension()
    {
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var step = new SaveImageStep
        {
            Settings = new()
            {
                SavePath = "C:\\captures",
                FileName = "image.png",
                ImageSource = new()
                {
                    SourceStepId = "capture",
                    PropertyId = "image",
                    PropertyPath = "Image"
                }
            }
        };

        Assert.True(JobValidation.ValidateStep([capture, step], step).IsValid);
        step.Settings.FileName = "image.webp";
        Assert.False(JobValidation.ValidateStep([capture, step], step).IsValid);
        step.Settings.FileName = "image.png";
        step.Settings.ImageSource = new();
        Assert.False(JobValidation.ValidateStep([capture, step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_ShowOnDesktopAcceptsTextOnlyAndRejectsEmptyOverlay()
    {
        var source = new ActiveWindowStep { Id = "source" };
        var step = new ShowOnDesktopStep
        {
            Settings = new()
            {
                Overlay = new()
                {
                    TextResults =
                    [
                        new()
                        {
                            Result = new()
                            {
                                SourceStepId = "source",
                                PropertyId = "is_active",
                                PropertyPath = "IsActive"
                            }
                        }
                    ]
                }
            }
        };

        Assert.True(JobValidation.ValidateStep([source, step], step).IsValid);
        step.Settings.Overlay.TextResults.Clear();
        Assert.False(JobValidation.ValidateStep([source, step], step).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.01)]
    public void ValidateStep_ShowTextOpacityOutsideUnitRange_IsInvalid(double opacity)
    {
        var step = new ShowTextStep { Settings = new() { Text = "x", Opacity = (float)opacity } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsQueryRequiredParameterMissing_IsInvalid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "filesystem.path" } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsQueryRequiredParameterPresent_IsValid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "filesystem.path",
            Parameters = new(StringComparer.OrdinalIgnoreCase) { ["path"] = "C:\\temp" } } };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_UnknownWindowsQuery_IsInvalid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "unknown.query" } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsSettingRequiredParameterMissing_IsInvalid()
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new() { SettingId = "audio.master_volume" }
        };

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsSettingRequiredParametersPresent_IsValid()
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "power.display_timeout",
                Parameters = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["minutes"] = "10",
                    ["power_source"] = "both"
                }
            }
        };

        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_UnknownWindowsSetting_IsInvalid()
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new() { SettingId = "unknown.setting" }
        };

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("loud")]
    public void ValidateStep_WindowsVolumeOutsideSupportedRange_IsInvalid(string value)
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "audio.master_volume",
                Parameters = new() { ["value"] = value }
            }
        };

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WifiConnectWithoutProfile_IsInvalid()
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "network.wifi_connection",
                Parameters = new() { ["action"] = "connect" }
            }
        };

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateJob_CompleteIfElseStructure_IsValid()
    {
        var source = AudioStep("audio");
        var condition = Condition("audio", "IsMuted", ConditionOperator.IsTrue);
        var job = new Job { Steps = [source, new IfStep { Settings = new() { Conditions = [condition] } },
            new ShowTextStep { Settings = new() { Text = "muted" } }, new ElseStep(),
            new ShowTextStep { Settings = new() { Text = "audible" } }, new EndIfStep()] };
        Assert.True(JobValidation.ValidateJob(job).IsValid);
    }

    [Fact]
    public void ValidateJob_IfWithoutEndIf_IsInvalid()
    {
        var source = AudioStep("audio");
        var @if = new IfStep { Settings = new() { Conditions = [Condition("audio", "IsMuted", ConditionOperator.IsTrue)] } };
        var result = JobValidation.ValidateJob(new Job { Steps = [source, @if] });
        Assert.False(result.IsValid);
        Assert.Contains(result.Steps, item => item.Step == @if && item.Error!.Contains("EndIf"));
    }

    [Fact]
    public void ValidateJob_ElseWithoutIf_IsInvalid() =>
        Assert.False(JobValidation.ValidateJob(new Job { Steps = [new ElseStep()] }).IsValid);

    [Fact]
    public void ValidateJob_ElseIfAfterElse_IsInvalid()
    {
        var source = AudioStep("audio");
        var settings = new IfConditionSettings { Conditions = [Condition("audio", "IsMuted", ConditionOperator.IsTrue)] };
        Assert.False(JobValidation.ValidateJob(new Job { Steps = [source, new IfStep { Settings = settings }, new ElseStep(),
            new ElseIfStep { Settings = settings }, new EndIfStep()] }).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToLaterStep_IsInvalid()
    {
        var display = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } };
        var source = AudioStep("audio");
        Assert.False(JobValidation.ValidateStep([display, source], display).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToPriorCompatibleStep_IsValid()
    {
        var source = AudioStep("audio");
        var display = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } };
        Assert.True(JobValidation.ValidateStep([source, display], display).IsValid);
    }

    [Fact]
    public void RemoveInvalidSourceSelections_RemovesMissingButPreservesTemporarilyInvalidReferences()
    {
        var source = AudioStep("audio");
        var missing = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "gone", PropertyPath = "Text" } } };
        var later = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Text" } } };
        JobValidation.RemoveInvalidSourceSelections([missing, later, source]);
        Assert.Equal(string.Empty, missing.Settings.TextResult.SourceStepId);
        Assert.Equal("audio", later.Settings.TextResult.SourceStepId);
    }

    private static WindowsStateQueryStep AudioStep(string id) => new() { Id = id, Settings = new() { QueryType = "audio.volume" } };
    private static StepCondition Condition(string id, string path, ConditionOperator op) => new()
        { SourceStepId = id, PropertyPath = path, Operator = op };
}
