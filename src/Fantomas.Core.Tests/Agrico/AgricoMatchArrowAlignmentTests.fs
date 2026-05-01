module Fantomas.Core.Tests.Agrico.AgricoMatchArrowAlignmentTests

// Agrico fork — target behaviour for the upcoming `MatchArrowAlignment`
// option.
//
// Feature: gofmt-style alignment of `->` across consecutive arms of a
// `match` (or `function`) expression. Mirrors the `=` alignment in
// `RecordFieldAlignment` for record construction expressions.
//
// Scope:
//   * Each contiguous group of match arms (separated by blank lines) has
//     its `->` padded to a shared column equal to one space past the
//     longest pattern length among arms in that group whose body fits on
//     the same line as the `->`.
//   * Arms whose body wraps to a continuation line keep their `->` on the
//     pattern line at the natural column; they do not influence the
//     alignment column for other arms in the group.
//   * Single-arm groups are emitted without padding.
//
// Kept in its own file so upstream merges never conflict with these tests.

open NUnit.Framework
open FsUnit

open Fantomas.Core
open Fantomas.Core.Tests.TestHelpers

[<Test>]
let ``aligns arrows across simple match arms`` () =
    formatSourceString
        """
let f x =
    match x with
    | A -> 1
    | Bb -> 2
    | LongCaseName -> 3
    | VeryLongCaseName -> 4
"""
        { config with
            MatchArrowAlignment = true }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A                -> 1
    | Bb               -> 2
    | LongCaseName     -> 3
    | VeryLongCaseName -> 4
"""

[<Test>]
let ``single-arm match is not padded`` () =
    formatSourceString
        """
let f x =
    match x with
    | A -> 1
"""
        { config with
            MatchArrowAlignment = true }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A -> 1
"""

[<Test>]
let ``blank line resets alignment group`` () =
    formatSourceString
        """
let f x =
    match x with
    | A -> 1
    | Bb -> 2

    | LongCaseName -> 3
    | C -> 4
"""
        { config with
            MatchArrowAlignment = true }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A  -> 1
    | Bb -> 2

    | LongCaseName -> 3
    | C            -> 4
"""

[<Test>]
let ``arm with wrapped body keeps natural arrow position`` () =
    // `VeryLongCaseNameWithLargeBody` has a body too long to fit on the
    // same line, so its `->` stays at its natural column. The other arms
    // still align with each other within the same group.
    formatSourceString
        """
let f x =
    match x with
    | A -> "first"
    | VeryLongCaseNameWithLargeBody ->
        "this is a body too long to fit on the same line as the case name"
    | LongCaseName -> "third"
"""
        { config with
            MatchArrowAlignment = true
            MaxLineLength = 80 }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A            -> "first"
    | VeryLongCaseNameWithLargeBody ->
        "this is a body too long to fit on the same line as the case name"
    | LongCaseName -> "third"
"""

[<Test>]
let ``aligns arrows in function-keyword shorthand`` () =
    formatSourceString
        """
let f =
    function
    | A -> 1
    | Bb -> 2
    | LongCaseName -> 3
"""
        { config with
            MatchArrowAlignment = true }
    |> prepend newline
    |> should
        equal
        """
let f =
    function
    | A            -> 1
    | Bb           -> 2
    | LongCaseName -> 3
"""

[<Test>]
let ``aligns arrows for arms with bound payloads`` () =
    // Single-identifier payloads keep test brittleness low (no parenthesis-
    // spacing ambiguity to worry about).
    formatSourceString
        """
let f x =
    match x with
    | A i -> i
    | LongCaseName s -> 0
    | C -> 0
"""
        { config with
            MatchArrowAlignment = true }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A i            -> i
    | LongCaseName s -> 0
    | C              -> 0
"""

[<Test>]
let ``wrapped aligned function argument uses valid multiline arm`` () =
    formatSourceString
        """
module Repro

let test result =
    task {
        match result with
        | Ok value -> Formatter.writeValue $"/long/xxx/242040bc?value={value.ToString()}"
        | Error(errorTitle, errorMessage) -> state.Set(errorTitle, errorMessage)
    }
"""
        { config with
            MatchArrowAlignment = true
            RecordFieldAlignment = true
            UnionCaseAlignment = true
            MultilineBracketStyle = Stroustrup
            MaxLineLength = 100 }
    |> prepend newline
    |> should
        equal
        """
module Repro

let test result =
    task {
        match result with
        | Ok value ->
            Formatter.writeValue $"/long/xxx/242040bc?value={value.ToString()}"
        | Error(errorTitle, errorMessage) -> state.Set(errorTitle, errorMessage)
    }
"""

[<Test>]
let ``wrapped aligned chained member access uses valid multiline arm`` () =
    formatSourceString
        """
module Repro

let test x =
    match x with
    | Short value -> veryLongValueNameXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.Apply()
    | MuchLongerPattern other -> other
"""
        { config with
            MatchArrowAlignment = true
            RecordFieldAlignment = true
            UnionCaseAlignment = true
            MultilineBracketStyle = Stroustrup
            MaxLineLength = 100 }
    |> prepend newline
    |> should
        equal
        """
module Repro

let test x =
    match x with
    | Short value ->
        veryLongValueNameXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.Apply()
    | MuchLongerPattern other -> other
"""

[<Test>]
let ``wrapped aligned computation expression return list uses valid multiline arm`` () =
    formatSourceString
        """
module Repro

let test command =
    task {
        match command with
        | CreateValue -> return [ VeryLongResultNameXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ]
        | UpdateValue(firstValue, secondValue, thirdValue) -> return [ ResultValue.Created(firstValue, secondValue, thirdValue) ]
        | HandleVeryLongOtherCaseName -> return! Error OtherFailure
    }
"""
        { config with
            MatchArrowAlignment = true
            RecordFieldAlignment = true
            UnionCaseAlignment = true
            MultilineBracketStyle = Stroustrup
            MaxLineLength = 100 }
    |> prepend newline
    |> should
        equal
        """
module Repro

let test command =
    task {
        match command with
        | CreateValue ->
            return [ VeryLongResultNameXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ]
        | UpdateValue(firstValue, secondValue, thirdValue) ->
            return [ ResultValue.Created(firstValue, secondValue, thirdValue) ]
        | HandleVeryLongOtherCaseName -> return! Error OtherFailure
    }
"""

[<Test>]
let ``no-op when feature is off`` () =
    formatSourceString
        """
let f x =
    match x with
    | A -> 1
    | Bb -> 2
    | LongCaseName -> 3
"""
        { config with
            MatchArrowAlignment = false }
    |> prepend newline
    |> should
        equal
        """
let f x =
    match x with
    | A -> 1
    | Bb -> 2
    | LongCaseName -> 3
"""
