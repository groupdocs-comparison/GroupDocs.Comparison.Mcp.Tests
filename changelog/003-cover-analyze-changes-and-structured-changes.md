---
id: 003
date: 2026-07-02
package-under-test: 26.7.0
type: feature
---

# Cover the new AnalyzeChanges tool and Compare's structured changes

## What changed
- Added `AnalyzeChangesTests` — exercises the new `AnalyzeChanges` tool (added in `GroupDocs.Comparison.Mcp` 26.7.0): asserts it returns a structured `Changes:` JSON array, reports "No changes detected" for a self-comparison, and — unlike `Compare` — does **not** write any `*_compared` output file to storage.
- `CompareTests` now also asserts the `Changes:` JSON section that `Compare` appends after the saved-file line (valid JSON array, each entry carrying `id` + `type`).
- `ToolCatalog` gained an `AnalyzeChanges` resolver (keyword `analyze`).
- `ToolDiscoveryTests` now expects **3** advertised tools (was 2) and asserts `AnalyzeChanges` is present.
- Bumped `<McpPackageVersion>` and all version references to `26.7.0`.

## Why
26.7.0 adds the `AnalyzeChanges` tool and enriches `Compare`'s response with a structured change list. The integration suite validates the published tool surface end-to-end, so it must exercise the new tool and the new response shape — otherwise a regression in either would ship unnoticed.

## Migration / impact
Test-only change. The new assertions target the `26.7.0` tool surface, so they pass only against `GroupDocs.Comparison.Mcp >= 26.7.0`; running the suite against an older `McpPackageVersion` will fail `ToolDiscoveryTests` (tool count) and `AnalyzeChangesTests` (tool absent). No impact on the server or on consumers of this repo.
