# Issue #2413: Fantomas alignment grouping ignores section comments

Repository: `AgricoZA/fantomas` fork only.

GitHub issue: https://github.com/AgricoZA/Ops/issues/2413

Selected base branch: `agrico-custom-main`

Work branch: `bugfix/2413-fantomas-alignment-comments`

PR target reminder: the eventual PR should target `AgricoZA/fantomas` branch `agrico-custom-main`. Do not target or push to `fsprojects/fantomas` upstream, and do not post issue comments in the upstream repository.

## Researched context

The Agrico Fantomas fork has custom alignment features in `src/Fantomas.Core/CodePrinter.fs`:

- `RecordFieldAlignment` for record type declarations (`:` alignment).
- `RecordFieldAlignment` for record construction/update expressions (`=` alignment).
- `UnionCaseAlignment` for discriminated union cases (`of` alignment).

The current grouping helpers split alignment groups by checking whether the next printable node has `TriviaContent.Newline` in `ContentBefore`:

- `groupFieldsByBlankLines`
- `groupRecordFieldsByBlankLines`
- `groupUnionCasesByBlankLines`

Issue #2413 reports that section comments inside aligned records interfere with grouping. A blank line before a section comment does not make the following field start a new alignment group, so fields before the comment are padded to match long fields in later sections. Deleting the comments makes blank-line grouping work again.

The existing Agrico tests cover plain blank-line grouping but not comment boundaries:

- `src/Fantomas.Core.Tests/Agrico/AgricoRecordFieldAlignmentTests.fs`
- `src/Fantomas.Core.Tests/Agrico/AgricoRecordExpressionAlignmentTests.fs`
- `src/Fantomas.Core.Tests/Agrico/AgricoUnionCaseAlignmentTests.fs`

## Desired behaviour

Alignment grouping should respect human-readable section boundaries. At minimum:

- A blank line before a section comment should split the alignment group for the fields/cases following that comment.

Preferred, if the implementation is straightforward and does not surprise existing formatting:

- A leading line comment immediately before a field/case can also act as a group boundary, because section comments usually label the group that follows.

Example desired record declaration shape:

```fsharp
type WriteRecord = {
    DocketGuid : DocketGuid
    Version    : Version
    Type       : string

    // Header event information
    SessionGuidO               : Option<SessionGuid>
    HeaderFieldUpdatedTypeO    : Option<string>
    ContactPersonEmailAddressO : Option<Core.EmailAddressL>

    // Line event information
    DocketLineGuidO       : Option<DocketLineGuid>
    LineEventTypeO        : Option<string>
    LineFieldUpdatedTypeO : Option<string>
}
```

## Implementation plan

1. Reproduce the bug with failing tests first.
   - Add a record type declaration test where groups are separated by `blank line + // section comment`.
   - Add a record expression test with the same pattern for `=` alignment.
   - Add a union case alignment test with `blank line + // section comment` before a new group.

2. Inspect trivia placement for the failing examples.
   - Use `scripts/oak.fsx` or targeted debug output if necessary after building the local Fantomas project.
   - Determine whether the blank line/comment is attached to the following field/case node, to the preceding node, or to trivia that is emitted between nodes.

3. Replace the duplicated grouping logic with a small shared helper if practical.
   - Keep the existing Agrico custom code isolated in `CodePrinter.fs` to minimise future upstream merge conflicts.
   - Avoid touching unrelated upstream formatting code.
   - Preserve existing plain blank-line grouping behaviour.

4. Teach alignment grouping about comment boundaries.
   - Detect a group boundary when the next field/case has leading trivia that includes either:
     - a blank line before a comment, or
     - a line comment that is intended as section-leading trivia.
   - Apply the same rule consistently to:
     - record type declaration fields,
     - record expression fields,
     - discriminated union cases.

5. Keep comments with the field/case group they introduce.
   - The section comment should print immediately above the first field/case in the new group.
   - The alignment width for the previous group must not include fields/cases after the section comment.

6. Add or update changelog if appropriate.
   - If the fix changes user-visible fork behaviour, add a `Fixed` entry to `CHANGELOG.md` in the current Agrico section, following the repository instruction to append entries to the end of the relevant section.

7. Verify.
   - Run the focused Agrico alignment tests first:
     - `dotnet test src/Fantomas.Core.Tests/ --filter AgricoRecordFieldAlignmentTests`
     - `dotnet test src/Fantomas.Core.Tests/ --filter AgricoRecordExpressionAlignmentTests`
     - `dotnet test src/Fantomas.Core.Tests/ --filter AgricoUnionCaseAlignmentTests`
   - Then run the Fantomas Core test project:
     - `dotnet test src/Fantomas.Core.Tests/`
   - Once implementation is complete, run the broader verification expected for this repository:
     - `dotnet build fantomas.slnx`
     - `dotnet fantomas src docs build.fsx`
     - `dotnet fsi build.fsx -- -p Analyze`

## Out of scope

- No changes to `fsprojects/fantomas` upstream.
- No comments or issue updates in the upstream repository.
- No package publishing as part of this issue.
- No whole-codebase reformat beyond files touched by the fix/tests.

## Final verification reminder

Once all implementation is complete, run the appropriate full verification for the Fantomas fork. In the Ops workflow, also remember that `/agr-run-all-tests` is the usual final verification skill for Ops work, but this issue's code changes live in the Fantomas fork and should primarily be verified with the Fantomas commands above.
