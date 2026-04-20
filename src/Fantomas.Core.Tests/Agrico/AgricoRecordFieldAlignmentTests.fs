module Fantomas.Core.Tests.Agrico.AgricoRecordFieldAlignmentTests

// Agrico fork — target behaviour for the upcoming `RecordFieldAlignment` option.
//
// Feature: gofmt-style colon alignment for record fields, scoped to groups
// separated by blank lines. On a function-type value that exceeds
// `MaxLineLength`, break at each top-level `->` with the continuation indented
// to `argCol` (the column of the first argument) and prefixed with "-> ".
// The result is that the `-` of `->` sits directly under the first character
// of the first argument above.
//
// Kept in its own file so upstream merges never conflict with these tests.
// Every test is [<Ignore>] until the feature is implemented; the assertions
// document the target output so the implementation is TDD-driven.

open NUnit.Framework
open FsUnit

open Fantomas.Core
open Fantomas.Core.Tests.TestHelpers

// When the feature lands, add `RecordFieldAlignment = true` to each config
// override and remove the [<Ignore>] attributes.
[<Literal>]
let private pending = "RecordFieldAlignment feature not yet implemented"

[<Test>]
let ``aligns colons across single-line fields`` () =
   formatSourceString
      """
type Foo = {
    Name : string
    Age : int
    EmailAddress : string
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Name         : string
    Age          : int
    EmailAddress : string
}
"""

[<Test>]
let ``blank line resets alignment group`` () =
   formatSourceString
      """
type Foo = {
    Name : string
    Age : int

    SomeVeryLongField : bool
    X : int
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Name : string
    Age  : int

    SomeVeryLongField : bool
    X                 : int
}
"""

[<Test>]
let ``single-field group is not padded`` () =
   // `MaxRecordWidth = 0` forces multi-line layout even for a one-field
   // record; verifies the alignment path gracefully emits no padding
   // when there's nothing to align against.
   formatSourceString
      """
type Foo = {
    OnlyField : string
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxRecordWidth = 0 }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    OnlyField : string
}
"""

[<Test>]
let ``function-type field wraps at arrow under first argument`` () =
   // Longest name is "GetByContextWithFallback" (24 chars). With a 4-space
   // indent and ": ", argCol is the column of `Key` on the first line.
   // Per the "all wrap or none" rule, once the function-type wraps, the
   // tuple argument also wraps; the continuation `->` is indented one
   // IndentSize (4) beyond argCol so the result parses as valid F#
   // (F# requires trailing-`*` tuples' continuation `->` to be strictly
   // deeper than the tuple items).
   formatSourceString
      """
type Foo = {
    Get                      : Key -> Value
    GetByContextWithFallback : Key * Context * Fallback * ExtraArg -> Value
    GetSimple                : Key * Context -> Value
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 60 }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Get                      : Key -> Value
    GetByContextWithFallback : Key *
                               Context *
                               Fallback *
                               ExtraArg
                                   -> Value
    GetSimple                : Key * Context -> Value
}
"""

[<Test>]
let ``function-type with nested tuple wraps recursively with leading separator`` () =
   // Recursive wrap rule: each operator (`->` or `*`) is placed at the
   // column of its own level's first item. Outer operators sit at argCol
   // (col 8 — under `FirstType`). A tuple nested inside an arrow argument
   // wraps at the column of that argument's first character (col 11 —
   // under `ThirdType`). The `-> FinalType` then returns to argCol.
   // When one operator wraps, every operator at the same level wraps —
   // there is no "fit some inline, break others" middle state.
   formatSourceString
      """
type Foo = {
    X : FirstType * SecondType -> MidType -> ThirdType * FourthType -> FinalType
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 30
         LeadingTupleSeparator = true }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    X : FirstType
        * SecondType
        -> MidType
        -> ThirdType
           * FourthType
        -> FinalType
}
"""

[<Test>]
let ``function-type with nested tuple wraps recursively with trailing separator (default)`` () =
   // Same recursive "all-or-none" wrap with `LeadingTupleSeparator = false`:
   // `*` trails each item except the last, and the first item of each
   // tuple stays on the line of the operator that introduces it
   // (`: FirstType *` and `-> ThirdType *`). Continuation items align
   // under their tuple's first item (col 8 outer, col 11 nested). Each
   // level's `->` is indented by IndentSize (4) from that level's
   // argCol, as required for F# to parse trailing-`*` layouts.
   formatSourceString
      """
type Foo = {
    X : FirstType * SecondType -> MidType -> ThirdType * FourthType -> FinalType
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 30 }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    X : FirstType *
        SecondType
            -> MidType
            -> ThirdType *
               FourthType
            -> FinalType
}
"""

[<Test>]
let ``wrapped field does not widen alignment column for other fields`` () =
   // Continuation lines do not participate in width calculation; only the
   // name-to-colon column matters when computing alignment. Arrow
   // continuation lines sit one IndentSize (4) beyond argCol under the
   // default (trailing) separator rule — required for F# to accept the
   // layout when a wrapped tuple is involved, and applied uniformly for
   // consistency even when it isn't.
   formatSourceString
      """
type Foo = {
    A : int
    LongField : SomeLongType -> AnotherLongType -> YetAnotherType
    B : string
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 50 }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    A         : int
    LongField : SomeLongType
                    -> AnotherLongType
                    -> YetAnotherType
    B         : string
}
"""

[<Test>]
let ``short tuple argument stays on one line`` () =
   // When the tuple fits within MaxLineLength, it is not split.
   formatSourceString
      """
type Foo = {
    Handler : (Key * Value * Context) -> Result
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Handler : (Key * Value * Context) -> Result
}
"""

[<Test>]
let ``long tuple argument wraps at '*' with trailing separator (default)`` () =
   // When the tuple cannot fit on one line, split at every `*`. With the
   // default `LeadingTupleSeparator = false`, the `*` trails the item;
   // continuation items align under the first argument (argCol). The
   // top-level `->` then sits at argCol + IndentSize, as required for F#
   // to parse the trailing-`*` layout.
   formatSourceString
      """
type Foo = {
    Handler : FirstLongInput * SecondLongInput * ThirdLongInput -> ResultValue
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 50 }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Handler : FirstLongInput *
              SecondLongInput *
              ThirdLongInput
                  -> ResultValue
}
"""

[<Test>]
let ``long tuple argument wraps at '*' with leading separator`` () =
   // Tuple-wrap uses the same `LeadingTupleSeparator` flag that governs
   // tuple expressions, patterns and union case fields. With it enabled,
   // `* ` sits at argCol on continuation lines — mirroring the `-> ` prefix
   // used when the top-level function-type wraps.
   formatSourceString
      """
type Foo = {
    Handler : FirstLongInput * SecondLongInput * ThirdLongInput -> ResultValue
}
"""
      { config with
         RecordFieldAlignment = true
         MultilineBracketStyle = Stroustrup
         MaxLineLength = 50
         LeadingTupleSeparator = true }
   |> prepend newline
   |> should
         equal
         """
type Foo = {
    Handler : FirstLongInput
              * SecondLongInput
              * ThirdLongInput
              -> ResultValue
}
"""

// Reference — current Fantomas output for a function-type record field with
// a long tuple argument and multiple arrows. Kept here to contrast against
// the target behaviour encoded in the tests above.
//
//     type X =
//        { X :
//           FirstTypeLOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONG *
//           AnotherType *
//           SecondType
//              -> MidType
//              -> ThirdType * FourthType
//              -> FinalType }
