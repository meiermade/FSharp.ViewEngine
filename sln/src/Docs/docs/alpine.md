# Alpine.js

FSharp.ViewEngine provides type-safe Alpine.js directives through the `Alpine` type.

## Setup

Open the `Alpine` type to access Alpine.js directives:

```fsharp
open FSharp.ViewEngine
open type Html
open type Alpine
```

## Directives

### x-data

Initialize a component's reactive data with `_xData`:

```fsharp
div {
    _xData "{ open: false, count: 0 }"
    button {
        _xOn ("click", "count++")
        "Increment"
    }
    span { _xText "count" }
}
```

### x-init

Run an expression when a component is initialized:

```fsharp
div {
    _xData "{ users: [] }"
    _xInit "users = await (await fetch('/api/users')).json()"
}
```

### x-show

Toggle the visibility of an element:

```fsharp
div {
    _xData "{ open: false }"
    button { _xOn ("click", "open = !open"); "Toggle" }
    div { _xShow "open"; "Content here" }
}
```

### x-if

Conditionally add or remove an element from the DOM:

```fsharp
template { _xIf "open"; div { "Shown when open is true" } }
```

### x-for

Loop over a list and render elements:

```fsharp
template {
    _xFor "item in items"
    li { _xText "item" }
}
```

### x-bind

Dynamically set HTML attributes:

```fsharp
div {
    _xBind ("class", "open ? 'block' : 'hidden'")
}
```

### x-on

Listen for browser events:

```fsharp
button {
    _xOn ("click", "handleClick()")
    _xOn ("mouseenter", "hovering = true")
    "Click me"
}
```

### x-text

Set the text content of an element:

```fsharp
span { _xText "message" }
```

### x-model

Two-way data binding for form elements:

```fsharp
div {
    _xData "{ search: '' }"
    input { _type "text"; _xModel "search" }
    p { _xText "search" }
}
```

You can also use modifiers:

```fsharp
input { _xModel ("value", ".debounce.500ms") }
```

### x-ref

Reference elements directly:

```fsharp
div {
    input { _xRef "nameInput" }
    button { _xOn ("click", "$refs.nameInput.focus()"); "Focus" }
}
```

## Additional Directives

### x-effect

Execute a script each time one of its dependencies change:

```fsharp
div { _xEffect "console.log(count)" }
```

### x-transition

Apply transition classes during enter/leave:

```fsharp
div {
    _xShow "open"
    _xTransition ()
    "Animated content"
}
```

### x-cloak

Hide an element until Alpine is initialized:

```fsharp
div { _xCloak; "Hidden until Alpine loads" }
```

### x-teleport

Move an element to another location in the DOM:

```fsharp
div { _xTeleport "body"; "Teleported to body" }
```

### x-trap

Trap focus within an element:

```fsharp
div { _xTrap "open"; "Focus is trapped here" }
```

### x-id

Scope generated IDs to the component:

```fsharp
div { _xId "['dropdown']" }
```
