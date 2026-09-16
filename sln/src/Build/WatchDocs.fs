module WatchDocs

open System
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading

[<Literal>]
let private productMarker = "fsharp-viewengine"

let defaultUrl = "http://127.0.0.1:5054"

type WatcherOwnership =
    { product:string
      watcher:string
      processName:string
      pid:int
      startedAtUtcTicks:int64 }

let watcherOwnershipFile (watcherName:string) =
    Path.Combine(Path.GetTempPath(), $"{productMarker}-{watcherName.ToLowerInvariant()}.owner")

let private ownershipForProcess (watcherName:string) (instance:Process) =
    { product = productMarker
      watcher = watcherName.ToLowerInvariant()
      processName = instance.ProcessName
      pid = instance.Id
      startedAtUtcTicks = instance.StartTime.ToUniversalTime().Ticks }

let private formatOwnership ownership =
    String.concat "|" [ ownership.product; ownership.watcher; ownership.processName; string ownership.pid; string ownership.startedAtUtcTicks ]

let private tryReadOwnership (watcherName:string) =
    let path = watcherOwnershipFile watcherName
    if File.Exists path then
        match File.ReadAllText(path).Trim().Split('|') with
        | [| product; watcher; processName; pidValue; startedAtUtcTicksValue |] ->
            match Int32.TryParse pidValue, Int64.TryParse startedAtUtcTicksValue with
            | (true, pid), (true, startedAtUtcTicks) when
                product = productMarker
                && watcher = watcherName.ToLowerInvariant()
                && not (String.IsNullOrWhiteSpace processName) ->
                Some
                    { product = product
                      watcher = watcher
                      processName = processName
                      pid = pid
                      startedAtUtcTicks = startedAtUtcTicks }
            | _ -> None
        | _ -> None
    else None

let private stopOwnedWatcher watcherName ownership =
    try
        use running = Process.GetProcessById ownership.pid
        if not running.HasExited && ownershipForProcess watcherName running = ownership then
            Fake.Core.Trace.trace $"Stopping existing {watcherName} process tree {ownership.pid}"
            running.Kill(entireProcessTree = true)
            running.WaitForExit(5000) |> ignore
        else
            Fake.Core.Trace.trace $"Ignoring stale {watcherName} ownership for process {ownership.pid}"
    with
    | :? ArgumentException -> ()
    | :? InvalidOperationException -> ()
    | :? ComponentModel.Win32Exception -> ()

let private stopExistingWatcher watcherName =
    tryReadOwnership watcherName |> Option.iter (stopOwnedWatcher watcherName)
    let path = watcherOwnershipFile watcherName
    if File.Exists path then File.Delete path

let configuredUrl () =
    Environment.GetEnvironmentVariable "DOCS_SERVER_URL"
    |> Option.ofObj
    |> Option.filter (String.IsNullOrWhiteSpace >> not)
    |> Option.defaultValue defaultUrl
    |> _.TrimEnd('/')

let ensureLoopbackUrlAvailable watcherName (serverUrl:string) =
    let uri =
        try Uri(serverUrl, UriKind.Absolute)
        with :? UriFormatException as error ->
            raise (ArgumentException($"{watcherName} URL is invalid: {serverUrl}", nameof serverUrl, error))

    if uri.Scheme <> Uri.UriSchemeHttp
       || uri.Host <> IPAddress.Loopback.ToString()
       || uri.Port <= 0
       || not (String.IsNullOrEmpty uri.UserInfo)
       || not (String.IsNullOrEmpty uri.Query)
       || not (String.IsNullOrEmpty uri.Fragment)
       || uri.AbsolutePath <> "/" then
        invalidArg (nameof serverUrl) $"{watcherName} URL must be an origin using http://127.0.0.1."

    try
        use listener = new TcpListener(IPAddress.Loopback, uri.Port)
        listener.Start()
    with :? SocketException as error ->
        raise (InvalidOperationException($"{watcherName} cannot start because {serverUrl} is already in use by a process that is not a verified FSharp.ViewEngine watcher.", error))

let private waitForLoopbackUrlAvailable watcherName serverUrl =
    let deadline = DateTime.UtcNow.AddSeconds 5.
    let rec wait () =
        try ensureLoopbackUrlAvailable watcherName serverUrl
        with :? InvalidOperationException when DateTime.UtcNow < deadline ->
            Thread.Sleep 100
            wait ()
    wait ()

let runExclusiveWatcher watcherName serverUrl run =
    stopExistingWatcher watcherName
    waitForLoopbackUrlAvailable watcherName serverUrl

    use current = Process.GetCurrentProcess()
    let ownership = ownershipForProcess watcherName current
    File.WriteAllText(watcherOwnershipFile watcherName, formatOwnership ownership)

    try run ()
    finally
        match tryReadOwnership watcherName with
        | Some recorded when recorded = ownership -> File.Delete(watcherOwnershipFile watcherName)
        | _ -> ()
