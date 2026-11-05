namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

type internal BottomNavigationItemData = { href:string; body:HtmlElement; leading:HtmlElement option; attributes:HtmlAttribute list }
type internal BottomNavigationConfig = { id:string; label:string; items:BottomNavigationItemData list; current:string option; attributes:HtmlAttribute list }

type internal BottomNavigationItemElement(item:BottomNavigationItemData) =
    inherit HtmlElement()
    member _.Item = item
    override _.Render _ = invalidOp "fveBottomNavigationItem belongs inside fveBottomNavigation."

module internal BottomNavigationView =
    let id (config:BottomNavigationConfig) = config.id
    let destinations (config:BottomNavigationConfig) = config.items |> List.map _.href

    let render (className:string) (config:BottomNavigationConfig) =
        ComponentElement.render "nav" [_id config.id; _ariaLabel config.label; _class className; _data("fve-bottom-navigation", "true")]
            (ComponentHtml.safeAttributes ["id"; "aria-label"] config.attributes) [
                ul {
                    _role "list"
                    _class "flex min-h-[calc(var(--fve-control-min-height)+1rem)] items-stretch"
                    for item in config.items do
                        let current = config.current = Some item.href || ComponentElement.value "aria-current" item.attributes = Some "page"
                        li {
                            _class "flex min-w-0 flex-1"
                            let defaults = [
                                yield _href item.href
                                if current then yield _ariaCurrent "page"
                                yield _class (ComponentHtml.classes [
                                    "flex min-w-0 flex-1 flex-col items-center justify-center gap-1 px-2 py-2 text-center text-xs font-semibold no-underline outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                                    if current then "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]" else "text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ]) ]
                            ComponentElement.render "a" defaults (ComponentHtml.safeAttributes ["href"; "aria-current"] item.attributes) [
                                match item.leading with Some leading -> yield span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; leading } | None -> ()
                                yield span { _class "max-w-full truncate"; item.body } ]
                        }
                } ]

type internal BottomNavigationElement(config:BottomNavigationConfig) =
    inherit HtmlElement()
    member _.Config = config
    override _.Render output = (BottomNavigationView.render "border-t border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]" config).Render output

module internal BottomNavigation =
    let configuration (element:HtmlElement) = match element with :? BottomNavigationElement as element -> element.Config | _ -> invalidArg "navigation" "Use fveBottomNavigation."

    let item attributes children =
        let parts, body = ComponentParts.split "BottomNavigationItem" children
        let href = ComponentElement.required "href" attributes
        if String.IsNullOrWhiteSpace(Render.toString (ComponentContent.fragment body)) then invalidArg "children" "A bottom-navigation item requires a visible label."
        BottomNavigationItemElement { href = href; body = ComponentContent.fragment body; leading = Map.tryFind "leading" parts; attributes = attributes } :> HtmlElement

    let leading attributes children =
        let content = if List.isEmpty attributes then ComponentContent.fragment children else ComponentElement.render "span" [] attributes children
        ComponentPart("BottomNavigationItem", "leading", content) :> HtmlElement

    let render attributes (children:HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "id" "A stable bottom-navigation ID without whitespace is required."
        let items = children |> List.choose (function :? BottomNavigationItemElement as item -> Some item.Item | :? NoopElement -> None | _ -> invalidArg "children" "Use fveBottomNavigationItem children.")
        if items.IsEmpty then invalidArg "children" "Bottom navigation requires at least one destination."
        let hrefs = items |> List.map _.href
        if hrefs.Length <> (hrefs |> List.distinct |> List.length) then invalidArg "href" "Bottom-navigation destinations must be unique."
        let current = ComponentElement.value "value" attributes
        if current |> Option.exists (fun href -> not (List.contains href hrefs)) then invalidArg "value" "The current destination must exist in the bottom navigation."
        BottomNavigationElement { id = id; label = ComponentElement.required "aria-label" attributes; items = items; current = current; attributes = ComponentHtml.safeAttributes ["value"] attributes } :> HtmlElement

/// <category>bottom-navigation</category>
[<AutoOpen>]
module BottomNavigationDsl =
    let fveBottomNavigation = ComponentElementBuilder(BottomNavigation.render)
    let fveBottomNavigationItem = ComponentElementBuilder(BottomNavigation.item)
    let fveBottomNavigationItemLeading = ComponentElementBuilder(BottomNavigation.leading)
