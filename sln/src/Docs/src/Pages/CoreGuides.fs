namespace Docs.Pages

open Docs.Common

module CoreGuides =
    let firstView =
        { id = "first-view"
          path = "/getting-started/first-view"
          aliases = []
          navLabel = "Build your first view"
          category = "Getting started"
          title = "Build your first view"
          browserTitle = "Build your first view · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Create a typed HTML view, render it as a fragment or complete document, and reuse ordinary F# values as components." ];
            Heading { id = "create-the-view"; title = "Create the view"; level = 2 }
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html

let greeting name =
    main {
        _class "content"
        h1 { $"Hello, {name}!" }
        p { "Built with typed F# computation expressions." }
    }""");
            Heading { id = "render-the-view"; title = "Render the view"; level = 2 };
            Paragraph [ Text "Use "; InlineContent.Code "Render.toString"; Text " for a fragment. Use "; InlineContent.Code "Render.toHtmlDocString"; Text " when the root is a complete "; InlineContent.Code "html"; Text " element and the response needs a doctype." ];
            CodeBlock("fsharp", """let fragment = greeting "Ada" |> Render.toString

let document =
    html {
        _lang "en"
        head { title { "Greeting" } }
        body { greeting "Ada" }
    }
    |> Render.toHtmlDocString""");
            Heading { id = "reuse-components"; title = "Reuse components"; level = 2 };
            Paragraph [ Text "A component is an ordinary function that returns an "; InlineContent.Code "HtmlElement"; Text ". Compose it directly inside another builder without a template runtime or component base class." ];
            CodeBlock("fsharp", """let badge label = span { _class "badge"; label }

div {
    badge "Typed"
    badge "Composable"
}""");
            Heading { id = "next"; title = "Next"; level = 2 };
            Paragraph [ Text "Continue with "; Link("Elements and attributes", "/guides/elements-and-attributes"); Text " to learn the core DSL." ] ] }

    let elementsAndAttributes =
        { id = "elements-and-attributes"
          path = "/guides/elements-and-attributes"
          aliases = []
          navLabel = "Elements and attributes"
          category = "Core concepts"
          title = "Elements and attributes"
          browserTitle = "Elements and attributes · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Build standard or custom markup with typed element builders, underscore-prefixed attributes, encoded text, and mixed attributes and children." ];
            Heading { id = "elements"; title = "Elements"; level = 2 }
            Paragraph [ Text "Open the static "; InlineContent.Code "Html"; Text " type to use standard HTML builders without qualification. Regular elements accept attributes and children; void elements accept attributes only." ];
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html

article {
    h2 { "Account" }
    p { "Manage your profile." }
    input { _type "email"; _name "email" }
}""");
            Heading { id = "attributes"; title = "Attributes"; level = 2 };
            Paragraph [ Text "Attribute helpers begin with an underscore, so they remain visually distinct from child elements. Boolean attributes render without a value." ];
            CodeBlock("fsharp", """button {
    _type "button"
    _class [ "button"; "button-primary" ]
    _disabled
    "Save"
}""");
            Heading { id = "mixed-content"; title = "Mixed content"; level = 2 };
            Paragraph [ Text "Attributes, strings, and child elements can be yielded in one computation expression. Strings become encoded text nodes." ];
            CodeBlock("fsharp", """a {
    "Read "
    strong { "the guide" }
    _href "/guide"
    _class "guide-link"
}""");
            Heading { id = "escape-hatches"; title = "Generic escape hatches"; level = 2 };
            Paragraph [ Text "Use "; InlineContent.Code "Html.el"; Text ", "; InlineContent.Code "Html.elVoid"; Text ", and "; InlineContent.Code "_attr"; Text " for custom elements and attributes. Names are developer-controlled markup and are not validated." ] ] }

    let composition =
        { id = "composition-control-flow"
          path = "/guides/composition-and-control-flow"
          aliases = []
          navLabel = "Composition and control flow"
          category = "Core concepts"
          title = "Composition and control flow"
          browserTitle = "Composition and control flow · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Compose views with ordinary functions and use F# conditionals and loops directly inside element computation expressions." ];
            Heading { id = "functions-as-components"; title = "Functions as components"; level = 2 }
            CodeBlock("fsharp", """let navigationItem current href label =
    li {
        a {
            _href href
            if current then _ariaCurrent "page"
            label
        }
    }""");
            Heading { id = "conditional-content"; title = "Conditional content"; level = 2 };
            CodeBlock("fsharp", """div {
    if validationErrors.IsEmpty then
        p { _class "success"; "Ready to submit." }
    else
        ul {
            for error in validationErrors do
                li { error }
        }
}""");
            Heading { id = "sequences"; title = "Sequences"; level = 2 };
            Paragraph [ Text "Element builders accept child collections directly, through "; InlineContent.Code "yield!"; Text ", or with an explicit "; InlineContent.Code "for"; Text " expression. Collection inputs are enumerated when the element tree is built, preserving source order and deterministic repeated rendering." ];
            CodeBlock("fsharp", """let rows = items |> List.map (fun item ->
    li { _data("item-id", item.id); item.label })

ul {
    rows                 // Direct collection
    yield! moreRows      // Explicit flattening
    for item in finalItems do
        li { item.label }
}""") ] }

    let rendering =
        { id = "rendering"
          path = "/guides/rendering"
          aliases = []
          navLabel = "Rendering"
          category = "Core concepts"
          title = "Rendering"
          browserTitle = "Rendering · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Choose fragment or document rendering deliberately and render each completed view at the application boundary." ];
            Heading { id = "fragments"; title = "Fragments"; level = 2 }
            Paragraph [ InlineContent.Code "fragment { ... }"; Text " composes sibling nodes without adding a wrapper. "; InlineContent.Code "Render.toString"; Text " serializes exactly those siblings and does not prepend a doctype." ];
            CodeBlock("fsharp", """let nodes = [ span { "One" }; span { "Two" } ]

let rendered =
    fragment {
        strong { "Items: " }
        yield! nodes
    }
    |> Render.toString
// <strong>Items: </strong><span>One</span><span>Two</span>""");
            Paragraph [ Text "Migration: replace "; InlineContent.Code "Html.fragment nodes"; Text " with "; InlineContent.Code "fragment { yield! nodes }"; Text ". Replace "; InlineContent.Code "title \"Account\""; Text " or "; InlineContent.Code "titleBuilder { ... }"; Text " with "; InlineContent.Code "title { \"Account\" }"; Text "." ];
            Heading { id = "documents"; title = "Documents"; level = 2 };
            Paragraph [ InlineContent.Code "Render.toHtmlDocString"; Text " prepends "; InlineContent.Code "<!DOCTYPE html>"; Text " and is intended for a complete HTML document." ];
            CodeBlock("fsharp", """let responseBody =
    html {
        _lang "en"
        head { title { "Account" } }
        body { main { h1 { "Account" } } }
    }
    |> Render.toHtmlDocString""");
            Heading { id = "application-boundaries"; title = "Application boundaries"; level = 2 };
            Paragraph [ Text "FSharp.ViewEngine has no web-framework dependency. Pass the resulting string to your framework's normal HTML response API. See the "; Link("Giraffe integration", "/usage"); Text " for a complete server example." ] ] }

    let encoding =
        { id = "encoding"
          path = "/guides/encoding-and-trusted-content"
          aliases = []
          navLabel = "Encoding and trusted content"
          category = "Core concepts"
          title = "Encoding and trusted content"
          browserTitle = "Encoding and trusted content · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Text and attribute values are HTML-encoded by default. Cross the raw markup and executable-expression boundaries only with trusted application-controlled values." ];
            Heading { id = "encoded-by-default"; title = "Encoded by default"; level = 2 }
            CodeBlock("fsharp", """div {
    _title "5 < 10"
    "<strong>User text</strong>"
}
// <div title="5 &lt; 10">&lt;strong&gt;User text&lt;/strong&gt;</div>""");
            Heading { id = "trusted-raw-content"; title = "Trusted raw content"; level = 2 };
            Paragraph [ InlineContent.Code "Html.raw"; Text " and "; InlineContent.Code "Html.js"; Text " bypass encoding. They do not sanitize HTML or JavaScript and must not receive user-controlled content." ];
            CodeBlock("fsharp", """script { Html.js "window.app.start()" }
div { Html.raw trustedSvgMarkup }""");
            Heading { id = "trusted-names-and-expressions"; title = "Trusted names and expressions"; level = 2 };
            Paragraph [ Text "Custom element names, custom attribute names, inline event handlers, and Alpine/Datastar expressions are executable or structural code. Encoding their values preserves HTML syntax but does not make untrusted code safe." ] ] }

    let accessibility =
        { id = "accessibility"
          path = "/guides/accessibility"
          aliases = []
          navLabel = "Accessibility"
          category = "Core concepts"
          title = "Accessibility"
          browserTitle = "Accessibility · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "FSharp.ViewEngine provides semantic HTML and WAI-ARIA helpers, while the application remains responsible for correct roles, names, states, focus behavior, contrast, and keyboard interaction." ];
            Heading { id = "prefer-native-html"; title = "Prefer native HTML"; level = 2 }
            Paragraph [ Text "Start with the native element whose behavior matches the interaction. A "; InlineContent.Code "button"; Text " already supplies keyboard activation semantics that a clickable "; InlineContent.Code "div"; Text " does not." ];
            CodeBlock("fsharp", """button {
    _type "button"
    _ariaExpanded isOpen
    _ariaControls "account-menu"
    "Account"
}""");
            Heading { id = "names-and-relationships"; title = "Names and relationships"; level = 2 };
            Paragraph [ Text "Use visible labels where possible and connect controls, descriptions, errors, tabs, and panels with stable unique IDs." ];
            CodeBlock("fsharp", """label { _for "email"; "Email" }
input {
    _id "email"
    _type "email"
    _ariaDescribedBy "email-help"
}
p { _id "email-help"; "Used for account notices." }""");
            Heading { id = "aria-coverage"; title = "ARIA coverage"; level = 2 };
            Paragraph [ Text "Dedicated helpers cover WAI-ARIA 1.2 attributes. Use the generic "; InlineContent.Code "_aria"; Text " helper only for future or extension attributes not represented by the pinned inventory." ];
            Heading { id = "test-behavior"; title = "Test behavior"; level = 2 };
            Paragraph [ Text "Rendering valid attributes is not enough. Test focus movement, keyboard operation, expanded/selected state, labels, landmarks, responsive behavior, and light/dark contrast in the running interface." ] ] }

    let all = [ firstView; elementsAndAttributes; composition; rendering; encoding; accessibility ]
