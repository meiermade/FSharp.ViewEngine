module HtmxTests

open FSharp.ViewEngine
open Expecto
open type Html
open type Htmx

let private renderAttribute (attribute: HtmlAttribute) =
    div { yield attribute }
    |> Render.toString

[<Tests>]
let tests =
    testList "Htmx Tests" [
        test "HTMX 2.0.9 valued attributes render correctly" {
            let attributes =
                [ "hx-boost", _hxBoost "value"
                  "hx-confirm", _hxConfirm "value"
                  "hx-delete", _hxDelete "value"
                  "hx-disabled-elt", _hxDisabledElt "value"
                  "hx-disinherit", _hxDisinherit "value"
                  "hx-encoding", _hxEncoding "value"
                  "hx-ext", _hxExt "value"
                  "hx-get", _hxGet "value"
                  "hx-headers", _hxHeaders "value"
                  "hx-history", _hxHistory "value"
                  "hx-include", _hxInclude "value"
                  "hx-indicator", _hxIndicator "value"
                  "hx-inherit", _hxInherit "value"
                  "hx-on:htmx:before-request", _hxOn ("htmx:before-request", "value")
                  "hx-params", _hxParams "value"
                  "hx-patch", _hxPatch "value"
                  "hx-post", _hxPost "value"
                  "hx-prompt", _hxPrompt "value"
                  "hx-push-url", _hxPushUrl "value"
                  "hx-put", _hxPut "value"
                  "hx-replace-url", _hxReplaceUrl "value"
                  "hx-request", _hxRequest "value"
                  "hx-select", _hxSelect "value"
                  "hx-select-oob", _hxSelectOOB "value"
                  "hx-swap", _hxSwap "value"
                  "hx-swap-oob", _hxSwapOOB "value"
                  "hx-sync", _hxSync "value"
                  "hx-target", _hxTarget "value"
                  "hx-trigger", _hxTrigger "value"
                  "hx-validate", _hxValidate "value"
                  "hx-vals", _hxVals "value" ]

            for name, attribute in attributes do
                let actual = renderAttribute attribute
                Expect.equal actual $"<div {name}=\"value\"></div>" name
        }

        test "HTMX 2.0.9 presence-only attributes render without values" {
            let attributes =
                [ "hx-disable", _hxDisable
                  "hx-history-elt", _hxHistoryElt
                  "hx-preserve", _hxPreserve ]

            for name, attribute in attributes do
                let actual = renderAttribute attribute
                Expect.equal actual $"<div {name}></div>" name
        }

        test "Generic HTMX attributes remain available and values are encoded" {
            let actual =
                div { _hx ("custom", "<value & more>") }
                |> Render.toString

            Expect.equal actual "<div hx-custom=\"&lt;value &amp; more&gt;\"></div>" "generic HTMX attribute"
        }
    ]
