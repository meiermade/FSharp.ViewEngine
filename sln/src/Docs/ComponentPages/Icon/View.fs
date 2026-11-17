namespace Docs.ComponentPages.Icon

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let iconRegistration =
        registration "components-icon" "/components/icon" "Icon" "Icon"

    let iconExample = fveIcon { _name IconName.Cog6Tooth }

    let brandIcons =
        div {
            _class "grid gap-6"
            div {
                _class "flex flex-wrap items-center justify-center gap-8"
                for name in [ IconName.GitHub; IconName.X; IconName.LinkedIn ] do
                    div {
                        _class "grid justify-items-center gap-3"
                        fveIcon { _name name }
                        span { _class "text-sm"; string name }
                    }
            }
            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Brand artwork from "
                a { _href "https://icons.getbootstrap.com/"; _class "text-[var(--fve-brand-text)] underline"; "Bootstrap Icons" }
                "."
            }
        }

    let iconFamilies =
        div {
            _class "flex flex-wrap items-center justify-center gap-8"
            for label, variant in [ "Outline · 24px", IconVariant.Outline; "Solid · 24px", IconVariant.Solid; "Mini · 20px", IconVariant.Mini; "Micro · 16px", IconVariant.Micro ] do
                div {
                    _class "grid justify-items-center gap-3"
                    fveIcon { _name IconName.Cog6Tooth; _variant variant }
                    span { _class "text-sm"; label }
                }
        }

    let iconSizes =
        div {
            _class "flex flex-wrap items-center justify-center gap-8"
            for size, label in [ Size.ExtraSmall, "12px"; Size.Small, "16px"; Size.Medium, "20px"; Size.Large, "24px"; Size.ExtraLarge, "32px" ] do
                div {
                    _class "grid justify-items-center gap-3"
                    fveIcon { _name IconName.Heart; _variant IconVariant.Solid; _size size }
                    span { _class "text-sm"; label }
                }
        }

    let examples () =
            [ sample "icon-default" "Default" [ "iconExample" ] (centered iconExample)
              sample "icon-families" "Heroicons families" [ "iconFamilies" ] (centered iconFamilies)
              sample "icon-brands" "Brand logos" [ "brandIcons" ] (centered brandIcons)
              |> note "Logos default to Solid at 24px and retain their original silhouettes and viewBoxes; _size changes their displayed size. X uses the current logo and is also searchable as Twitter."
              |> note "Heroicons Outline and Solid use 24×24 artwork; Mini uses 20×20 and Micro 16×16. Each family has purpose-drawn paths, not just a different rendered size. Icons inherit currentColor and are decorative by default. Name an icon-only control with _ariaLabel on its Button or Link."
              sample "icon-sizes" "Rendered-size overrides" [ "iconSizes" ] (centered iconSizes)
              |> note "Size changes the rendered square, not the selected artwork: Extra small 12px, Small 16px, Medium 20px, Large 24px, Extra large 32px. Omit it to use the family's natural size. The bundled MIT-licensed Heroicons artwork needs no JavaScript or external request." ]


    let private galleryContent : Content =
        { description = "Typed inline SVG with Heroicons artwork families, brand logos and inherited color."
          usage = []
          sections = []
          apiCategory = Some "icon" }

    let iconPageFor count search =
        let page = gallery iconRegistration galleryContent (examples ())
        { page with
            descriptionContent = Some(span {
                "Typed inline SVG with "
                a { _href "https://heroicons.com/"; _class "text-[var(--fve-brand-text)] underline underline-offset-2"; "Heroicons" }
                " artwork families, brand logos and inherited color."
            })
            lead = IconBrowser.render count search :: page.lead }

    let iconPage = iconPageFor IconBrowser.batchSize ""
