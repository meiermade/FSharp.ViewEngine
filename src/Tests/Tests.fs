module Tests

open FSharp.ViewEngine
open FSharp.ViewEngine.Html
open FSharp.ViewEngine.Htmx
open FSharp.ViewEngine.Alpine
open System.Text.RegularExpressions
open Xunit
open type Html
open type Htmx
open type Alpine

let clean (s:string) = Regex.Replace(s, @"\s{2,}|\r|\n|\r\n", "")

[<Fact>]
let ``Should render html document`` () =
    let expected = """
<!DOCTYPE html>
<html lang="en">
    <head>
        <title>Test</title>
        <meta charset="utf-8">
        <link href="/css/compiled.css" rel="stylesheet">
    </head>
    <body x-data="{showContent: false}" class="bg-gray-50">
        <div id="page" class="flex flex-col">
            <h1 hx-get="/hello" hx-target="#page">Hello</h1>
            <h1 hx-get="/world" hx-target="#page">World</h1>
        </div>
        <br>
        <div x-show="showContent">
            <h2>Content</h2>
            <p>Some content</p>
            <ul>
                <li>One</li>
                <li>Two</li>
            </ul>
        </div>
    </body>
</html>
"""
    let actual =
        html [
            _lang "en"
            _children [
                head [
                    title "Test"
                    meta [ _charset "utf-8" ]
                    link [ _href "/css/compiled.css"; _rel "stylesheet" ]
                ]
                body [
                    _xData "{showContent: false}"
                    _class "bg-gray-50"
                    _children [
                        div [
                            _id "page"
                            _class [ "flex"; "flex-col" ]
                            _children [
                                h1 [ _hxGet "/hello"; _hxTarget "#page"; _children "Hello" ]
                                h1 [ _hxGet "/world"; _hxTarget "#page"; _children "World" ]
                            ]
                        ]
                        br
                        div [
                            _xShow "showContent"
                            _children [
                                h2 [ _children "Content" ]
                                p [ _children "Some content" ]
                                ul [
                                    _children [
                                        li [ _children "One" ]
                                        li [ _children "Two" ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
        |> Element.render
    Assert.Equal(clean expected, clean actual)
