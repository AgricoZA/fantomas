# Fantomas

This file provides canonical guidance to coding agents working in this repository.

## Issue Tracking

Issues for this repository are tracked at: https://github.com/AgricoZA/Ops/issues

## Build Commands

```bash
# Initialize repository (downloads FCS files from F# compiler)
dotnet fsi build.fsx -p Init

# Full build pipeline (format check, build, test, pack)
dotnet fsi build.fsx

# Build only
dotnet build -c Release

# Run all tests
dotnet test

# Run specific test project
dotnet test src/Fantomas.Core.Tests

# Run a specific test by name
dotnet test src/Fantomas.Core.Tests --filter "FullyQualifiedName~TestClassName.TestName"

# Format all code
dotnet fsi build.fsx -p FormatAll

# Format only changed files
dotnet fsi build.fsx -p FormatChanged

# Check formatting without changing files
dotnet fantomas src docs build.fsx --check

# Run benchmarks
dotnet fsi build.fsx -p Benchmark

# Run analyzers
dotnet fsi build.fsx -p Analyze

# Build and serve documentation locally
dotnet fsi build.fsx -p Docs
```

## Diagnostic Scripts

All scripts accept a file path or stdin, with optional `--signature` and `--editorconfig <content>` flags.

- `scripts/ast.fsx` — untyped AST
- `scripts/oak.fsx` — Oak tree
- `scripts/format.fsx` — format with local build
- `scripts/writer-events.fsx` — writer events produced during formatting

Scripts require a debug build first (`dotnet build src/Fantomas/Fantomas.fsproj`).

## Architecture Overview

Fantomas is an opinionated F# source code formatter. It parses F# source into an AST, transforms it to a custom tree model (Oak), and reconstructs formatted source code.

### Project Structure

```
Fantomas.FCS → Fantomas.Core → Fantomas (CLI)
                    ↓
              Fantomas.Client (editor integration)
```

- **Fantomas.FCS**: Custom fork of F# compiler exposing only the parser. Downloads files from `dotnet/fsharp` repo via `Init` pipeline.
- **Fantomas.Core**: Core formatting library (netstandard2.0). Public API in `CodeFormatter.fsi`.
- **Fantomas**: CLI tool handling `.editorconfig` and `.fantomasignore` files.
- **Fantomas.Client**: LSP-based library for editor integration (not direct dependency on Core).

### Formatting Pipeline

1. **Parse**: `Fantomas.FCS.parseFile` → Untyped AST
2. **Transform**: `ASTTransformer.fs` → Oak (custom tree model)
3. **Collect Trivia**: `Trivia.fs` → Comments, blank lines, directives
4. **Print**: `CodePrinter.fs` → Formatted source string

### Key Source Files

- `src/Fantomas.Core/SyntaxOak.fs` - Oak tree model definitions
- `src/Fantomas.Core/ASTTransformer.fs` - AST to Oak transformation (uses partial active patterns extensively)
- `src/Fantomas.Core/CodePrinter.fs` - Main formatting logic (~165KB, core of formatter)
- `src/Fantomas.Core/Trivia.fs` - Comment and whitespace handling
- `src/Fantomas.Core/Context.fs` - Formatting context and event sourcing for output
- `src/Fantomas.Core/FormatConfig.fs` - Configuration options

### F# Patterns Used

- **Partial Active Patterns**: Heavy use in `ASTTransformer.fs` for pattern matching on AST nodes
- **Custom Operators**: `!-` and `+>` in `CodePrinter.fs`
- **Signature Files (.fsi)**: Define module boundaries and public API
- **Type Extensions**: Extend AST types with range information
- **Event Sourcing**: Instructions written to event list in `Context.fs` before final output

### Trivia Handling

Trivia (comments, blank lines, conditional directives) are not part of the AST. They're:
1. Detected from `ParsedImplFileInputTrivia`/`ParsedSigFileInputTrivia` and `ISourceText`
2. Inserted into Oak nodes as `ContentBefore`/`ContentAfter`

Common trivia bug fix: Use `sepNlnConsideringTriviaContentBeforeForMainNode` instead of `sepNln` in `CodePrinter.fs`.

## Testing

Tests use NUnit with FsUnit assertions. Core tests are in `src/Fantomas.Core.Tests/`.

### Test fixture style

Formatter tests should use neutral Fantomas-style sample code, not Agrico Ops domain names, business concepts, or naming conventions. When reducing a bug found in Ops, replace domain-specific names such as `Invoice`, `Docket`, `StockViewer`, `itemGuidO`, or `SuccessfulDomainEventU` with generic names while preserving only the syntactic shape needed to reproduce the formatter behaviour.

```bash
# Run single test file's tests
dotnet test src/Fantomas.Core.Tests --filter "FullyQualifiedName~CommentTests"

# Run specific test
dotnet test src/Fantomas.Core.Tests --filter "TestName~my_test_name"
```

## Online Tools

- AST Viewer: https://fsprojects.github.io/fantomas-tools/#/ast
- Trivia Viewer: https://fsprojects.github.io/fantomas-tools/#/trivia
- Fantomas Online: https://fsprojects.github.io/fantomas-tools/#/fantomas/preview
- F# Tokens: https://fsprojects.github.io/fantomas-tools/#/tokens

## Style Guides

Fantomas implements:
- Microsoft F# Style Guide: https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting
- G-Research Style Guide: https://github.com/G-Research/fsharp-formatting-conventions

Stylistic feature requests should be discussed on those repos first, not here.

## Code Quality

```xml
<!-- Warnings as errors -->
FS0025: Incomplete pattern matches
FS1182: Unused variables
```

Self-formatted using Fantomas 7.0.1.

## Post-task Steps

Run these after completing a task, not during iterative development — analyzers can be slow.

### Format

```bash
dotnet fantomas src docs build.fsx
```

### Analyzers

```bash
dotnet fsi build.fsx -- -p Analyze
```

Output goes to `analysis.sarif` in the repo root.

## Internal Publishing (AgricoZA Fork)

This is a fork of [fsprojects/fantomas](https://github.com/fsprojects/fantomas) with custom features (e.g., `LeadingTupleSeparator` for union case fields).

### Versioning Scheme

Versions follow the pattern `{upstream-version}-agrico-{NNN}`, e.g. `8.0.0-alpha-003-agrico-001`. This embeds the upstream version we're based on.

**Reset `NNN` to `001` each time we rebase onto a new upstream version.** Within a given upstream, `NNN` counts our own iterations.

### Package ID

This fork publishes under the upstream package id `fantomas`. The CLI command name is also `fantomas` (via `<ToolCommandName>fantomas</ToolCommandName>`).

A previous attempt (`agrico-003`) renamed the package id to `fantomas.agrico` to dodge SemVer collision with upstream Fantomas on nuget.org. That broke any tooling that looks Fantomas up by package id — most notably JetBrains Rider's *Settings → Languages & Frameworks → F# → Fantomas → Location → Local dotnet tool* detection, which silently fell back to its bundled Fantomas when the manifest entry was keyed on `fantomas.agrico`. Result: on-save formatting in Rider drifted from `dotnet fantomas` CLI output.

`agrico-004` reverted to package id `fantomas`. The collision with upstream Fantomas is now handled on the **consumer side** via NuGet `packageSourceMapping`: the `fantomas` package id is locked to the AgricoZA GitHub Packages feed (`<package pattern="fantomas" />` mapped to `github-agrico`), so `dotnet tool restore`/`update` never sees upstream's `fantomas` builds on nuget.org. See AgricoZA/Ops#2320 for the consumer-side change.

### CHANGELOG Constraints

Versions are extracted from `CHANGELOG.md` by `Ionide.KeepAChangelog.Tasks`. Subsection headings must be standard Keep a Changelog types (`Added`, `Changed`, `Fixed`, `Removed`, etc.) — custom headings like `### Upstream` will cause build failures.

### Publishing to GitHub Packages

Packages are published to the **AgricoZA GitHub Packages NuGet feed** (`https://nuget.pkg.github.com/AgricoZA/index.json`), which is configured as a source in the Ops repo's `NuGet.config`. Published versions are visible at https://github.com/orgs/AgricoZA/packages/nuget/package/fantomas.

**Steps to publish a new version:**

1. **Update version in CHANGELOG.md**:
   ```markdown
   ## [8.0.0-alpha-012-agrico-NNN] - YYYY-MM-DD

   ### Added
   - Description of new feature
   ```

2. **Build and test:**
   ```bash
   dotnet fsi build.fsx
   ```
   Packages are output to `artifacts/package/release/`

3. **Push to GitHub Packages:**
   ```bash
   dotnet nuget push artifacts/package/release/fantomas.8.0.0-alpha-012-agrico-NNN.nupkg \
     --source "https://nuget.pkg.github.com/AgricoZA/index.json" \
     --api-key $(gh auth token)
   ```

4. **In Ops workspace, update the tool:**
   ```bash
   cd ../ops4/Workspace
   dotnet tool update fantomas --version 8.0.0-alpha-012-agrico-NNN
   ```

   The Ops repo's `NuGet.config` files include a `<packageSourceMapping>` block that locks the `fantomas` package id to the AgricoZA feed, so this resolves to the fork even though both feeds carry packages named `fantomas`.

5. **Verify and commit** the updated `.config/dotnet-tools.json` in the Ops repo.

### Syncing Upstream Changes

The `custom` branch is rebased onto `upstream/main` to maintain a clean linear history. Our custom commits sit on top of upstream.

```bash
git fetch upstream
git rebase --onto upstream/main <old-upstream-head> custom
```

### Minimising Upstream Merge Conflicts

When adding a fork-specific feature, arrange the code so that rebasing onto `upstream/main` produces at most a few trivial conflicts. The rules below apply to every Agrico feature (`LeadingTupleSeparator`, `RecordFieldAlignment`, anything we add next):

**Tests**
- New tests go in their own file under `src/Fantomas.Core.Tests/Agrico/` with an `Agrico`-prefixed filename. Upstream never edits these files.
- Register them at the bottom of the `Fantomas.Core.Tests.fsproj` `<ItemGroup>` under the comment `<!-- Agrico fork: keep custom tests isolated so upstream merges never conflict. -->`. Conflicts on this line are trivial to resolve.
- Never extend an upstream-owned test file (`TupleTests.fs`, `LetBindingTests.fs`, etc.) with Agrico test cases — move them out.

**Config fields in `FormatConfig.fs`**
- Append new fields at the end of the `FormatConfig` record and at the end of `FormatConfig.Default`. Both are touchpoints upstream also grows over time; end-of-record placement keeps the textual diff isolated and the merge trivial.

**Behaviour changes in `CodePrinter.fs`**
- Large helpers (new `gen*` functions, policy predicates, grouping logic) live at the **tail of the file**, after `genField`, clustered together under a section-header comment block that names the feature. Upstream rarely modifies the tail. `module internal rec Fantomas.Core.CodePrinter` means these helpers can be called from anywhere earlier in the file without let-rec acrobatics.
- The **inline edit** at the upstream call site must collapse to a **single function call** — not a multi-line conditional. Example for `RecordFieldAlignment` in the `TypeDefn.Record` branch:
  ```fsharp
  +> indentSepNlnUnindent (genMaybeAlignedFieldList ctx.Config node.Fields)
  ```
  Replaces one line of upstream code with one line of our code. If upstream refactors the surrounding function, the merge conflict is a single-line substitution.
- Add a leading comment at the inline edit (`// Agrico: see <helper-name>.`) so a merger knows exactly what to preserve.
- The helper should internally fall back to the upstream default when the feature is off, so inserting the call is behaviour-neutral without the config flag.

**Conflict triage when rebasing**
- The predictable hot spots are: `FormatConfig.fs` (end of record + end of Default), `CodePrinter.fs` (the 1–3 inline call sites per feature), and `Fantomas.Core.Tests.fsproj` (the bottom `<Compile>` block). All other Agrico code lives in files upstream doesn't touch.
- If upstream refactored around a call site, preserve **the call** (`+> indentSepNlnUnindent (genMaybeAligned...)`), not the surrounding boilerplate — let upstream's boilerplate win, keep our one-line hook.

### Custom Features in This Fork

- **LeadingTupleSeparator**: Also applies to discriminated union case fields (upstream only supports expressions, types, and patterns)
- **RecordFieldAlignment**: gofmt-style alignment for Stroustrup records, with blank-line-delimited groups. Applies to two cases, both gated by the same flag:
  - **Type declarations**: aligns `:` across record fields. Long function-type field values wrap at each top-level `->` under the first argument's column; tuple-separator placement reuses the `LeadingTupleSeparator` flag.
  - **Construction expressions**: aligns `=` across record assignments. Stroustrup-style value expressions (nested records, lists) keep their normal layout; other long values (e.g. function applications) stay on the `=` line and wrap from there, mirroring the type-declaration wrap-from-colon behaviour.
