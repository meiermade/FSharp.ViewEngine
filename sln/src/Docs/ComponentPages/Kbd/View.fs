namespace Docs.ComponentPages.Kbd

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
    let kbdRegistration = registration "components-kbd" "/components/kbd" "Kbd" "Kbd"

    let keyboardKey = fveKbd { "Esc" }

    let keyboardShortcut =
        fveKbdShortcut {
            _ariaLabel "Open search"

            for key in [ "⌘"; "K" ] do
                fveKbd { key }
        }

    let examples () =
            [ sample "kbd" "Default" [ "keyboardKey" ] (centered keyboardKey)
              sample "kbd-shortcut" "Shortcut group" [ "keyboardShortcut" ] (centered keyboardShortcut) ]


    let private galleryContent : Content =
        { description = "Render keyboard keys and shortcut groups as compact instructional content."
          usage = []
          sections = []
          apiCategory = Some "kbd" }

    let kbdPage = gallery kbdRegistration galleryContent (examples ())
