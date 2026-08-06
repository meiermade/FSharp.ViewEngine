module Workloads

open BenchmarkDotNet.Attributes
open FSharp.ViewEngine
open type Html

module AttributeValues =
    let plain = "plain attribute value"
    let encoded = "{\"quoted\":\"<tag>\",\"ampersand\":\"A&B\",\"apostrophe\":\"'\"}"

    let render value =
        div {
            _title value
            "content"
        }
        |> Render.toString

module Shapes =
    let attributes count =
        match count with
        | 0 -> div { "content" }
        | 1 -> div { _attr("data-1", "1"); "content" }
        | 2 -> div { _attr("data-1", "1"); _attr("data-2", "2"); "content" }
        | 8 ->
            div {
                _attr("data-1", "1")
                _attr("data-2", "2")
                _attr("data-3", "3")
                _attr("data-4", "4")
                _attr("data-5", "5")
                _attr("data-6", "6")
                _attr("data-7", "7")
                _attr("data-8", "8")
                "content"
            }
        | unsupported -> invalidArg (nameof count) $"Unsupported attribute count: {unsupported}"

    let children count =
        match count with
        | 0 -> div { () }
        | 1 -> div { span { "1" } }
        | 2 -> div { span { "1" }; span { "2" } }
        | 8 ->
            div {
                span { "1" }
                span { "2" }
                span { "3" }
                span { "4" }
                span { "5" }
                span { "6" }
                span { "7" }
                span { "8" }
            }
        | unsupported -> invalidArg (nameof count) $"Unsupported child count: {unsupported}"

module Collections =
    let private valuesArray = Array.init 16 (fun index -> $"item-{index}")
    let private valuesList = valuesArray |> Array.toList
    let private valuesSequence = valuesArray |> Seq.map id

    let fromArray () =
        ul {
            for value in valuesArray do
                li { value }
        }

    let fromList () =
        ul {
            for value in valuesList do
                li { value }
        }

    let fromSequence () =
        ul {
            for value in valuesSequence do
                li { value }
        }

module Documents =
    let private largeIndexes = Array.init 1_000 id

    let small () =
        section {
            _class "card"
            h2 { "Small fragment" }
            p { "A representative fragment with attributes and encoded text: <>&." }
        }

    let representative () = Benchmarks.ViewEngineApi.buildDocument ()

    let rec private nestedElement depth =
        if depth = 0 then
            span { "leaf" }
        else
            div {
                _class "level"
                nestedElement (depth - 1)
            }

    let deeplyNested () = nestedElement 64

    let large () =
        html {
            _lang "en"
            body {
                main {
                    for index in largeIndexes do
                        article {
                            _id $"item-{index}"
                            h2 { $"Item {index}" }
                            p { "A repeated row with plain content." }
                            p { "Encoded content: <tag> & metadata." }
                        }
                }
            }
        }

module Validation =
    let private occurrences (needle: string) (value: string) =
        let rec countFrom offset count =
            let index = value.IndexOf(needle, offset, System.StringComparison.Ordinal)
            if index < 0 then count else countFrom (index + needle.Length) (count + 1)
        countFrom 0 0

    let run () =
        let encoded = AttributeValues.render AttributeValues.encoded
        if encoded.Contains("\"<tag>\"", System.StringComparison.Ordinal)
           || not (encoded.Contains("&quot;", System.StringComparison.Ordinal))
           || not (encoded.Contains("&lt;tag&gt;", System.StringComparison.Ordinal))
           || not (encoded.Contains("A&amp;B", System.StringComparison.Ordinal)) then
            failwith $"Encoded attribute benchmark does not exercise HTML encoding: {encoded}"

        for count in [ 0; 1; 2; 8 ] do
            let attributes = Shapes.attributes count |> Render.toString
            if occurrences " data-" attributes <> count then
                failwith $"Attribute shape {count} rendered an unexpected value: {attributes}"

            let children = Shapes.children count |> Render.toString
            if occurrences "<span>" children <> count then
                failwith $"Child shape {count} rendered an unexpected value: {children}"

        let collectionOutputs =
            [ Collections.fromArray (); Collections.fromList (); Collections.fromSequence () ]
            |> List.map Render.toString

        if collectionOutputs |> List.distinct |> List.length <> 1 then
            failwith "Array, list, and sequence benchmarks must render equivalent output."

        let nested = Documents.deeplyNested () |> Render.toString
        if occurrences "<div" nested <> 64 then
            failwith "Deeply nested workload must contain exactly 64 nested div elements."

        let large = Documents.large () |> Render.toHtmlDocString
        if occurrences "<article" large <> 1_000 then
            failwith "Large workload must contain exactly 1,000 article elements."

[<MemoryDiagnoser>]
type AttributeEncodingBenchmarks() =
    [<Benchmark(Baseline = true)>]
    member _.Plain() = AttributeValues.render AttributeValues.plain

    [<Benchmark>]
    member _.Encoded() = AttributeValues.render AttributeValues.encoded

[<MemoryDiagnoser>]
type AttributeShapeBenchmarks() =
    [<Params(0, 1, 2, 8)>]
    member val Count = 0 with get, set

    [<Benchmark>]
    member this.BuildAndRender() = Shapes.attributes this.Count |> Render.toString

[<MemoryDiagnoser>]
type ChildShapeBenchmarks() =
    [<Params(0, 1, 2, 8)>]
    member val Count = 0 with get, set

    [<Benchmark>]
    member this.BuildAndRender() = Shapes.children this.Count |> Render.toString

[<MemoryDiagnoser>]
type CollectionBenchmarks() =
    [<Benchmark(Baseline = true)>]
    member _.Array() = Collections.fromArray () |> Render.toString

    [<Benchmark>]
    member _.List() = Collections.fromList () |> Render.toString

    [<Benchmark>]
    member _.Sequence() = Collections.fromSequence () |> Render.toString

[<MemoryDiagnoser>]
type BuildAndRenderWorkloads() =
    [<Benchmark>]
    member _.SmallFragment() = Documents.small () |> Render.toString

    [<Benchmark>]
    member _.RepresentativePage() = Documents.representative () |> Render.toHtmlDocString

    [<Benchmark>]
    member _.DeeplyNested() = Documents.deeplyNested () |> Render.toString

    [<Benchmark>]
    member _.LargeResponse() = Documents.large () |> Render.toHtmlDocString

[<MemoryDiagnoser>]
type RenderOnlyWorkloads() =
    let mutable small = Unchecked.defaultof<HtmlElement>
    let mutable representative = Unchecked.defaultof<HtmlElement>
    let mutable nested = Unchecked.defaultof<HtmlElement>
    let mutable large = Unchecked.defaultof<HtmlElement>

    [<GlobalSetup>]
    member _.Setup() =
        small <- Documents.small ()
        representative <- Documents.representative ()
        nested <- Documents.deeplyNested ()
        large <- Documents.large ()

    [<Benchmark>]
    member _.SmallFragment() = small |> Render.toString

    [<Benchmark>]
    member _.RepresentativePage() = representative |> Render.toHtmlDocString

    [<Benchmark>]
    member _.DeeplyNested() = nested |> Render.toString

    [<Benchmark>]
    member _.LargeResponse() = large |> Render.toHtmlDocString
