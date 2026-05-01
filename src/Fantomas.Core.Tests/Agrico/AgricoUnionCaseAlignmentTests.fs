module Fantomas.Core.Tests.Agrico.AgricoUnionCaseAlignmentTests

// Agrico fork — target behaviour for the upcoming `UnionCaseAlignment` option.
//
// Feature: gofmt-style alignment of `of` across consecutive cases of a
// discriminated union type declaration. Mirrors the colon alignment in
// `RecordFieldAlignment` (Stroustrup record type declarations).
//
// Scope:
//   * Each contiguous group of DU cases (separated by blank lines) has its
//     `of` keyword padded to a shared column equal to one space past the
//     longest case-name length among cases in that group that carry `of`.
//   * Cases without `of` (no payload) emit no padding and do not influence
//     the alignment column.
//   * For multi-field cases that already wrap (driven by line length and
//     `LeadingTupleSeparator`), only the column of `of` itself is adjusted;
//     the wrapped continuation lines for fields keep their existing layout.
//
// Kept in its own file so upstream merges never conflict with these tests.

open NUnit.Framework
open FsUnit

open Fantomas.Core
open Fantomas.Core.Tests.TestHelpers

[<Test>]
let ``aligns of across single-line cases`` () =
    formatSourceString
        """
type Foo =
    | A of int
    | Bb of string
    | LongCaseName of bool
    | VeryLongCaseName of obj
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A                of int
    | Bb               of string
    | LongCaseName     of bool
    | VeryLongCaseName of obj
"""

[<Test>]
let ``single-case DU is not padded`` () =
    // Verifies that when only one case is present, the alignment code
    // path emits no trailing padding. MaxLineLength = 25 forces the
    // multi-line layout (the inline form ` OnlyCase of int` is 16 chars
    // measured from after `type Foo =` at col 10, which exceeds the
    // remaining-on-line budget of 15).
    formatSourceString
        """
type Foo =
    | OnlyCase of int
"""
        { config with
            UnionCaseAlignment = true
            MaxLineLength = 25 }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | OnlyCase of int
"""

[<Test>]
let ``blank line resets alignment group`` () =
    formatSourceString
        """
type Foo =
    | A of int
    | Bb of string

    | LongCaseName of bool
    | C of obj
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A  of int
    | Bb of string

    | LongCaseName of bool
    | C            of obj
"""

[<Test>]
let ``section comment resets alignment group`` () =
    formatSourceString
        """
type Foo =
    | A of int
    | Bb of string
    // Header event information
    | LongCaseName of bool
    | C of obj
    // Metadata
    | At of System.DateTimeOffset
    | By of string
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A  of int
    | Bb of string
    // Header event information
    | LongCaseName of bool
    | C            of obj
    // Metadata
    | At of System.DateTimeOffset
    | By of string
"""

[<Test>]
let ``blank line before section comment resets alignment group`` () =
    formatSourceString
        """
type Foo =
    | A of int
    | Bb of string

    // Header event information
    | LongCaseName of bool
    | C of obj
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A  of int
    | Bb of string

    // Header event information
    | LongCaseName of bool
    | C            of obj
"""

[<Test>]
let ``cases without of do not participate in alignment`` () =
    // `NoPayload` has no `of`; it neither receives padding nor extends the
    // alignment column. The other cases align as if it weren't there.
    formatSourceString
        """
type Foo =
    | NoPayload
    | A of int
    | Bbb of string
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | NoPayload
    | A   of int
    | Bbb of string
"""

[<Test>]
let ``no-payload-only DU is unchanged`` () =
    formatSourceString
        """
type Foo =
    | A
    | Bb
    | LongCaseName
    | VeryLongCaseName
"""
        { config with
            UnionCaseAlignment = true }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A
    | Bb
    | LongCaseName
    | VeryLongCaseName
"""

[<Test>]
let ``multi-field case keeps first field on the case-name line when it fits`` () =
    // When a multi-field DU case can't fit on a single line but its first
    // field can sit inline with `of`, that's the preferred wrap. The
    // continuation lines (`*`) align under the first character of the
    // first field's name (the `N` of `Name` here) so the field columns
    // line up vertically. UnionCaseAlignment still pads `A` so its `of`
    // lines up with `Big`'s `of`; that propagates to the column of
    // `Name` (and therefore the `*`s) being the same as if `A` weren't
    // padded.
    //
    // MaxLineLength = 40 forces the wrap: the all-on-one-line form
    // `    | Big of Name: string * Age: int * Address: string` is 53
    // chars and overflows; the inline-first-field form fits with the
    // longest wrapped line `             * Address: string` at 30 chars.
    formatSourceString
        """
type Foo =
    | Big of
        Name: string *
        Age: int *
        Address: string
    | A of int
"""
        { config with
            UnionCaseAlignment = true
            LeadingTupleSeparator = true
            MaxLineLength = 40 }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | Big of Name: string
             * Age: int
             * Address: string
    | A   of int
"""

[<Test>]
let ``multi-field case falls back when first field would overflow`` () =
    // Fallback rule: if any field, placed at the inline column (under the
    // `N` of the first field's name), would exceed MaxLineLength, fall
    // back to the layout where `of` ends the case line and all fields
    // wrap below it (col 8 here, under the `N` in the wrapped layout).
    // Here it's the first field that overflows.
    //
    // Inline column would be col 13. At col 13, `VeryLongFieldName: string`
    // is 38 chars — exceeds MaxLineLength=35. In the fallback (col 8), the
    // same field is 33 chars and fits. UnionCaseAlignment still aligns
    // `of` on the single-line cases in the group.
    formatSourceString
        """
type Foo =
    | Big of
        VeryLongFieldName: string *
        Age: int *
        Address: string
    | A of int
"""
        { config with
            UnionCaseAlignment = true
            LeadingTupleSeparator = true
            MaxLineLength = 35 }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | Big of
        VeryLongFieldName: string
        * Age: int
        * Address: string
    | A   of int
"""

[<Test>]
let ``multi-field case falls back when a later field would overflow`` () =
    // The first field (`Name: string`) fits on the case-name line, but a
    // later field at the same column would overflow MaxLineLength. The
    // entire case falls back to the of-on-its-own-line layout, since the
    // trigger is "any field overflows", not just the first one.
    //
    // Inline column is col 13 (under the `N` of `Name`). At col 13,
    // `* VeryLongFieldName: string` is 40 chars — exceeds MaxLineLength=35.
    // In the fallback (col 8), the same field is 33 chars and fits.
    formatSourceString
        """
type Foo =
    | Big of
        Name: string *
        VeryLongFieldName: string *
        Age: int
    | A of int
"""
        { config with
            UnionCaseAlignment = true
            LeadingTupleSeparator = true
            MaxLineLength = 35 }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | Big of
        Name: string
        * VeryLongFieldName: string
        * Age: int
    | A   of int
"""

[<Test>]
let ``anonymous record payload uses valid multiline layout`` () =
    formatSourceString
        """
module Repro

type Shape =
    | WithOptions of {| firstValue : Option<int>; secondValue : Option<string> |}
    | SingleValue of {| name : Option<string> |}
"""
        { config with
            UnionCaseAlignment = true
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup
            MaxLineLength = 100 }
    |> prepend newline
    |> should
        equal
        """
module Repro

type Shape =
    | WithOptions of
        {|
            firstValue: Option<int>
            secondValue: Option<string>
        |}
    | SingleValue of {| name: Option<string> |}
"""

[<Test>]
let ``no-op when feature is off`` () =
    // Sanity check: the same input that the first test aligns is left
    // untouched when UnionCaseAlignment is false (Fantomas's existing
    // single-line layout for DU cases is preserved).
    formatSourceString
        """
type Foo =
    | A of int
    | Bb of string
    | LongCaseName of bool
    | VeryLongCaseName of obj
"""
        { config with
            UnionCaseAlignment = false }
    |> prepend newline
    |> should
        equal
        """
type Foo =
    | A of int
    | Bb of string
    | LongCaseName of bool
    | VeryLongCaseName of obj
"""
