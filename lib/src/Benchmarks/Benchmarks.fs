module Benchmarks
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Toolchains.InProcess.NoEmit

module ViewEngineApi =
    open FSharp.ViewEngine
    open type Html

    let buildDocument () =
        html {
            _lang "en"
            head {
                meta { _charset "utf-8" }
                meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
                title "Benchmark"
                link { _href "/css/site.css"; _rel "stylesheet" }
            }
            body {
                _class "page"
                header {
                    _class "site-header"
                    h1 { "Benchmark Page" }
                    nav {
                        ul {
                            li { a { _href "/"; "Home" } }
                            li { a { _href "/docs"; "Docs" } }
                            li { a { _href "/about"; "About" } }
                        }
                    }
                }
                main {
                    section {
                        _id "intro"
                        h2 { "Intro" }
                        p { "This is a simple benchmark document." }
                        p { "It includes common HTML elements." }
                        pre {
                            code { _class "language-html"; text "<p>Hello</p>" }
                        }
                    }
                    article {
                        h3 { "Highlights" }
                        ul {
                            li { "Lists" }
                            li { "Forms" }
                            li { "Tables" }
                        }
                        p {
                            text "Some inline "
                            raw "<strong>bold</strong>"
                            text " text."
                        }
                    }
                    form {
                        _id "signup"
                        _class "form"
                        label { _for "email"; "Email" }
                        input { _id "email"; _name "email"; _type "email"; _placeholder "name@example.com" }
                        label { _for "plan"; "Plan" }
                        select {
                            _id "plan"
                            _name "plan"
                            option { _value "free"; "Free" }
                            option { _value "pro"; "Pro" }
                            option { _value "team"; "Team" }
                        }
                        label {
                            _for "terms"
                            input { _id "terms"; _type "checkbox"; _checked true }
                            text " Accept terms"
                        }
                        button { _type "submit"; "Submit" }
                    }
                    table {
                        _class "data"
                        thead {
                            tr {
                                th { "Name" }
                                th { "Value" }
                            }
                        }
                        tbody {
                            tr { td { "Alpha" }; td { "1" } }
                            tr { td { "Beta" }; td { "2" } }
                            tr { td { "Gamma" }; td { "3" } }
                        }
                    }
                }
                footer {
                    _class "site-footer"
                    small { "© 2026 Example Co." }
                    a { _href "/privacy"; "Privacy" }
                }
            }
        }

module OxpeckerApi =
    open Oxpecker.ViewEngine

    let buildDocument () =
        html().attr("lang", "en") {
            head() {
                meta().attr("charset", "utf-8")
                meta().attr("name", "viewport").attr("content", "width=device-width, initial-scale=1")
                title() { "Benchmark" }
                link().attr("href", "/css/site.css").attr("rel", "stylesheet")
            }
            body().attr("class", "page") {
                header().attr("class", "site-header") {
                    h1() { "Benchmark Page" }
                    nav() {
                        ul() {
                            li() { a().attr("href", "/") { "Home" } }
                            li() { a().attr("href", "/docs") { "Docs" } }
                            li() { a().attr("href", "/about") { "About" } }
                        }
                    }
                }
                main() {
                    section().attr("id", "intro") {
                        h2() { "Intro" }
                        p() { "This is a simple benchmark document." }
                        p() { "It includes common HTML elements." }
                        pre() {
                            code().attr("class", "language-html") { "<p>Hello</p>" }
                        }
                    }
                    article() {
                        h3() { "Highlights" }
                        ul() {
                            li() { "Lists" }
                            li() { "Forms" }
                            li() { "Tables" }
                        }
                        p() {
                            "Some inline "
                            raw "<strong>bold</strong>"
                            " text."
                        }
                    }
                    form().attr("id", "signup").attr("class", "form") {
                        label().attr("for", "email") { "Email" }
                        input()
                            .attr("id", "email")
                            .attr("name", "email")
                            .attr("type", "email")
                            .attr("placeholder", "name@example.com")
                        label().attr("for", "plan") { "Plan" }
                        select().attr("id", "plan").attr("name", "plan") {
                            option().attr("value", "free") { "Free" }
                            option().attr("value", "pro") { "Pro" }
                            option().attr("value", "team") { "Team" }
                        }
                        label().attr("for", "terms") {
                            input().attr("id", "terms").attr("type", "checkbox").bool("checked", true)
                            " Accept terms"
                        }
                        button().attr("type", "submit") { "Submit" }
                    }
                    table().attr("class", "data") {
                        thead() {
                            tr() {
                                th() { "Name" }
                                th() { "Value" }
                            }
                        }
                        tbody() {
                            tr() { td() { "Alpha" }; td() { "1" } }
                            tr() { td() { "Beta" }; td() { "2" } }
                            tr() { td() { "Gamma" }; td() { "3" } }
                        }
                    }
                }
                footer().attr("class", "site-footer") {
                    small() { "© 2026 Example Co." }
                    a().attr("href", "/privacy") { "Privacy" }
                }
            }
        }

module GiraffeApi =
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Giraffe.ViewEngine.Attributes

    let buildDocument () =
        html [ _lang "en" ] [
            head [] [
                meta [ _charset "utf-8" ]
                meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
                title [] [ str "Benchmark" ]
                link [ _href "/css/site.css"; _rel "stylesheet" ]
            ]
            body [ _class "page" ] [
                header [ _class "site-header" ] [
                    h1 [] [ str "Benchmark Page" ]
                    nav [] [
                        ul [] [
                            li [] [ a [ _href "/" ] [ str "Home" ] ]
                            li [] [ a [ _href "/docs" ] [ str "Docs" ] ]
                            li [] [ a [ _href "/about" ] [ str "About" ] ]
                        ]
                    ]
                ]
                main [] [
                    section [ _id "intro" ] [
                        h2 [] [ str "Intro" ]
                        p [] [ str "This is a simple benchmark document." ]
                        p [] [ str "It includes common HTML elements." ]
                        pre [] [
                            code [ _class "language-html" ] [ str "<p>Hello</p>" ]
                        ]
                    ]
                    article [] [
                        h3 [] [ str "Highlights" ]
                        ul [] [
                            li [] [ str "Lists" ]
                            li [] [ str "Forms" ]
                            li [] [ str "Tables" ]
                        ]
                        p [] [
                            str "Some inline "
                            rawText "<strong>bold</strong>"
                            str " text."
                        ]
                    ]
                    form [ _id "signup"; _class "form" ] [
                        label [ _for "email" ] [ str "Email" ]
                        input [ _id "email"; _name "email"; _type "email"; _placeholder "name@example.com" ]
                        label [ _for "plan" ] [ str "Plan" ]
                        select [ _id "plan"; _name "plan" ] [
                            option [ _value "free" ] [ str "Free" ]
                            option [ _value "pro" ] [ str "Pro" ]
                            option [ _value "team" ] [ str "Team" ]
                        ]
                        label [ _for "terms" ] [
                            input [ _id "terms"; _type "checkbox"; _checked ]
                            str " Accept terms"
                        ]
                        button [ _type "submit" ] [ str "Submit" ]
                    ]
                    table [ _class "data" ] [
                        thead [] [
                            tr [] [
                                th [] [ str "Name" ]
                                th [] [ str "Value" ]
                            ]
                        ]
                        tbody [] [
                            tr [] [ td [] [ str "Alpha" ]; td [] [ str "1" ] ]
                            tr [] [ td [] [ str "Beta" ]; td [] [ str "2" ] ]
                            tr [] [ td [] [ str "Gamma" ]; td [] [ str "3" ] ]
                        ]
                    ]
                ]
                footer [ _class "site-footer" ] [
                    small [] [ str "© 2026 Example Co." ]
                    a [ _href "/privacy" ] [ str "Privacy" ]
                ]
            ]
        ]

module FelizApi =
    open Feliz.ViewEngine

    let buildDocument () =
        Html.html [
            prop.lang "en"
            prop.children [
                Html.head [
                    prop.children [
                        Html.meta [ prop.charset "utf-8" ]
                        Html.meta [ prop.name "viewport"; prop.content "width=device-width, initial-scale=1" ]
                        Html.title "Benchmark"
                        Html.link [ prop.href "/css/site.css"; prop.rel "stylesheet" ]
                    ]
                ]
                Html.body [
                    prop.className "page"
                    prop.children [
                        Html.header [
                            prop.className "site-header"
                            prop.children [
                                Html.h1 [ prop.children [ Html.text "Benchmark Page" ] ]
                                Html.nav [
                                    prop.children [
                                        Html.ul [
                                            prop.children [
                                                Html.li [ prop.children [ Html.a [ prop.href "/"; prop.children [ Html.text "Home" ] ] ] ]
                                                Html.li [ prop.children [ Html.a [ prop.href "/docs"; prop.children [ Html.text "Docs" ] ] ] ]
                                                Html.li [ prop.children [ Html.a [ prop.href "/about"; prop.children [ Html.text "About" ] ] ] ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.main [
                            prop.children [
                                Html.section [
                                    prop.id "intro"
                                    prop.children [
                                        Html.h2 [ prop.children [ Html.text "Intro" ] ]
                                        Html.p [ prop.children [ Html.text "This is a simple benchmark document." ] ]
                                        Html.p [ prop.children [ Html.text "It includes common HTML elements." ] ]
                                        Html.pre [
                                            prop.children [
                                                Html.code [ prop.className "language-html"; prop.children [ Html.text "<p>Hello</p>" ] ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.article [
                                    prop.children [
                                        Html.h3 [ prop.children [ Html.text "Highlights" ] ]
                                        Html.ul [
                                            prop.children [
                                                Html.li [ prop.children [ Html.text "Lists" ] ]
                                                Html.li [ prop.children [ Html.text "Forms" ] ]
                                                Html.li [ prop.children [ Html.text "Tables" ] ]
                                            ]
                                        ]
                                        Html.p [
                                            prop.children [
                                                Html.text "Some inline "
                                                Html.rawText "<strong>bold</strong>"
                                                Html.text " text."
                                            ]
                                        ]
                                    ]
                                ]
                                Html.form [
                                    prop.id "signup"
                                    prop.className "form"
                                    prop.children [
                                        Html.label [ prop.htmlFor "email"; prop.children [ Html.text "Email" ] ]
                                        Html.input [
                                            prop.id "email"
                                            prop.name "email"
                                            prop.type' "email"
                                            prop.placeholder "name@example.com"
                                        ]
                                        Html.label [ prop.htmlFor "plan"; prop.children [ Html.text "Plan" ] ]
                                        Html.select [
                                            prop.id "plan"
                                            prop.name "plan"
                                            prop.children [
                                                Html.option [ prop.value "free"; prop.children [ Html.text "Free" ] ]
                                                Html.option [ prop.value "pro"; prop.children [ Html.text "Pro" ] ]
                                                Html.option [ prop.value "team"; prop.children [ Html.text "Team" ] ]
                                            ]
                                        ]
                                        Html.label [
                                            prop.htmlFor "terms"
                                            prop.children [
                                                Html.input [ prop.id "terms"; prop.type' "checkbox"; prop.isChecked true ]
                                                Html.text " Accept terms"
                                            ]
                                        ]
                                        Html.button [ prop.type' "submit"; prop.children [ Html.text "Submit" ] ]
                                    ]
                                ]
                                Html.table [
                                    prop.className "data"
                                    prop.children [
                                        Html.thead [
                                            prop.children [
                                                Html.tr [
                                                    prop.children [
                                                        Html.th [ prop.children [ Html.text "Name" ] ]
                                                        Html.th [ prop.children [ Html.text "Value" ] ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                        Html.tbody [
                                            prop.children [
                                                Html.tr [ prop.children [ Html.td [ prop.children [ Html.text "Alpha" ] ]; Html.td [ prop.children [ Html.text "1" ] ] ] ]
                                                Html.tr [ prop.children [ Html.td [ prop.children [ Html.text "Beta" ] ]; Html.td [ prop.children [ Html.text "2" ] ] ] ]
                                                Html.tr [ prop.children [ Html.td [ prop.children [ Html.text "Gamma" ] ]; Html.td [ prop.children [ Html.text "3" ] ] ] ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.footer [
                            prop.className "site-footer"
                            prop.children [
                                Html.small [ prop.children [ Html.text "© 2026 Example Co." ] ]
                                Html.a [ prop.href "/privacy"; prop.children [ Html.text "Privacy" ] ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
[<MemoryDiagnoser>]
type BuildAndRender() =

    [<Benchmark(Baseline = true)>]
    member _.ViewEngineApi() =
        ViewEngineApi.buildDocument() |> FSharp.ViewEngine.Render.toHtmlDocString

    [<Benchmark>]
    member _.OxpeckerApi() =
        OxpeckerApi.buildDocument() |> Oxpecker.ViewEngine.Render.toHtmlDocString

    [<Benchmark>]
    member _.GiraffeApi() =
        GiraffeApi.buildDocument() |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument

    [<Benchmark>]
    member _.FelizApi() =
        FelizApi.buildDocument() |> Feliz.ViewEngine.Render.htmlDocument

[<MemoryDiagnoser>]
type RenderOnly() =
    let viewDoc = ViewEngineApi.buildDocument()
    let oxDoc = OxpeckerApi.buildDocument()
    let giraffeDoc = GiraffeApi.buildDocument()
    let felizDoc = FelizApi.buildDocument()

    [<Benchmark(Baseline = true)>]
    member _.ViewEngineApi() =
        viewDoc |> FSharp.ViewEngine.Render.toHtmlDocString

    [<Benchmark>]
    member _.OxpeckerApi() =
        oxDoc |> Oxpecker.ViewEngine.Render.toHtmlDocString

    [<Benchmark>]
    member _.GiraffeApi() =
        giraffeDoc |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument

    [<Benchmark>]
    member _.FelizApi() =
        felizDoc |> Feliz.ViewEngine.Render.htmlDocument

[<MemoryDiagnoser>]
type BuildOnly() =

    [<Benchmark(Baseline = true)>]
    member _.ViewEngineApi() =
        ViewEngineApi.buildDocument()

    [<Benchmark>]
    member _.OxpeckerApi() =
        OxpeckerApi.buildDocument()

    [<Benchmark>]
    member _.GiraffeApi() =
        GiraffeApi.buildDocument()

    [<Benchmark>]
    member _.FelizApi() =
        FelizApi.buildDocument()

let runBenchmarks () =
    let medJob =
        Job.MediumRun
            .WithToolchain(InProcessNoEmitToolchain.Instance)

    let config =
        ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(medJob)
    BenchmarkRunner.Run<BuildAndRender>(config) |> ignore
    BenchmarkRunner.Run<RenderOnly>(config) |> ignore
    BenchmarkRunner.Run<BuildOnly>(config) |> ignore
