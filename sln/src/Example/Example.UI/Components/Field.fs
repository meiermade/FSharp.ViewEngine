namespace Example.Components

open FSharp.ViewEngine
open type Html

/// <category>field</category>
[<RequireQualifiedAccess>]
type FieldLayout = Vertical | Horizontal | Responsive

/// <category>field</category>
[<AutoOpen>]
module FieldAttributes =
    type Html with
        static member _layout(value:FieldLayout) =
            Html._attr("layout", match value with FieldLayout.Vertical -> "vertical" | FieldLayout.Horizontal -> "horizontal" | FieldLayout.Responsive -> "responsive")

module internal Field =
    let render attributes children =
        let layout = ComponentElement.choice "layout" "vertical" ["vertical"; "horizontal"; "responsive"] attributes
        let classes =
            match layout with
            | "horizontal" -> "flex items-start gap-3 [&>input:not([type=checkbox]):not([type=radio])]:w-auto [&>input:not([type=checkbox]):not([type=radio])]:flex-1"
            | "responsive" -> "grid content-start gap-1.5 sm:grid-cols-[minmax(8rem,0.4fr)_minmax(0,1fr)] sm:items-start [&>:not(label)]:sm:col-start-2 [&>label]:sm:row-start-1"
            | _ -> "grid content-start gap-1.5"
        ComponentElement.render "div" [_class ("min-w-0 [overflow-wrap:anywhere] " + classes)] (ComponentHtml.safeAttributes ["layout"] attributes) children

    let part tag (classes:string) defaults attributes children =
        ComponentElement.render tag (_class classes :: defaults) attributes children

/// <category>field</category>
[<AutoOpen>]
module FieldDsl =
    /// Layout only. Labels and controls use ordinary for/id and ARIA relationships.
    let fveField = ComponentElementBuilder(Field.render)
    let fveLabel = ComponentElementBuilder(Field.part "label" "flex min-w-0 items-center gap-2 text-sm font-medium text-[var(--fve-text)]" [])
    let fveFieldContent = ComponentElementBuilder(Field.part "div" "grid min-w-0 flex-1 content-start gap-1.5" [])
    let fveFieldDescription = ComponentElementBuilder(Field.part "p" "text-sm text-[var(--fve-muted-text)]" [])
    let fveFieldError = ComponentElementBuilder(Field.part "p" "text-sm text-[var(--fve-critical-text)]" [_role "alert"])
