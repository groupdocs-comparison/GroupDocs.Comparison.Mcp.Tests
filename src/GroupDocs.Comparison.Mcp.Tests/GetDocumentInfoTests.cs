using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class GetDocumentInfoTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GetDocumentInfoTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task GetDocumentInfo_SourcePdf_ReturnsFileTypeAndPageCount()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal(SampleDocuments.SourcePdf, json.GetProperty("fileName").GetString());
        Assert.Equal("pdf", json.GetProperty("fileType").GetProperty("extension").GetString(), ignoreCase: true);
        Assert.True(json.GetProperty("pageCount").GetInt32() >= 1,
            "Expected at least one page in the synthetic PDF.");
        Assert.True(json.GetProperty("sizeBytes").GetInt64() > 0,
            "Expected a non-zero size for the synthetic PDF.");
    }

    public static IEnumerable<object[]> RealSampleData() => new[]
    {
        new object[] { SampleDocuments.SamplePdf  },
        new object[] { SampleDocuments.SampleDocx },
        new object[] { SampleDocuments.SampleXlsx },
        new object[] { SampleDocuments.SamplePptx },
    };

    [Theory]
    [MemberData(nameof(RealSampleData))]
    public async Task GetDocumentInfo_RealSample_ReturnsValidInfo(string fileName)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error for '{fileName}': {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        // OOXML / large-PDF info responses can exceed the tool's output budget;
        // verify by substring rather than by full JsonDocument.Parse.
        Assert.Contains("\"fileType\"", body);
        Assert.Contains("\"pageCount\"", body);
        Assert.Contains("\"sizeBytes\"", body);
        Assert.Contains(fileName, body);
    }
}
