# Tailwind

FSharp.ViewEngine provides custom elements for Tailwind UI components through the `Tailwind` type.

## Setup

Open the `Tailwind` type to access Tailwind custom elements:

```fsharp
open FSharp.ViewEngine
open type Html
open type Tailwind
```

## Form Elements

### Autocomplete

Build an autocomplete input with filtered options:

```fsharp
elAutocomplete {
    _class "w-64"
    input { _type "text"; _placeholder "Search..." }
    elOptions {
        elOption { "Option 1" }
        elOption { "Option 2" }
        elOption { "Option 3" }
    }
}
```

### Select

Create a custom select dropdown:

```fsharp
elSelect {
    _class "w-48"
    elSelectedContent { "Choose an option" }
    elOptions {
        elOption { "Small" }
        elOption { "Medium" }
        elOption { "Large" }
    }
}
```

## Overlay Elements

### Dialog

Build a modal dialog with a backdrop:

```fsharp
elDialog {
    elDialogBackdrop { _class "fixed inset-0 bg-black/30" }
    elDialogPanel {
        _class "mx-auto max-w-sm rounded bg-white p-6"
        h2 { "Dialog Title" }
        p { "Dialog content goes here." }
        button { "Close" }
    }
}
```

### Dropdown

Create a dropdown menu:

```fsharp
elDropdown {
    button { "Options" }
    elMenu {
        _class "absolute mt-2 w-48 rounded bg-white shadow-lg"
        a { _href "#"; "Edit" }
        a { _href "#"; "Delete" }
    }
}
```

## Navigation Elements

### Tab Group

Build a tabbed interface:

```fsharp
elTabGroup {
    elTabList {
        _class "flex space-x-1 rounded-xl bg-blue-900/20 p-1"
        button { "Tab 1" }
        button { "Tab 2" }
        button { "Tab 3" }
    }
    elTabPanels {
        div { "Content for Tab 1" }
        div { "Content for Tab 2" }
        div { "Content for Tab 3" }
    }
}
```

## Command Palette

### Command Palette

Build a command palette for search and navigation:

```fsharp
elCommandPalette {
    input { _type "text"; _placeholder "Search commands..." }
    elCommandList {
        elCommandGroup {
            _class "p-2"
            div { "Open File" }
            div { "Run Command" }
        }
        elCommandPreview {
            _class "p-4"
            "Preview content here"
        }
    }
    elNoResults { "No results found." }
}
```

## Attributes

### popover

Mark an element as a popover:

```fsharp
div { _popover; "Popover content" }
```

### anchor

Position an element relative to a reference using anchor positioning:

```fsharp
div {
    _anchor "bottom-start"
    "Anchored content"
}
```

## Defaults

Use `elDefaults` to set default values for child components:

```fsharp
elDefaults {
    _class "text-sm"
    elSelect {
        elOptions {
            elOption { "A" }
            elOption { "B" }
        }
    }
}
```

## Complete Example

Here's a complete example combining several Tailwind UI elements:

```fsharp
div {
    _class "p-8"
    elTabGroup {
        elTabList {
            _class "flex space-x-1 rounded-xl bg-blue-900/20 p-1"
            button { _class "rounded-lg px-3 py-2"; "Search" }
            button { _class "rounded-lg px-3 py-2"; "Browse" }
        }
        elTabPanels {
            div {
                elAutocomplete {
                    _class "mt-4 w-full"
                    input { _type "text"; _placeholder "Search items..." }
                    elOptions {
                        elOption { "Item 1" }
                        elOption { "Item 2" }
                    }
                }
            }
            div {
                elSelect {
                    _class "mt-4 w-full"
                    elOptions {
                        elOption { "Category A" }
                        elOption { "Category B" }
                    }
                }
            }
        }
    }
}
```
