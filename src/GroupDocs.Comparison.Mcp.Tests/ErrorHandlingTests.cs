using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class ErrorHandlingTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ErrorHandlingTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Compare_UnknownSourceFile_ReturnsErrorListingAvailableFiles()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Compare.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = "does-not-exist.pdf" },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        var isErrorReported = (response.IsError ?? false)
            || body.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || body.Contains("available", StringComparison.OrdinalIgnoreCase)
            || body.Contains(SampleDocuments.SourcePdf, StringComparison.OrdinalIgnoreCase);

        Assert.True(isErrorReported,
            $"Expected an error / available-files hint for an unknown source file. Response:\n{body}");
    }

    [Fact]
    public async Task Compare_CorruptedFile_DoesNotCrashServer()
    {
        var corrupted = "corrupted.pdf";
        await File.WriteAllBytesAsync(
            Path.Combine(_fixture.StoragePath, corrupted),
            new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0xDE, 0xAD, 0xBE, 0xEF });

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        try
        {
            var response = await _fixture.Client.CallToolAsync(
                catalog.Compare.Name,
                new Dictionary<string, object?>
                {
                    ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = corrupted },
                    ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                });
            _output.WriteLine($"Tool response: {ToolResponse.Text(response)}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Call threw (acceptable): {ex.GetType().Name}: {ex.Message}");
        }

        // Prove the server is still alive by making another call.
        var listAfter = await _fixture.Client.ListToolsAsync();
        Assert.NotEmpty(listAfter);
    }

    [Fact]
    public async Task PasswordParameters_AreAcceptedByCompare()
    {
        // Wrong passwords on non-protected files — the tool should treat them as
        // no-ops or return a clear error, but must accept the schema.
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Compare.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
                ["sourcePassword"] = "not-a-real-password",
                ["targetPassword"] = "not-a-real-password",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.False(string.IsNullOrEmpty(body));
    }
}
