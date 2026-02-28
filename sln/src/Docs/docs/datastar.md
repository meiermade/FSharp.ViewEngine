# Datastar

FSharp.ViewEngine provides type-safe Datastar attributes through the `Datastar` type.

## Setup

Open the `Datastar` type to access Datastar attributes:

```fsharp
open FSharp.ViewEngine
open type Html
open type Datastar
```

## Core Attributes

### data-signals

Define reactive signals on an element:

```fsharp
div {
    _dataSignals ("count", "0")
    _dataSignals ("name", "'World'")
}
```

### data-on

Listen for events and run expressions:

```fsharp
button {
    _dataOn ("click", "$count++")
    "Increment"
}
```

### data-bind

Two-way bind a signal to an input element:

```fsharp
input { _type "text"; _dataBind "name" }
```

### data-show

Conditionally show or hide an element:

```fsharp
div { _dataShow "$count > 0"; "Count is positive" }
```

### data-text

Set the text content of an element reactively:

```fsharp
span { _dataText "$count" }
```

### data-effect

Run an expression whenever its dependencies change:

```fsharp
div { _dataEffect "console.log($count)" }
```

### data-init

Run an expression when the element is initialized:

```fsharp
div { _dataInit "console.log('initialized')" }
```

### data-attr

Dynamically set an HTML attribute:

```fsharp
div { _dataAttr ("disabled", "$count === 0") }
```

### data-class

Toggle a CSS class based on an expression:

```fsharp
div { _dataClass ("active", "$isActive") }
```

### data-computed

Define a computed signal derived from other signals:

```fsharp
div { _dataComputed ("double", "$count * 2") }
```

### data-style

Dynamically set a CSS style property:

```fsharp
div { _dataStyle ("color", "$isError ? 'red' : 'green'") }
```

### data-ref

Reference an element by name:

```fsharp
input { _dataRef "myInput" }
```

### data-indicator

Bind a loading indicator signal:

```fsharp
button { _dataIndicator "loading" }
```

### data-json-signals

Render signals as JSON for debugging:

```fsharp
pre { _dataJsonSignals () }
pre { _dataJsonSignals "{include: /counter/, exclude: /temp$/}" }
```

### data-ignore

Prevent Datastar from processing an element:

```fsharp
div { _dataIgnore }
```

### data-ignore-morph

Prevent morphing of an element during updates:

```fsharp
div { _dataIgnoreMorph }
```

### data-on-intersect

Run an expression when an element enters the viewport:

```fsharp
div { _dataOnIntersect "$count++" }
```

### data-on-interval

Run an expression on a timed interval:

```fsharp
div { _dataOnInterval "$count++" }
```

### data-on-signal-patch

Run an expression when signals are patched:

```fsharp
div { _dataOnSignalPatch "console.log('patched')" }
```

### data-on-signal-patch-filter

Filter which signal patches trigger the expression:

```fsharp
div { _dataOnSignalPatchFilter "{include: /^count$/}" }
```

### data-preserve-attr

Preserve specified attributes during morphing:

```fsharp
div { _dataPreserveAttr "class" }
```

## Pro Attributes

### data-animate

Apply animations to an element:

```fsharp
div { _dataAnimate "fadeIn 0.5s" }
```

### data-custom-validity

Set custom validation messages:

```fsharp
input { _dataCustomValidity "$name === '' ? 'Name is required' : ''" }
```

### data-on-raf

Run an expression on every animation frame:

```fsharp
canvas { _dataOnRaf "draw()" }
```

### data-on-resize

Run an expression when the element is resized:

```fsharp
div { _dataOnResize "console.log('resized')" }
```

### data-persist

Persist signals to local storage (or session storage with modifiers):

```fsharp
div { _dataPersist () }                                  // default key: datastar
div { _dataPersist "mykey" }                           // custom storage key
div { _dataPersist ("mykey", "{include: /foo/}") }    // key + filter object
```

### data-query-string

Sync signals with URL query parameters:

```fsharp
div { _dataQueryString () }
div { _dataQueryString "{include: /foo/, exclude: /temp$/}" }
```

### data-replace-url

Replace the current URL:

```fsharp
div { _dataReplaceUrl "/new-path" }
```

### data-rocket

Create a Rocket web component:

```fsharp
div { _dataRocket "{ endpoint: '/stream' }" }
```

### data-scroll-into-view

Scroll the element into view:

```fsharp
div { _dataScrollIntoView }
```

### data-view-transition

Apply view transitions:

```fsharp
div { _dataViewTransition "fade" }
```

## Complete Example

Here's a complete example combining multiple Datastar attributes:

```fsharp
div {
    _dataSignals ("count", "0")
    _dataSignals ("name", "'World'")
    _dataComputed ("greeting", "'Hello, ' + $name + '!'")

    input { _type "text"; _dataBind "name" }
    span { _dataText "$greeting" }

    button {
        _dataOn ("click", "$count++")
        _dataClass ("active", "$count > 0")
        "Clicked "
    }
    span { _dataText "$count" }
    span { _dataShow "$count > 0"; " times" }
}
```
