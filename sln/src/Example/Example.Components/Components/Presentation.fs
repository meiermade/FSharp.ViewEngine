namespace Example.Components

open FSharp.ViewEngine
open type Html

module internal Presentation =
    let private printStyles = """
@media print {
  body:has([data-fve-presentation]) *:not(:has([data-fve-presentation])):not([data-fve-presentation]):not([data-fve-presentation] *) {
    display: none !important;
  }
  [data-fve-presentation-controls], [data-fve-presentation]::backdrop { display: none !important; }
}
"""

    let control action label (icon:IconName) disabled =
        fveButton {
            _variant ButtonVariant.Outline
            _size Size.Small
            _ariaLabel label
            _data("fve-presentation-action", action)
            _disabled disabled
            fveIcon { _name icon; _variant IconVariant.Mini }
        }

    let render attributes (children:HtmlElement list) =
        let label = ComponentElement.required "label" attributes
        let slides =
            children |> List.choose (function
                | :? NoopElement -> None
                | :? SlideElement as slide -> Some slide
                | _ -> invalidArg "children" "Presentation accepts only Slide children.")
        if slides.IsEmpty then invalidArg "children" "Presentation requires at least one slide."
        ComponentElement.render "fve-presentation"
            [_role "group"; _ariaLabel label; _data("fve-presentation", "")
             _class "m-0 block min-w-0 max-w-full border-0 bg-[var(--fve-surface-active,#e5e7eb)] p-4 text-[var(--fve-text,#101828)] md:p-8 data-[expanded=true]:fixed data-[expanded=true]:inset-0 data-[expanded=true]:m-0 data-[expanded=true]:grid data-[expanded=true]:h-dvh data-[expanded=true]:w-screen data-[expanded=true]:max-h-none data-[expanded=true]:max-w-none data-[expanded=true]:grid-rows-[minmax(0,1fr)] data-[expanded=true]:gap-4 data-[expanded=true]:overflow-hidden [&[data-expanded=true]_[data-fve-slide-viewport]]:absolute [&[data-expanded=true]_[data-fve-slide-viewport]]:inset-0 [&[data-expanded=true]_[data-fve-slide-viewport]]:overflow-hidden [&[data-expanded=true]_[data-fve-slide-frame]]:absolute [&[data-expanded=true]_[data-fve-slide-frame]]:left-1/2 [&[data-expanded=true]_[data-fve-slide-frame]]:top-[var(--fve-internal-presentation-canvas-center)] [&[data-expanded=true]_[data-fve-slide-frame]]:w-[1280px] [&[data-expanded=true]_[data-fve-slide-frame]]:min-w-0 [&[data-expanded=true]_[data-fve-slide-frame]]:max-w-none [&[data-expanded=true]_[data-fve-slide-frame]]:[transform:translate(-50%,-50%)_scale(var(--fve-internal-presentation-scale,1))] [&[data-expanded=true]>[data-fve-presentation-controls]]:absolute [&[data-expanded=true]>[data-fve-presentation-controls]]:m-0 [&[data-expanded=true]>[data-fve-presentation-controls]]:w-max [&[data-expanded=true]>[data-fve-presentation-controls]]:max-w-[var(--fve-internal-presentation-controls-width,100%)] [&[data-expanded=true]>[data-fve-presentation-controls]]:top-[var(--fve-internal-presentation-controls-top)] [&[data-expanded=true]>[data-fve-presentation-controls]]:right-[var(--fve-internal-presentation-controls-right)] [&[data-expanded=true]>[data-fve-presentation-controls]]:py-0"]
            (ComponentHtml.safeAttributes ["label"] attributes)
            [ Html.style { raw printStyles }
              div {
                  _hidden true
                  _data("fve-presentation-controls", "")
                  _role "group"
                  _ariaLabel "Presentation controls"
                  _class "sticky top-0 z-10 mx-auto mb-4 flex w-full max-w-[80rem] flex-wrap items-center justify-end gap-1 bg-[var(--fve-surface-active,#e5e7eb)] py-2 sm:gap-2"
                  div {
                      _hidden true
                      _data("fve-presentation-navigation", "")
                      _class "flex items-center gap-1 sm:gap-2"
                      control "previous" "Previous slide" IconName.ChevronLeft true
                      span { _data("fve-presentation-position", ""); _role "status"; _ariaLive "polite"; _class "min-w-[4ch] text-center text-sm tabular-nums"; $"1 of {slides.Length}" }
                      control "next" "Next slide" IconName.ChevronRight (slides.Length = 1)
                  }
                  span { _class "flex items-center"; _data("fve-presentation-expand", ""); control "expand" "Expand presentation" IconName.ArrowsPointingOut false }
                  span { _hidden true; _class "flex items-center"; _data("fve-presentation-restore", ""); control "restore" "Restore presentation" IconName.ArrowsPointingIn false }
              }
              div {
                  _data("fve-presentation-stage", "")
                  _class "relative grid min-h-0 min-w-0 gap-8 [&>[data-fve-slide-viewport]]:scroll-mt-24 [&_[data-fve-slide-frame]]:shadow-md [&_[data-fve-slide-frame]]:ring-1 [&_[data-fve-slide-frame]]:ring-inset [&_[data-fve-slide-frame]]:ring-[var(--fve-border,#d1d5db)]"
                  for index, slide in List.indexed slides do
                      slide.InPresentation { number=index + 1; total=slides.Length }
              } ]

/// <summary>Separate and number slides, enhanced with an expand/restore viewer.</summary>
/// <category>presentation</category>
[<AutoOpen>]
module PresentationDsl =
    let fvePresentation = ComponentElementBuilder(Presentation.render)
