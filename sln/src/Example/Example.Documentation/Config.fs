namespace Example.Documentation.Web

open System

type ServerConfig =
    { sourceUrl: string
      docsUrl: string
      publicOrigin: Uri option
      commit: string
      image: string }

module Config =
    let private variable name =
        match Environment.GetEnvironmentVariable name with
        | value when String.IsNullOrWhiteSpace value -> None
        | value -> Some value

    let private publicOrigin (value: string) =
        match Uri.TryCreate(value, UriKind.Absolute) with
        | true, uri when (uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps)
                         && uri.AbsolutePath = "/" && uri.UserInfo = "" && uri.Query = "" && uri.Fragment = "" -> uri
        | _ -> invalidOp "EXAMPLE_PUBLIC_ORIGIN must be an HTTP(S) origin."

    let load () =
        let commit = variable "RELEASE_COMMIT" |> Option.defaultValue "local"
        let repository = "https://github.com/meiermade/FSharp.ViewEngine"
        { sourceUrl =
            // Unpublished local work has no GitHub directory yet.
            if commit = "local" then repository
            else repository + "/tree/" + Uri.EscapeDataString(commit) + "/sln/src/Example"
          docsUrl = variable "EXAMPLE_DOCS_URL" |> Option.defaultValue "https://fve.meiermade.com/examples/documentation"
          publicOrigin = variable "EXAMPLE_PUBLIC_ORIGIN" |> Option.map publicOrigin
          commit = commit
          image = variable "RELEASE_IMAGE" |> Option.defaultValue "local" }
