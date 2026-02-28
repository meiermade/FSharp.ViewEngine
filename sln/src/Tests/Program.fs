module Program

open Expecto

[<EntryPoint>]
let main args =
    let allTests = testList "All Tests" [
        CoreTests.tests
        DatastarTests.tests
        HtmxTests.tests
        AlpineTests.tests
        SvgTests.tests
        TailwindTests.tests
    ]
    runTestsWithCLIArgs [] args allTests
