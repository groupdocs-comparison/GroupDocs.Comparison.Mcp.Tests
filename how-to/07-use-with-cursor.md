# Use with Cursor

Connect the MCP server to [Cursor](https://cursor.com) so you can ask its Agent
to compare two documents or inspect a source document's info.

## Prerequisites

- Cursor installed and updated (MCP support is in **Settings → Tools & MCP**).
- One of:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the `dnx` route — recommended), or
  - [Docker](https://www.docker.com/products/docker-desktop) (for the container route).

## Config file location

Cursor uses the **`mcpServers`** key (like Claude Desktop) — **not** `servers`
as in VS Code. Two scopes:

| Scope | Path |
|---|---|
| Global (all projects) | `~/.cursor/mcp.json` (macOS/Linux) · `%USERPROFILE%\.cursor\mcp.json` (Windows) |
| Project-only | `.cursor/mcp.json` in the workspace root |

Create the file if it doesn't exist.

## Option A — dnx (recommended)

```json
{
  "mcpServers": {
    "groupdocs-comparison": {
      "command": "dnx",
      "args": ["GroupDocs.Comparison.Mcp@26.7.0", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

- Replace the storage path with an **absolute path** to the folder Cursor should
  operate on (source and target documents, plus the generated `*_compared.*`
  output). On Windows use `"C:\\Users\\you\\Documents"` (double-escaped) or
  forward slashes.
- Omit `@26.7.0` to always pull the latest stable.
- Add `"GROUPDOCS_LICENSE_PATH": "…/GroupDocs.Total.lic"` to `env` to remove the
  evaluation watermark from compared output. Unlike Metadata, `compare` and
  `get_document_info` both run **without** a license — evaluation output is just
  watermarked, never blocked.

Copy-paste starter: [examples/cursor-mcp.json](../examples/cursor-mcp.json).

## Option B — Windows: full path to `dotnet.exe` (SSL / timeout workaround)

On Windows, Cursor launching `dnx` can fail with an **SSL / ~30 s timeout** on
the first package probe. Bypass `dnx` by running the already-cached tool DLL
directly with `dotnet.exe`:

```json
{
  "mcpServers": {
    "groupdocs-comparison": {
      "command": "C:\\Program Files\\dotnet\\dotnet.exe",
      "args": [
        "C:\\Users\\you\\.nuget\\packages\\groupdocs.comparison.mcp\\26.7.0\\tools\\net10.0\\any\\GroupDocs.Comparison.Mcp.dll"
      ],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "C:\\Users\\you\\Documents"
      }
    }
  }
}
```

Populate the cache first by running `dnx GroupDocs.Comparison.Mcp@26.7.0 --yes` once
in a terminal, then point `args[0]` at the resulting
`…\.nuget\packages\groupdocs.comparison.mcp\<version>\tools\net10.0\any\GroupDocs.Comparison.Mcp.dll`.

## Option C — Docker

```json
{
  "mcpServers": {
    "groupdocs-comparison": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "/Users/you/Documents:/data",
        "ghcr.io/groupdocs-comparison/comparison-net-mcp:26.7.0"
      ]
    }
  }
}
```

## Reload and verify

1. Save `mcp.json`.
2. **Settings → Tools & MCP** → find `groupdocs-comparison` → toggle it on (or hit
   the reload icon). A green dot means it connected.
3. Expand it — you should see `compare` and `get_document_info`.

## Example prompts (Agent mode)

```
Compare source.docx against target.docx and tell me how many changes there are.

What file type and page count does report.pdf have?

Diff contract-v1.pdf and contract-v2.pdf, then summarize the differences.
```

The Agent will call `compare` / `get_document_info` and compose its answer from
the results. A successful `compare` returns a `<N> change(s) detected` (or
`No changes detected`) line plus the saved path of the marked-up
`<source-stem>_compared<ext>` document.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Server greyed out / won't start on Windows | `dnx` SSL/timeout — use **Option B** (full `dotnet.exe` path + cached DLL). |
| Server not listed | JSON typo — Cursor silently drops unparseable entries. Validate with `jq . mcp.json`. Confirm the key is `mcpServers`, not `servers`. |
| Compared output has a watermark | Expected in evaluation mode. Add `GROUPDOCS_LICENSE_PATH` to `env` to remove it. |
| `Compare failed for '…' vs '…'` with a font error (macOS/Linux) | Install native deps — `brew install mono-libgdiplus` (macOS) / `apt-get install libgdiplus libfontconfig1 ttf-mscorefonts-installer` (Linux), or use the Docker option (fonts are baked in). |

## Next steps

- [04 — Use with Claude Desktop](04-use-with-claude-desktop.md)
- [05 — Use with VS Code / Copilot](05-use-with-vscode-copilot.md)
