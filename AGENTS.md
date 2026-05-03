# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

**Integration tests** for the [`GroupDocs.Comparison.Mcp`](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp) NuGet package — an MCP server that exposes GroupDocs.Comparison for .NET as an AI-callable tool.

This repo is **not** the server itself. The server lives at [groupdocs-comparison/GroupDocs.Comparison.Mcp](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp). This repo:

1. Consumes only the **published** NuGet artifact (no project references).
2. Launches the server via `dnx`, connects as an MCP stdio client, and exercises the advertised tool.
3. Doubles as a copy-pasteable set of example configs and how-to guides for all deployment channels (NuGet, Docker, MCP registry, Claude Desktop, VS Code).

## Folder layout

```
src/GroupDocs.Comparison.Mcp.Tests/
  Fixtures/
    McpServerFixture.cs          ← launches dnx child process, wires stdio MCP client
    SampleDocuments.cs           ← builds two synthetic PDFs (source vs target) at runtime
    ToolCatalog.cs               ← keyword-based tool name resolution (compare, documentinfo)
    ToolResponse.cs              ← CallToolResult text/JSON extraction
    CommandResolver.cs           ← cross-platform dnx.cmd resolution on Windows
    PackageVersion.cs            ← pulls version from env / assembly metadata / default
  ToolDiscoveryTests.cs          ← handshake, tools/list, schema validation
  CompareTests.cs                ← source vs target PDF diff + self-comparison + real-sample theory
  GetDocumentInfoTests.cs        ← file type, page count, size, per-page dimensions for synthetic + real samples
  ErrorHandlingTests.cs          ← unknown file, corrupted bytes, password parameters
  GroupDocs.Comparison.Mcp.Tests.csproj
.github/workflows/integration.yml  ← matrix × 3 OS, nightly cron, release-smoke dispatch
changelog/                         ← one MD file per change (NNN-slug.md)
how-to/                            ← user-facing guides for every deployment channel
examples/                          ← claude-desktop.json, vscode-mcp.json, docker-compose.yml
sample-docs/                       ← drop real fixture files here; copied to test output
Directory.Build.props              ← McpPackageVersion property (overridable)
global.json                        ← pinned to .NET 10.0.100
```

## What gets tested

| Area | Covered by |
|---|---|
| Package installs and starts via `dnx` | `McpServerFixture` |
| MCP handshake, server info, version | `ToolDiscoveryTests` |
| `Compare` — source vs target PDF diff + self-comparison + real-sample theory | `CompareTests` |
| `GetDocumentInfo` — file type, page count, size, per-page dimensions for synthetic + real samples | `GetDocumentInfoTests` |
| Unknown / corrupted files, password parameters | `ErrorHandlingTests` |

## Commands you can run

```bash
# Restore + build
dotnet restore
dotnet build -c Release

# Run all tests against the default package version
dotnet test -c Release

# Run against a specific published version
dotnet test -c Release -p:McpPackageVersion=26.5.1
# or
MCP_PACKAGE_VERSION=26.5.1 dotnet test -c Release

# Unlock licensed-mode tests (drops watermarks)
GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic dotnet test -c Release

# Run just the discovery suite (fastest — no tool invocations)
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"
```

## Key design decisions

1. **Keyword-based tool resolution.** `ToolCatalog.Compare` resolves by case-insensitive keyword match (`"compare"`). The MCP C# SDK currently uses the method name verbatim (PascalCase: `Compare`) — keyword resolution keeps tests robust against future renames / casing convention changes.

2. **Two synthetic PDFs.** `SampleDocuments.cs` builds `source.pdf` (title "Original Document") and `target.pdf` (title "Modified Document") from byte arrays at startup. Comparing them must yield ≥ 1 change; comparing source against itself must yield "No changes detected." Real samples live under `sample-docs/` and are auto-copied via the csproj's `<None Include="..\..\sample-docs\**\*">` item; the real-sample theory uses self-comparison since we don't ship paired fixtures.

3. **Evaluation-mode handling.** `GroupDocs.Comparison.Compare()` produces output (watermarked) in eval mode. Tests assert non-error responses and output-file existence in both modes — they don't branch on `GROUPDOCS_LICENSE_PATH` for happy-path assertions.

4. **No project references to the server.** The csproj only references `ModelContextProtocol` 1.1.0. If the server source breaks in the sibling repo, these tests still pass — they validate the shipped NuGet artifact.

## House rules

1. **Changelog entries required** — any PR that changes behaviour adds `changelog/NNN-slug.md` (schema in `changelog/README.md`).
2. **How-to guides track deployment reality** — if the main repo publishes a new channel (e.g. new Docker registry), add a guide under `how-to/` *and* update `README.md`.
3. **Version bumps flow through `Directory.Build.props`** — `<McpPackageVersion>` is the single source of truth for "what version are we testing." CI overrides it via env var / workflow input.
4. **Tests must not require the main repo's source.** If a test needs a server-side change, file an issue there — don't work around it here.
5. **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.

## Release smoke hook

The main repo's `publish_prod.yml` should fire a `repository_dispatch` with `event_type=nuget-published` after `dotnet nuget push` succeeds. The workflow in `.github/workflows/integration.yml` consumes `client_payload.package_version` and runs the matrix against the just-published version.

## What NOT to change

- Do not add a `ProjectReference` to the main repo's `GroupDocs.Comparison.Mcp.csproj`. This repo exists to test the shipped NuGet, not the source.
- Do not hardcode tool names as string literals (`"compare"`). Use `ToolCatalog.Compare.Name`.
- Do not commit real license files or binary fixtures with unclear provenance. License goes through the `GROUPDOCS_LICENSE` CI secret; fixtures in `sample-docs/` must be self-authored or CC0/Apache-2.0.
