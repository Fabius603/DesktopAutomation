using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class FileSystemOperationStepHandlerTests
{
    [Fact]
    public async Task CopyFile_CreatesMissingParentsAndReturnsAffectedTarget()
    {
        using var temp = new TempDirectory();
        var source = temp.File("source.txt", "content");
        var target = Path.Combine(temp.Path, "nested", "target.txt");
        var context = new PipelineContextStub();
        var step = Step(FileSystemOperation.Copy, source, target);

        var result = Assert.IsType<FileSystemOperationResult>(
            await new FileSystemOperationStepHandler().ExecuteAsync(step, context, default));

        Assert.Equal("content", File.ReadAllText(target));
        Assert.Equal(Path.GetFullPath(target), result.TargetPath);
        Assert.Equal(1, result.AffectedFileCount);
        Assert.Equal(new FileInfo(target).Length, result.AffectedBytes);
        Assert.Contains(Path.GetFullPath(target), result.AffectedPaths);
        Assert.Same(result, context.Results.GetRaw(step.Id));
    }

    [Fact]
    public async Task CopyFile_ToExistingDirectory_PreservesSourceName()
    {
        using var temp = new TempDirectory();
        var source = temp.File("source.txt", "content");
        var targetDirectory = Path.Combine(temp.Path, "target");
        Directory.CreateDirectory(targetDirectory);
        var step = Step(FileSystemOperation.Copy, source, targetDirectory);

        var result = Assert.IsType<FileSystemOperationResult>(
            await new FileSystemOperationStepHandler().ExecuteAsync(
                step, new PipelineContextStub(), default));

        var expectedTarget = Path.Combine(targetDirectory, "source.txt");
        Assert.Equal("content", File.ReadAllText(expectedTarget));
        Assert.Equal(Path.GetFullPath(expectedTarget), result.TargetPath);
    }

    [Fact]
    public async Task Move_ResolvesBothPathsFromPreviousStepResults()
    {
        using var temp = new TempDirectory();
        var source = temp.File("source.txt", "move");
        var target = Path.Combine(temp.Path, "target.txt");
        var context = new PipelineContextStub();
        context.Results.Set<WindowsStateQueryStep>(
            new FileSystemPathQueryResult { WasExecuted = true, Path = source }, "source");
        context.Results.Set<WindowsStateQueryStep>(
            new FileSystemPathQueryResult { WasExecuted = true, Path = target }, "target");
        var step = new FileSystemOperationStep
        {
            Id = "operation",
            Settings = new()
            {
                Operation = FileSystemOperation.Move,
                SourceMode = FileSystemPathSource.TaskResult,
                SourceResult = new() { SourceStepId = "source", PropertyPath = "Path", PropertyId = "path" },
                TargetMode = FileSystemPathSource.TaskResult,
                TargetResult = new() { SourceStepId = "target", PropertyPath = "Path", PropertyId = "path" }
            }
        };

        var result = Assert.IsType<FileSystemOperationResult>(
            await new FileSystemOperationStepHandler().ExecuteAsync(step, context, default));

        Assert.False(File.Exists(source));
        Assert.Equal("move", File.ReadAllText(target));
        Assert.Equal(Path.GetFullPath(target), result.TargetPath);
    }

    [Fact]
    public async Task Rename_ChangesOnlyNameAndPreservesDirectory()
    {
        using var temp = new TempDirectory();
        var source = temp.File("before.txt", "rename");
        var step = new FileSystemOperationStep
        {
            Settings = new()
            {
                Operation = FileSystemOperation.Rename,
                SourcePath = source,
                NewName = "after.txt"
            }
        };

        var result = Assert.IsType<FileSystemOperationResult>(
            await new FileSystemOperationStepHandler().ExecuteAsync(
                step, new PipelineContextStub(), default));

        var target = Path.Combine(temp.Path, "after.txt");
        Assert.False(File.Exists(source));
        Assert.True(File.Exists(target));
        Assert.Equal(Path.GetFullPath(target), result.TargetPath);
    }

    [Fact]
    public async Task DeleteWithFilter_DeletesOnlyDirectMatchesAndKeepsSourceFolder()
    {
        using var temp = new TempDirectory();
        temp.File("delete.tmp", "x");
        temp.File("keep.txt", "y");
        Directory.CreateDirectory(Path.Combine(temp.Path, "nested"));
        File.WriteAllText(Path.Combine(temp.Path, "nested", "nested.tmp"), "z");
        var step = new FileSystemOperationStep
        {
            Settings = new()
            {
                Operation = FileSystemOperation.Delete,
                SourcePath = temp.Path,
                Filter = "*.tmp"
            }
        };

        var result = Assert.IsType<FileSystemOperationResult>(
            await new FileSystemOperationStepHandler().ExecuteAsync(
                step, new PipelineContextStub(), default));

        Assert.True(Directory.Exists(temp.Path));
        Assert.False(File.Exists(Path.Combine(temp.Path, "delete.tmp")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "keep.txt")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "nested", "nested.tmp")));
        Assert.Equal(1, result.AffectedCount);
    }

    [Fact]
    public async Task MissingSourceAndExistingTargetFailWithoutStoredResult()
    {
        using var temp = new TempDirectory();
        var context = new PipelineContextStub();
        var missing = Step(FileSystemOperation.Copy,
            Path.Combine(temp.Path, "missing.txt"), Path.Combine(temp.Path, "target.txt"));
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => new FileSystemOperationStepHandler().ExecuteAsync(missing, context, default));
        Assert.Null(context.Results.GetRaw(missing.Id));

        var source = temp.File("source.txt", "x");
        var target = temp.File("existing.txt", "y");
        var existing = Step(FileSystemOperation.Copy, source, target);
        await Assert.ThrowsAsync<IOException>(
            () => new FileSystemOperationStepHandler().ExecuteAsync(existing, context, default));
        Assert.Equal("y", File.ReadAllText(target));
        Assert.Null(context.Results.GetRaw(existing.Id));
    }

    private static FileSystemOperationStep Step(
        FileSystemOperation operation, string source, string target) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Settings = new() { Operation = operation, SourcePath = source, TargetPath = target }
    };

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "DesktopAutomation.Tests", Guid.NewGuid().ToString("N"));
        public TempDirectory() => Directory.CreateDirectory(Path);
        public string File(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(path, content);
            return path;
        }
        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
