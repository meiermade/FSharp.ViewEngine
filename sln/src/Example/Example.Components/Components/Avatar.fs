namespace Example.Components

open FSharp.ViewEngine
open type Html

module internal Avatar =
    let render attributes children =
        let decorative = ComponentElement.value "aria-hidden" attributes = Some "true"
        if not decorative then ComponentElement.required "aria-label" attributes |> ignore
        if List.isEmpty children then invalidArg "children" "Avatar fallback content is required."
        let size = ComponentElement.size (Some Size.Medium) attributes |> Option.get
        let sizeClass =
            match size with
            | Size.ExtraSmall -> "size-6" | Size.Small -> "size-8" | Size.Medium -> "size-10"
            | Size.Large -> "size-12" | Size.ExtraLarge -> "size-14"
        let content =
            match ComponentElement.value "src" attributes with
            | Some source -> img { _src source; _alt ""; _class "size-full object-cover" }
            | None -> span { _ariaHidden true; _class "text-sm"; children }
        ComponentElement.render "span"
            ([ _class ("relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--fve-neutral-subtle)] font-semibold text-[var(--fve-neutral-text)] " + sizeClass) ]
             @ if decorative then [_ariaHidden true] else [_role "img"])
            (ComponentHtml.safeAttributes ["src"; "size"] attributes) [content]

/// <category>avatar</category>
[<AutoOpen>]
module AvatarDsl =
    /// A native avatar with accessible naming, optional image and ordinary fallback content.
    let fveAvatar = ComponentElementBuilder(Avatar.render)
