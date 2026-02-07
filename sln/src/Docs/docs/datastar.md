# Datastar

FSharp.ViewEngine provides type-safe Datastar attributes through the `Datastar` type.

## Setup

Open the `Datastar` type to access Datastar attributes:

```fsharp
open FSharp.ViewEngine
open type Html
open type Datastar
```

## Generic Attribute

### data-*

Use `_ds` for any Datastar `data-*` attribute:

```fsharp
div {
    _ds ("star", "true")
    _ds "loading"
}
```

## Core Attributes

### data-signals

Define reactive signals on an element:

```fsharp
div {
    _dsSignals ("count", "0")
    _dsSignals ("name", "'World'")
}
```

### data-on

Listen for events and run expressions:

```fsharp
button {
    _dsOn ("click", "$count++")
    "Increment"
}
```

### data-bind

Two-way bind a signal to an input element:

```fsharp
input { _type "text"; _dsBind "name" }
input { _type "text"; _dsBind ("name", "value") }
```

### data-show

Conditionally show or hide an element:

```fsharp
div { _dsShow "$count > 0"; "Count is positive" }
```

### data-text

Set the text content of an element reactively:

```fsharp
span { _dsText "$count" }
```

### data-effect

Run an expression whenever its dependencies change:

```fsharp
div { _dsEffect "console.log($count)" }
```

### data-init

Run an expression when the element is initialized:

```fsharp
div { _dsInit "console.log('initialized')" }
```

### data-attr

Dynamically set an HTML attribute:

```fsharp
div { _dsAttr ("disabled", "$count === 0") }
```

### data-class

Toggle a CSS class based on an expression:

```fsharp
div { _dsClass ("active", "$isActive") }
```

### data-computed

Define a computed signal derived from other signals:

```fsharp
div { _dsComputed ("double", "$count * 2") }
```

### data-style

Dynamically set a CSS style property:

```fsharp
div { _dsStyle ("color", "$isError ? 'red' : 'green'") }
```

### data-ref

Reference an element by name:

```fsharp
input { _dsRef "myInput" }
input { _dsRef ("myInput", "value") }
```

### data-indicator

Bind a loading indicator signal:

```fsharp
button { _dsIndicator "loading" }
button { _dsIndicator ("loading", "true") }
```

### data-json-signals

Merge JSON signals into the signal store:

```fsharp
div { _dsJsonSignals """{"count": 0}""" }
div { _dsJsonSignals () }
```

### data-ignore

Prevent Datastar from processing an element:

```fsharp
div { _dsIgnore }
```

### data-ignore-morph

Prevent morphing of an element during updates:

```fsharp
div { _dsIgnoreMorph }
```

### data-on-intersect

Run an expression when an element enters the viewport:

```fsharp
div { _dsOnIntersect "$count++" }
```

### data-on-interval

Run an expression on a timed interval:

```fsharp
div { _dsOnInterval "$count++" }
```

### data-on-signal-patch

Run an expression when signals are patched:

```fsharp
div { _dsOnSignalPatch "console.log('patched')" }
```

### data-on-signal-patch-filter

Filter which signal patches trigger the expression:

```fsharp
div { _dsOnSignalPatchFilter "count" }
```

### data-preserve-attr

Preserve specified attributes during morphing:

```fsharp
div { _dsPreserveAttr "class" }
```

## Pro Attributes

### data-animate

Apply animations to an element:

```fsharp
div { _dsAnimate "fadeIn 0.5s" }
```

### data-custom-validity

Set custom validation messages:

```fsharp
input { _dsCustomValidity "$name === '' ? 'Name is required' : ''" }
```

### data-on-raf

Run an expression on every animation frame:

```fsharp
canvas { _dsOnRaf "draw()" }
```

### data-on-resize

Run an expression when the element is resized:

```fsharp
div { _dsOnResize "console.log('resized')" }
```

### data-persist

Persist signals to local storage:

```fsharp
div { _dsPersist "count" }
div { _dsPersist ("count", "session") }
```

### data-query-string

Sync signals with URL query parameters:

```fsharp
div { _dsQueryString "count" }
div { _dsQueryString () }
```

### data-replace-url

Replace the current URL:

```fsharp
div { _dsReplaceUrl "/new-path" }
```

### data-rocket

Prefetch pages for instant navigation:

```fsharp
a { _dsRocket "true"; _href "/next-page"; "Next" }
```

### data-scroll-into-view

Scroll the element into view:

```fsharp
div { _dsScrollIntoView }
```

### data-view-transition

Apply view transitions:

```fsharp
div { _dsViewTransition "fade" }
```

## Complete Example

Here's a complete example combining multiple Datastar attributes:

```fsharp
div {
    _dsSignals ("count", "0")
    _dsSignals ("name", "'World'")
    _dsComputed ("greeting", "'Hello, ' + $name + '!'")

    input { _type "text"; _dsBind "name" }
    span { _dsText "$greeting" }

    button {
        _dsOn ("click", "$count++")
        _dsClass ("active", "$count > 0")
        "Clicked "
    }
    span { _dsText "$count" }
    span { _dsShow "$count > 0"; " times" }
}
```
