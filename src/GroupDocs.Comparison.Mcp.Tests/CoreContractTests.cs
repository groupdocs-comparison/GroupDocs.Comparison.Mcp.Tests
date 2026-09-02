using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

/// <summary>
/// The input and error contract shared by every GroupDocs MCP server (GroupDocs.Mcp.Core).
/// </summary>
/// <remarks>
/// Each test here corresponds to a defect confirmed by the 2026-08-16 external audit on all 12
/// products. They cost nothing to run — no metered key, no licence — and they are written to fail
/// on the old behaviour rather than merely tolerate it.
///
/// The audit's sharpest finding was that the existing oracles could not see these defects: an
/// unknown-file assertion of the form <c>IsError || text.Contains("not found")</c> passes on the
/// opaque error it was meant to catch, because the opaque error sets <c>IsError</c>. So these
/// assert the <i>promised</i> text, not merely that something went wrong.
/// </remarks>
public class CoreContractTests : IClassFixture<McpServerFixture>
{
    private const string LicenseStatusTool = "get_license_status";

    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CoreContractTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    // ---- S1: the fileName form the tool descriptions recommend -------------

    [Fact]
    public async Task GetDocumentInfo_WithFileNameOnly_Resolves()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // The descriptions say "just pass the filename the user provided", and the schema
        // allows it — yet this form used to throw an unhandled ArgumentException that the
        // client saw only as "An error occurred invoking 'get_document_info'".
        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["fileName"] = SampleDocuments.SourcePdf },
            });

        Assert.False(response.IsError ?? false,
            $"[{_fixture.Channel}] fileName-only input failed: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        Assert.Equal(SampleDocuments.SourcePdf, json.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task GetDocumentInfo_WithFilePath_StillResolves()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
            });

        Assert.False(response.IsError ?? false,
            $"[{_fixture.Channel}] filePath input regressed: {ToolResponse.Text(response)}");
    }

    // ---- S2: the available-files listing the descriptions promise ----------

    [Fact]
    public async Task GetDocumentInfo_MissingFile_ReturnsTheAvailableFilesListing()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = "definitely-not-here.pdf" },
            });

        var text = ToolResponse.Text(response);
        _output.WriteLine(text);

        // Assert what the tool description actually promises. A loose
        // `IsError || contains("not found")` would pass on the opaque error too.
        Assert.Contains("Available files:", text, StringComparison.Ordinal);
        Assert.Contains(SampleDocuments.SourcePdf, text, StringComparison.Ordinal);
        Assert.DoesNotContain("An error occurred invoking", text, StringComparison.Ordinal);

        // S3: a real failure must be flagged, not just described.
        Assert.True(response.IsError ?? false,
            "A failed operation must set isError so a client can detect it without parsing prose.");
    }

    // ---- S2c: a missing required parameter must be self-correctable --------

    [Fact]
    public async Task GetDocumentInfo_WithNoArguments_NamesTheMissingParameter()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name, new Dictionary<string, object?>());

        var text = ToolResponse.Text(response);
        _output.WriteLine(text);

        Assert.True(response.IsError ?? false, "A missing required parameter is a failure.");
        Assert.Contains("file", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("An error occurred invoking", text, StringComparison.Ordinal);
    }

    // ---- The Core-shipped status tool --------------------------------------

    [Fact]
    public async Task GetLicenseStatus_IsRegisteredAndDescribesTheServer()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);
        Assert.Contains(catalog.All, t => t.Name == LicenseStatusTool);

        var response = await _fixture.Client.CallToolAsync(
            LicenseStatusTool, new Dictionary<string, object?>());

        Assert.False(response.IsError ?? false,
            $"[{_fixture.Channel}] {LicenseStatusTool} failed: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        var mode = json.GetProperty("mode").GetString();
        Assert.Contains(mode, new[] { "evaluation", "licensed", "metered" });

        // Before this tool existed there was no way for a client to discover it was
        // running unlicensed — the audit called that out explicitly.
        Assert.Equal(mode != "evaluation", json.GetProperty("licensed").GetBoolean());

        // The engine version was likewise invisible; the family spans 26.3 to 26.8.
        var engine = json.GetProperty("engine");
        Assert.Equal("GroupDocs.Comparison", engine.GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(engine.GetProperty("version").GetString()));

        // The status tool must not report the server assembly as the engine.
        Assert.NotEqual(
            json.GetProperty("server").GetProperty("name").GetString(),
            engine.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetLicenseStatus_WithoutMeteredKeys_ReportsNoConsumption()
    {
        if (MeteredKeys.Configured)
        {
            // Metered is configured for this run, so the no-consumption assertion does not
            // apply. MeteredLicensingTests covers that case.
            return;
        }

        var response = await _fixture.Client.CallToolAsync(
            LicenseStatusTool, new Dictionary<string, object?>());

        var json = ToolResponse.Json(response);

        // Absent, not zero. "0 consumed" would be a plausible-looking lie outside metered mode.
        Assert.False(json.TryGetProperty("consumption", out _),
            "Consumption must be omitted entirely when not running under a metered key.");
    }
}
