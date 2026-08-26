# GitHub Copilot instructions — GroupDocs.Comparison.Mcp.Tests

This repository holds **integration tests** and deployment how-to guides for the [`GroupDocs.Comparison.Mcp`](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp) NuGet package — an MCP server that exposes GroupDocs.Comparison for .NET as AI-callable tools.

This repo is **not** the server itself. The server source lives at https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp. This repo consumes only the **published** NuGet artifact (no project references to the server), launches it via `dnx`, connects as an MCP stdio client, and exercises the advertised tools.

## Run the tests

```bash
dotnet restore
dotnet build -c Release
dotnet test  -c Release

# Test a specific published version
dotnet test -c Release -p:McpPackageVersion=26.8.0
# or:  MCP_PACKAGE_VERSION=26.8.0 dotnet test -c Release

# Unlock licensed-mode tests (drops watermarks)
GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic dotnet test -c Release

# Fastest smoke: discovery suite only (no tool invocations)
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"
```

## Tools under test (3)

The server advertises three tools; wire-format names are PascalCase:

- **Compare** — source vs target diff → marked-up result file + structured `Changes:` JSON.
- **AnalyzeChanges** — structured change list **without** rendering a file (asserts no `*_compared` output is written).
- **GetDocumentInfo** — file type, page count, size, per-page dimensions; read-only, unaffected by license state.

`ToolDiscoveryTests` expects exactly 3 tools.

## Key facts

- **Tests the shipped NuGet, not the source** — no `ProjectReference` to the server; only references `ModelContextProtocol`. If a test needs a server-side change, file an issue in the server repo instead of working around it here.
- **Version under test flows through `Directory.Build.props`** (`<McpPackageVersion>`) — single source of truth; CI overrides it via env var / workflow input.
- **Keyword-based tool resolution** (`ToolCatalog`) — resolve tools by case-insensitive keyword (`compare`, `analyze`, `document_info`), not hardcoded string literals, to stay robust against renames/casing.
- **Two synthetic PDFs** (`source.pdf` "Original Document", `target.pdf` "Modified Document") are built at startup so Compare always has a known diff; self-comparison must yield "No changes detected." Real samples under `sample-docs/` are exercised via self-comparison theories.
- **Evaluation mode:** Compare produces watermarked output (does not throw); tests assert non-error responses and output existence in both eval and licensed mode.
- **Target framework is `net10.0` only.**
- Any behaviour change adds a `changelog/NNN-slug.md` entry; new deployment channels get a guide under `how-to/` and a `README.md` update.

## Environment variables (passed through to the server)

- `GROUPDOCS_MCP_STORAGE_PATH` — base folder for input + output (defaults to cwd)
- `GROUPDOCS_MCP_OUTPUT_PATH` — optional, routes output to a separate folder
- `GROUPDOCS_LICENSE_PATH` — path to `GroupDocs.Total.lic`; omit for evaluation mode

## What this is NOT

- Not the MCP server (separate repo, linked above), not the GroupDocs.Comparison **SDK**, and not **GroupDocs.Comparison Cloud** (a separate REST API product).

## Links

- Server repository: https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp
- NuGet package: https://www.nuget.org/packages/GroupDocs.Comparison.Mcp
- Docker images: ghcr.io/groupdocs-comparison/comparison-net-mcp and docker.io/groupdocs/comparison-net-mcp
- How-to guides: see the `how-to/` folder in this repo
- How-to articles (blog): https://blog.groupdocs.com/categories/groupdocs.comparison-product-family/
