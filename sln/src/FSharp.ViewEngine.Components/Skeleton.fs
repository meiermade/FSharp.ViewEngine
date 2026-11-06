namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Skeleton =
    let render attributes children =
        ComponentContent.noChildren "Skeleton" children
        ComponentElement.choice "aria-hidden" "true" ["true"] attributes |> ignore
        let dimensions =
            ["width"; "height"]
            |> List.choose (fun name ->
                ComponentElement.attribute name attributes
                |> Option.map (fun _ -> name + ": " + ComponentElement.required name attributes))
        ComponentElement.render "span"
            [ _ariaHidden true
              // Zero-specificity rounding lets caller utilities override the default regardless of CSS order.
              _class "block animate-pulse bg-[var(--fve-surface-subtle)] motion-reduce:animate-none [:where(&)]:rounded-[var(--fve-radius-control)]"
              if not dimensions.IsEmpty then _style (String.concat "; " dimensions) ]
            (ComponentHtml.safeAttributes ["width"; "height"] attributes) []

    let region attributes children =
        let label = ComponentElement.required "aria-label" attributes
        for name, value in ["role", "status"; "aria-busy", "true"; "aria-live", "polite"] do
            ComponentElement.choice name value [value] attributes |> ignore
        ComponentElement.render "div" [_role "status"; _ariaBusy true; _ariaLive "polite"; _class "w-full min-w-0"] attributes
            [ span { _class "sr-only"; label }; ComponentContent.fragment children ]

/// <category>skeleton</category>
[<AutoOpen>]
module SkeletonDsl =
    /// A presentational loading placeholder, hidden from assistive technology.
    let fveSkeleton = ComponentElementBuilder(Skeleton.render)
    /// The single status owner around an application-authored placeholder layout.
    let fveSkeletonRegion = ComponentElementBuilder(Skeleton.region)
