---
id: 001
date: 2026-05-01
version: 26.5.0
type: feature
---

# Initial integration-tests suite for GroupDocs.Comparison.Mcp

## What changed
- Test repo bootstrapped: launches the published `GroupDocs.Comparison.Mcp@26.5.0` NuGet via `dnx`, wires an MCP stdio client, and exercises every advertised tool.
- Four test classes:
  - `ToolDiscoveryTests` — server info advertises `GroupDocs.Comparison.Mcp`, tool list contains exactly `Compare` and `GetDocumentInfo`, every tool has a description + input schema.
  - `CompareTests` — `Compare` produces a marked-up output for synthetic source vs target PDFs (≥ 1 change detected via real text-content differences in `/Contents` streams), reports "No changes detected" for self-comparison, and exercises real samples (DOCX, XLSX, PDF) via self-comparison theory.
  - `GetDocumentInfoTests` — `GetDocumentInfo` returns the expected file type / page count / size for the synthetic source PDF, plus theory coverage across real samples (DOCX, XLSX, PPTX, PDF).
  - `ErrorHandlingTests` — unknown source filename returns a clear error, corrupted file does not crash the server, `sourcePassword` / `targetPassword` parameters are accepted without schema rejection.
- Two synthetic PDFs with **real visible text** (`source.pdf`: "Original Document Body / Section A: Introduction / Created on Monday morning / For internal review only", `target.pdf`: corresponding "Modified" / "Revised" / "Tuesday afternoon" / "external distribution" lines) generated at test startup so Compare's content-diff finds substantive changes.
- Five real samples shipped under `sample-docs/` (sample.docx, .xlsx, .pptx, .pdf, .png) auto-copied to test output and exercised via self-comparison.
- How-to guides under `how-to/` cover NuGet install, Docker, MCP registry verification, Claude Desktop, VS Code / GitHub Copilot, and running the test suite locally.
- `examples/` ships `claude-desktop.json`, `vscode-mcp.json`, `docker-compose.yml` pinned to `26.5.0`.

## Why
Closes the loop on the published `GroupDocs.Comparison.Mcp` NuGet artifact — every release is exercised end-to-end against live nuget.org so packaging or dnx-shim regressions surface immediately rather than at user install time.

## Migration / impact
First release — no migration required.
