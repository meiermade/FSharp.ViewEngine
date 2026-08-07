namespace Docs.Pages

open Docs.Common

module Htmx =
    let private section id title =
        [ Heading { id = id; title = title; level = 2 } ]

    let private attribute id description source =
        [ Heading { id = id; title = id; level = 3 }
          Paragraph [ Text description ]
          CodeBlock("fsharp", source) ]

    let private nodes =
        [ [ Paragraph [ Text "FSharp.ViewEngine provides all non-deprecated HTMX 2.0.9 attributes through the "; InlineContent.Code "Htmx"; Text " type. See the "; Link("official HTMX 2 reference", "https://v2-0v2-0.htmx.org/reference/"); Text " for the complete value grammar and inheritance rules." ] ]
          section "setup" "Setup"
          [ Paragraph [ Text "Open the "; InlineContent.Code "Htmx"; Text " type to access HTMX attributes:" ]
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type Htmx""") ]

          section "request-methods" "Request Methods"
          attribute "hx-get" "Issue a GET request to a URL with _hxGet." """button {
    _hxGet "/api/items"
    "Load items"
}"""
          attribute "hx-post" "Issue a POST request to a URL with _hxPost." """form {
    _hxPost "/api/items"
    input { _name "title" }
    button { "Create item" }
}"""
          attribute "hx-put" "Issue a PUT request to a URL with _hxPut." """button {
    _hxPut "/api/items/1"
    "Replace item"
}"""
          attribute "hx-patch" "Issue a PATCH request to a URL with _hxPatch." """button {
    _hxPatch "/api/items/1"
    "Update item"
}"""
          attribute "hx-delete" "Issue a DELETE request to a URL with _hxDelete." """button {
    _hxDelete "/api/items/1"
    "Delete item"
}"""

          section "request-data-and-configuration" "Request Data and Configuration"
          attribute "hx-encoding" "Change the request encoding with _hxEncoding, typically for file uploads." """form {
    _hxPost "/api/upload"
    _hxEncoding "multipart/form-data"
}"""
          attribute "hx-headers" "Add request headers as JSON with _hxHeaders." "button {\n    _hxPost \"/api/items\"\n    _hxHeaders \"\"\"{\"X-CSRF-Token\": \"token\"}\"\"\"\n    \"Create item\"\n}"
          attribute "hx-include" "Include values from additional elements with _hxInclude." """button {
    _hxPost "/api/search"
    _hxInclude "[name='query']"
    "Search"
}"""
          attribute "hx-params" "Filter submitted parameters with _hxParams. Values include *, none, a comma-separated list, or not followed by a list." """form {
    _hxPost "/api/profile"
    _hxParams "name,email"
}"""
          attribute "hx-request" "Configure request timeout, credentials, or headers with _hxRequest." "button {\n    _hxGet \"/api/report\"\n    _hxRequest \"\"\"{\"timeout\": 5000}\"\"\"\n    \"Load report\"\n}"
          attribute "hx-vals" "Add values to the request as JSON with _hxVals." "button {\n    _hxPost \"/api/action\"\n    _hxVals \"\"\"{\"source\": \"toolbar\"}\"\"\"\n    \"Run action\"\n}"

          section "targeting-and-swapping" "Targeting and Swapping"
          attribute "hx-target" "Choose the element that receives the response with _hxTarget." """button {
    _hxGet "/api/items"
    _hxTarget "#results"
    "Load items"
}
div { _id "results" }"""
          attribute "hx-select" "Select a fragment from the response with _hxSelect." """button {
    _hxGet "/items/1"
    _hxSelect "#item-details"
    "Load details"
}"""
          attribute "hx-select-oob" "Select one or more response fragments for out-of-band swaps with _hxSelectOOB." """button {
    _hxGet "/dashboard"
    _hxSelectOOB "#alerts,#navigation:outerHTML"
    "Refresh dashboard"
}"""
          attribute "hx-swap" "Control how the response is swapped with _hxSwap. Swap modifiers can be included in the same value." """button {
    _hxGet "/api/items"
    _hxSwap "beforeend settle:200ms"
    "Append items"
}"""
          attribute "hx-swap-oob" "Mark response content for an out-of-band swap with _hxSwapOOB." """div {
    _id "notifications"
    _hxSwapOOB "true"
    "Updated notifications"
}"""
          attribute "hx-preserve" "Preserve an element by id across ancestor updates with the presence-only _hxPreserve attribute." """video {
    _id "tutorial"
    _hxPreserve
}"""

          section "requests-in-flight" "Requests in Flight"
          attribute "hx-indicator" "Choose the element that receives the htmx-request class with _hxIndicator." """button {
    _hxGet "/api/report"
    _hxIndicator "#spinner"
    "Load report"
}
span { _id "spinner"; _class "htmx-indicator"; "Loading..." }"""
          attribute "hx-disabled-elt" "Disable selected elements while a request is in flight with _hxDisabledElt." """button {
    _hxPost "/api/items"
    _hxDisabledElt "this"
    "Create item"
}"""
          attribute "hx-sync" "Coordinate requests with _hxSync using a selector and synchronization strategy." """input {
    _name "query"
    _hxGet "/api/search"
    _hxTrigger "input changed delay:300ms"
    _hxSync "this:replace"
}"""
          attribute "hx-validate" "Force an element to run HTML validation before a request with _hxValidate." """input {
    _type "email"
    _hxPost "/api/validate-email"
    _hxValidate "true"
}"""

          section "triggering-and-interaction" "Triggering and Interaction"
          attribute "hx-trigger" "Specify the events and modifiers that trigger a request with _hxTrigger." """input {
    _name "query"
    _hxGet "/api/search"
    _hxTrigger "keyup changed delay:500ms"
}"""
          attribute "hx-confirm" "Ask for confirmation before issuing a request with _hxConfirm." """button {
    _hxDelete "/account"
    _hxConfirm "Delete your account?"
    "Delete account"
}"""
          attribute "hx-prompt" "Prompt for a value before issuing a request with _hxPrompt. HTMX sends the result in the HX-Prompt header." """button {
    _hxDelete "/account"
    _hxPrompt "Enter your account name to confirm"
    "Delete account"
}"""
          attribute "hx-on" "Handle DOM or HTMX events with _hxOn. Attribute names are case-insensitive, so use kebab-case HTMX event names rather than camelCase." """form {
    _hxPost "/api/items"
    _hxOn ("htmx:before-request", "showSpinner()")
    _hxOn ("htmx:after-request", "hideSpinner()")
}"""
          [ Paragraph [ Strong [ Text "Security:" ]; Text " "; InlineContent.Code "_hxOn"; Text " executes inline JavaScript. Only use trusted script content and follow your application's Content Security Policy." ] ]

          section "navigation-and-history" "Navigation and History"
          attribute "hx-boost" "Progressively enhance links and forms with _hxBoost." """main {
    _hxBoost "true"
    a { _href "/account"; "Account" }
}"""
          attribute "hx-push-url" "Push the fetched URL, a custom URL, or no URL into browser history with _hxPushUrl." """button {
    _hxGet "/account"
    _hxPushUrl "true"
    "Open account"
}"""
          attribute "hx-replace-url" "Replace the current browser-history URL with _hxReplaceUrl." """button {
    _hxGet "/account"
    _hxReplaceUrl "/account/home"
    "Open account"
}"""
          attribute "hx-history" "Prevent sensitive page state from entering the HTMX history cache with _hxHistory \"false\"." """section {
    _hxHistory "false"
    "Sensitive account details"
}"""
          attribute "hx-history-elt" "Choose a narrower history snapshot element with the presence-only _hxHistoryElt attribute." """main {
    _id "content"
    _hxHistoryElt
}"""

          section "inheritance-and-processing" "Inheritance and Processing"
          attribute "hx-disable" "Disable HTMX processing for an element and its descendants with the presence-only _hxDisable attribute." """section {
    _hxDisable
    "HTMX ignores this subtree"
}"""
          attribute "hx-disinherit" "Disable inheritance for selected attributes, or all attributes with *, using _hxDisinherit." """section {
    _hxDisinherit "hx-target hx-swap"
}"""
          attribute "hx-inherit" "Explicitly enable inheritance when HTMX's disableInheritance configuration is active with _hxInherit." """section {
    _hxTarget "#content"
    _hxInherit "hx-target"
}"""
          attribute "hx-ext" "Enable one or more HTMX extensions for an element and its descendants with _hxExt." """body {
    _hxExt "preload,morph"
}"""

          section "generic-and-deprecated-attributes" "Generic and Deprecated Attributes"
          [ Paragraph [ Text "Use "; InlineContent.Code "_hx"; Text " for extension attributes or newer HTMX attributes that do not yet have a dedicated helper:" ];
            CodeBlock("fsharp", """div {
    _hx ("custom-extension-option", "value")
}""");
            Paragraph [ InlineContent.Code "hx-vars"; Text " is deprecated in HTMX 2; use "; InlineContent.Code "_hxVals"; Text ". The former "; InlineContent.Code "hx-sse"; Text " and "; InlineContent.Code "hx-ws"; Text " core attributes moved to extensions and therefore do not have dedicated core helpers." ] ]

          section "complete-example" "Complete Example"
          [ Paragraph [ Text "A search form combining request, synchronization, targeting, indicator, and history attributes:" ]
            CodeBlock("fsharp", """form {
    _hxGet "/api/search"
    _hxTrigger "input changed delay:300ms, search"
    _hxTarget "#search-results"
    _hxIndicator "#search-spinner"
    _hxDisabledElt "find button"
    _hxSync "this:replace"
    _hxPushUrl "true"

    input { _type "search"; _name "query"; _hxValidate "true" }
    button { _type "submit"; "Search" }
    span { _id "search-spinner"; _class "htmx-indicator"; "Searching..." }
    div { _id "search-results" }
}""") ] ]
        |> List.concat

    let page =
        { id = "htmx"
          path = "/extensions/htmx"
          aliases = []
          navLabel = "HTMX"
          category = "Extensions"
          title = "HTMX"
          browserTitle = "HTMX - FSharp.ViewEngine"
          nodes = nodes }
