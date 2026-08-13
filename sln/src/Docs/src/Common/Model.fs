namespace Docs.Common

open FSharp.ViewEngine

type InlineContent =
    | Text of string
    | Strong of InlineContent list
    | Code of string
    | Link of label:string * href:string

type DocHeading =
    { id:string
      title:string
      level:int }

type ComparisonBar =
    { label:string
      duration:string
      comparison:string
      widthPercent:int
      highlighted:bool }

type ComparisonChart =
    { label:string
      title:string
      description:string
      bars:ComparisonBar list }

type DocNode =
    | Heading of DocHeading
    | Paragraph of InlineContent list
    | UnorderedList of InlineContent list list
    | OrderedList of InlineContent list list
    | BarChart of ComparisonChart
    | DataTable of headers:string list * rows:string list list
    | CodeBlock of language:string * source:string
    | Example of id:string * label:string * language:string * source:string * preview:HtmlElement

type DocPage =
    { id:string
      path:string
      aliases:string list
      navLabel:string
      category:string
      title:string
      browserTitle:string
      nodes:DocNode list }

type NavSection =
    { label:string
      pages:DocPage list
      sections:NavSection list }

module DocPage =
    let headings page =
        page.nodes
        |> List.choose (function
            | Heading heading -> Some heading
            | _ -> None)

    let tableOfContents page =
        headings page
        |> List.filter (fun heading -> heading.level <= 3)
