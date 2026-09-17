namespace FSharp.ViewEngine.Components.Documentation

open System
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
        | DELETE
        | OPTIONS
        | HEAD -> "docs-method-neutral"

[<NoEquality; NoComparison>]
type DocsEndpoint =
    private
        { method':DocsHttpMethod
          path:string
          description:string option }

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

[<NoEquality; NoComparison>]
type DocsApiParameter =
    private
        { name:string
          typeName:string
          location:DocsParameterLocation
          required:bool
          defaultValue:string option
          enumValues:string list
          example:string option
          description:string option }

[<NoEquality; NoComparison>]
type DocsApiResponse =
    private
        { status:string
          description:string option
          language:string option
          example:string option }

[<NoEquality; NoComparison>]
type DocsApiError =
    private
        { code:string
          description:string }

[<NoEquality; NoComparison>]
type DocsApiOperation =
    private
        { endpoint:DocsEndpoint
          authentication:string option
          parameters:DocsApiParameter list
          responses:DocsApiResponse list
          errors:DocsApiError list
          idempotency:string option
          apiVersion:string option
          deprecated:bool }

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

[<NoEquality; NoComparison>]
type DocsCodeBlock = private { language:string; source:string }

[<RequireQualifiedAccess>]
module CodeBlock =
    let create language source : DocsCodeBlock =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "Code source cannot be empty."
        { language = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
          source = source }

    let render (block:DocsCodeBlock) =
        let prismLanguage = if block.language = "fs" then "fsharp" else block.language
        CopyableCode.render ($"spec-code language-{prismLanguage}") ($"language-{prismLanguage}") "code" block.source

module private ApiReferenceView =
    let endpoint (endpoint:DocsEndpoint) =
        let methodName = DocsHttpMethod.value endpoint.method'
        div {
            _class "docs-api-endpoint"
            _data("http-method", methodName)
            div {
                _class "docs-api-endpoint-line"
                span { _class $"docs-http-method {DocsHttpMethod.className endpoint.method'}"; methodName }
                code { _class "docs-api-path"; endpoint.path }
            }
            match endpoint.description with
            | Some description -> p { _class "docs-api-description"; description }
            | None -> ()
        }

    let compactParameter (parameter:DocsParameter) =
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

    let parameter (parameter:DocsApiParameter) =
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
            match parameter.description with
            | Some description -> p { _class "docs-parameter-description"; description }
            | None -> ()
            match parameter.defaultValue with
            | Some value -> p { _class "docs-parameter-description"; "Default: "; code { value } }
            | None -> ()
            if not parameter.enumValues.IsEmpty then
                p { _class "docs-parameter-description"; "Values: "; String.concat ", " parameter.enumValues }
            match parameter.example with
            | Some value -> p { _class "docs-parameter-description"; "Example: "; code { value } }
            | None -> ()
        }

    let codeExample (title:string) (language:string) (source:string) =
        let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        section {
            _class "docs-code-panel"
            div { _class "docs-code-panel-header"; span { title }; span { _class "docs-code-language"; normalized } }
            CopyableCode.render ($"docs-code-panel-source language-{prismLanguage}") ($"language-{prismLanguage}") title source
        }

    let response (response:DocsApiResponse) =
        section {
            _class "docs-response-panel"
            div {
                _class "docs-response-status"
                code { response.status }
                match response.description with
                | Some description -> span { description }
                | None -> ()
            }
            match response.language, response.example with
            | Some language, Some source -> codeExample response.status language source
            | _ -> ()
        }


[<RequireQualifiedAccess>]
module Endpoint =
    let create method' path : DocsEndpoint =
        if String.IsNullOrWhiteSpace path || not (path.StartsWith('/')) then
            invalidArg (nameof path) "An endpoint requires an absolute API path."
        { method' = method'; path = path; description = None }

    let withDescription description (endpoint:DocsEndpoint) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An endpoint description cannot be empty."
        { endpoint with description = Some description }

    let render (endpoint:DocsEndpoint) = ApiReferenceView.endpoint endpoint

[<RequireQualifiedAccess>]
module Parameter =
    let create name typeName location : DocsApiParameter =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "An API parameter name is required."
        if String.IsNullOrWhiteSpace typeName then invalidArg (nameof typeName) "An API parameter type is required."
        { name = name
          typeName = typeName
          location = location
          required = false
          defaultValue = None
          enumValues = []
          example = None
          description = None }

    let required (parameter:DocsApiParameter) = { parameter with required = true }
    let withDescription description (parameter:DocsApiParameter) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API parameter description cannot be empty."
        { parameter with description = Some description }
    let withDefaultValue value (parameter:DocsApiParameter) =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "An API parameter default cannot be empty."
        { parameter with defaultValue = Some value }
    let withEnumValues values (parameter:DocsApiParameter) =
        if List.isEmpty values || values |> List.exists String.IsNullOrWhiteSpace then
            invalidArg (nameof values) "API parameter values must be non-empty."
        { parameter with enumValues = values }
    let withExample value (parameter:DocsApiParameter) =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "An API parameter example cannot be empty."
        { parameter with example = Some value }
    let render (parameter:DocsApiParameter) = ApiReferenceView.parameter parameter

[<RequireQualifiedAccess>]
module Response =
    let create status : DocsApiResponse =
        if String.IsNullOrWhiteSpace status then invalidArg (nameof status) "An API response status is required."
        { status = status; description = None; language = None; example = None }

    let withDescription description (response:DocsApiResponse) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API response description cannot be empty."
        { response with description = Some description }
    let withExample language source (response:DocsApiResponse) =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "An API response example cannot be empty."
        { response with language = Some language; example = Some source }
    let render (response:DocsApiResponse) = ApiReferenceView.response response

[<RequireQualifiedAccess>]
module Error =
    let create code description : DocsApiError =
        if String.IsNullOrWhiteSpace code then invalidArg (nameof code) "An API error code is required."
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API error description is required."
        { code = code; description = description }

[<RequireQualifiedAccess>]
module Operation =
    let create method' path : DocsApiOperation =
        { endpoint = Endpoint.create method' path
          authentication = None
          parameters = []
          responses = []
          errors = []
          idempotency = None
          apiVersion = None
          deprecated = false }

    let withDescription description (operation:DocsApiOperation) = { operation with endpoint = operation.endpoint |> Endpoint.withDescription description }
    let withAuthentication authentication (operation:DocsApiOperation) =
        if String.IsNullOrWhiteSpace authentication then invalidArg (nameof authentication) "Authentication guidance cannot be empty."
        { operation with authentication = Some authentication }
    let withParameters parameters (operation:DocsApiOperation) = { operation with parameters = parameters }
    let withResponses responses (operation:DocsApiOperation) = { operation with responses = responses }
    let withErrors errors (operation:DocsApiOperation) = { operation with errors = errors }
    let withIdempotency guidance (operation:DocsApiOperation) =
        if String.IsNullOrWhiteSpace guidance then invalidArg (nameof guidance) "Idempotency guidance cannot be empty."
        { operation with idempotency = Some guidance }
    let withApiVersion version (operation:DocsApiOperation) =
        if String.IsNullOrWhiteSpace version then invalidArg (nameof version) "An API version cannot be empty."
        { operation with apiVersion = Some version }
    let deprecated (operation:DocsApiOperation) = { operation with deprecated = true }

    let render (operation:DocsApiOperation) =
        section {
            _class "docs-api-operation"
            Endpoint.render operation.endpoint
            if operation.deprecated then span { _class "docs-page-badge docs-page-badge-warning"; "Deprecated" }
            match operation.authentication with
            | Some value -> div { _class "docs-api-policy"; strong { "Authentication" }; p { value } }
            | None -> ()
            match operation.apiVersion with
            | Some value -> div { _class "docs-api-policy"; strong { "API version" }; code { value } }
            | None -> ()
            match operation.idempotency with
            | Some value -> div { _class "docs-api-policy"; strong { "Idempotency" }; p { value } }
            | None -> ()
            if not operation.parameters.IsEmpty then
                div { _class "docs-parameters"; for parameter in operation.parameters do Parameter.render parameter }
            if not operation.responses.IsEmpty then
                div { _class "docs-api-responses"; for response in operation.responses do Response.render response }
            if not operation.errors.IsEmpty then
                div {
                    _class "docs-parameters"
                    for error in operation.errors do
                        div { _class "docs-parameter"; code { _class "docs-parameter-name"; error.code }; p { _class "docs-parameter-description"; error.description } }
                }
        }

module ApiReference =
    /// Compatibility render helper. Prefer Endpoint.create |> Endpoint.withDescription |> Endpoint.render.
    let endpoint method' path description = Endpoint.create method' path |> Endpoint.withDescription description |> Endpoint.render

    /// Compact parameter data for short reference tables. Prefer Parameter for operation parameters.
    let parameter name typeName required description : DocsParameter =
        { name = name; typeName = typeName; required = required; description = description }

    let parameters values =
        div { _class "docs-parameters"; for parameter in values do ApiReferenceView.compactParameter parameter }

    let codeExample = ApiReferenceView.codeExample

    let responseExample (status:string) (language:string) (source:string) =
        section {
            _class "docs-response-panel"
            div { _class "docs-response-status"; span { "Response" }; code { status } }
            ApiReferenceView.codeExample status language source
        }

    /// Compatibility render helper. Prefer Operation.create with its named modifiers and Operation.render.
    let operation method' path description authentication parameters responses errors idempotency apiVersion deprecated =
        let operation =
            Operation.create method' path
            |> Operation.withDescription description
            |> Operation.withParameters parameters
            |> Operation.withResponses responses
            |> Operation.withErrors errors
        let operation = authentication |> Option.map (fun value -> Operation.withAuthentication value operation) |> Option.defaultValue operation
        let operation = idempotency |> Option.map (fun value -> Operation.withIdempotency value operation) |> Option.defaultValue operation
        let operation = apiVersion |> Option.map (fun value -> Operation.withApiVersion value operation) |> Option.defaultValue operation
        let operation = if deprecated then Operation.deprecated operation else operation
        Operation.render operation
