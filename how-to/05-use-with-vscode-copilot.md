# Use with VS Code / GitHub Copilot

VS Code's MCP support (used by GitHub Copilot agents and other MCP-aware
extensions) reads server definitions from `.vscode/mcp.json` in your workspace
or from the user-level MCP settings.

## Prerequisites

- VS Code (stable or Insiders) with a recent build that supports MCP.
- GitHub Copilot extension installed and signed in (optional — other MCP-aware
  extensions work too).
- One of: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0),
  Docker, or the global dotnet tool installed.

## Workspace config — `.vscode/mcp.json`

Create the file at the root of your workspace:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "storage_path",
      "description": "Base folder for input and output files.",
      "password": false
    }
  ],
  "servers": {
    "groupdocs-comparison": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Comparison.Mcp@26.5.0", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "${input:storage_path}"
      }
    }
  }
}
```

Full example: [examples/vscode-mcp.json](../examples/vscode-mcp.json).

The `inputs` block makes VS Code prompt for the storage path the first time
the server starts — handy for workspaces where the answer differs per machine
or contributor. For a fixed path, drop the `inputs` block and hardcode the env
value.

## Alternative configurations

### Docker launcher

```json
{
  "servers": {
    "groupdocs-comparison": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "${workspaceFolder}/documents:/data",
        "ghcr.io/groupdocs-comparison/comparison-net-mcp:26.5.0"
      ]
    }
  }
}
```

`${workspaceFolder}` is substituted by VS Code at launch.

### Global dotnet tool launcher

```json
{
  "servers": {
    "groupdocs-comparison": {
      "type": "stdio",
      "command": "groupdocs-comparison-mcp",
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "${workspaceFolder}/documents"
      }
    }
  }
}
```

## Start the server

VS Code shows MCP servers in the Copilot chat panel. The first time after
editing `mcp.json`:

1. Reload the window: `Cmd/Ctrl+Shift+P` → **"Developer: Reload Window"**.
2. Open Copilot chat.
3. Click the tools / MCP icon — `groupdocs-comparison` should appear with
   `Compare` and `GetDocumentInfo` listed.
4. If you used `inputs`, VS Code prompts for `storage_path` on first use and
   caches the answer for the session.

## Discovery snippet from nuget.org

The [NuGet.org package page](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp)
renders a ready-to-use snippet on its "MCP server" card. That snippet is
generated from the package's embedded `.mcp/server.json` and is functionally
identical to the config above — you can paste it directly into
`.vscode/mcp.json`.

If the NuGet-generated snippet diverges from this file, treat the NuGet one as
authoritative for the `command` / `args` / `env` names — it's what users will
copy into their editors.

## Example prompts for Copilot

```
@workspace compare /docs/contract-v1.pdf with /docs/contract-v2.pdf

Use compare to compare the authors of old.docx and new.docx.

Diff every pair of `*-old.docx` / `*-new.docx` files in /docs and report the change counts.
```

Copilot will negotiate the right tool calls. If you want deterministic tool
selection, reference the tool by name in your prompt.

## License configuration

Add to the `env` block:

```json
"env": {
  "GROUPDOCS_MCP_STORAGE_PATH": "${input:storage_path}",
  "GROUPDOCS_LICENSE_PATH": "/absolute/path/to/GroupDocs.Total.lic"
}
```

Without a license, `Compare` still works — it just produces watermarked
output (see [01 — NuGet install § License](01-install-from-nuget.md#license)).

## Troubleshooting

| Symptom | Fix |
|---|---|
| MCP icon doesn't appear | Update VS Code to a build with MCP support; ensure the Copilot extension is signed in. |
| Server listed but greyed out | Launch failed. Open **Output** panel → select the MCP extension from the dropdown to see stderr from the server process. |
| `dnx: command not found` | VS Code's integrated PATH may not include the .NET 10 SDK. Use the absolute path in `command` — see [04 — Claude Desktop § If dnx can't be found](04-use-with-claude-desktop.md#if-claude-cant-find-dnx). |
| Storage prompt doesn't appear | `inputs` block missing or mistyped. VS Code silently drops invalid entries. |
| Tool calls return "file not found" | `storage_path` points at a folder that doesn't contain the files you're asking about. Remember — tools resolve by filename relative to storage, not by absolute path. |

## Next steps

- [04 — Claude Desktop](04-use-with-claude-desktop.md) — same server, different client
- [06 — Integration tests](06-run-integration-tests.md) — run the test suite locally
