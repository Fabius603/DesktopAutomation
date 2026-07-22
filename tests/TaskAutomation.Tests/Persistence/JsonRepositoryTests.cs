using Common.JsonRepository;
using System.Text.Json;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Persistence;

public sealed class JsonRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItem()
    {
        using var directory = new TemporaryDirectory();
        using var repository = Repository(directory.Path);
        var item = new Item("one", "Änderung", 42);
        await repository.SaveAsync(item);
        Assert.Equal(item, await repository.LoadAsync("one"));
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingItemAtomicallyAndRemovesTempFile()
    {
        using var directory = new TemporaryDirectory();
        using var repository = Repository(directory.Path);
        await repository.SaveAsync(new("one", "old", 1));
        await repository.SaveAsync(new("one", "new", 2));
        Assert.Equal("new", (await repository.LoadAsync("one"))!.Text);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task SaveAll_RemovesFilesNoLongerPresent()
    {
        using var directory = new TemporaryDirectory();
        using var repository = Repository(directory.Path);
        await repository.SaveAllAsync([new("one", "1", 1), new("two", "2", 2)]);
        await repository.SaveAllAsync([new("two", "updated", 3)]);
        Assert.Null(await repository.LoadAsync("one"));
        Assert.Equal("updated", (await repository.LoadAsync("two"))!.Text);
    }

    [Fact]
    public async Task LoadAll_IgnoresCorruptJsonAndReadsValidFiles()
    {
        using var directory = new TemporaryDirectory();
        using var repository = Repository(directory.Path);
        await repository.SaveAsync(new("valid", "ok", 1));
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "broken.json"), "{broken");
        var item = Assert.Single(await repository.LoadAllAsync());
        Assert.Equal("valid", item.Key);
    }

    [Fact]
    public async Task LoadAsync_AcceptsCommentsTrailingCommaAndCaseInsensitiveProperties()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "manual.json"),
            """{/*comment*/"KEY":"manual","text":"ok","number":7,}""");
        using var repository = Repository(directory.Path);
        var item = await repository.LoadAsync("manual");
        Assert.Equal(new Item("manual", "ok", 7), item);
    }

    [Fact]
    public async Task EmptyKeysDoNotReadOrDeleteFiles()
    {
        using var directory = new TemporaryDirectory();
        using var repository = Repository(directory.Path);
        await repository.SaveAsync(new("one", "ok", 1));
        Assert.Null(await repository.LoadAsync(" "));
        await repository.DeleteAsync("");
        Assert.NotNull(await repository.LoadAsync("one"));
    }

    [Theory]
    [InlineData(null, "Unnamed")]
    [InlineData("  ", "Unnamed")]
    [InlineData(" a   b ", "a b")]
    [InlineData("ä/b:c", "b_c")]
    public void NamePolicy_SanitizeProducesSafeStableName(string? input, string expected) =>
        Assert.Equal(expected, NamePolicy.Sanitize(input));

    [Fact]
    public void NamePolicy_SanitizeLimitsLengthAndMakeUniqueAddsCounter()
    {
        Assert.Equal(30, NamePolicy.Sanitize(new string('x', 100)).Length);
        Assert.Equal("Name (3)", NamePolicy.MakeUnique("Name", new HashSet<string> { "Name", "Name (2)" }));
    }

    private static JsonRepository<Item> Repository(string path) => new(new JsonRepositoryOptions
        { DirectoryPath = path, JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true } }, item => item.Key);
    private sealed record Item(string Key, string Text, int Number);
}
