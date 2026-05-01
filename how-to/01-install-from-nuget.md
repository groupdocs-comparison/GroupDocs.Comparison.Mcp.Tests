# Install from NuGet

Two install routes from [`nuget.org/packages/GroupDocs.Comparison.Mcp`](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp):

1. **`dnx`** — run-on-demand (recommended for MCP clients). No install step.
2. **Global dotnet tool** — installed once, runs by name.

Both require the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## Prerequisites

```bash
dotnet --version
# must print 10.0.x or higher
```

If this returns an older version or "command not found," install the .NET 10
SDK first. On Windows, verify `dnx` is on PATH:

```bash
dnx --help           # bash / Linux / macOS
dnx.cmd --help       # Windows (older shells)
```

`dnx` ships inside `.NET 10 SDK` at `C:\Program Files\dotnet\dnx.cmd` on
Windows and `~/.dotnet/dnx` on Linux/macOS.

## Option 1 — dnx (recommended)

```bash
dnx GroupDocs.Comparison.Mcp@26.5.0 --yes
```

The first invocation downloads the package into the NuGet cache; subsequent
invocations reuse it. `--yes` auto-confirms the package-trust prompt.

### Pinned vs always-latest

`@<version>` pins to that exact release. **Omit it to always pull the latest
stable** on each invocation:

```bash
dnx GroupDocs.Comparison.Mcp --yes                # latest stable, refreshed every run
dnx GroupDocs.Comparison.Mcp --prerelease --yes   # latest including pre-releases
```

| | Pinned (`@26.5.0`) | Unpinned |
|---|---|---|
| Use for | Client configs committed to repos, CI, shared team setups | Quick local smoke tests, dev machines that should track latest |
| Reproducibility | identical version on every machine / session | depends on when each machine first pulled |
| Behaviour on a new release | unaffected — keeps using cached version until you bump | downloads + uses the new version on next launch |
| Risk of unexpected breakage | low | a release that renames a tool / changes a schema will surprise you mid-session |
| Startup time on day-of-release | instant from cache | +1–10s for the version probe + download |

> **`dnx` does not support npm-style ranges** (`^26.4`, `~26.4.0`). It's pinned-exact
> or latest-stable — nothing in between. If you want a floor without bumping
> on every release, you'll need to update the pinned value manually.

The cache lives at `%USERPROFILE%\.nuget\packages\groupdocs.comparison.mcp\<version>\`
on Windows and `~/.nuget/packages/groupdocs.comparison.mcp/<version>/` on
Linux/macOS. Old versions accumulate there until you delete them.

**What you should see on stderr:**

```
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: ModelContextProtocol.Server.StdioServerTransport[...]
      Server (stream) (GroupDocs.Comparison.Mcp) transport reading messages.
warn: GroupDocs.Mcp.Core.Licensing.LicenseManager[0]
      No license configured. Running in evaluation mode. ...
```

stdout is reserved for JSON-RPC — the process is waiting for a client to speak
MCP on its stdin. Press `Ctrl+C` to exit.

### Smoke test with a raw JSON-RPC request

Pipe an `initialize` + `tools/list` sequence to see the advertised tools:

```bash
# bash
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"probe","version":"1"}}}'
  echo '{"jsonrpc":"2.0","method":"notifications/initialized"}'
  echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
  sleep 2
) | GROUPDOCS_MCP_STORAGE_PATH=./docs dnx GroupDocs.Comparison.Mcp@26.5.0 --yes
```

You should see two JSON-RPC responses containing the `Compare` and
`GetDocumentInfo` tool definitions.

## Option 2 — global dotnet tool

```bash
dotnet tool install -g GroupDocs.Comparison.Mcp
groupdocs-comparison-mcp
```

To update:

```bash
dotnet tool update -g GroupDocs.Comparison.Mcp
```

To uninstall:

```bash
dotnet tool uninstall -g GroupDocs.Comparison.Mcp
```

The tool runs from `~/.dotnet/tools/groupdocs-comparison-mcp` (Linux/macOS) or
`%USERPROFILE%\.dotnet\tools\groupdocs-comparison-mcp.exe` (Windows). Make sure
that directory is on your `PATH` — the `dotnet tool install` output will warn
you if it isn't.

## Configuration

Set via environment variables when launching:

| Variable | Purpose | Default |
|---|---|---|
| `GROUPDOCS_MCP_STORAGE_PATH` | Folder the server reads input from and writes cleaned copies to | current working directory |
| `GROUPDOCS_MCP_OUTPUT_PATH` | Optional — route output files to a different folder | same as storage |
| `GROUPDOCS_LICENSE_PATH` | Path to `GroupDocs.Total.lic` — drops the evaluation-mode watermark from `Compare` output | *(evaluation mode — output watermarked)* |

```bash
GROUPDOCS_MCP_STORAGE_PATH=/data/documents \
GROUPDOCS_LICENSE_PATH=/secrets/GroupDocs.Total.lic \
dnx GroupDocs.Comparison.Mcp@26.5.0 --yes
```

## License

`Compare` works in evaluation mode — it produces the marked-up output
file but with an evaluation-mode watermark. The tool's response includes a
`[Evaluation mode]` prefix to flag this.

Get a license from [purchase.groupdocs.com](https://purchase.groupdocs.com/).
Point `GROUPDOCS_LICENSE_PATH` at the `.lic` file and `Compare` output loses
the watermark and the evaluation-mode prefix.

## Verifying version at runtime

The server's `initialize` response includes `serverInfo.version`. With an MCP
client:

```text
initialize response → serverInfo: { name: "GroupDocs.Comparison.Mcp", version: "26.5.0" }
```

This value comes from the published assembly's `AssemblyInformationalVersion`
and is enforced to match the NuGet package version at build time — so it's
authoritative. If you want to script-check it, see
[06 — Integration tests](06-run-integration-tests.md) — the first test in
`ToolDiscoveryTests.cs` asserts this.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `dnx: command not found` | .NET 10 SDK not installed, or not on PATH | Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0). Ensure `C:\Program Files\dotnet\` (Windows) or equivalent is on PATH. |
| First run hangs for ~30 s | Package is downloading from nuget.org into cache | Normal. Subsequent runs are fast. |
| `No license configured. Running in evaluation mode.` | No `GROUPDOCS_LICENSE_PATH` | Expected. `Compare` and `GetDocumentInfo` both still work; `Compare` output is watermarked. Set the path to drop the watermark. |
| `Compare` output has a watermark | Evaluation mode | Set `GROUPDOCS_LICENSE_PATH` to a valid `.lic` file. |
| Client can't see any tools | MCP client didn't finish `initialize` handshake before issuing `tools/list` | Check your client config — most handle this automatically. If hand-rolling, always send `notifications/initialized` after `initialize`. |

## Next steps

- Wire into a client: [Claude Desktop](04-use-with-claude-desktop.md) · [VS Code / Copilot](05-use-with-vscode-copilot.md)
- Or try it in Docker: [02 — Run via Docker](02-run-via-docker.md)
- Verify the package's MCP registry listing: [03 — MCP registry](03-verify-mcp-registry.md)
