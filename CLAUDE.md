# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

## Internal Publishing (AgricoZA Fork)

This is a fork of [fsprojects/fantomas](https://github.com/fsprojects/fantomas) with custom features (e.g., `LeadingTupleSeparator` for union case fields).

### Publishing to Ops Workspace

The Ops workspace (`../ops1/Workspace`) consumes Fantomas as a dotnet tool configured in `.config/dotnet-tools.json`.

**Steps to publish a new version:**

1. **Update version in CHANGELOG.md** (e.g., `8.0.0-alpha-003`):
   ```markdown
   ## [8.0.0-alpha-003] - 2026-02-04

   ### Added
   - LeadingTupleSeparator now applies to union case fields
   ```

2. **Build packages:**
   ```bash
   dotnet fsi build.fsx
   ```
   Packages are output to `artifacts/package/release/`

3. **In Ops workspace, update the tool:**
   ```bash
   cd ../ops1/Workspace
   dotnet tool update fantomas --add-source ../fantomas/artifacts/package/release --version 8.0.0-alpha-003
   ```

4. **Verify and commit** the updated `.config/dotnet-tools.json` in Ops repo.

### Custom Features in This Fork

- **LeadingTupleSeparator**: Also applies to discriminated union case fields (upstream only supports expressions, types, and patterns)
