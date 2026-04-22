module Fantomas.Core.Tests.Agrico.AgricoRecordExpressionAlignmentTests

// Agrico fork — target behaviour for `=` alignment in record construction
// expressions (assignments), gated by the existing `RecordFieldAlignment`
// setting. Complements the colon alignment in record type declarations.
//
// Kept in its own file so upstream merges never conflict with these tests.

open NUnit.Framework
open FsUnit

open Fantomas.Core
open Fantomas.Core.Tests.TestHelpers

[<Test>]
let ``aligns equals across record expression fields`` () =
    formatSourceString
        """
let private registeredUser =
    State.Registered (
        RegisteredUser {
            DisplayName = DisplayName.create "Test User"
            EmailAddress = EmailAddress.createX "test@domain.com"
            EmployeeIdO = None
            EvoAgentIdO = None
            AsanaAgentGuidO = None
            DefaultSalesRepGuidO = None
            RoleSpecificationS = Set.empty
            BranchAccess = BranchAccessU.AllBranchAccess
            ActiveBranchGuidO = None
        }
    )
"""
        { config with
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup }
    |> prepend newline
    |> should
        equal
        """
let private registeredUser =
    State.Registered(
        RegisteredUser {
            DisplayName          = DisplayName.create "Test User"
            EmailAddress         = EmailAddress.createX "test@domain.com"
            EmployeeIdO          = None
            EvoAgentIdO          = None
            AsanaAgentGuidO      = None
            DefaultSalesRepGuidO = None
            RoleSpecificationS   = Set.empty
            BranchAccess         = BranchAccessU.AllBranchAccess
            ActiveBranchGuidO    = None
        }
    )
"""

[<Test>]
let ``aligns equals in simple record expression`` () =
    formatSourceString
        """
let x = {
    Name = "Alice"
    Age = 30
    EmailAddress = "alice@example.com"
}
"""
        { config with
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup }
    |> prepend newline
    |> should
        equal
        """
let x = {
    Name         = "Alice"
    Age          = 30
    EmailAddress = "alice@example.com"
}
"""

[<Test>]
let ``blank line resets alignment group in record expression`` () =
    formatSourceString
        """
let x = {
    Name = "Alice"
    Age = 30

    SomeVeryLongField = true
    X = 1
}
"""
        { config with
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup }
    |> prepend newline
    |> should
        equal
        """
let x = {
    Name = "Alice"
    Age  = 30

    SomeVeryLongField = true
    X                 = 1
}
"""

[<Test>]
let ``single-field record expression is not padded`` () =
    formatSourceString
        """
let x = {
    OnlyField = "value"
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
let x = {
    OnlyField = "value"
}
"""

[<Test>]
let ``long expression wraps from equals line`` () =
    formatSourceString
        """
let x = {
    A = 1
    LongField = someFunction arg1 arg2 arg3 arg4 arg5 arg6
    B = "hello"
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
let x = {
    A         = 1
    LongField = someFunction
                    arg1
                    arg2
                    arg3
                    arg4
                    arg5
                    arg6
    B         = "hello"
}
"""

[<Test>]
let ``copy-and-update record aligns fields after with`` () =
    formatSourceString
        """
let y = {
    x with
        Name = "Bob"
        Age = 25
        EmailAddress = "bob@example.com"
}
"""
        { config with
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup }
    |> prepend newline
    |> should
        equal
        """
let y = {
    x with
        Name         = "Bob"
        Age          = 25
        EmailAddress = "bob@example.com"
}
"""

[<Test>]
let ``nested record expressions align independently`` () =
    formatSourceString
        """
let x = {
    Name = "Alice"
    Address = {
        Street = "123 Main St"
        City = "Springfield"
        ZipCode = "12345"
    }
    Age = 30
}
"""
        { config with
            RecordFieldAlignment = true
            MultilineBracketStyle = Stroustrup }
    |> prepend newline
    |> should
        equal
        """
let x = {
    Name    = "Alice"
    Address = {
        Street  = "123 Main St"
        City    = "Springfield"
        ZipCode = "12345"
    }
    Age     = 30
}
"""
