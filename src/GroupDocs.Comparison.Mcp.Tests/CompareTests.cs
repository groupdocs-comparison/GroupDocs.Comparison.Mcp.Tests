using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

/// GroupDocs.Comparison.Compare produces output in evaluation mode (with
/// watermarks) so happy-path assertions work in both eval and licensed mode.
[Collection(McpServerCollection.Name)]
public class CompareTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CompareTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Compare_DifferentSyntheticPdfs_ProducesMarkedUpOutputAndChangeSummary()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Compare.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        // The Compare tool writes <source-stem>_compared<source-ext> to storage.
        var outputPath = Path.Combine(_fixture.StoragePath, "source_compared.pdf");
        Assert.True(File.Exists(outputPath),
            $"Expected marked-up output at '{outputPath}'. Response body:\n{body}");

        // Either a positive change count or "No changes detected" must appear.
        Assert.True(
            body.Contains("change(s) detected", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("No changes detected", StringComparison.OrdinalIgnoreCase),
            $"Expected change-count summary in response:\n{body}");

        // Source vs target with different titles must produce at least one change.
        Assert.Contains("change(s) detected", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compare_SamePdfTwice_ReportsNoChanges()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Compare.Name,
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
    }

    public static IEnumerable<object[]> RealSampleSelfComparisons() => new[]
    {
        new object[] { SampleDocuments.SamplePdf  },
        new object[] { SampleDocuments.SampleDocx },
        new object[] { SampleDocuments.SampleXlsx },
    };

    [Theory]
    [MemberData(nameof(RealSampleSelfComparisons))]
    public async Task Compare_RealSampleAgainstItself_ProducesOutputFile(string fileName)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Compare.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = fileName },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Compare failed for '{fileName}': {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var outputPath = Path.Combine(_fixture.StoragePath, $"{stem}_compared{ext}");
        Assert.True(File.Exists(outputPath),
            $"Expected marked-up output at '{outputPath}'. Response body:\n{body}");
    }
}
