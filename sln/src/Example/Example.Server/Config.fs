namespace Example.Server

open System

/// Settings owned by the host; request origins still come from the actual request.
type ServerConfig = { sourceUrl: string }

module Config =
    let load () =
        let commit = Environment.GetEnvironmentVariable "RELEASE_COMMIT"
        let repository = "https://github.com/meiermade/FSharp.ViewEngine"
        { sourceUrl =
            // Unpublished local work has no GitHub directory yet.
            if String.IsNullOrWhiteSpace commit || commit = "local" then repository
            else repository + "/tree/" + Uri.EscapeDataString(commit) + "/sln/src/Example" }
