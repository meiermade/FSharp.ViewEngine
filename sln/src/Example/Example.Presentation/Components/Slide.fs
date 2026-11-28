namespace Example.Components

open System.Globalization
open FSharp.ViewEngine
open type Html

/// <category>slide</category>
[<RequireQualifiedAccess>]
type SlideVariant = Standard | Cover

/// <category>slide</category>
[<AutoOpen>]
module SlideAttributes =
    type Html with
        static member _variant(value:SlideVariant) = Html._attr("slide-variant", match value with SlideVariant.Standard -> "standard" | SlideVariant.Cover -> "cover")

/// <exclude/>
type internal SlideContext =
    { number:int; total:int }

/// <exclude/>
type internal SlideElement(render:SlideContext -> HtmlElement) =
    inherit HtmlElement()
    member _.InPresentation context = render context
    override _.Render(writer) = (render {number=1; total=1}).Render writer

/// <exclude/>
type internal SlideNumberElement(attributes:HtmlAttribute list) =
    inherit HtmlElement()
    member _.At (number:int) =
        ComponentElement.render "span"
            [_data("fve-slide-number", ""); _ariaHidden true; _class "shrink-0 tabular-nums font-semibold"]
            attributes [Html.text (number.ToString("D2", CultureInfo.InvariantCulture))]
    override this.Render(writer) = (this.At 1).Render writer

module internal Slide =
    let private printStyles = """
@page fve-slide { size: 1280px 720px; margin: 0; }
@media print {
  html:has([data-fve-slide-viewport]), body:has([data-fve-slide-viewport]),
  body:has([data-fve-slide-viewport]) :has([data-fve-slide-viewport]) {
    display: block !important; position: static !important; inset: auto !important;
    width: auto !important; min-width: 0 !important; max-width: none !important;
    height: auto !important; min-height: 0 !important; max-height: none !important;
    margin: 0 !important; padding: 0 !important; border: 0 !important;
    overflow: visible !important; transform: none !important; contain: none !important;
    background: transparent !important; box-shadow: none !important;
  }
  [data-fve-slide-viewport], [data-fve-slide-viewport][hidden] {
    display: block !important; page: fve-slide; break-after: page; break-inside: avoid;
    width: 1280px !important; max-width: none !important; height: 720px !important;
    margin: 0 !important; padding: 0 !important; overflow: visible !important;
    position: static !important; outline: none !important;
    print-color-adjust: exact; -webkit-print-color-adjust: exact;
  }
  [data-fve-slide-frame] {
    position: static !important; inset: auto !important; transform: none !important;
    width: 1280px !important; min-width: 0 !important; max-width: none !important;
    height: 720px !important; margin: 0 !important;
    box-shadow: none !important; outline: none !important;
  }
  [data-fve-slide] { width: 1280px !important; height: 720px !important; }
  [data-fve-slide] > div { overflow: visible !important; }
}
"""

    let part name tag (classes:string) attributes children =
        ComponentPart("slide", name, ComponentElement.render tag [_class classes] attributes children) :> HtmlElement

    let content attributes children =
        part "content" "div" "min-h-0 min-w-0 overflow-auto text-[length:1.5625cqw] leading-normal" attributes children

    let render attributes (children:HtmlElement list) (context:SlideContext) =
        let variant = ComponentElement.choice "slide-variant" "standard" ["standard"; "cover"] attributes
        let numbers, children = children |> List.partition (fun child -> child :? SlideNumberElement)
        if numbers.Length > 1 then invalidArg "children" "Only one slide number is supported."
        let parts, body = ComponentParts.split "slide" children
        if parts.ContainsKey "content" && not body.IsEmpty then invalidArg "children" "Put slide body content inside SlideContent, not beside it."
        let branding = Map.tryFind "branding" parts
        let footer = Map.tryFind "footer" parts
        let number =
            match numbers with
            | (:? SlideNumberElement as value) :: _ -> value.At context.number
            | _ -> (SlideNumberElement []).At context.number
        let palette =
            if variant = "cover" then
                "bg-[var(--fve-slide-cover-background,#101828)] text-[var(--fve-slide-cover-text,white)] [--fve-text:var(--fve-slide-cover-text,white)] [--fve-muted-text:var(--fve-slide-cover-muted,#d1d5db)] [&_[data-fve-slide-title]]:text-[length:4.6875cqw]"
            else "bg-[var(--fve-surface,white)] text-[var(--fve-text,#101828)]"
        div {
            _class "min-w-0 max-w-full overflow-x-auto"
            _data("fve-slide-viewport", "")
            _tabindex 0
            _role "group"
            _ariaLabel $"Slide {context.number} of {context.total} canvas"
            if context.number = 1 then
                Html.style { raw printStyles }
            div {
                _class "@container/fve-slide mx-auto w-full min-w-[48rem] max-w-[80rem]"
                _data("fve-slide-frame", "")
                ComponentElement.render "section"
                    [_data("fve-slide", variant); _ariaLabel $"Slide {context.number} of {context.total}"
                     _class ("grid aspect-[16/9] min-w-0 grid-rows-[auto_minmax(0,1fr)_auto] gap-[1.25cqw] px-[5cqw] py-[3.125cqw] font-sans " + palette)]
                    (ComponentHtml.safeAttributes ["slide-variant"] attributes)
                    [ Html.header {
                          _class "flex min-w-0 items-start justify-between gap-[2.5cqw]"
                          match Map.tryFind "header" parts with Some value -> value | None -> ()
                          match branding with
                          | Some value -> div { _class "max-w-[12cqw] shrink-0 break-words [&_img]:h-auto [&_img]:max-w-full [&_svg]:max-w-full"; value }
                          | None -> () }
                      Map.tryFind "content" parts |> Option.defaultWith (fun () -> content [] body)
                      Html.footer {
                          _class "flex min-w-0 items-end justify-between gap-[2.5cqw] text-[length:0.9375cqw] leading-snug text-[var(--fve-muted-text,#667085)]"
                          div { _class "min-w-0 break-words"; match footer with Some value -> value | None -> () }
                          number } ]
            }
        }

/// <summary>Compose a 16:9 HTML slide. Header, body, footer and numbering share one frame.</summary>
/// <category>slide</category>
[<AutoOpen>]
module SlideDsl =
    let fveSlide = ComponentElementBuilder(fun attributes children -> SlideElement(Slide.render attributes children) :> HtmlElement)
    let fveSlideHeader = ComponentElementBuilder(Slide.part "header" "div" "grid min-w-0 flex-1 content-start gap-[0.9375cqw]")
    let fveSlideTitle = ComponentElementBuilder(fun attributes children -> ComponentElement.render "h2" [_data("fve-slide-title", ""); _class "m-0 break-words text-[length:3.125cqw] font-semibold leading-[1.08] tracking-[-0.035em] text-inherit"] attributes children)
    let fveSlideSubtitle = ComponentElementBuilder(fun attributes children -> ComponentElement.render "p" [_class "m-0 max-w-[65cqw] text-[length:1.5625cqw] leading-snug text-[var(--fve-muted-text,#667085)]"] attributes children)
    let fveSlideContent = ComponentElementBuilder(Slide.content)
    let fveSlideFooter = ComponentElementBuilder(Slide.part "footer" "div" "min-w-0")
    let fveSlideBranding = ComponentElementBuilder(Slide.part "branding" "div" "min-w-0")
    let fveSlideNumber = ComponentElementBuilder(fun attributes children -> ComponentContent.noChildren "SlideNumber" children; SlideNumberElement(attributes) :> HtmlElement)
