using System.Text.Json;
using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

/// AnalyzeChanges returns the structured change list WITHOUT rendering a result
/// file. Added in package 26.7.0 — against older packages the tool is absent and
/// ToolCatalog.AnalyzeChanges throws, which correctly fails these tests.
[Collection(McpServerCollection.Name)]
public class AnalyzeChangesTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AnalyzeChangesTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task AnalyzeChanges_DifferentSyntheticPdfs_ReturnsStructuredChanges()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.AnalyzeChanges.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("change(s) detected", body, StringComparison.OrdinalIgnoreCase);

        var changes = ParseChangesJson(body);
        Assert.Equal(JsonValueKind.Array, changes.ValueKind);
        Assert.True(changes.GetArrayLength() >= 1,
            $"Expected at least one change:\n{body}");

        var first = changes[0];
        Assert.True(first.TryGetProperty("id", out _), $"Change missing 'id':\n{body}");
        Assert.True(first.TryGetProperty("type", out _), $"Change missing 'type':\n{body}");
    }

    [Fact]
    public async Task AnalyzeChanges_DoesNotWriteAnOutputFile()
    {
        var before = new HashSet<string>(
            Directory.EnumerateFiles(_fixture.StoragePath).Select(Path.GetFileName)!);

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.AnalyzeChanges.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var after = Directory.EnumerateFiles(_fixture.StoragePath).Select(Path.GetFileName).ToList();
        var added = after.Where(f => !before.Contains(f!)).ToList();

        // Unlike Compare, AnalyzeChanges must not render/save any *_compared file.
        Assert.DoesNotContain(added, f => f!.Contains("_compared", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeChanges_SamePdfTwice_ReportsNoChanges()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.AnalyzeChanges.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("No changes detected", body, StringComparison.OrdinalIgnoreCase);

        // The JSON array is still present and empty.
        var changes = ParseChangesJson(body);
        Assert.Equal(JsonValueKind.Array, changes.ValueKind);
        Assert.Equal(0, changes.GetArrayLength());
    }

    private static JsonElement ParseChangesJson(string body)
    {
        const string marker = "Changes:";
        var idx = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"Expected a 'Changes:' JSON section in response:\n{body}");

        var json = body[(idx + marker.Length)..].Trim();
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
