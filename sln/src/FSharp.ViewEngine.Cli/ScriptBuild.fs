namespace FSharp.ViewEngine.Cli

open System
open System.Diagnostics
open System.IO
open System.Text.Json

[<NoEquality; NoComparison>]
type ScriptBuildOptions =
    { ProjectPath:string
      Modules:string array
      Open:bool }

[<RequireQualifiedAccess>]
module ScriptBuild =
    [<Literal>]
    let TailwindVersion = "4.2.2"
    [<Literal>]
    let EsbuildVersion = "0.28.2"

    let private run directory program arguments environment =
        let start = ProcessStartInfo(program: string)
        start.WorkingDirectory <- directory
        start.UseShellExecute <- false
        start.RedirectStandardOutput <- true
        start.RedirectStandardError <- true
        for argument in arguments do start.ArgumentList.Add argument
        for name, value in environment do start.Environment[name] <- value
        use child = Process.Start start
        let stdout, stderr = child.StandardOutput.ReadToEndAsync(), child.StandardError.ReadToEndAsync()
        if not (child.WaitForExit 120000) then
            child.Kill true
            child.WaitForExit()
            invalidOp $"'{program}' did not finish within two minutes."
        if child.ExitCode <> 0 then
            invalidOp $"'{program}' exited with code {child.ExitCode}.\n{stdout.Result}{stderr.Result}"
        stdout.Result

    let private executable name =
        let filename = if OperatingSystem.IsWindows() then name + (if name = "npm" then ".cmd" else ".exe") else name
        (Environment.GetEnvironmentVariable("PATH") |> Option.ofObj |> Option.defaultValue "").Split(Path.PathSeparator)
        |> Array.map (fun directory -> Path.Combine(directory.Trim('"'), filename))
        |> Array.tryFind File.Exists
        |> Option.defaultWith (fun () -> invalidOp $"'{name}' was not found on PATH. Script builds require Node.js 20 or newer with npm, and a .NET SDK (10 recommended).")

    let private packageVersion path =
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            document.RootElement.GetProperty("version").GetString()
        with :? IOException | :? JsonException | :? Collections.Generic.KeyNotFoundException | :? InvalidOperationException -> ""

    let private tools (output:TextWriter) =
        let node = executable "node"
        let version = (run (Directory.GetCurrentDirectory()) node ["--version"] []).Trim().TrimStart('v').Split('.')[0]
        match Int32.TryParse version with
        | true, major when major >= 20 -> ()
        | _ -> invalidOp "Script builds require Node.js 20 or newer."
        let cacheRoot =
            Environment.GetEnvironmentVariable "FVE_BUILD_CACHE"
            |> Option.ofObj
            |> Option.filter (String.IsNullOrWhiteSpace >> not)
            |> Option.defaultWith (fun () -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "fsharp-viewengine", "build"))
        let directory = Path.GetFullPath(Path.Combine(cacheRoot, $"tailwind-{TailwindVersion}-esbuild-{EsbuildVersion}"))
        Directory.CreateDirectory directory |> ignore
        use gate =
            try new FileStream(Path.Combine(directory, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            with :? IOException -> invalidOp "Another build is initializing the frontend tool cache. Run again after it completes."
        let modules = Path.Combine(directory, "node_modules")
        let packages = ["tailwindcss", TailwindVersion; "@tailwindcss/cli", TailwindVersion; "esbuild", EsbuildVersion]
        let tailwind = Path.Combine(modules, "@tailwindcss", "cli", "dist", "index.mjs")
        let esbuild = Path.Combine(modules, "esbuild", "lib", "main.js")
        if not (File.Exists tailwind && File.Exists esbuild) || (packages |> List.exists (fun (name, version) -> packageVersion (Path.Combine(modules, name, "package.json")) <> version)) then
            let npm = executable "npm"
            let npmCli =
                if OperatingSystem.IsWindows() then Path.Combine(Path.GetDirectoryName npm, "node_modules", "npm", "bin", "npm-cli.js")
                else
                    let target = File.ResolveLinkTarget(npm, true)
                    if isNull target then npm else target.FullName
            if not (File.Exists npmCli) then invalidOp "Could not locate npm-cli.js. Install Node.js with npm and retry."
            let dependencies = packages |> dict
            let manifest = JsonSerializer.Serialize({| ``private`` = true; dependencies = dependencies |})
            Text.writeAtomic (Path.Combine(directory, "package.json")) manifest
            output.WriteLine($"Preparing Tailwind {TailwindVersion} and esbuild {EsbuildVersion} (cached for later builds)...")
            let install = if File.Exists(Path.Combine(directory, "package-lock.json")) then "ci" else "install"
            run directory node [npmCli; install; "--no-audit"; "--no-fund"] [] |> ignore
        if not (File.Exists tailwind && File.Exists esbuild) then invalidOp "The frontend tool cache is incomplete. Remove its version directory and rebuild."
        node, modules, tailwind, esbuild

    let private buildPage (output:TextWriter) (options:ScriptBuildOptions) =
        let project = Path.GetFullPath options.ProjectPath
        if not (String.Equals(Path.GetExtension project, ".fsx", StringComparison.OrdinalIgnoreCase)) then
            invalidArg "project" "fve build supports .fsx consumers only. Use dotnet build for application projects."
        let root = Path.GetDirectoryName project
        let inputCss = Path.Combine(root, "input.css")
        if not (File.Exists project) then invalidOp $"Script '{project}' was not found."
        if not (File.Exists inputCss) then invalidOp $"Stylesheet '{inputCss}' was not found. Restore or create input.css; build never rewrites authored styles."
        let modules = options.Modules |> Array.map (fun path -> Path.GetFullPath(Path.Combine(root, path))) |> Array.distinct
        for path in modules do
            if not (File.Exists path) then invalidOp $"Installed browser module '{path}' was not found. Restore the installed file before building."
        let html, css, js = Path.ChangeExtension(project, ".html"), Path.ChangeExtension(project, ".css"), Path.ChangeExtension(project, ".js")
        for path in [html; css; js] do
            if Directory.Exists path then invalidOp $"Output '{path}' is a directory. Build outputs must be files."
        // Stage beside the final assets so CSS url() rebasing retains the same directory.
        let temporary = Path.Combine(root, ".fve-" + Guid.NewGuid().ToString("N"))
        let backup, stagedCss, stagedJs = temporary + ".html", temporary + ".css", temporary + ".js"
        let node, nodeModules, tailwind, esbuild = tools output
        let existed = File.Exists html
        let mutable completed = false
        let mutable renderingStarted = false
        try
            if existed then File.Move(html, backup)
            renderingStarted <- true
            output.WriteLine($"Rendering {Path.GetFileName project}...")
            run root "dotnet" ["fsi"; "--exec"; project] [] |> ignore
            if not (File.Exists html) then invalidOp $"The script must write '{Path.GetFileName html}'. No HTML output was produced."
            if not ((File.ReadAllText html).TrimStart().StartsWith("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase)) then
                invalidOp $"'{Path.GetFileName html}' must be a complete HTML document. Use Render.toHtmlDocString."
            output.WriteLine "Building stylesheet and browser bundle..."
            run root node [tailwind; "--input"; inputCss; "--output"; stagedCss; "--minify"] ["NODE_PATH", nodeModules] |> ignore
            let imports = modules |> Array.map (fun path -> "import " + JsonSerializer.Serialize(path.Replace('\\', '/')) + ";") |> String.concat "\n"
            let bundle = JsonSerializer.Serialize({| stdin = {| contents=imports; resolveDir=root |}; bundle=true; format="iife"; outfile=stagedJs; minify=true |})
            run root node ["--eval"; "require(" + JsonSerializer.Serialize(esbuild) + ").buildSync(" + bundle + ")"] [] |> ignore
            File.Move(stagedCss, css, true)
            File.Move(stagedJs, js, true)
            completed <- true
        finally
            if renderingStarted && not completed then
                if File.Exists html then File.Delete html
                if existed && File.Exists backup then File.Move(backup, html)
            for path in [backup; stagedCss; stagedJs] do
                if File.Exists path then File.Delete path
        for path in [html; css; js] do output.WriteLine($"Built {path}")
        if options.Open then
            let start = ProcessStartInfo(html)
            start.UseShellExecute <- true
            use opened = Process.Start start
            ()

    let build output options =
        let identity = Path.GetFullPath options.ProjectPath
        use gate = new Threading.Mutex(false, "FSharpViewEngine.Build." + Text.checksum identity)
        let acquired = try gate.WaitOne(0) with :? Threading.AbandonedMutexException -> true
        if not acquired then invalidOp "Another build is already writing this script's outputs."
        try buildPage output options
        finally gate.ReleaseMutex()
