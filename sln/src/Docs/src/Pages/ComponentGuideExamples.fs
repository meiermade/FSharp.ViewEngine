namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Datastar

module ComponentGuideExamples =
    let usingSignals =
        div {
            _class "grid gap-4"
            _dataSignals "{_guide_name: ''}"

            fveField {
                fveLabel {
                    _for "guide-name"
                    "Name"
                }

                fveInput {
                    _id "guide-name"
                    _name "name"
                    _dataBind "_guide_name"
                }
            }

            p {
                "Preview: "
                span { _dataText "$_guide_name || 'Untitled'" }
            }

            fveButton {
                _class "justify-self-start"
                _dataOn ("click", "$_guide_name = ''")
                _dataAttr ("disabled", "$_guide_name === ''")
                "Reset"
            }
        }

    let themedControls =
        let theme =
            ComponentsTheme.emerald
            |> ComponentsTheme.withRadius Radius.Large
            |> ComponentsTheme.withControlSize Size.Small

        div {
            _class ("grid gap-4 " + ComponentsTheme.className theme)

            fveField {
                fveLabel {
                    _for "theme-name"
                    "Name"
                }

                fveInput {
                    _id "theme-name"
                    _name "name"
                    _placeholder "Your name"
                }
            }

            fveButton {
                _class "justify-self-start"
                _color Color.Primary
                _variant ButtonVariant.Solid
                "Save"
            }
        }

    let customRadius =
        div {
            _style "--fve-radius-control:1rem"
            fveButton { "Rounded action" }
        }

    let appearanceHead =
        head {
            ColorMode.assets "my-app-appearance" ColorMode.System

            link {
                _rel "stylesheet"
                _href "/css/output.css"
            }
        }

    let appearancePicker =
        fveRadioGroup {
            _dataSignals "{_guide_appearance: window.fveColorMode.current()}"
            _dataOn ("fve-color-mode__window", "$_guide_appearance = window.fveColorMode.current()")
            yield! ColorMode.nativeChoiceAttributes
            fveFieldLegend { "Appearance" }

            for value, label in [ "system", "System"; "light", "Light"; "dark", "Dark" ] do
                let selected = value = "system"

                fveLabel {
                    fveRadio {
                        _name "guide-appearance"
                        _value value
                        _checked selected
                        _dataBind "_guide_appearance"
                    }

                    label
                }
        }

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.ComponentGuideExamples.fs")

    let source name =
        "open FSharp.ViewEngine\nopen Acme.Components\nopen type Html\nopen type Datastar\n\n"
        + SourceRegion.declarations [ name ] sourceText.Value

    let copiedSources () =
        [ for name in
              [ "usingSignals"
                "themedControls"
                "customRadius"
                "appearanceHead"
                "appearancePicker" ] do
              "guide-" + name, source name ]

    let private prose (text: string) =
        p {
            _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
            text
        }

    let private link href (text: string) =
        a {
            _href href
            _class "font-semibold text-[var(--fve-brand-text)] underline"
            text
        }

    let private code language (text: string) =
        fveCodeBlock {
            _language language
            text
        }

    let private example name title (preview: HtmlElement) =
        fveExample {
            _name ("guide-" + name)
            _label title
            _variant ExampleVariant.Lead
            _language "fsharp"
            fveExampleCode { source name }

            div {
                _class "grid max-w-md gap-4 p-6"
                preview
            }
        }

    let signalsPage (registration: DocPage) =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withDescription
            "Bind component values to a signal, react to changes, and handle a button action."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "binding"
                  "Bind a value and handle an action"
                  [ example "usingSignals" "Name preview" usingSignals
                    prose
                        "Type a name to update the preview. Reset clears the same signal; its disabled state follows that value. Datastar attributes compose directly with component attributes."
                    p {
                        "The "
                        link "/extensions/datastar" "Datastar guide"
                        " covers signal syntax and modifiers. Complete "
                        link "/components/installation" "Installation"
                        " first; its shared runtime already includes Datastar."
                    } ]
              DocumentationSection.create
                  "next"
                  "Related examples"
                  [ p {
                        "For a real request and pending state, see "
                        link "/components/button" "Button"
                        ". For server-backed search and selection, see "
                        link "/components/select" "Select"
                        ". Validate submitted values and permissions on the server."
                    } ] ]

    let themingPage (registration: DocPage) =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withDescription
            "Apply a shared theme, override a token, and let the user choose light, dark or system appearance."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "theme"
                  "Apply a theme to a region"
                  [ example "themedControls" "Shared theme and size" themedControls
                    prose
                        "A theme applies to its owning region and its components. This example selects Emerald, larger corners and Small controls; it does not change the surrounding page." ]
              DocumentationSection.create
                  "customization"
                  "Override only what you need"
                  [ example "customRadius" "Custom control radius" customRadius
                    prose
                        "Use ordinary classes and semantic CSS variables for local changes. Edit the installed source when the component structure itself needs to change."
                    p {
                        "For an action-specific color palette, see "
                        link "/components/button" "Button"
                        ". The "
                        link "/components/installation#updating" "update instructions"
                        " explain how local edits are preserved."
                    } ]
              DocumentationSection.create
                  "appearance"
                  "Add an appearance picker"
                  [ prose "After Installation, add the initializer and radio controls:"
                    code "shell" "dotnet fve add color-mode radio-group --config src/Acme.Components/fve.json"
                    prose
                        "Add the initializer before the stylesheet in your existing head. The key identifies the browser-local preference shared by the controls in that document."
                    code "fsharp" (source "appearanceHead")
                    example "appearancePicker" "Appearance" appearancePicker
                    prose
                        "The radios bind to the saved preference and follow changes from other controls. System stays selected while the operating system decides the resolved appearance. This preview uses the catalog's existing preference."
                    p {
                        "For a nonce-enabled host, use ColorMode.assetsWithNonce and the existing "
                        link "/extensions/datastar#content-security-policy" "CSP setup"
                        ". The "
                        link "/components/theming/document" "complete runnable example"
                        " is available separately."
                    } ]
              DocumentationSection.create
                  "api-reference"
                  "Theme API"
                  [ details {
                        summary {
                            _class "cursor-pointer font-semibold text-[var(--fve-brand-text)]"
                            "Shared theme types and helpers"
                        }

                        fveFSharpApiReference {
                            yield! FSharpApiReference.forCategory "theming" (typeof<ButtonBuilder>)
                        }
                    } ] ]
