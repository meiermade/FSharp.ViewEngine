# Custom Elements & Attributes

FSharp.ViewEngine covers all standard HTML elements and attributes, but you may need custom ones for web components or non-standard attributes.

## Custom Elements

### el

Use `Html.el` to create a custom element with children. This is useful for web components:

```fsharp
open FSharp.ViewEngine
open type Html

el "my-component" {
    _class "container"
    p { "Hello from a web component!" }
}
```

Renders:

```html
<my-component class="container">
    <p>Hello from a web component!</p>
</my-component>
```

### elVoid

Use `Html.elVoid` to create a custom self-closing (void) element:

```fsharp
elVoid "my-icon" {
    _attr("name", "star")
    _attr("size", "24")
}
```

Renders:

```html
<my-icon name="star" size="24">
```

### Nested Web Components

Custom elements can be nested just like regular elements:

```fsharp
el "my-card" {
    _attr("variant", "outlined")
    el "my-card-header" {
        h2 { "Card Title" }
    }
    el "my-card-body" {
        p { "Card content goes here." }
    }
    el "my-card-footer" {
        button { _onclick "handleClick()"; "Action" }
    }
}
```

## Custom Attributes

### _attr

Use `Html._attr` to add any attribute not covered by the built-in helpers.

#### Key-value attribute

```fsharp
div {
    _attr("my-custom-attr", "value")
    "Content"
}
```

Renders:

```html
<div my-custom-attr="value">Content</div>
```

#### Boolean attribute

Pass only the name to render a valueless (boolean) attribute:

```fsharp
div {
    _attr "my-flag"
    "Content"
}
```

Renders:

```html
<div my-flag>Content</div>
```

### Combining with Built-in Attributes

Custom attributes work alongside all built-in attributes:

```fsharp
el "sl-button" {
    _attr("variant", "primary")
    _attr("size", "large")
    _attr "pill"
    _onclick "handleClick()"
    _class "my-button"
    "Click Me"
}
```

Renders:

```html
<sl-button variant="primary" size="large" pill onclick="handleClick()" class="my-button">
    Click Me
</sl-button>
```

## Extending the Html Type

F# supports [type extensions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/type-extensions) which let you add your own elements and attributes to the `Html` type. This is useful for project-specific conventions or design system components.

### Adding Custom Elements

```fsharp
open FSharp.ViewEngine

type Html with
    static member val myCard = TagBuilder("my-card") with get
    static member val myIcon = VoidBuilder("my-icon") with get
```

Then use them just like built-in elements:

```fsharp
open type Html

myCard {
    _class "shadow-lg"
    h2 { "Title" }
    p { "Card content" }
}

myIcon { _attr("name", "star") }
```

### Adding Custom Attributes

```fsharp
open FSharp.ViewEngine

type Html with
    static member inline _theme (v: string) = { Name = "data-theme"; Value = ValueSome v }
    static member inline _variant (v: string) = { Name = "variant"; Value = ValueSome v }
    static member inline _loading = { Name = "data-loading"; Value = ValueNone }
```

Then use them alongside built-in attributes:

```fsharp
open type Html

div {
    _theme "dark"
    _variant "outlined"
    _loading
    "Content"
}
```

### Design System Example

You can build a full design system module with reusable elements and attributes:

```fsharp
open FSharp.ViewEngine

type Ds =
    static member val alert = TagBuilder("ds-alert") with get
    static member val badge = TagBuilder("ds-badge") with get
    static member val tooltip = TagBuilder("ds-tooltip") with get
    static member inline _severity (v: string) = { Name = "severity"; Value = ValueSome v }
    static member inline _placement (v: string) = { Name = "placement"; Value = ValueSome v }
    static member inline _dismissible = { Name = "dismissible"; Value = ValueNone }
```

```fsharp
open type Html
open type Ds

alert {
    _severity "warning"
    _dismissible
    "This is a warning message."
}

tooltip {
    _placement "top"
    button { "Hover me" }
}
```

## Shoelace Example

Here's a more complete example using [Shoelace](https://shoelace.style/) web components:

```fsharp
el "sl-dialog" {
    _attr("label", "Confirm")
    _attr "open"
    p { "Are you sure?" }
    div {
        _slot "footer"
        el "sl-button" {
            _attr("variant", "primary")
            _onclick "this.closest('sl-dialog').hide()"
            "Confirm"
        }
    }
}
```
