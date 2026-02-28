module Tests

open FSharp.ViewEngine
open System.Text
open System.Web
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open type Html
open type Htmx
open type Alpine
open type Datastar
open type Svg
open type Tailwind

module String =
    let replace (oldValue:string) (newValue:string) (s:string) = s.Replace(oldValue, newValue)
    let clean (s:string) = Regex.Replace(s, @"\s{2,}|\r|\n|\r\n", "")

module ViewEngineApi =
    open type Html
    open type Htmx
    open type Alpine
    open type Svg
    open type Tailwind

    let buildDocument () =
        html {
            _lang "en"
            head {
                title "Test"
                meta { _charset "utf-8" }
                link { _href "/css/compiled.css"; _rel "stylesheet" }
            }
            body {
                _class "bg-gray-50"
                div {
                    _id "page"
                    _class [ "flex"; "flex-col" ]
                    h1 { _hxGet "/hello"; _hxTarget "#page"; "Hello" }
                    h1 { _hxGet "/world"; _hxTarget "#page"; "World" }
                }
                br
                div {
                    _xShow "showContent"
                    h2 { "Content" }
                    p { "Some content" }
                    raw "<p>Some more content</p>"
                    pre {
                        _class "language-html"
                        code {
                            _class "language-html"
                            text "<p>Even more content</p>"
                        }
                    }
                    ul {
                        li { "One" }
                        li { "Two" }
                    }
                    a {
                        _href "https://github.com/meiermade/FSharp.ViewEngine"
                        _class "rounded-lg text-gray-800 font-semibold flex items-center gap-3 p-1"
                        svg {
                            _viewBox "0 0 24 24"
                            _class "h-6 w-6 fill-current"
                            path {
                                _fillRule "evenodd"
                                _clipRule "evenodd"
                                _d "M12 2C6.477 2 2 6.463 2 11.97c0 4.404 2.865 8.14 6.839 9.458.5.092.682-.216.682-.48 0-.236-.008-.864-.013-1.695-2.782.602-3.369-1.337-3.369-1.337-.454-1.151-1.11-1.458-1.11-1.458-.908-.618.069-.606.069-.606 1.003.07 1.531 1.027 1.531 1.027.892 1.524 2.341 1.084 2.91.828.092-.643.35-1.083.636-1.332-2.22-.251-4.555-1.107-4.555-4.927 0-1.088.39-1.979 1.029-2.675-.103-.252-.446-1.266.098-2.638 0 0 .84-.268 2.75 1.022A9.607 9.607 0 0 1 12 6.82c.85.004 1.705.114 2.504.336 1.909-1.29 2.747-1.022 2.747-1.022.546 1.372.202 2.386.1 2.638.64.696 1.028 1.587 1.028 2.675 0 3.83-2.339 4.673-4.566 4.92.359.307.678.915.678 1.846 0 1.332-.012 2.407-.012 2.734 0 .267.18.577.688.48 3.97-1.32 6.833-5.054 6.833-9.458C22 6.463 17.522 2 12 2Z"
                            }
                        }
                        raw "Documentation"
                    }
                    elSelect {
                        _name "status"
                        _value "active"
                        button {
                            _type "button"
                            elSelectedContent { "Active" }
                        }
                        elOptions {
                            _popover
                            elOption { _value "active"; "Active" }
                            elOption { _value "inactive"; "Inactive" }
                            elOption { _value "archived"; "Archived" }
                        }
                    }
                }
            }
        }

// language=HTML
let expectedHtml = """
<!DOCTYPE html>
<html lang="en">
    <head>
        <title>Test</title>
        <meta charset="utf-8">
        <link href="/css/compiled.css" rel="stylesheet">
    </head>
    <body class="bg-gray-50">
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
            <a href="https://github.com/meiermade/FSharp.ViewEngine" class="rounded-lg text-gray-800 font-semibold flex items-center gap-3 p-1">
                <svg viewBox="0 0 24 24" class="h-6 w-6 fill-current">
                    <path fill-rule="evenodd" clip-rule="evenodd" d="M12 2C6.477 2 2 6.463 2 11.97c0 4.404 2.865 8.14 6.839 9.458.5.092.682-.216.682-.48 0-.236-.008-.864-.013-1.695-2.782.602-3.369-1.337-3.369-1.337-.454-1.151-1.11-1.458-1.11-1.458-.908-.618.069-.606.069-.606 1.003.07 1.531 1.027 1.531 1.027.892 1.524 2.341 1.084 2.91.828.092-.643.35-1.083.636-1.332-2.22-.251-4.555-1.107-4.555-4.927 0-1.088.39-1.979 1.029-2.675-.103-.252-.446-1.266.098-2.638 0 0 .84-.268 2.75 1.022A9.607 9.607 0 0 1 12 6.82c.85.004 1.705.114 2.504.336 1.909-1.29 2.747-1.022 2.747-1.022.546 1.372.202 2.386.1 2.638.64.696 1.028 1.587 1.028 2.675 0 3.83-2.339 4.673-4.566 4.92.359.307.678.915.678 1.846 0 1.332-.012 2.407-.012 2.734 0 .267.18.577.688.48 3.97-1.32 6.833-5.054 6.833-9.458C22 6.463 17.522 2 12 2Z"></path>
                </svg>
                Documentation
            </a>
            <el-select name="status" value="active">
                <button type="button">
                    <el-selectedcontent>Active</el-selectedcontent>
                </button>
                <el-options popover>
                    <el-option value="active">Active</el-option>
                    <el-option value="inactive">Inactive</el-option>
                    <el-option value="archived">Archived</el-option>
                </el-options>
            </el-select>
        </div>
    </body>
</html>
"""

[<Tests>]
let tests =
  testList "ViewEngine Tests" [
    test "ViewEngine should render html document" {
        let actual = ViewEngineApi.buildDocument() |> Render.toHtmlDocString
        Expect.equal (String.clean actual) (String.clean expectedHtml) "Rendered HTML should match expected"
    }

    test "Datastar attributes should render with data- prefix" {
        let actual =
            div {
                _dataSignals ("count", "0")
                _dataOn ("click", "$count++")
                _dataShow "$count > 0"
                _dataText "$count"
                _dataBind "name"
                _dataEffect "console.log($count)"
                _dataClass ("active", "$isActive")
                _dataAttr ("disabled", "$count === 0")
                _dataComputed ("double", "$count * 2")
                _dataInit "console.log('init')"
                _dataIgnore
                _dataIgnoreMorph
                _dataStyle ("color", "red")
                _dataRef "myInput"
                _dataIndicator "loading"
                _dataAnimate "fadeIn"
                _dataPersist ()
                _dataPersist "count"
                _dataScrollIntoView
                "Content"
            } |> Render.toString
        Expect.stringContains actual "data-signals:count=\"0\"" "data-signals"
        Expect.stringContains actual "data-on:click=\"$count++\"" "data-on"
        Expect.stringContains actual "data-show=\"$count > 0\"" "data-show"
        Expect.stringContains actual "data-text=\"$count\"" "data-text"
        Expect.stringContains actual "data-bind:name" "data-bind"
        Expect.stringContains actual "data-effect=\"console.log($count)\"" "data-effect"
        Expect.stringContains actual "data-class:active=\"$isActive\"" "data-class"
        Expect.stringContains actual "data-attr:disabled=\"$count === 0\"" "data-attr"
        Expect.stringContains actual "data-computed:double=\"$count * 2\"" "data-computed"
        Expect.stringContains actual "data-init=\"console.log('init')\"" "data-init"
        Expect.stringContains actual "data-ignore-morph" "data-ignore-morph"
        Expect.stringContains actual "data-ignore" "data-ignore"
        Expect.stringContains actual "data-style:color=\"red\"" "data-style"
        Expect.stringContains actual "data-ref:myInput" "data-ref"
        Expect.stringContains actual "data-indicator:loading" "data-indicator"
        Expect.stringContains actual "data-animate=\"fadeIn\"" "data-animate"
        Expect.stringContains actual "data-persist data-persist:count" "data-persist"
        Expect.stringContains actual "data-scroll-into-view" "data-scroll-into-view"
    }

    test "HtmlEncode should match HttpUtility.HtmlEncode" {
        let inputs =
            [
                ""
                "plain"
                "<tag>"
                "Tom & Jerry"
                "\"quote\" and 'apostrophe'"
                "accent \u00E9"
                "emoji \U0001F600"
                "mix <&> \u00E9 \U0001F600"
            ]

        for input in inputs do
            let expected = HttpUtility.HtmlEncode(input)

            let actual =
                let sb = StringBuilder()
                let el = TextElement(input)
                el.Render(sb)
                sb.ToString()

            Expect.equal actual expected $"HtmlEncode should match HttpUtility for input: {input}"
    }

  ]
