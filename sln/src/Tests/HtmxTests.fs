module HtmxTests

open FSharp.ViewEngine
open Expecto
open type Html
open type Htmx

[<Tests>]
let tests =
  testList "Htmx Tests" [
    test "Htmx attributes render correctly" {
        let actual =
            div {
                _hxGet "/get"
                _hxPost "/post"
                _hxDelete "/delete"
                _hxTrigger "click"
                _hxTarget "#target"
                _hxIndicator "#spinner"
                _hxInclude "[name='q']"
                _hxSwap "innerHTML"
                _hxSwapOOB "true"
                _hxEncoding "multipart/form-data"
                _hxOn ("click", "alert('hi')")
                _hxHistory "false"
                _hxVals """{"key": "val"}"""
                _hx ("custom", "value")
                "Content"
            } |> Render.toString
        Expect.stringContains actual "hx-get=\"/get\"" "hx-get"
        Expect.stringContains actual "hx-post=\"/post\"" "hx-post"
        Expect.stringContains actual "hx-delete=\"/delete\"" "hx-delete"
        Expect.stringContains actual "hx-trigger=\"click\"" "hx-trigger"
        Expect.stringContains actual "hx-target=\"#target\"" "hx-target"
        Expect.stringContains actual "hx-indicator=\"#spinner\"" "hx-indicator"
        Expect.stringContains actual "hx-include=\"[name='q']\"" "hx-include"
        Expect.stringContains actual "hx-swap=\"innerHTML\"" "hx-swap"
        Expect.stringContains actual "hx-swap-oob=\"true\"" "hx-swap-oob"
        Expect.stringContains actual "hx-encoding=\"multipart/form-data\"" "hx-encoding"
        Expect.stringContains actual "hx-on:click=\"alert('hi')\"" "hx-on"
        Expect.stringContains actual "hx-history=\"false\"" "hx-history"
        Expect.stringContains actual "hx-vals=\"{\"key\": \"val\"}\"" "hx-vals"
        Expect.stringContains actual "hx-custom=\"value\"" "hx generic"
    }
  ]
