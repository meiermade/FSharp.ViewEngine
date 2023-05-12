module Tests

open FSharp.ViewEngine
open System.Text.RegularExpressions
open Xunit
open type Html
open type Htmx
open type Alpine
open type Svg

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
            <p>Some more content</p>
            <pre class="language-html">
                <code class="language-html">
                    &lt;p&gt;Even more content&lt;/p&gt;
                </code>
            </pre>
            <ul>
                <li>One</li>
                <li>Two</li>
            </ul>
            <a href="https://github.com/ameier38/FSharp.ViewEngine" class="rounded-lg text-gray-800 font-semibold flex items-center gap-3 p-1">
                <svg viewBox="0 0 24 24" class="h-6 w-6 fill-current">
                    <path fill-rule="evenodd" clip-rule="evenodd" d="M12 2C6.477 2 2 6.463 2 11.97c0 4.404 2.865 8.14 6.839 9.458.5.092.682-.216.682-.48 0-.236-.008-.864-.013-1.695-2.782.602-3.369-1.337-3.369-1.337-.454-1.151-1.11-1.458-1.11-1.458-.908-.618.069-.606.069-.606 1.003.07 1.531 1.027 1.531 1.027.892 1.524 2.341 1.084 2.91.828.092-.643.35-1.083.636-1.332-2.22-.251-4.555-1.107-4.555-4.927 0-1.088.39-1.979 1.029-2.675-.103-.252-.446-1.266.098-2.638 0 0 .84-.268 2.75 1.022A9.607 9.607 0 0 1 12 6.82c.85.004 1.705.114 2.504.336 1.909-1.29 2.747-1.022 2.747-1.022.546 1.372.202 2.386.1 2.638.64.696 1.028 1.587 1.028 2.675 0 3.83-2.339 4.673-4.566 4.92.359.307.678.915.678 1.846 0 1.332-.012 2.407-.012 2.734 0 .267.18.577.688.48 3.97-1.32 6.833-5.054 6.833-9.458C22 6.463 17.522 2 12 2Z"></path>
                </svg>
                Documentation
            </a>
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
                                raw "<p>Some more content</p>"
                                pre [
                                    _class "language-html"
                                    _children [
                                        code [
                                            _class "language-html"
                                            _children [
                                                text "<p>Even more content</p>"
                                            ]
                                        ]
                                    ]
                                ]
                                ul [
                                    _children [
                                        li [ _children "One" ]
                                        li [ _children "Two" ]
                                    ]
                                ]
                                a [
                                    _href "https://github.com/ameier38/FSharp.ViewEngine"
                                    _class "rounded-lg text-gray-800 font-semibold flex items-center gap-3 p-1"
                                    _children [
                                        svg [
                                            _viewBox "0 0 24 24"
                                            _class "h-6 w-6 fill-current"
                                            _children [
                                                path [
                                                    _fillRule "evenodd"
                                                    _clipRule "evenodd"
                                                    _d "M12 2C6.477 2 2 6.463 2 11.97c0 4.404 2.865 8.14 6.839 9.458.5.092.682-.216.682-.48 0-.236-.008-.864-.013-1.695-2.782.602-3.369-1.337-3.369-1.337-.454-1.151-1.11-1.458-1.11-1.458-.908-.618.069-.606.069-.606 1.003.07 1.531 1.027 1.531 1.027.892 1.524 2.341 1.084 2.91.828.092-.643.35-1.083.636-1.332-2.22-.251-4.555-1.107-4.555-4.927 0-1.088.39-1.979 1.029-2.675-.103-.252-.446-1.266.098-2.638 0 0 .84-.268 2.75 1.022A9.607 9.607 0 0 1 12 6.82c.85.004 1.705.114 2.504.336 1.909-1.29 2.747-1.022 2.747-1.022.546 1.372.202 2.386.1 2.638.64.696 1.028 1.587 1.028 2.675 0 3.83-2.339 4.673-4.566 4.92.359.307.678.915.678 1.846 0 1.332-.012 2.407-.012 2.734 0 .267.18.577.688.48 3.97-1.32 6.833-5.054 6.833-9.458C22 6.463 17.522 2 12 2Z"
                                                ]
                                            ]
                                        ]
                                        raw "Documentation"
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
