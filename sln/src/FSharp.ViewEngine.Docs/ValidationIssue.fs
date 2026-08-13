namespace FSharp.ViewEngine.Docs

[<Struct>]
type ValidationIssue =
    { code:string
      message:string }

[<AutoOpen>]
module internal ValidationIssueHelpers =
    let issue code message =
        { code = code
          message = message }
