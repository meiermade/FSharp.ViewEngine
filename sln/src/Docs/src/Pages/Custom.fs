namespace Docs.Pages

open Docs.Common

module Custom =
    let page =
        { id = "custom"
          path = "/custom"
          aliases = [  ]
          navLabel = "Custom elements and extensions"
          category = "Getting started"
          title = "Custom Elements & Attributes"
          browserTitle = "Custom Elements & Attributes - FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "FSharp.ViewEngine covers all standard HTML elements and attributes, but you may need custom ones for web components or non-standard attributes." ];
            Heading { id = "trusted-content-boundaries"; title = "Trusted Content Boundaries"; level = 2 };
            Paragraph [ Text "Text and attribute values are HTML-encoded by default. The following APIs intentionally cross that safety boundary and must only receive trusted, developer-controlled content:" ];
            UnorderedList [
                [ InlineContent.Code "Html.raw"; Text " and "; InlineContent.Code "Html.js"; Text " emit their values without encoding." ];
                [ Text "Inline event-handler helpers such as "; InlineContent.Code "_onclick"; Text " contain executable JavaScript. HTML encoding preserves valid markup but does not make untrusted JavaScript safe." ];
                [ InlineContent.Code "Html.el"; Text ", "; InlineContent.Code "Html.elVoid"; Text ", and the name passed to "; InlineContent.Code "_attr"; Text " are emitted as markup names without validation." ]
            ];
            Paragraph [ Text "Keep user-controlled data in normal text nodes and attribute "; Strong [ Text "values" ]; Text ", where it will be encoded. Do not use user input as raw markup, JavaScript, element names, or attribute names." ];
            Heading { id = "custom-elements"; title = "Custom Elements"; level = 2 };
            Heading { id = "el"; title = "el"; level = 3 };
            Paragraph [ Text "Use "; InlineContent.Code "Html.el"; Text " to create a custom element with children. This is useful for web components:" ];
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html

el "my-component" {
    _class "container"
    p { "Hello from a web component!" }
}""");
            Paragraph [ Text "Renders:" ];
            CodeBlock("html", """<my-component class="container">
    <p>Hello from a web component!</p>
</my-component>""");
            Heading { id = "elvoid"; title = "elVoid"; level = 3 };
            Paragraph [ Text "Use "; InlineContent.Code "Html.elVoid"; Text " to create a custom self-closing (void) element:" ];
            CodeBlock("fsharp", """elVoid "my-icon" {
    _attr("name", "star")
    _attr("size", "24")
}""");
            Paragraph [ Text "Renders:" ];
            CodeBlock("html", """<my-icon name="star" size="24">""");
            Heading { id = "nested-web-components"; title = "Nested Web Components"; level = 3 };
            Paragraph [ Text "Custom elements can be nested just like regular elements:" ];
            CodeBlock("fsharp", """el "my-card" {
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
}""");
            Heading { id = "custom-attributes"; title = "Custom Attributes"; level = 2 };
            Heading { id = "attr"; title = "_attr"; level = 3 };
            Paragraph [ Text "Use "; InlineContent.Code "Html._attr"; Text " to add any attribute not covered by the built-in helpers." ];
            Heading { id = "key-value-attribute"; title = "Key-value attribute"; level = 4 };
            CodeBlock("fsharp", """div {
    _attr("my-custom-attr", "value")
    "Content"
}""");
            Paragraph [ Text "Renders:" ];
            CodeBlock("html", """<div my-custom-attr="value">Content</div>""");
            Heading { id = "boolean-attribute"; title = "Boolean attribute"; level = 4 };
            Paragraph [ Text "Pass only the name to render a valueless (boolean) attribute:" ];
            CodeBlock("fsharp", """div {
    _attr "my-flag"
    "Content"
}""");
            Paragraph [ Text "Renders:" ];
            CodeBlock("html", """<div my-flag>Content</div>""");
            Heading { id = "combining-with-built-in-attributes"; title = "Combining with Built-in Attributes"; level = 3 };
            Paragraph [ Text "Custom attributes work alongside all built-in attributes:" ];
            CodeBlock("fsharp", """el "sl-button" {
    _attr("variant", "primary")
    _attr("size", "large")
    _attr "pill"
    _onclick "handleClick()"
    _class "my-button"
    "Click Me"
}""");
            Paragraph [ Text "Renders:" ];
            CodeBlock("html", """<sl-button variant="primary" size="large" pill onclick="handleClick()" class="my-button">
    Click Me
</sl-button>""");
            Heading { id = "extending-the-html-type"; title = "Extending the Html Type"; level = 2 };
            Paragraph [ Text "F# supports "; Link("type extensions", "https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/type-extensions"); Text " which let you add your own elements and attributes to the "; InlineContent.Code "Html"; Text " type. This is useful for project-specific conventions or design system components." ];
            Heading { id = "adding-custom-elements"; title = "Adding Custom Elements"; level = 3 };
            CodeBlock("fsharp", """open FSharp.ViewEngine

type Html with
    static member val myCard = TagBuilder("my-card") with get
    static member val myIcon = VoidBuilder("my-icon") with get""");
            Paragraph [ Text "Then use them just like built-in elements:" ];
            CodeBlock("fsharp", """open type Html

myCard {
    _class "shadow-lg"
    h2 { "Title" }
    p { "Card content" }
}

myIcon { _attr("name", "star") }""");
            Heading { id = "adding-custom-attributes"; title = "Adding Custom Attributes"; level = 3 };
            CodeBlock("fsharp", """open FSharp.ViewEngine

type Html with
    static member inline _theme (v: string) = { Name = "data-theme"; Value = ValueSome v }
    static member inline _variant (v: string) = { Name = "variant"; Value = ValueSome v }
    static member inline _loading = { Name = "data-loading"; Value = ValueNone }""");
            Paragraph [ Text "Then use them alongside built-in attributes:" ];
            CodeBlock("fsharp", """open type Html

div {
    _theme "dark"
    _variant "outlined"
    _loading
    "Content"
}""");
            Heading { id = "design-system-example"; title = "Design System Example"; level = 3 };
            Paragraph [ Text "You can build a full design system module with reusable elements and attributes:" ];
            CodeBlock("fsharp", """open FSharp.ViewEngine

type Ds =
    static member val alert = TagBuilder("ds-alert") with get
    static member val badge = TagBuilder("ds-badge") with get
    static member val tooltip = TagBuilder("ds-tooltip") with get
    static member inline _severity (v: string) = { Name = "severity"; Value = ValueSome v }
    static member inline _placement (v: string) = { Name = "placement"; Value = ValueSome v }
    static member inline _dismissible = { Name = "dismissible"; Value = ValueNone }""");
            CodeBlock("fsharp", """open type Html
open type Ds

alert {
    _severity "warning"
    _dismissible
    "This is a warning message."
}

tooltip {
    _placement "top"
    button { "Hover me" }
}""");
            Heading { id = "shoelace-example"; title = "Shoelace Example"; level = 2 };
            Paragraph [ Text "Here's a more complete example using "; Link("Shoelace", "https://shoelace.style/"); Text " web components:" ];
            CodeBlock("fsharp", """el "sl-dialog" {
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
}""");
          ] }
