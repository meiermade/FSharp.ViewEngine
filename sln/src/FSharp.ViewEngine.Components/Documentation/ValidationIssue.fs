namespace FSharp.ViewEngine.Components.Documentation

[<Struct>]
type ValidationIssue =
    { code:string
      message:string }

[<AutoOpen>]
module internal ValidationIssueHelpers =
    let issue code message =
        { code = code
          message = message }
