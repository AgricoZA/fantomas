module Fantomas.Core.Tests.Agrico.AgricoBlankLinesBetweenMultilineArrayAndListItemsTests

// Agrico fork — custom tests for the `BlankLinesBetweenMultilineArrayAndListItems` option.
// Kept in its own file so upstream merges touching list/array tests never conflict.
// See CHANGELOG.md [8.0.0-alpha-012-agrico-010] and LOB-1686.

open NUnit.Framework
open FsUnit

open Fantomas.Core
open Fantomas.Core.Tests.TestHelpers

// R1 — list with multiline items followed by a single-line item: a blank line
// falls between every adjacent pair, INCLUDING before the trailing single-line
// item (the "either neighbour is multiline" rule).
[<Test>]
let ``blank lines between multiline list items, including before trailing single-line item`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [ reasonGenerator |> Gen.map FailedDomainEventName.CreateFailed
          reasonGenerator |> Gen.map FailedDomainEventName.AttachFailed
          Gen.constant AuthorisationFailed ]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed

            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed

            Gen.constant AuthorisationFailed
        ]
"""

// R2 — the same behaviour applies to array literals.
[<Test>]
let ``blank lines between multiline array items`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [| reasonGenerator |> Gen.map FailedDomainEventName.CreateFailed
           reasonGenerator |> Gen.map FailedDomainEventName.AttachFailed |]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [|
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed

            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed
        |]
"""

// R3 — the option also applies under the cramped bracket style.
[<Test>]
let ``blank lines between multiline list items in cramped bracket style`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [ reasonGenerator |> Gen.map FailedDomainEventName.CreateFailed
          reasonGenerator |> Gen.map FailedDomainEventName.AttachFailed ]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MultilineBracketStyle = Cramped
            MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [ reasonGenerator
          |> Gen.map FailedDomainEventName.CreateFailed

          reasonGenerator
          |> Gen.map FailedDomainEventName.AttachFailed ]
"""

// G1 — with the option OFF (the default), output is unchanged: no blank lines
// are inserted between the multiline items.
[<Test>]
let ``option off leaves multiline list items unspaced`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [ reasonGenerator |> Gen.map FailedDomainEventName.CreateFailed
          reasonGenerator |> Gen.map FailedDomainEventName.AttachFailed
          Gen.constant AuthorisationFailed ]
"""
        { config with MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed
            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed
            Gen.constant AuthorisationFailed
        ]
"""

// G2 — a short list that fits on one line stays on one line; the option never
// forces a compact list to break across multiple lines.
[<Test>]
let ``option on leaves short single-line list untouched`` () =
    formatSourceString
        """
let xs = [ a; b ]
"""
        { config with BlankLinesBetweenMultilineArrayAndListItems = true }
    |> prepend newline
    |> should
        equal
        """
let xs = [ a; b ]
"""

// G3 — a list that is multiline only because of item count/width, but whose
// items are each single-line, gets NO blank lines (no neighbour is multiline).
[<Test>]
let ``option on does not space single-line items in a multiline list`` () =
    formatSourceString
        """
let xs = [ itemNumberOne; itemNumberTwo; itemNumberThree; itemNumberFour; itemNumberFive ]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MaxLineLength = 40 }
    |> prepend newline
    |> should
        equal
        """
let xs =
    [
        itemNumberOne
        itemNumberTwo
        itemNumberThree
        itemNumberFour
        itemNumberFive
    ]
"""

// G4 — idempotency: formatting already-spaced output again yields itself.
[<Test>]
let ``option on is idempotent`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed

            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed

            Gen.constant AuthorisationFailed
        ]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed

            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed

            Gen.constant AuthorisationFailed
        ]
"""

// G5 — an author-written blank line between two multiline items is preserved
// but not doubled when the option is on.
[<Test>]
let ``option on does not double an author-written blank line`` () =
    formatSourceString
        """
let events =
    Gen.oneof
        [ reasonGenerator |> Gen.map FailedDomainEventName.CreateFailed

          reasonGenerator |> Gen.map FailedDomainEventName.AttachFailed ]
"""
        { config with
            BlankLinesBetweenMultilineArrayAndListItems = true
            MaxLineLength = 65 }
    |> prepend newline
    |> should
        equal
        """
let events =
    Gen.oneof
        [
            reasonGenerator
            |> Gen.map FailedDomainEventName.CreateFailed

            reasonGenerator
            |> Gen.map FailedDomainEventName.AttachFailed
        ]
"""
