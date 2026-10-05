namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

/// <category>navigation</category>
[<Struct>]
type ValidationIssue =
    { code:string
      message:string }

[<AutoOpen>]
module internal ValidationIssueHelpers =
    let issue code message =
        { code = code
          message = message }
