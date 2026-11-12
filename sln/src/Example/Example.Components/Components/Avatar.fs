namespace Example.Components

open System
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
        let content = [
            yield span { _ariaHidden true; _class "text-sm"; children }
            match ComponentElement.value "src" attributes |> Option.filter (String.IsNullOrWhiteSpace >> not) with
            | Some source ->
                yield img {
                    _src source; _alt ""; _hidden true
                    _data ("fve-avatar-image", "true")
                    _class "absolute inset-0 size-full object-cover"
                    match ComponentElement.value "referrerpolicy" attributes with
                    | Some policy -> _referrerpolicy policy
                    | None -> ()
                }
            | None -> ()
        ]
        ComponentElement.render "fve-avatar"
            ([ _class ("relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--fve-neutral-subtle)] font-semibold text-[var(--fve-neutral-text)] " + sizeClass) ]
             @ if decorative then [_ariaHidden true] else [_role "img"])
            (ComponentHtml.safeAttributes ["src"; "size"; "referrerpolicy"] attributes) content

/// <category>avatar</category>
[<AutoOpen>]
module AvatarDsl =
    /// An accessible avatar with native fallback content; Assets/avatar.js reveals successfully loaded images.
    let fveAvatar = ComponentElementBuilder(Avatar.render)
