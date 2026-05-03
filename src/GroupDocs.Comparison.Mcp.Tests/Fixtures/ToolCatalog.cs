using ModelContextProtocol.Client;

namespace GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;

/// Resolves tool names by keyword. The server-side attribute [McpServerTool] uses
/// the method name verbatim today (PascalCase: Compare, GetDocumentInfo).
/// Keyword-based resolution keeps tests robust against future renames / casing
/// convention changes.
internal sealed class ToolCatalog
{
    private readonly IReadOnlyList<McpClientTool> _tools;

    private ToolCatalog(IReadOnlyList<McpClientTool> tools) => _tools = tools;

    public static async Task<ToolCatalog> LoadAsync(McpClient client, CancellationToken ct = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return new ToolCatalog(tools.ToList());
    }

    public IReadOnlyList<McpClientTool> All => _tools;

    public McpClientTool Compare => Resolve("compare");
    public McpClientTool DocumentInfo => Resolve("document_info");

    private McpClientTool Resolve(string keyword) =>
        _tools.FirstOrDefault(t => t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No tool with name containing '{keyword}'. Found: {string.Join(", ", _tools.Select(t => t.Name))}");
}
