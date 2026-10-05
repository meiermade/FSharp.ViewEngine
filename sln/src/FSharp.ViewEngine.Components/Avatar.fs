namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>avatar</category>
[<RequireQualifiedAccess>]
type AvatarSize = Small | Medium | Large

/// <category>avatar</category>
[<NoEquality; NoComparison>]
type AvatarConfig = private { name:string; source:string option; fallback:string; decorative:bool; size:AvatarSize }

/// <category>avatar</category>
[<RequireQualifiedAccess>]
module Avatar =
    let create name fallback =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "An avatar name is required."
        if String.IsNullOrWhiteSpace fallback then invalidArg (nameof fallback) "Avatar fallback text is required."
        { name = name; source = None; fallback = fallback; decorative = false; size = AvatarSize.Medium }
    let withImage source (config:AvatarConfig) = { config with source = Some source }
    let decorative (config:AvatarConfig) = { config with decorative = true }
    let withSize size (config:AvatarConfig) = { config with size = size }
    let render config =
        let sizeClass =
            match config.size with
            | AvatarSize.Small -> "size-8"
            | AvatarSize.Medium -> "size-10"
            | AvatarSize.Large -> "size-12"
        let accessibleName = if config.decorative then None else Some config.name
        span {
            match accessibleName with
            | Some name -> _role "img"; _ariaLabel name
            | None -> _ariaHidden true
            _class ("relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--fve-neutral-subtle)] font-semibold text-[var(--fve-neutral-text)] " + sizeClass)
            match config.source with
            | Some source ->
                img {
                    _src source
                    _alt ""
                    _class "size-full object-cover"
                }
            | None ->
                span {
                    _ariaHidden true
                    _class "text-sm"
                    config.fallback
                }
        }
