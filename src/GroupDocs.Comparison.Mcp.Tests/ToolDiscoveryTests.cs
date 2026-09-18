using System.Text.RegularExpressions;
using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

public class ToolDiscoveryTests : IClassFixture<McpServerFixture>
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ToolDiscoveryTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void ServerInfo_AdvertisesGroupDocsComparisonMcp()
    {
        var info = _fixture.Client.ServerInfo;

        Assert.NotNull(info);
        Assert.Equal("GroupDocs.Comparison.Mcp", info!.Name);
        Assert.False(string.IsNullOrWhiteSpace(info.Version));

        _output.WriteLine($"Server: {info.Name} {info.Version}  (package under test: {_fixture.PackageVersionUnderTest})");
    }

    [Fact]
    public async Task ListTools_ExposesTheFullToolSurface()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        foreach (var tool in catalog.All)
            _output.WriteLine($"tool: {tool.Name} — {tool.Description}");

        // Three product tools plus get_license_status, which GroupDocs.Mcp.Core registers
        // on every server from 26.9.0. Asserted as an exact set rather than a count so a
        // renamed or accidentally-dropped tool fails loudly instead of cancelling out.
        Assert.Equal(
            new[] { "analyze_changes", "compare", "get_document_info", "get_license_status" },
            catalog.All.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task ToolDescriptions_OnlyReferenceToolsThatAreRegistered()
    {
        // An agent reads a tool named in another tool's description as advice to call it.
        // 26.9.0 told agents to "use `compare_documents` instead" — a tool that was never
        // registered (audit finding M3), so following the advice produced "unknown tool".
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);
        var registered = catalog.All.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        // Descriptions name tools in backticked snake_case; result fields are camelCase
        // (`changedText`, `fileName`), so they are not caught by the first pattern.
        var snakeCaseName = new Regex(@"`([a-z][a-z0-9]*(?:_[a-z0-9]+)+)`");
        // A single-word name is still a tool reference when the text calls it one.
        var namedAsTool = new Regex(@"`([a-z][a-z0-9_]*)`\s+tool\b");

        var dangling = new List<string>();
        foreach (var tool in catalog.All)
        {
            var text = tool.Description ?? string.Empty;
            foreach (var match in snakeCaseName.Matches(text).Concat(namedAsTool.Matches(text)))
            {
                var name = match.Groups[1].Value;
                if (!registered.Contains(name))
                    dangling.Add($"{tool.Name} -> `{name}`");
            }
        }

        Assert.True(dangling.Count == 0,
            "Tool descriptions reference tools that are not registered: " +
            string.Join(", ", dangling.Distinct()));
    }

    [Fact]
    public async Task AllTools_HaveNonEmptyDescriptionAndInputSchema()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        Assert.NotEmpty(catalog.All);
        foreach (var tool in catalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' has no description.");

            var schema = tool.JsonSchema;
            Assert.True(schema.ValueKind == System.Text.Json.JsonValueKind.Object,
                $"Tool '{tool.Name}' has no object input schema.");
            Assert.True(schema.TryGetProperty("properties", out _),
                $"Tool '{tool.Name}' schema missing 'properties'.");
        }
    }
}
