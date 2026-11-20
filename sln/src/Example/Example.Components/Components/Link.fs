namespace Example.Components

open System
open FSharp.ViewEngine
open type Html

module internal Link =
    let render attributes children =
        let destinations = attributes |> List.filter (fun (attribute:HtmlAttribute) -> String.Equals(attribute.Name, "href", StringComparison.OrdinalIgnoreCase))
        if destinations.Length <> 1 then invalidArg "attributes" "Link requires exactly one href."
        let href = ComponentElement.required "href" attributes
        for attribute in attributes do
            match attribute.Name.ToLowerInvariant(), attribute.Value with
            | ("disabled" | "form" | "formaction" | "formenctype" | "formmethod" | "formnovalidate" | "formtarget"), _
            | "type", ValueSome ("button" | "submit" | "reset") ->
                invalidArg "attributes" "Link does not support button-only attributes; use fveButton for actions."
            | "aria-label", ValueSome value when String.IsNullOrWhiteSpace value ->
                invalidArg "attributes" "An accessible link label cannot be empty."
            | _ -> ()
        let variant = ComponentElement.choice "variant" "text" ["text"; "plain"; "solid"; "soft"; "outline"; "ghost"] attributes
        let size = ComponentElement.size None attributes
        let style = ComponentElement.actionStyle (_color Color.Primary :: attributes)
        if List.contains variant ["text"; "plain"] && ComponentElement.value "padding" attributes = Some "uniform" then
            invalidArg "attributes" "Uniform padding requires a control-style Link variant."
        if variant <> "text" && (ComponentElement.value "underline" attributes |> Option.isSome) then
            invalidArg "attributes" "Underline requires the Text Link variant."
        let underline = ComponentElement.choice "underline" "always" ["always"; "on-interaction"] attributes
        let quietInteraction =
            if ComponentElement.value "color" attributes = Some "neutral" then ComponentColors.neutralInteraction
            else "hover:bg-[var(--fve-internal-color-soft-hover)] active:bg-[var(--fve-internal-color-soft-active)]"
        let appearance =
            match variant with
            | "solid" -> ComponentColors.solid + " hover:bg-[var(--fve-internal-color-hover)] active:bg-[var(--fve-internal-color-active)]"
            | "soft" -> ComponentColors.soft + " hover:bg-[var(--fve-internal-color-soft-hover)] active:bg-[var(--fve-internal-color-soft-active)]"
            | "outline" -> "[--fve-internal-control-border-width:1px] border border-[color-mix(in_srgb,var(--fve-internal-color-border)_20%,transparent)] bg-transparent text-[var(--fve-internal-color-text)] " + quietInteraction
            | "ghost" -> ComponentColors.ghost + " " + quietInteraction
            | "plain" -> "outline-none focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-current"
            | _ ->
                let decoration = if underline = "on-interaction" then "hover:underline focus-visible:underline" else "underline"
                "font-medium text-[var(--fve-internal-color-text)] underline-offset-4 hover:decoration-2 outline-none focus-visible:ring-2 focus-visible:ring-offset-2 " + decoration
        let classes = ComponentHtml.classes [
            if variant = "plain" then ()
            elif variant = "text" then
                ActionStyles.iconClasses
                "inline-flex items-center gap-2"
                ComponentHtml.controlSizeClass size
                if size.IsSome then "text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)]"
            else
                ActionStyles.baseClasses
                if variant <> "ghost" then "shadow-xs"
                ComponentHtml.controlSizeClass size
                ActionStyles.geometryClasses
            appearance
            if variant <> "plain" && ComponentElement.value "color" attributes <> Some "custom" then
                if variant = "text" then ComponentColors.textInteraction
                elif variant <> "solid" then ComponentColors.quietTextInteraction
            if variant <> "plain" then ComponentColors.focus ]
        ComponentElement.render "a"
            [ _href href
              _data ("fve-group-control", if variant = "outline" then "outline" else "plain")
              _class classes
              if variant <> "plain" then _style style ]
            (ComponentHtml.safeAttributes ["href"; "variant"; "size"; "color"; "palette"; "padding"; "underline"] attributes) children

/// <summary>A native anchor with ordinary typed presentation and native destination attributes.</summary>
/// <category>link</category>
type LinkBuilder internal () =
    inherit ComponentElementBuilder(Link.render)

/// <category>link</category>
[<AutoOpen>]
module LinkDsl =
    /// Requires href. LinkVariant.Text is the default; other LinkVariant cases provide control-like links.
    let fveLink = LinkBuilder()
