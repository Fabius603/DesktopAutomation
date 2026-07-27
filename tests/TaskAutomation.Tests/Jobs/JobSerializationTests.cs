using System.Text.Json;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesPolymorphicStepsBindingsAndWindowsParameters()
    {
        var job = new Job
        {
            Name = "Audio monitor", Repeating = true,
            StartSteps = [new ShowTextStep { Id = "start", Settings = new() { Text = "starting" } }],
            Steps =
            [
                new WindowsStateQueryStep { Id = "audio", Settings = new()
                    { QueryType = "audio.volume", Parameters = new(StringComparer.OrdinalIgnoreCase) { ["device"] = "default" } } },
                new ShowTextStep { Id = "display", Settings = new() { TextSource = ShowTextSource.TaskResult,
                    TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } }
            ],
            EndSteps = [new EndJobStep { Settings = new() { SkipEndSteps = true } }]
        };

        var json = JsonSerializer.Serialize(job);
        var restored = Assert.IsType<Job>(JsonSerializer.Deserialize<Job>(json));

        Assert.Equal(job.Id, restored.Id);
        Assert.True(restored.Repeating);
        Assert.IsType<ShowTextStep>(Assert.Single(restored.StartSteps));
        var audio = Assert.IsType<WindowsStateQueryStep>(restored.Steps[0]);
        Assert.Equal("audio.volume", audio.Settings.QueryType);
        Assert.Equal("default", audio.Settings.Parameters["DEVICE"]);
        var display = Assert.IsType<ShowTextStep>(restored.Steps[1]);
        Assert.Equal("audio", display.Settings.TextResult.SourceStepId);
        Assert.Equal("Percentage", display.Settings.TextResult.PropertyPath);
    }

    [Fact]
    public void Deserialize_LegacyConditionValue_RemainsEffectiveComparison()
    {
        const string json = """
            {"type":"if","id":"if","is_enabled":true,"settings":{"match_mode":"All","conditions":[{"source_step_id":"source","property_path":"Count","operator":"GreaterThan","comparison_value":"2"}]}}
            """;
        var step = Assert.IsType<IfStep>(JsonSerializer.Deserialize<JobStep>(json));
        var condition = Assert.Single(step.Settings.Conditions);
        Assert.Equal(ComparisonOperandKind.Literal, condition.EffectiveComparison.Kind);
        Assert.Equal("2", condition.EffectiveComparison.Value);
    }

    [Fact]
    public void RoundTrip_PreservesStableResultPropertyIds()
    {
        var step = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding
                {
                    SourceStepId = "audio",
                    PropertyId = "volume_percentage",
                    PropertyPath = "Percentage"
                }
            }
        };

        var restored = Assert.IsType<ShowTextStep>(
            JsonSerializer.Deserialize<JobStep>(JsonSerializer.Serialize<JobStep>(step)));
        Assert.Equal("volume_percentage", restored.Settings.TextResult.PropertyId);
        Assert.Equal("Percentage", restored.Settings.TextResult.PropertyPath);
    }

    [Fact]
    public void RoundTrip_PreservesCameraCaptureDeviceSelection()
    {
        JobStep step = new CameraCaptureStep
        {
            Settings = new()
            {
                CameraId = "@device:pnp:camera-id",
                CameraName = "USB Camera",
                QualityMode = CameraQualityMode.Specific,
                Width = 1280,
                Height = 720,
                FramesPerSecond = 29.97,
                PixelFormat = "MJPG"
            }
        };

        var json = JsonSerializer.Serialize(step);
        var restored = Assert.IsType<CameraCaptureStep>(
            JsonSerializer.Deserialize<JobStep>(json));

        Assert.Contains("\"type\":\"camera_capture\"", json);
        Assert.Equal("@device:pnp:camera-id", restored.Settings.CameraId);
        Assert.Equal("USB Camera", restored.Settings.CameraName);
        Assert.Equal(CameraQualityMode.Specific, restored.Settings.QualityMode);
        Assert.Equal(1280, restored.Settings.Width);
        Assert.Equal(720, restored.Settings.Height);
        Assert.Equal(29.97, restored.Settings.FramesPerSecond);
        Assert.Equal("MJPG", restored.Settings.PixelFormat);
    }

    [Fact]
    public void Deserialize_LegacyCameraCaptureDefaultsToAutomaticQuality()
    {
        const string json =
            """{"type":"camera_capture","settings":{"camera_id":"legacy-camera","camera_name":"Camera"}}""";

        var restored = Assert.IsType<CameraCaptureStep>(JsonSerializer.Deserialize<JobStep>(json));

        Assert.Equal(CameraQualityMode.Automatic, restored.Settings.QualityMode);
        Assert.Equal(0, restored.Settings.Width);
        Assert.Equal(0, restored.Settings.Height);
    }

    [Fact]
    public void RoundTrip_PreservesFileSystemOperationPathSourcesAndRetryDefaults()
    {
        JobStep step = new FileSystemOperationStep
        {
            Settings = new()
            {
                Operation = FileSystemOperation.Move,
                SourceMode = FileSystemPathSource.TaskResult,
                SourceResult = new() { SourceStepId = "source", PropertyId = "path", PropertyPath = "Path" },
                TargetMode = FileSystemPathSource.TaskResult,
                TargetResult = new() { SourceStepId = "target", PropertyId = "path", PropertyPath = "Path" }
            }
        };

        var json = JsonSerializer.Serialize(step);
        var restored = Assert.IsType<FileSystemOperationStep>(
            JsonSerializer.Deserialize<JobStep>(json));

        Assert.Contains("\"type\":\"file_system_operation\"", json);
        Assert.Equal(FileSystemOperation.Move, restored.Settings.Operation);
        Assert.Equal("source", restored.Settings.SourceResult.SourceStepId);
        Assert.Equal("target", restored.Settings.TargetResult.SourceStepId);
        Assert.True(restored.Settings.CreateParentDirectories);
        Assert.True(restored.Settings.RetryLockedFiles);
        Assert.Equal(3, restored.Settings.RetryCount);
        Assert.Equal(100, restored.Settings.RetryDelayMs);
    }

    [Fact]
    public void RoundTrip_PreservesSaveImageSettingsAndBinding()
    {
        JobStep step = new SaveImageStep
        {
            Settings = new()
            {
                SavePath = "C:\\captures",
                FileName = "snapshot.png",
                ImageSource = new()
                {
                    SourceStepId = "capture",
                    PropertyId = "image",
                    PropertyPath = "Image"
                }
            }
        };

        var json = JsonSerializer.Serialize(step);
        var restored = Assert.IsType<SaveImageStep>(
            JsonSerializer.Deserialize<JobStep>(json));

        Assert.Contains("\"type\":\"save_image\"", json);
        Assert.Equal("C:\\captures", restored.Settings.SavePath);
        Assert.Equal("snapshot.png", restored.Settings.FileName);
        Assert.Equal("capture", restored.Settings.ImageSource.SourceStepId);
        Assert.Equal("image", restored.Settings.ImageSource.PropertyId);
    }

    [Fact]
    public void RoundTrip_PreservesMultipleVisualOverlayResults()
    {
        var textId = Guid.NewGuid();
        JobStep step = new SaveImageStep
        {
            Settings = new()
            {
                Overlay = new()
                {
                    DetectionResults =
                    [
                        new() { SourceStepId = "first", PropertyId = "all_detections", PropertyPath = "AllDetections" },
                        new() { SourceStepId = "second", PropertyId = "all_detections", PropertyPath = "AllDetections" }
                    ],
                    TextResults =
                    [
                        new()
                        {
                            Id = textId,
                            Result = new() { SourceStepId = "value", PropertyId = "is_active", PropertyPath = "IsActive" },
                            FontSize = 31,
                            FontColor = "#123456",
                            Opacity = .6f,
                            OffsetX = 12,
                            OffsetY = 34
                        }
                    ]
                }
            }
        };

        var restored = Assert.IsType<SaveImageStep>(
            JsonSerializer.Deserialize<JobStep>(JsonSerializer.Serialize(step)));

        Assert.Equal(["first", "second"],
            restored.Settings.Overlay.DetectionResults.Select(binding => binding.SourceStepId));
        var text = Assert.Single(restored.Settings.Overlay.TextResults);
        Assert.Equal(textId, text.Id);
        Assert.Equal("value", text.Result.SourceStepId);
        Assert.Equal(31, text.FontSize);
        Assert.Equal("#123456", text.FontColor);
        Assert.Equal(.6f, text.Opacity);
        Assert.Equal(12, text.OffsetX);
        Assert.Equal(34, text.OffsetY);
    }

    [Fact]
    public void NewJobStepIds_AreUniqueAndNonEmpty()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => new TimeoutStep().Id).ToArray();
        Assert.All(ids, id => Assert.True(Guid.TryParse(id, out _)));
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ActiveStepCount_ExcludesDisabledAndFlowControlStepsAcrossAllPhases()
    {
        var job = new Job
        {
            StartSteps = [new TimeoutStep()],
            Steps = [new IfStep(), new ShowTextStep { IsEnabled = false }, new ElseStep(), new EndIfStep(), new TimeoutStep()],
            EndSteps = [new TimeoutStep()]
        };
        Assert.Equal(3, job.ActiveStepCount);
    }
}
