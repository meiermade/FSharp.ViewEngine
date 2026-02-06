# HTMX

FSharp.ViewEngine provides type-safe HTMX attributes through the `Htmx` type.

## Setup

Open the `Htmx` type to access HTMX attributes:

```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx
```

## Request Attributes

### hx-get

Issue a GET request to a URL:

```fsharp
button {
    _hxGet "/api/data"
    "Load Data"
}
```

### hx-post

Issue a POST request to a URL:

```fsharp
form {
    _hxPost "/api/submit"
    input { _type "text"; _name "email" }
    button { "Submit" }
}
```

### hx-delete

Issue a DELETE request to a URL:

```fsharp
button {
    _hxDelete "/api/items/1"
    "Delete"
}
```

### Generic hx attribute

Use `_hx` for any HTMX attribute not covered by a dedicated helper:

```fsharp
div {
    _hx ("put", "/api/items/1")
    _hx ("patch", "/api/items/1")
}
```

## Targeting and Swapping

### hx-target

Specify the target element for the response:

```fsharp
button {
    _hxGet "/api/data"
    _hxTarget "#result"
    "Load into #result"
}
div { _id "result" }
```

### hx-swap

Control how the response content is swapped in:

```fsharp
button {
    _hxGet "/api/items"
    _hxSwap "beforeend"
    "Append Items"
}
```

Common swap values: `innerHTML`, `outerHTML`, `beforebegin`, `afterbegin`, `beforeend`, `afterend`, `delete`, `none`.

### hx-swap-oob

Swap content out-of-band (outside the target):

```fsharp
div {
    _hxSwapOOB "true"
    _id "notifications"
    "Updated notification content"
}
```

## Triggering and Events

### hx-trigger

Specify the event that triggers the request:

```fsharp
input {
    _type "text"
    _hxGet "/api/search"
    _hxTrigger "keyup changed delay:500ms"
    _hxTarget "#results"
}
```

### hx-on

Listen for HTMX events:

```fsharp
form {
    _hxPost "/api/submit"
    _hxOn ("htmx:beforeRequest", "showSpinner()")
    _hxOn ("htmx:afterRequest", "hideSpinner()")
}
```

## Other Attributes

### hx-indicator

Show a loading indicator during requests:

```fsharp
button {
    _hxGet "/api/slow"
    _hxIndicator "#spinner"
    "Load"
}
div { _id "spinner"; _class "htmx-indicator"; "Loading..." }
```

### hx-include

Include additional element values in the request:

```fsharp
button {
    _hxPost "/api/submit"
    _hxInclude "[name='email']"
    "Submit"
}
```

### hx-encoding

Set the encoding type for requests:

```fsharp
form {
    _hxPost "/api/upload"
    _hxEncoding "multipart/form-data"
}
```

### hx-vals

Add additional values to the request:

```fsharp
button {
    _hxPost "/api/action"
    _hxVals """{"key": "value"}"""
    "Submit"
}
```

### hx-history

Control the history behavior:

```fsharp
div { _hxHistory "false" }
```

## Complete Example

Here's a complete example combining multiple HTMX attributes:

```fsharp
div {
    _class "search-container"
    input {
        _type "text"
        _name "q"
        _hxGet "/api/search"
        _hxTrigger "keyup changed delay:300ms"
        _hxTarget "#search-results"
        _hxIndicator "#search-spinner"
    }
    span { _id "search-spinner"; _class "htmx-indicator"; "Searching..." }
    div { _id "search-results" }
}
```
