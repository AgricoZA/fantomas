module Fantomas.Core.Tests.Agrico.AgricoLeadingTupleSeparatorTests

// Agrico fork — custom tests for the `LeadingTupleSeparator` option.
// Kept in its own file so upstream merges touching `TupleTests.fs` never conflict.
// See CHANGELOG.md [8.0.0-alpha-009-agrico-001].

open NUnit.Framework
open FsUnit

open Fantomas.Core.Tests.TestHelpers

[<Test>]
let ``leading tuple separator in expression`` () =
    formatSourceString
        """
let x =
    1,
    2,
    3
"""
        { config with
            LeadingTupleSeparator = true
            MaxLineLength = 10 }
    |> prepend newline
    |> should
        equal
        """
let x =
    1
    , 2
    , 3
"""

[<Test>]
let ``leading tuple separator in type`` () =
    formatSourceString
        """
type T = int * string * bool
"""
        { config with
            LeadingTupleSeparator = true
            MaxLineLength = 20 }
    |> prepend newline
    |> should
        equal
        """
type T =
    int
       * string
       * bool
"""

[<Test>]
let ``leading tuple separator in pattern`` () =
    formatSourceString
        """
let (longVariableName1,
     longVariableName2) = someFunc ()
"""
        { config with
            LeadingTupleSeparator = true
            MaxLineLength = 40 }
    |> prepend newline
    |> should
        equal
        """
let (longVariableName1
     , longVariableName2) =
    someFunc ()
"""

[<Test>]
let ``leading tuple separator preserves match behavior`` () =
    formatSourceString
        """
match "Hello" with
| "first" -> 1
| "second" -> 2
, []
"""
        { config with
            LeadingTupleSeparator = true
            MaxLineLength = 80 }
    |> prepend newline
    |> should
        equal
        """
match "Hello" with
| "first" -> 1
| "second" -> 2
, []
"""

[<Test>]
let ``leading tuple separator in type respects indent size`` () =
    formatSourceString
        """
type T = int * string * bool
"""
        { config with
            LeadingTupleSeparator = true
            IndentSize = 2
            MaxLineLength = 15 }
    |> prepend newline
    |> should
        equal
        """
type T =
  int
   * string
   * bool
"""

[<Test>]
let ``leading tuple separator in type respects indent size of 5`` () =
    formatSourceString
        """
type T = int * string * bool
"""
        { config with
            LeadingTupleSeparator = true
            IndentSize = 5
            MaxLineLength = 15 }
    |> prepend newline
    |> should
        equal
        """
type T =
     int
         * string
         * bool
"""

[<Test>]
let ``leading tuple separator in union case fields`` () =
    formatSourceString
        """
type Animal =
    | Human of
        Name: string *
        Age: int *
        Address: string
"""
        { config with
            LeadingTupleSeparator = true
            MaxLineLength = 40 }
    |> prepend newline
    |> should
        equal
        """
type Animal =
    | Human of
        Name: string
        * Age: int
        * Address: string
"""
