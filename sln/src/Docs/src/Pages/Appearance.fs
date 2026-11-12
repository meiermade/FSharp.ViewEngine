namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar

/// Consumer-owned appearance patterns; only the document initializer is installed support.
module Appearance =
    let appearanceChoices =
        [ ColorMode.System, "System"; ColorMode.Light, "Light"; ColorMode.Dark, "Dark" ]

    let appearanceBindings (signal: string) =
        [ _dataSignals (signal, ["ifmissing"], "window.fveColorMode.current()")
          _dataInit $"${signal} = window.fveColorMode.current()"
          _dataOn ("fve-color-mode", ["window"], $"${signal} = window.fveColorMode.current()") ]

    let appearanceIcon =
        span {
            _ariaHidden true
            _class "inline-flex"
            span { _class "inline-flex dark:hidden"; fveIcon { _name IconName.Sun; _size Size.Small } }
            span { _class "hidden dark:inline-flex"; fveIcon { _name IconName.Moon; _size Size.Small } }
        }

    let appearanceMenu (id: string) (trigger: HtmlAttribute list -> HtmlElement) =
        let signal = "_" + id.Replace('-', '_') + "_mode"

        div {
            for attribute in appearanceBindings signal do
                attribute

            fveDropdownMenu {
                _id id
                fveDropdownMenuTrigger { trigger }

                for mode, caption in appearanceChoices do
                    let value = ColorMode.value mode
                    let isChecked = mode = ColorMode.System

                    fveDropdownMenuItem {
                        _role "menuitemradio"
                        _ariaChecked isChecked
                        _dataAttr ("aria-checked", $"${signal} == '{value}' ? 'true' : 'false'")
                        _dataOn ("click", $"window.fveColorMode.set('{value}')")
                        caption
                    }
            }
        }

    let iconAppearance (id: string) =
        appearanceMenu id (fun attributes ->
            button {
                for attribute in attributes do
                    attribute

                _ariaLabel "Choose appearance"

                _class
                    "inline-flex size-10 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                appearanceIcon
            })

    let buttonAppearance (id: string) =
        appearanceMenu id (fun attributes ->
            fveButton {
                yield! attributes
                _variant ButtonVariant.Outline
                "Appearance"
            })

    let radioAppearance (id: string) =
        let signal = "_" + id.Replace('-', '_') + "_mode"

        fveRadioGroup {
            _id id
            yield! appearanceBindings signal
            yield! ColorMode.nativeChoiceAttributes
            fveFieldLegend { "Appearance choices" }

            for mode, caption in appearanceChoices do
                let value = ColorMode.value mode
                let isChecked = mode = ColorMode.System

                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id (id + "-" + value)
                        _name id
                        _value value
                        _checked isChecked
                        _dataBind signal
                    }

                    fveLabel {
                        _for (id + "-" + value)
                        caption
                    }
                }
        }

    let segmentedAppearance (id: string) =
        let signal = "_" + id.Replace('-', '_') + "_mode"

        fveRadioGroup {
            _id id
            yield! appearanceBindings signal
            yield! ColorMode.nativeChoiceAttributes
            fveFieldLegend { "Segmented appearance" }

            div {
                _class
                    "inline-flex w-fit max-w-full flex-wrap items-center gap-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-1"

                for mode, caption in appearanceChoices do
                    let value = ColorMode.value mode
                    let isChecked = mode = ColorMode.System

                    label {
                        _class "relative cursor-pointer"

                        fveRadio {
                            _id (id + "-" + value)
                            _name id
                            _value value
                            _checked isChecked
                            _dataBind signal
                            _class "peer sr-only"
                        }

                        span {
                            _class
                                "block min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] peer-checked:bg-[var(--fve-surface)] peer-checked:text-[var(--fve-brand-text)] peer-checked:shadow-sm peer-focus-visible:ring-2 peer-focus-visible:ring-inset peer-focus-visible:ring-[var(--fve-brand-ring)] forced-colors:peer-focus-visible:outline-2 forced-colors:peer-focus-visible:outline-[Highlight]"

                            caption
                        }
                    }
            }
        }

    let appearanceDocument (nonce: string option) =
        html {
            _lang "en"

            match nonce with
            | Some value -> _attr ("data-nonce", value)
            | None -> ()

            head {
                meta { _charset "utf-8" }

                meta {
                    _name "viewport"
                    _content "width=device-width, initial-scale=1"
                }

                title { "Appearance preferences" }
                // This is the localStorage key used by every control in this document.
                // The initializer runs before CSS and independently of the module bundle.
                ColorMode.assetsWithNonce "appearance-guide" ColorMode.System nonce

                link {
                    _rel "stylesheet"
                    _href "/css/output.css"
                }

                script {
                    _type "module"
                    _src "/assets/appearance.js"

                    match nonce with
                    | Some value -> _attr ("nonce", value)
                    | None -> ()
                }
            }

            body {
                _class (
                    ComponentsTheme.className ComponentsTheme.sky
                    + " m-0 min-h-screen bg-[var(--fve-background)] font-sans text-base text-[var(--fve-text)] antialiased"
                )

                main {
                    _class "mx-auto grid min-w-0 max-w-2xl grid-cols-1 gap-8 p-4 [overflow-wrap:anywhere] sm:p-6"

                    h1 {
                        _class "text-xl font-semibold sm:text-2xl"
                        "Appearance preferences"
                    }

                    p {
                        "These controls share the appearance-guide browser preference. System follows your operating system without changing the selected preference."
                    }

                    div {
                        _class "flex flex-wrap items-center gap-4"
                        iconAppearance "document-icon-appearance"
                        buttonAppearance "document-button-appearance"
                    }

                    radioAppearance "document-radio-appearance"
                    segmentedAppearance "document-segmented-appearance"

                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "This preference is stored in this browser, not saved to a server-side profile."
                    }

                    a {
                        _href "/guides/theming"
                        _class "text-[var(--fve-brand-text)] underline"
                        "Read the Theming guide"
                    }
                }
            }
        }

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Appearance.fs")

    let source names =
        "open FSharp.ViewEngine\nopen Acme.Components\nopen type Html\nopen type Datastar\n\n"
        + SourceRegion.declarations names sourceText.Value

    let menuDeclarations =
        [ "appearanceChoices"; "appearanceBindings"; "appearanceMenu" ]

    let radioDeclarations =
        [ "appearanceChoices"; "appearanceBindings"; "radioAppearance" ]

    let segmentedDeclarations =
        [ "appearanceChoices"; "appearanceBindings"; "segmentedAppearance" ]

    let documentDeclarations =
        [ "appearanceChoices"
          "appearanceBindings"
          "appearanceIcon"
          "appearanceMenu"
          "iconAppearance"
          "buttonAppearance"
          "radioAppearance"
          "segmentedAppearance"
          "appearanceDocument" ]

    let copiedSources () =
        [ "appearance-document", source documentDeclarations
          "appearance-icon-menu", source (menuDeclarations @ [ "appearanceIcon"; "iconAppearance" ])
          "appearance-button-menu", source (menuDeclarations @ [ "buttonAppearance" ])
          "appearance-radios", source radioDeclarations
          "appearance-segmented", source segmentedDeclarations ]
