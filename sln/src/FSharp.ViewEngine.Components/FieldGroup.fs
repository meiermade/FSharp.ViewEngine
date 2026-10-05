namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>field-group</category>
[<NoEquality; NoComparison>]
type FieldGroupConfig =
    private
        { legend:string
          content:HtmlElement
          description:string option
          layout:FieldLayout
          attributes:HtmlAttribute list }

/// <summary>
/// A semantic fieldset for related controls. Individual controls retain their own Field or complete-control labels.
/// </summary>
/// <category>field-group</category>
[<RequireQualifiedAccess>]
module FieldGroup =
    let create legend content =
        { legend = TextField.requiredText (nameof legend) legend
          content = content
          description = None
          layout = FieldLayout.Vertical
          attributes = [] }

    let withDescription description (config:FieldGroupConfig) = { config with description = Some(TextField.requiredText (nameof description) description) }
    let withLayout layout (config:FieldGroupConfig) = { config with layout = layout }
    let withAttributes attributes (config:FieldGroupConfig) = { config with attributes = attributes }

    let render (config:FieldGroupConfig) =
        fieldset {
            _class "grid min-w-0 gap-3"
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            legend { _class "text-sm font-semibold text-[var(--fve-text)]"; config.legend }
            match config.description with Some description -> p { _class "-mt-2 text-sm text-[var(--fve-muted-text)]"; description } | None -> ()
            div {
                _class (match config.layout with FieldLayout.Vertical -> "grid gap-4" | FieldLayout.Horizontal -> "flex flex-wrap items-start gap-4" | FieldLayout.Responsive -> "grid gap-4 sm:grid-cols-2")
                config.content
            }
        }
