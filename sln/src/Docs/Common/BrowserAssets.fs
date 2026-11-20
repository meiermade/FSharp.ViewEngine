module internal Docs.Common.BrowserAssets

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Microsoft.AspNetCore.StaticFiles

let private paths =
    let manifest = Path.Combine(AppContext.BaseDirectory, "browser-assets.json")
    if File.Exists manifest then
        JsonSerializer.Deserialize<Collections.Generic.Dictionary<string, string>>(File.ReadAllText manifest)
    else
        Collections.Generic.Dictionary<string, string>()

let script = if paths.Count = 0 then "/js/app.js" else paths["script"]
let stylesheet = if paths.Count = 0 then "/css/output.css" else paths["stylesheet"]

let prepareResponse (context: StaticFileResponseContext) =
    let name = context.File.Name
    if Regex.IsMatch(name, @"-[A-Z0-9]{8}\.js$|^output-[0-9a-f]{16}\.css$") then
        context.Context.Response.Headers.CacheControl <- "public, max-age=31536000, immutable"
    elif name = "app.js" || name = "output.css" then
        context.Context.Response.Headers.CacheControl <- "no-cache"
