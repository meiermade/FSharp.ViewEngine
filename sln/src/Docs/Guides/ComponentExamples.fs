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

    let scopedTokens =
        div {
            _id "theming-scoped-controls"
            _class (ComponentsTheme.className ComponentsTheme.emerald + " grid gap-4")
            _style "--fve-radius-control:0.75rem;--fve-hover-wash:light-dark(rgb(0 0 0 / 0.07),rgb(255 255 255 / 0.12));--fve-pressed-wash:light-dark(rgb(0 0 0 / 0.12),rgb(255 255 255 / 0.18));--fve-primary-hover:light-dark(#166534,#34d399);--fve-primary-active:light-dark(#14532d,#10b981)"
            fveButton { _color Color.Primary; _variant ButtonVariant.Solid; "Save changes" }
            fveLink { _href "/components/button"; _color Color.Neutral; _variant LinkVariant.Outline; "Button reference" }
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
            _dataOn ("fve-color-mode", ["window"], "$_guide_appearance = window.fveColorMode.current()")
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
                "scopedTokens"
                "appearanceHead"
                "appearancePicker" ] do
              "guide-" + name, source name ]

    let private prose (text: string) =
        p {
            _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
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

    let themingPageFor state (registration: DocPage) =
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
                    prose "Neutral interactive controls paint a translucent background wash without fading their content or replacing their selected color. --fve-hover-wash defaults to black at 5% in light mode and white at 10% in dark mode; --fve-pressed-wash uses 10% and 15%. Override these on the owning region, not the opaque --fve-surface or --fve-surface-hover tokens."
                    code "css" ".quiet-region {\n  --fve-hover-wash: light-dark(rgb(0 0 0 / 0.05), rgb(255 255 255 / 0.10));\n  --fve-pressed-wash: light-dark(rgb(0 0 0 / 0.10), rgb(255 255 255 / 0.15));\n}"
                    p {
                        "Table rows keep an opaque hover fill so sticky cells match. Use "
                        link "/components/table#components-table-hover" "Table’s --fve-table-row-hover override"
                        " for a table-specific color."
                    }
                    p {
                        "For an action-specific color palette, see "
                        link "/components/button" "Button"
                        ". The "
                        link "/components/installation#updating" "update instructions"
                        " explain how local edits are preserved."
                    } ]
              DocumentationSection.create
                  "scoped-overrides"
                  "Scope CSS variables to a region"
                  [ prose "Put overrides on the owning ComponentsTheme region or the specific component. A nested theme resets its own defaults; an explicit component _size or _density can set more-local values. Ordinary inherited overrides never need a new theme or a JavaScript observer."
                    example "scopedTokens" "Scoped light/dark overrides" scopedTokens
                    prose "The F# example uses _style for locality. The equivalent stylesheet can target the same region ID; light-dark() follows that region’s color-scheme, including manual appearance preferences."
                    code "css" "#theming-scoped-controls {\n  --fve-radius-control: 0.75rem;\n  --fve-hover-wash: light-dark(rgb(0 0 0 / 0.07), rgb(255 255 255 / 0.12));\n  --fve-pressed-wash: light-dark(rgb(0 0 0 / 0.12), rgb(255 255 255 / 0.18));\n  --fve-primary-hover: light-dark(#166534, #34d399);\n  --fve-primary-active: light-dark(#14532d, #10b981);\n}" ]
              DocumentationSection.create
                  "css-variables"
                  "CSS-variable reference"
                  [ prose "These are the supported customization variables. Defaults below come from ComponentsTheme.sky with its default Medium controls and Comfortable density, before application overrides. Other named themes change the brand values; changing appearance selects the corresponding mode. The catalog itself uses Compact density."
                    prose "Search by variable name, purpose or scope, or filter to theme defaults, action palettes or component overrides. Each variable appears once, with ten results per page."
                    prose "Each of the seven built-in action colors has independently overridable fields. The reference reads the actual palette fallbacks and shared Sky semantic defaults; var(...) defaults follow the owning theme. Neutral, Warning and Info text variables also serve their corresponding semantic controls. Neutral Outline/Ghost actions use the shared background-wash tokens instead of the opaque Soft fills. Color.Custom uses its supplied ColorPalette, rather than these built-in token defaults."
                    ThemeTokens.reference state ]
              DocumentationSection.create
                  "internal-variables"
                  "Internal variables"
                  [ prose "Variables prefixed --fve-internal- are component-managed implementation details. Use the customization variables listed above." ]
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
                        _class "[overflow-wrap:anywhere]"
                        "For a nonce-enabled host, use ColorMode.assetsWithNonce and the existing "
                        link "/extensions/datastar#content-security-policy" "CSP setup"
                        ". The "
                        link "/guides/theming/document" "complete runnable example"
                        " is available separately."
                    } ]
              DocumentationSection.create
                  "api-reference"
                  "Theme API"
                  [ fveFSharpApiReference {
                        yield! FSharpApiReference.forCategory "theming" (typeof<ButtonBuilder>)
                    } ] ]

    let themingPage registration = themingPageFor (ThemeTokens.select "" "all" 1) registration
