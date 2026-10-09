using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobValidationTests
{
    [Fact]
    public void ValidateJob_RejectsInvalidKnownLocalValue()
    {
        var local = new LocalValue
        {
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(-1)
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = local.Id.ToString("D")
        };

        var result = JobValidation.ValidateJob(new Job { Steps = [step], LocalValues = [local] });

        Assert.False(result.IsValid);
        Assert.False(Assert.Single(result.Steps).IsValid);
    }

    [Fact]
    public void ValidateJob_AcceptsCompatibleVariableSubproperty()
    {
        var rectangle = new JobVariable
        {
            Name = "Bereich",
            ValueKind = ResultValueKind.Rectangle,
            Value = System.Text.Json.JsonSerializer.SerializeToNode(
                new TaskAutomation.Contracts.Geometry.PixelRegion(250, 20, 30, 40))
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = rectangle.Id.ToString("D"),
            ValuePath = "X"
        };

        var result = JobValidation.ValidateJob(new Job { Steps = [step], Variables = [rectangle] });

        Assert.True(result.IsValid, Assert.Single(result.Steps).Error);
    }

    [Fact]
    public void ValidateJob_RejectsMissingEmbeddedProcessReference()
    {
        var step = new ActiveProcessStep
        {
            Settings = new()
            {
                Target = new()
                {
                    ProcessSource = new ResultBinding
                    {
                        ProviderId = ValueProviderIds.JobVariable,
                        SourceId = Guid.NewGuid().ToString("D")
                    }
                }
            }
        };

        var result = JobValidation.ValidateJob(new Job { Steps = [step] });

        Assert.False(result.IsValid);
        Assert.Contains("nicht vorhandene Wertquelle", Assert.Single(result.Steps).Error);
    }

    [Fact]
    public void ValidateJob_RejectsStoredValueWhosePayloadDoesNotMatchItsDeclaredType()
    {
        var variable = new JobVariable
        {
            Name = "Delay",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create("not-an-integer")
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };

        var result = JobValidation.ValidateJob(new Job { Steps = [step], Variables = [variable] });

        Assert.False(result.IsValid);
        Assert.Contains("keinen gültigen Wert", Assert.Single(result.Steps).Error);
    }

    [Fact]
    public void ValidateJob_IgnoresReferencesInInactiveFields()
    {
        var step = new StartProcessStep
        {
            Settings = new()
            {
                Action = StartProcessAction.Start,
                ExecutablePath = "notepad.exe",
                Target = new()
                {
                    ProcessSource = new ResultBinding
                    {
                        ProviderId = ValueProviderIds.JobVariable,
                        SourceId = Guid.NewGuid().ToString("D")
                    }
                }
            }
        };

        var result = JobValidation.ValidateJob(new Job { Steps = [step] });

        Assert.True(result.IsValid, Assert.Single(result.Steps).Error);
    }

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
    public void ValidateCandidate_AcceptsRoiWithLegacyEmptyEnabledBinding()
    {
        var coordinate = new JobVariable
        {
            Name = "Coordinate",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(10)
        };
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var step = new OcrStep
        {
            Id = "ocr",
            Settings = new()
            {
                ImageSource = new ResultBinding
                {
                    SourceStepId = capture.Id,
                    PropertyId = "image",
                    PropertyPath = nameof(DesktopDuplicationResult.Image)
                },
                EnableROI = true,
                ROI = new TaskAutomation.Contracts.Geometry.PixelRegion(0, 0, 10, 10)
            }
        };
        step.Inputs["roi"] = new ResultBinding
        {
            SchemaId = ValueBindingSchemaRegistry.Roi,
            Members = new Dictionary<string, ResultBinding>
            {
                ["enabled"] = new(),
                ["x"] = LocalBinding(coordinate),
                ["y"] = LocalBinding(coordinate),
                ["width"] = LocalBinding(coordinate),
                ["height"] = LocalBinding(coordinate)
            }
        };

        var result = JobValidation.ValidateCandidate([capture], step, [capture, step], [coordinate]);

        Assert.True(result.IsValid, result.Error);
    }

    private static ResultBinding LocalBinding(JobVariable variable) => new()
    {
        ProviderId = ValueProviderIds.JobVariable,
        SourceId = variable.Id.ToString("D")
    };

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
    public void ValidateJob_RejectsComparisonBetweenDifferentEnumTypes()
    {
        var actual = new JobVariable
        {
            Name = "Workflow state",
            ValueKind = ResultValueKind.Enum,
            Value = System.Text.Json.Nodes.JsonValue.Create("Active"),
            EnumTypeName = "workflow.state",
            EnumValues = ["Active", "Inactive"]
        };
        var expected = new JobVariable
        {
            Name = "Window state",
            ValueKind = ResultValueKind.Enum,
            Value = System.Text.Json.Nodes.JsonValue.Create("Active"),
            EnumTypeName = "window.state",
            EnumValues = ["Active", "Inactive"]
        };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = actual.Id.ToString("D"),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = expected.Id.ToString("D")
            }
        };
        var job = new Job
        {
            Variables = [actual, expected],
            Steps = [new IfStep { Settings = new() { Conditions = [condition] } }, new EndIfStep()]
        };

        var result = JobValidation.ValidateJob(job);

        Assert.False(result.IsValid);
        Assert.Contains("denselben Datentyp", result.Steps.Single(step => !step.IsValid).Error);
    }

    [Fact]
    public void ValidateJob_AcceptsLegacyTextTypedDirectEnumComparisonForUserChoiceOption()
    {
        var source = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test" }
                ]
            }
        };
        var comparison = new LocalValue
        {
            Name = "Selected choice",
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("mode-prod")
        };
        var condition = new StepCondition
        {
            SourceStepId = source.Id,
            PropertyId = "selected_option_id",
            PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = comparison.Id.ToString("D")
            }
        };
        var job = new Job
        {
            Steps =
            [
                source,
                new IfStep { Settings = new IfConditionSettings { Conditions = [condition] } },
                new EndIfStep()
            ],
            LocalValues = [comparison]
        };

        var validation = JobValidation.ValidateJob(job);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine,
            validation.Steps.Where(step => !step.IsValid).Select(step => step.Error)));
    }

    [Fact]
    public void ValidateJob_AcceptsLegacyGenericUserChoiceEnumForDirectComparison()
    {
        var source = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test" }
                ]
            }
        };
        var comparison = new LocalValue
        {
            Name = "Selected choice",
            ValueKind = ResultValueKind.Enum,
            Value = System.Text.Json.Nodes.JsonValue.Create("mode-prod"),
            EnumTypeName = nameof(UserChoiceResult),
            EnumValues = ["mode-prod", "mode-test"]
        };
        var condition = new StepCondition
        {
            SourceStepId = source.Id,
            PropertyId = "selected_option_id",
            PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = comparison.Id.ToString("D")
            }
        };
        var job = new Job
        {
            Steps =
            [
                source,
                new IfStep { Settings = new IfConditionSettings { Conditions = [condition] } },
                new EndIfStep()
            ],
            LocalValues = [comparison]
        };

        var validation = JobValidation.ValidateJob(job);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine,
            validation.Steps.Where(step => !step.IsValid).Select(step => step.Error)));
    }

    [Fact]
    public void ValidateJob_EmptyJob_IsValid() => Assert.True(JobValidation.ValidateJob(new Job()).IsValid);

    [Fact]
    public void ValidateJob_RejectsWrongTypeInsideStructuredBinding()
    {
        var step = new ActiveProcessStep();
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        var invalidTitle = new LocalValue
        {
            OwnerStepId = step.Id,
            InputPath = $"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains",
            ValueKind = ResultValueKind.Boolean,
            Value = System.Text.Json.Nodes.JsonValue.Create(true)
        };
        job.LocalValues.Add(invalidTitle);
        ValueBindingTree.Set(step.Inputs, invalidTitle.InputPath, new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = invalidTitle.Id.ToString("D")
        });
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByType(typeof(ActiveProcessStep), out var definition));
        ValueBindingTree.ApplySchemas(step.Inputs, definition.Descriptor.Fields);

        var result = JobValidation.ValidateJob(job);

        Assert.False(result.IsValid);
        Assert.Contains("window_title_contains", Assert.Single(result.Steps, item => !item.IsValid).Error);
    }

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
        var step = new WindowsStateQueryStep
        {
            Settings = new()
            {
                QueryType = "filesystem.path",
                Parameters = new(StringComparer.OrdinalIgnoreCase) { ["path"] = "C:\\temp" }
            }
        };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_UnknownWindowsQuery_IsInvalid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "unknown.query" } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Theory]
    [InlineData("personalization.wallpaper", "path", null)]
    [InlineData("audio.master_volume", "value", "")]
    public void ValidateStep_WindowsSettingRequiredParameterWithoutUsableValue_IsInvalid(string settingId, string parameter, string? value)
    {
        var step = new WindowsSettingChangeStep
        {
            Settings = new() { SettingId = settingId, Parameters = new() { [parameter] = value } }
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
        var job = new Job
        {
            Steps = [source, new IfStep { Settings = new() { Conditions = [condition] } },
            new ShowTextStep { Settings = new() { Text = "muted" } }, new ElseStep(),
            new ShowTextStep { Settings = new() { Text = "audible" } }, new EndIfStep()]
        };
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
        Assert.False(JobValidation.ValidateJob(new Job
        {
            Steps = [source, new IfStep { Settings = settings }, new ElseStep(),
            new ElseIfStep { Settings = settings }, new EndIfStep()]
        }).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToLaterStep_IsInvalid()
    {
        var display = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" }
            }
        };
        var source = AudioStep("audio");
        Assert.False(JobValidation.ValidateStep([display, source], display).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToPriorCompatibleStep_IsValid()
    {
        var source = AudioStep("audio");
        var display = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" }
            }
        };
        Assert.True(JobValidation.ValidateStep([source, display], display).IsValid);
    }

    [Fact]
    public void ValidateJob_UsesPersistedConditionInputInsteadOfStaleStepSettings()
    {
        var source = AudioStep("audio");
        var settings = new IfConditionSettings
        {
            Conditions = [Condition(source.Id, "IsMuted", ConditionOperator.IsTrue)]
        };
        var stored = new LocalValue
        {
            Name = "Conditions",
            ValueKind = ResultValueKind.ResultObject,
            Value = System.Text.Json.JsonSerializer.SerializeToNode(settings)
        };
        var step = new IfStep();
        step.Inputs[IfStepDefinition.ConditionsFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = stored.Id.ToString("D")
        };

        var result = JobValidation.ValidateJob(new Job
        {
            Steps = [source, step, new EndIfStep()],
            LocalValues = [stored]
        });

        Assert.True(result.IsValid, string.Join(Environment.NewLine,
            result.Steps.Where(item => !item.IsValid).Select(item => item.Error)));
    }

    [Fact]
    public void ValidateStep_RejectsDisabledInvalidControlFlowStep()
    {
        var step = new IfStep { IsEnabled = false };

        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_AcceptsStepReferenceWithDifferentIdCasing()
    {
        var source = AudioStep("Audio");
        var step = new IfStep
        {
            Settings = new() { Conditions = [Condition("audio", "IsMuted", ConditionOperator.IsTrue)] }
        };

        Assert.True(JobValidation.ValidateStep([source, step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_RejectsCollectionConditionProperty()
    {
        var source = new YOLODetectionStep { Id = "detections" };
        var property = StepResultMetadata.GetResultTypeForStep(source)!.Properties
            .First(candidate => candidate.Cardinality == ResultCardinality.Collection);
        var condition = new StepCondition
        {
            SourceStepId = source.Id,
            PropertyId = property.StableId,
            PropertyPath = property.Name,
            Operator = ConditionOperator.Equals,
            ComparisonValue = "value"
        };
        var step = new IfStep { Settings = new() { Conditions = [condition] } };

        var result = JobValidation.ValidateStep([source, step], step);

        Assert.False(result.IsValid);
        Assert.Contains("Sammlung", result.Error);
    }

    [Fact]
    public void ValidateStep_RejectsUnknownComparisonKind()
    {
        var source = AudioStep("audio");
        var condition = Condition(source.Id, "IsMuted", ConditionOperator.Equals);
        condition.Comparison = new ComparisonOperand
        {
            Kind = (ComparisonOperandKind)999,
            Value = bool.TrueString
        };
        var step = new IfStep { Settings = new() { Conditions = [condition] } };

        var result = JobValidation.ValidateStep([source, step], step);

        Assert.False(result.IsValid);
        Assert.Contains("Vergleichsart", result.Error);
    }

    [Fact]
    public void ValidateStep_ReportsEveryInvalidCondition()
    {
        var step = new IfStep
        {
            Settings = new()
            {
                Conditions =
                [
                    Condition("missing-1", "Value", ConditionOperator.Equals),
                    Condition("missing-2", "Value", ConditionOperator.Equals)
                ]
            }
        };

        var result = JobValidation.ValidateStep([step], step);

        Assert.False(result.IsValid);
        Assert.Contains("Bedingung 1", result.Error);
        Assert.Contains("Bedingung 2", result.Error);
    }

    [Fact]
    public void RemoveInvalidSourceSelections_RemovesMissingButPreservesTemporarilyInvalidReferences()
    {
        var source = AudioStep("audio");
        var missing = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { SourceStepId = "gone", PropertyPath = "Text" }
            }
        };
        var later = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { SourceStepId = "audio", PropertyPath = "Text" }
            }
        };
        JobValidation.RemoveInvalidSourceSelections([missing, later, source]);
        Assert.Equal(string.Empty, missing.Settings.TextResult.SourceStepId);
        Assert.Equal("audio", later.Settings.TextResult.SourceStepId);
    }

    private static WindowsStateQueryStep AudioStep(string id) => new() { Id = id, Settings = new() { QueryType = "audio.volume" } };
    private static StepCondition Condition(string id, string path, ConditionOperator op) => new()
    { SourceStepId = id, PropertyPath = path, Operator = op };
}
