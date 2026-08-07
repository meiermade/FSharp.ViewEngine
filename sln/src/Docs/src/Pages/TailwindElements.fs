namespace Docs.Pages

open Docs.Common

module TailwindElements =
    let private heading id title =
        Heading { id = id; title = title; level = 2 }

    let private componentNodes id title description source =
        [ heading id title;
          Paragraph [ Text description ];
          CodeBlock("fsharp", source) ]

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
    "dotnet package add FSharp.ViewEngine"
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
