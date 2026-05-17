---
id: 002
date: 2026-05-18
package-under-test: 26.5.2
type: fix
---

# Fix file-extension assertion in GetDocumentInfoTests

## What changed
- `GetDocumentInfoTests` asserted the document extension as `pdf`, but `GetDocumentInfo`'s JSON reports `fileType.extension` straight from `IDocumentInfo.FileType.Extension`, which is **dot-prefixed** (`.pdf`). The assertion now reads `fileType.extension` and compares it against `.pdf` case-insensitively.
- Bumped `<McpPackageVersion>` and all version references to `26.5.2` — the suite now targets the just-released `GroupDocs.Comparison.Mcp` 26.5.2.

## Why
The test was written against an assumed bare-extension value and failed against the actual engine output. The dot-prefixed value is correct (it matches `Path.GetExtension`); the test expectation was wrong.

## Migration / impact
Test-only change — no impact on the server or on consumers of this repo.
