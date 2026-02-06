module Profile

open System

type Mode =
    | BuildAndRender
    | BuildOnly
    | RenderOnly

type Api =
    | ViewEngine
    | Oxpecker
    | Giraffe
    | Feliz

let private parseMode (args: string array) =
    if args |> Array.contains "--build-only" then
        BuildOnly
    elif args |> Array.contains "--render-only" then
        RenderOnly
    else
        BuildAndRender

let private parseApi (args: string array) =
    if args |> Array.contains "--oxpecker" then
        Oxpecker
    elif args |> Array.contains "--giraffe" then
        Giraffe
    elif args |> Array.contains "--feliz" then
        Feliz
    else
        ViewEngine

let private parseDurationMs (args: string array) =
    args
    |> Array.tryFind (fun arg -> arg.StartsWith("--duration-ms=", StringComparison.OrdinalIgnoreCase))
    |> Option.bind (fun arg ->
        let value = arg.Substring("--duration-ms=".Length)
        match Int32.TryParse(value) with
        | true, ms when ms > 0 -> Some ms
        | _ -> None)
    |> Option.defaultValue 10_000

let run (mode: Mode) (api: Api) (durationMs: int) =
    let buildAndRender () =
        match api with
        | ViewEngine ->
            Benchmarks.ViewEngineApi.buildDocument() |> FSharp.ViewEngine.Render.toHtmlDocString |> ignore
        | Oxpecker ->
            Benchmarks.OxpeckerApi.buildDocument() |> Oxpecker.ViewEngine.Render.toHtmlDocString |> ignore
        | Giraffe ->
            Benchmarks.GiraffeApi.buildDocument() |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument |> ignore
        | Feliz ->
            Benchmarks.FelizApi.buildDocument() |> Feliz.ViewEngine.Render.htmlDocument |> ignore

    let buildOnly () =
        match api with
        | ViewEngine -> Benchmarks.ViewEngineApi.buildDocument() |> ignore
        | Oxpecker -> Benchmarks.OxpeckerApi.buildDocument() |> ignore
        | Giraffe -> Benchmarks.GiraffeApi.buildDocument() |> ignore
        | Feliz -> Benchmarks.FelizApi.buildDocument() |> ignore

    let renderOnly () =
        match api with
        | ViewEngine ->
            let doc = Benchmarks.ViewEngineApi.buildDocument()
            doc |> FSharp.ViewEngine.Render.toHtmlDocString |> ignore
        | Oxpecker ->
            let doc = Benchmarks.OxpeckerApi.buildDocument()
            doc |> Oxpecker.ViewEngine.Render.toHtmlDocString |> ignore
        | Giraffe ->
            let doc = Benchmarks.GiraffeApi.buildDocument()
            doc |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument |> ignore
        | Feliz ->
            let doc = Benchmarks.FelizApi.buildDocument()
            doc |> Feliz.ViewEngine.Render.htmlDocument |> ignore

    let invoke =
        match mode with
        | BuildAndRender -> buildAndRender
        | BuildOnly -> buildOnly
        | RenderOnly -> renderOnly

    // Warm up
    for _ = 1 to 1000 do
        invoke ()

    // Signal ready
    Console.Error.WriteLine("READY")
    Console.Error.Flush()

    // Hot loop for profiling
    let mutable count = 0
    let sw = Diagnostics.Stopwatch.StartNew()
    while sw.ElapsedMilliseconds < int64 durationMs do
        invoke ()
        count <- count + 1
    Console.Error.WriteLine($"Completed {count} iterations in {sw.ElapsedMilliseconds}ms")

[<EntryPoint>]
let main args =
    if args |> Array.contains "--profile" then
        let mode = parseMode args
        let api = parseApi args
        let durationMs = parseDurationMs args
        run mode api durationMs
        0
    else
        Benchmarks.runBenchmarks ()
        0
