namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open type Html
open type FSharp.ViewEngine.TailwindElements

module TailwindElements =
    let private previewSurfaceWith modifier (content:HtmlElement) =
        div {
            _data("example-surface", "true")
            _class $"twe-preview-surface {modifier}"
            content
        }

    let private previewSurface content = previewSurfaceWith "" content
    let private floatingPreviewSurface content = previewSurfaceWith "twe-preview-surface-floating" content

    let private chevron =
        raw """<svg class="twe-chevron" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 11.168l3.71-3.938a.75.75 0 1 1 1.08 1.04l-4.25 4.5a.75.75 0 0 1-1.08 0l-4.25-4.5a.75.75 0 0 1 .02-1.06Z" clip-rule="evenodd" /></svg>"""

    let private searchIcon =
        raw """<svg class="twe-field-icon" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><circle cx="8.5" cy="8.5" r="4.75"/><path d="m12 12 4 4"/></svg>"""

    let private copyIcon =
        raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><rect x="6.5" y="6.5" width="9" height="9" rx="1.5"/><path d="M4.5 13.5h-.25A1.75 1.75 0 0 1 2.5 11.75v-7.5A1.75 1.75 0 0 1 4.25 2.5h7.5a1.75 1.75 0 0 1 1.75 1.75v.25"/></svg>"""

    let private warningIcon =
        raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M10 6.5v4.25m0 2.75h.01"/><circle cx="10" cy="10" r="7.25"/></svg>"""

    let private componentPreview id =
        match id with
        | "autocomplete" ->
            floatingPreviewSurface (
                elAutocomplete {
                    _class "twe-autocomplete"
                    input { _name "preview-user"; _placeholder "Search people…"; _class "twe-input" }
                    button { _type "button"; _ariaLabel "Show people"; _class "twe-field-button"; chevron }
                    elOptions {
                        _popover; _anchor "bottom start"; _class "twe-floating twe-options"
                        elOption { _value "wade"; _class "twe-option"; "Wade Cooper" }
                        elOption { _value "jane"; _class "twe-option"; "Jane Doe" }
                        elOption { _value "ariana"; _class "twe-option"; "Ariana Bell" }
                    }
                })
        | "command-palette" ->
            previewSurface (
                elCommandPalette {
                    _name "preview-command"; _class "twe-command-palette"
                    div { _class "twe-command-search"; searchIcon; input { _placeholder "Search commands…"; _class "twe-command-input" }; kbd { "⌘K" } }
                    elCommandList {
                        _class "twe-command-list"
                        elDefaults { button { _type "button"; _hidden true; _class "twe-command-item"; span { "Recent command" }; kbd { "↵" } } }
                        elCommandGroup {
                            button { _id "preview-open-file"; _type "button"; _hidden true; _class "twe-command-item"; span { "Open file" }; kbd { "↵" } }
                            button { _id "preview-search-project"; _type "button"; _hidden true; _class "twe-command-item"; span { "Search project" }; kbd { "↵" } }
                        }
                    }
                    elNoResults { _hidden true; _class "twe-no-results"; "No results found." }
                    elCommandPreview { _for "preview-open-file"; _class "twe-command-preview"; "Open a file from the current project" }
                })
        | "copy-button" ->
            previewSurface (
                div {
                    _class "twe-copy-row"
                    elCopyable { _id "preview-install-command"; _class "twe-copy-value"; "dotnet add package FSharp.ViewEngine" }
                    button { _type "button"; _command "--copy"; _commandfor "preview-install-command"; _class "twe-button twe-copy-button"; copyIcon; span { _class "in-data-copied:hidden"; "Copy" }; span { _class "not-in-data-copied:hidden"; "Copied" } }
                })
        | "dialog" ->
            previewSurface (
                div {
                    button { _type "button"; _command "show-modal"; _commandfor "preview-delete-profile"; _class "twe-button"; "Delete profile" }
                    elDialog {
                        dialog {
                            _id "preview-delete-profile"; _class "twe-dialog"
                            elDialogBackdrop { _class "twe-dialog-backdrop" }
                            elDialogPanel {
                                _class "twe-dialog-panel"
                                form {
                                    _method "dialog"
                                    div { _class "twe-dialog-icon"; warningIcon }
                                    div { _class "twe-dialog-copy"; h3 { "Delete profile?" }; p { "This action cannot be undone. All profile data will be permanently removed." } }
                                    div { _class "twe-dialog-actions"; button { _type "button"; _command "close"; _commandfor "preview-delete-profile"; _class "twe-button"; "Cancel" }; button { _type "submit"; _class "twe-button twe-button-danger"; "Delete" } }
                                }
                            }
                        }
                    }
                })
        | "disclosure" ->
            previewSurface (
                div {
                    _class "twe-disclosure"
                    button { _type "button"; _command "--toggle"; _commandfor "preview-answer"; _class "twe-disclosure-trigger"; span { "What does the answer mean?" }; chevron }
                    elDisclosure { _id "preview-answer"; _hidden true; _class "twe-disclosure-panel"; "The answer is 42 — a playful reference to The Hitchhiker’s Guide to the Galaxy." }
                })
        | "dropdown-menu" ->
            floatingPreviewSurface (
                elDropdown {
                    _class "twe-dropdown"
                    button { _type "button"; _class "twe-button"; "Options"; chevron }
                    elMenu {
                        _popover; _anchor "bottom start"; _class "twe-floating twe-menu"
                        button { _type "button"; _class "twe-option"; "Edit" }
                        button { _type "button"; _class "twe-option"; "Duplicate" }
                        hr { _role "none" }
                        button { _type "button"; _class "twe-option twe-option-danger"; "Delete" }
                    }
                })
        | "popover" ->
            floatingPreviewSurface (
                elPopoverGroup {
                    _class "twe-popover-group"
                    button { _type "button"; _popovertarget "preview-account-menu"; _class "twe-button"; "Account"; chevron }
                    elPopover {
                        _id "preview-account-menu"; _popover; _anchor "bottom start"; _class "twe-floating twe-popover"
                        div { _class "twe-avatar"; "AS" }
                        div { _class "twe-popover-copy"; strong { "Avery Stone" }; p { "avery@example.com" } }
                        button { _type "button"; _class "twe-popover-action"; "View profile" }
                    }
                })
        | "select" ->
            floatingPreviewSurface (
                elSelect {
                    _name "preview-status"; _value "active"; _class "twe-select"
                    button { _type "button"; _class "twe-button twe-select-button"; elSelectedContent { "Active" }; chevron }
                    elOptions {
                        _popover; _anchor "bottom start"; _class "twe-floating twe-options"
                        elOption { _value "active"; _class "twe-option"; "Active" }
                        elOption { _value "inactive"; _class "twe-option"; "Inactive" }
                        elOption { _value "archived"; _class "twe-option"; "Archived" }
                    }
                })
        | "tabs" ->
            previewSurface (
                elTabGroup {
                    _class "twe-tabs"
                    elTabList { _class "twe-tab-list"; button { _type "button"; _class "twe-tab"; "Account" }; button { _type "button"; _class "twe-tab"; "Security" } }
                    elTabPanels { _class "twe-tab-panels"; div { h3 { "Account settings" }; p { "Update your profile details and contact information." } }; div { _hidden true; h3 { "Security settings" }; p { "Manage your password and two-factor authentication." } } }
                })
        | _ -> previewSurface (div { "Component preview" })

    let private heading id title =
        Heading { id = id; title = title; level = 2 }

    let private componentNodes id title description source =
        [ heading id title;
          Paragraph [ Text description ];
          Example($"tailwind-elements-{id}", title, "fsharp", source, componentPreview id) ]

    let private nodes =
        [ [ Paragraph [ Text "FSharp.ViewEngine covers all 23 custom elements published by "; Link("Tailwind Plus Elements 1.0.22", "https://www.npmjs.com/package/@tailwindplus/elements/v/1.0.22"); Text ", a framework-independent library for the interactive behavior in Tailwind Plus HTML components." ];
            heading "setup" "Setup";
            Paragraph [ Text "Install Elements in the application and open the "; InlineContent.Code "TailwindElements"; Text " type when building views:" ];
            CodeBlock("shell", "npm install @tailwindplus/elements@1.0.22");
            CodeBlock("javascript", "import '@tailwindplus/elements'");
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type TailwindElements""");
            Paragraph [ Text "For applications without a JavaScript build pipeline, load the pinned module from a CDN:" ];
            CodeBlock("fsharp", """script {
    _src "https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22"
    _type "module"
}""");
            Paragraph [ Text "Elements targets modern browsers supported by Tailwind CSS v4: Chrome 111+, Safari 16.4+, and Firefox 128+." ];
            Paragraph [ Text "This page loads the pinned Elements 1.0.22 runtime, so each Preview tab below renders and operates the actual custom elements emitted by the F# builders." ];
            heading "shared-attributes" "Shared Attributes";
            Paragraph [ Text "Use the Elements-specific helpers for popover positioning. Anchor values follow the official grammar, such as "; InlineContent.Code "bottom start"; Text "." ];
            CodeBlock("fsharp", """elPopover {
    _popover
    _anchor "bottom start"
    _anchorStrategy "fixed"
}""");
            Paragraph [ Text "Invoker commands and other platform attributes already belong to "; InlineContent.Code "Html"; Text ". Use helpers such as "; InlineContent.Code "_command"; Text ", "; InlineContent.Code "_commandfor"; Text ", "; InlineContent.Code "_popovertarget"; Text ", "; InlineContent.Code "_open"; Text ", and "; InlineContent.Code "_hidden"; Text " alongside the custom element builders." ];
            Paragraph [ Text "Transition state attributes such as "; InlineContent.Code "data-closed"; Text ", "; InlineContent.Code "data-enter"; Text ", and "; InlineContent.Code "data-leave"; Text " are managed by Elements and should be treated as read-only styling hooks." ] ];

          componentNodes "autocomplete" "Autocomplete" "Combine elAutocomplete, elOptions, elOption, and elSelectedContent with native form controls." """elAutocomplete {
    input { _name "user" }
    button {
        _type "button"
        elSelectedContent { "Choose a user" }
    }
    elOptions {
        _popover
        _anchor "bottom start"
        elOption { _value "wade"; "Wade Cooper" }
        elOption { _value "jane"; "Jane Doe" }
    }
}""";

          componentNodes "command-palette" "Command Palette" "Use elCommandPalette with its list, defaults, grouping, empty-state, and preview helpers." """elCommandPalette {
    _name "command"
    input { _autofocus true; _placeholder "Search…" }
    elCommandList {
        elDefaults { button { _type "button"; "Recent command" } }
        elCommandGroup {
            button { _id "open-file"; _type "button"; "Open file" }
        }
    }
    elNoResults { _hidden true; "No results found." }
    elCommandPreview { _for "open-file"; "Open a file" }
}""";

          componentNodes "copy-button" "Copy Button" "Wrap copyable text with elCopyable and target it with the standard invoker-command helpers." """elCopyable {
    _id "install-command"
    "dotnet add package FSharp.ViewEngine"
}
button {
    _type "button"
    _command "--copy"
    _commandfor "install-command"
    "Copy"
}""";

          componentNodes "dialog" "Dialog" "Nest a native dialog inside elDialog, then use elDialogBackdrop and elDialogPanel for transitionable presentation." """button {
    _type "button"
    _command "show-modal"
    _commandfor "delete-profile"
    "Delete profile"
}
elDialog {
    dialog {
        _id "delete-profile"
        elDialogBackdrop { _class "fixed inset-0 bg-black/50" }
        elDialogPanel {
            form {
                _method "dialog"
                p { "Delete this profile?" }
                button {
                    _type "button"
                    _command "close"
                    _commandfor "delete-profile"
                    "Cancel"
                }
                button { _type "submit"; "Delete" }
            }
        }
    }
}""";

          componentNodes "disclosure" "Disclosure" "Pair elDisclosure with show, hide, or toggle invoker commands." """button {
    _type "button"
    _command "--toggle"
    _commandfor "answer"
    "Show answer"
}
elDisclosure {
    _id "answer"
    _hidden true
    "The answer is 42."
}""";

          componentNodes "dropdown-menu" "Dropdown Menu" "Use elDropdown to connect a native trigger button with an anchored elMenu." """elDropdown {
    button { _type "button"; "Options" }
    elMenu {
        _popover
        _anchor "bottom start"
        button { _type "button"; "Edit" }
        button { _type "button"; "Delete" }
    }
}""";

          componentNodes "popover" "Popover" "Use elPopover for arbitrary floating content and elPopoverGroup to keep related popovers open while focus moves within the group." """elPopoverGroup {
    button {
        _type "button"
        _popovertarget "account-menu"
        "Account"
    }
    elPopover {
        _id "account-menu"
        _popover
        _anchor "bottom end"
        _anchorStrategy "fixed"
        "Account options"
    }
}""";

          componentNodes "select" "Select" "Build an accessible custom select with elSelect, elSelectedContent, elOptions, and elOption." """elSelect {
    _name "status"
    _value "active"
    button {
        _type "button"
        elSelectedContent { "Active" }
    }
    elOptions {
        _popover
        _anchor "bottom start"
        elOption { _value "active"; "Active" }
        elOption { _value "inactive"; "Inactive" }
        elOption { _value "archived"; "Archived" }
    }
}""";

          componentNodes "tabs" "Tabs" "Place native tab buttons in elTabList and corresponding direct-child panels in elTabPanels." """elTabGroup {
    elTabList {
        button { _type "button"; "Account" }
        button { _type "button"; "Security" }
    }
    elTabPanels {
        div { "Account settings" }
        div { _hidden true; "Security settings" }
    }
}""";

          [ heading "runtime-readiness" "Runtime Readiness";
            Paragraph [ Text "If application JavaScript needs to call component methods, first check "; InlineContent.Code "customElements.get"; Text " or wait for the "; InlineContent.Code "elements:ready"; Text " window event. Rendering helpers only produce markup; they do not install or initialize the Elements runtime." ] ] ]
        |> List.concat

    let page =
        { id = "tailwind-elements"
          path = "/extensions/tailwind-elements"
          aliases = []
          navLabel = "Tailwind Plus Elements"
          category = "Extensions"
          title = "Tailwind Plus Elements"
          browserTitle = "Tailwind Plus Elements - FSharp.ViewEngine"
          nodes = nodes }
