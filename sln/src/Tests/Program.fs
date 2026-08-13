module Program

open Expecto

[<EntryPoint>]
let main args =
    let allTests = testList "All Tests" [
        CoreTests.tests
        HtmlCoverageTests.tests
        DatastarTests.tests
        HtmxTests.tests
        AlpineTests.tests
        SvgTests.tests
        TailwindTests.tests
        DocsTests.tests
    ]
    runTestsWithCLIArgs [] args allTests
