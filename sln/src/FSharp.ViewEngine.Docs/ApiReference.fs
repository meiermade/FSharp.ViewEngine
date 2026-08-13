namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open type Html

type DocsHttpMethod =
    | GET
    | POST
    | PUT
    | PATCH
    | DELETE
    | OPTIONS
    | HEAD

module DocsHttpMethod =
    let value = function
        | GET -> "GET"
        | POST -> "POST"
        | PUT -> "PUT"
        | PATCH -> "PATCH"
        | DELETE -> "DELETE"
        | OPTIONS -> "OPTIONS"
        | HEAD -> "HEAD"

    let className = function
        | GET -> "docs-method-get"
        | POST -> "docs-method-post"
        | PUT -> "docs-method-put"
        | PATCH -> "docs-method-patch"
        | DELETE -> "docs-method-delete"
        | OPTIONS
        | HEAD -> "docs-method-neutral"

type DocsParameter =
    { name:string
      typeName:string
      required:bool
      description:string }

type DocsParameterLocation =
    | Path
    | Query
    | Header
    | Body

module DocsParameterLocation =
    let value = function Path -> "path" | Query -> "query" | Header -> "header" | Body -> "body"

type DocsApiParameter =
    { name:string
      typeName:string
      location:DocsParameterLocation
      required:bool
      defaultValue:string option
      enumValues:string list option
      example:string option
      description:string }

type DocsApiResponse =
    { status:string
      description:string
      language:string option
      example:string option }

type DocsApiError =
    { code:string
      description:string }

module internal CopyableCode =
    let render (className:string) (codeClass:string) (label:string) (source:string) =
        div {
            _class "docs-copyable-code"
            button {
                _type "button"
                _ariaLabel $"Copy {label}"
                _class "docs-copy-code"
                _data("on:click", "window.fsharpDocsCopy(evt.currentTarget)")
                span { _data("docs-copy-label", "true"); "Copy" }
            }
            pre { _class className; _tabindex 0; code { _class codeClass; _data("docs-copy-source", "true"); source } }
        }

module ApiReference =
    let endpoint (method:DocsHttpMethod) (path:string) (description:string) =
        let methodName = DocsHttpMethod.value method
        div {
            _class "docs-api-endpoint"
            _data("http-method", methodName)
            div {
                _class "docs-api-endpoint-line"
                span { _class $"docs-http-method {DocsHttpMethod.className method}"; methodName }
                code { _class "docs-api-path"; path }
            }
            if not (System.String.IsNullOrWhiteSpace description) then
                p { _class "docs-api-description"; description }
        }

    let parameter (name:string) (typeName:string) (required:bool) (description:string) : DocsParameter =
        { name = name
          typeName = typeName
          required = required
          description = description }

    let parameters (values:DocsParameter list) =
        div {
            _class "docs-parameters"
            for parameter in values do
                div {
                    _class "docs-parameter"
                    div {
                        _class "docs-parameter-header"
                        code { _class "docs-parameter-name"; parameter.name }
                        span { _class "docs-parameter-type"; parameter.typeName }
                        if parameter.required then span { _class "docs-required"; "Required" }
                    }
                    p { _class "docs-parameter-description"; parameter.description }
                }
        }

    let codeExample (title:string) (language:string) (source:string) =
        let normalized = if System.String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        section {
            _class "docs-code-panel"
            div { _class "docs-code-panel-header"; span { title }; span { _class "docs-code-language"; normalized } }
            CopyableCode.render ($"docs-code-panel-source language-{prismLanguage}") ($"language-{prismLanguage}") title source
        }

    let responseExample (status:string) (language:string) (source:string) =
        section {
            _class "docs-response-panel"
            div { _class "docs-response-status"; span { "Response" }; code { status } }
            codeExample status language source
        }

    let operation
        (method:DocsHttpMethod)
        (path:string)
        (description:string)
        (authentication:string option)
        (parameters:DocsApiParameter list)
        (responses:DocsApiResponse list)
        (errors:DocsApiError list)
        (idempotency:string option)
        (apiVersion:string option)
        (deprecated:bool) =
        section {
            _class "docs-api-operation"
            endpoint method path description
            if deprecated then span { _class "docs-page-badge docs-page-badge-warning"; "Deprecated" }
            match authentication with
            | Some value -> div { _class "docs-api-policy"; strong { "Authentication" }; p { value } }
            | None -> ()
            match apiVersion with
            | Some value -> div { _class "docs-api-policy"; strong { "API version" }; code { value } }
            | None -> ()
            match idempotency with
            | Some value -> div { _class "docs-api-policy"; strong { "Idempotency" }; p { value } }
            | None -> ()
            if not parameters.IsEmpty then
                div {
                    _class "docs-parameters"
                    for parameter in parameters do
                        div {
                            _class "docs-parameter"
                            _data("parameter-location", DocsParameterLocation.value parameter.location)
                            div {
                                _class "docs-parameter-header"
                                code { _class "docs-parameter-name"; parameter.name }
                                span { _class "docs-parameter-type"; parameter.typeName }
                                span { _class "docs-parameter-type"; DocsParameterLocation.value parameter.location }
                                if parameter.required then span { _class "docs-required"; "Required" }
                            }
                            p { _class "docs-parameter-description"; parameter.description }
                            match parameter.defaultValue with | Some value -> p { _class "docs-parameter-description"; "Default: "; code { value } } | None -> ()
                            match parameter.enumValues with | Some values -> p { _class "docs-parameter-description"; "Values: "; String.concat ", " values } | None -> ()
                            match parameter.example with | Some value -> p { _class "docs-parameter-description"; "Example: "; code { value } } | None -> ()
                        }
                }
            if not responses.IsEmpty then
                div {
                    _class "docs-api-responses"
                    for response in responses do
                        section {
                            _class "docs-response-panel"
                            div { _class "docs-response-status"; code { response.status }; span { response.description } }
                            match response.language, response.example with
                            | Some language, Some source -> codeExample response.status language source
                            | _ -> ()
                        }
                }
            if not errors.IsEmpty then
                div {
                    _class "docs-parameters"
                    for error in errors do
                        div { _class "docs-parameter"; code { _class "docs-parameter-name"; error.code }; p { _class "docs-parameter-description"; error.description } }
                }
        }
