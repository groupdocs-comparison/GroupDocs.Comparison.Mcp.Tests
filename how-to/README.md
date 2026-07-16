# How-to guides

Step-by-step guides for verifying and using every deployment channel of
[`GroupDocs.Comparison.Mcp`](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp).

Each guide is self-contained — pick the one that matches your workflow. They
all point at the same published artifact (`26.7.0` at time of writing).

| # | Guide | When to use |
|---|---|---|
| 01 | [Install from NuGet (dnx + dotnet tool)](01-install-from-nuget.md) | You have the .NET 10 SDK. Fastest path — no Docker required. |
| 02 | [Run via Docker](02-run-via-docker.md) | You'd rather not install .NET, or want isolation from the host. |
| 03 | [Verify on the MCP registry](03-verify-mcp-registry.md) | You want to confirm the package shows up in MCP clients' discovery UIs and that its `server.json` metadata is correct. |
| 04 | [Use with Claude Desktop](04-use-with-claude-desktop.md) | Connect from Claude Desktop (macOS / Windows). |
| 05 | [Use with VS Code / GitHub Copilot](05-use-with-vscode-copilot.md) | Connect from VS Code's MCP support or GitHub Copilot agents. |
| 06 | [Run the integration tests](06-run-integration-tests.md) | Validate a specific published version end-to-end; set up CI. |
| 07 | [Use with Cursor](07-use-with-cursor.md) | Connect from the Cursor editor's Agent (uses the `mcpServers` key). |

## Which guide first?

- **Trying the server for the first time** → start with
  [01 — NuGet via dnx](01-install-from-nuget.md). One command, no install.
- **Debugging a broken release** → [06 — Integration tests](06-run-integration-tests.md),
  then cross-check with [03 — MCP registry](03-verify-mcp-registry.md).
- **Wiring an AI agent to production documents** → pick your client:
  [04 — Claude Desktop](04-use-with-claude-desktop.md) or
  [05 — VS Code](05-use-with-vscode-copilot.md).

## Common context

- All guides target `GroupDocs.Comparison.Mcp@26.7.0`. Substitute a newer version
  freely — the interfaces are additive-only.
- Tools exposed on the wire are `Compare`, `AnalyzeChanges`, and `GetDocumentInfo` (snake_case).
  `AnalyzeChanges` (added in 26.7.0) returns the structured change list without rendering a result file.
- `Compare` does the same as `AnalyzeChanges` and additionally renders a result document with the
  detected changes visually highlighted. All tools work without a license, but with some evaluation-mode
  limitations (e.g. a watermark in the output). Configure a license via `GROUPDOCS_LICENSE_PATH` to remove
  them — see each guide's "License" section.
- Evaluation-mode output may include a watermark prefix. The server surfaces
  this as `"[Evaluation mode] Output may include watermarks."` in responses.
