namespace Docs.Pages

open Docs.Common

open Docs.ComponentPages.Card.View
open Docs.ComponentPages.SectionHeader.View
open Docs.ComponentPages.FieldGroup.View
open Docs.ComponentPages.CodeBlock.View
open Docs.ComponentPages.Callout.View
open Docs.ComponentPages.Mermaid.View
open Docs.ComponentPages.Example.View
open Docs.ComponentPages.FSharpApiReference.View

module ComponentDocumentation =
    let registrations =
        [ cardRegistration
          sectionHeaderRegistration
          fieldGroupRegistration
          codeBlockRegistration
          calloutRegistration
          mermaidRegistration
          exampleRegistration
          fsharpReferenceRegistration ]

    let examples = function
        | "card" -> Docs.ComponentPages.Card.View.examples ()
        | "section-header" -> Docs.ComponentPages.SectionHeader.View.examples ()
        | "field-group" -> Docs.ComponentPages.FieldGroup.View.examples ()
        | "code-block" -> Docs.ComponentPages.CodeBlock.View.examples ()
        | "callout" -> Docs.ComponentPages.Callout.View.examples ()
        | "mermaid" -> Docs.ComponentPages.Mermaid.View.examples ()
        | "example" -> Docs.ComponentPages.Example.View.examples ()
        | "fsharp-api-reference" -> Docs.ComponentPages.FSharpApiReference.View.examples ()
        | _ -> []

    let allExamples () =
        registrations
        |> List.collect (fun item -> examples (item.path.Substring("/components/".Length)))

    let page (registration: DocPage) =
        match registration.path with
        | "/components/card" -> Docs.ComponentPages.Card.View.page
        | "/components/section-header" -> Docs.ComponentPages.SectionHeader.View.page
        | "/components/field-group" -> Docs.ComponentPages.FieldGroup.View.page
        | "/components/code-block" -> Docs.ComponentPages.CodeBlock.View.page
        | "/components/callout" -> Docs.ComponentPages.Callout.View.page
        | "/components/mermaid" -> Docs.ComponentPages.Mermaid.View.page
        | "/components/example" -> Docs.ComponentPages.Example.View.page
        | "/components/fsharp-api-reference" -> Docs.ComponentPages.FSharpApiReference.View.page
        | _ -> invalidArg (nameof registration) "Unknown component page."

    let tryPage path =
        registrations
        |> List.tryFind (fun item -> item.path = path || List.contains path item.aliases)
        |> Option.map page
