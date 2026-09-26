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
        | GET -> "bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-200"
        | POST -> "bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-200"
        | PUT -> "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-200"
        | PATCH -> "bg-orange-100 text-orange-800 dark:bg-orange-950 dark:text-orange-200"
        | DELETE -> "bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-200"
        | OPTIONS
        | HEAD -> "bg-[var(--fve-surface-hover)] text-[var(--fve-muted-text)]"

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

module private ApiStyles =
    let code =
        "m-0 overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-neutral-subtle)] p-4 font-mono text-sm leading-[1.55] text-[var(--fve-text)] shadow-sm [&_.token.comment]:text-[var(--fve-muted-text)] [&_.token.prolog]:text-[var(--fve-muted-text)] [&_.token.doctype]:text-[var(--fve-muted-text)] [&_.token.cdata]:text-[var(--fve-muted-text)] [&_.token.punctuation]:text-[var(--fve-text)] [&_.token.deleted]:text-red-700 dark:[&_.token.deleted]:text-red-300 [&_.token.property]:text-red-700 dark:[&_.token.property]:text-red-300 [&_.token.tag]:text-red-700 dark:[&_.token.tag]:text-red-300 [&_.token.boolean]:text-blue-700 dark:[&_.token.boolean]:text-blue-300 [&_.token.constant]:text-blue-700 dark:[&_.token.constant]:text-blue-300 [&_.token.number]:text-blue-700 dark:[&_.token.number]:text-blue-300 [&_.token.symbol]:text-blue-700 dark:[&_.token.symbol]:text-blue-300 [&_.token.attr-name]:text-green-800 dark:[&_.token.attr-name]:text-green-300 [&_.token.builtin]:text-green-800 dark:[&_.token.builtin]:text-green-300 [&_.token.string]:text-green-800 dark:[&_.token.string]:text-green-300 [&_.token.operator]:text-cyan-800 dark:[&_.token.operator]:text-cyan-200 [&_.token.keyword]:text-red-700 dark:[&_.token.keyword]:text-red-300 [&_.token.class-name]:text-purple-700 dark:[&_.token.class-name]:text-purple-300 [&_.token.function]:text-purple-700 dark:[&_.token.function]:text-purple-300 [&_.token.variable]:text-orange-800 dark:[&_.token.variable]:text-orange-300 [&_.token.important]:font-bold [&_.token.bold]:font-bold [&_.token.italic]:italic"
    let panel = "overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-neutral-subtle)] text-[var(--fve-text)]"
    let panelHeader = "flex items-center justify-between border-b border-[var(--fve-border)] px-3.5 py-3 text-xs font-semibold text-[var(--fve-text)]"
    let parameter = "py-4 [&+&]:border-t [&+&]:border-[var(--fve-border)]"
    let parameterDescription = "mt-2 text-sm leading-relaxed text-[var(--fve-muted-text)]"

module internal CopyableCode =
    let renderButton (label:string) =
        button {
            _type "button"
            _ariaLabel label
            _title label
            _class "group absolute top-2 right-2 z-2 grid size-8 min-w-8 cursor-pointer place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] p-0 text-xs leading-4 whitespace-nowrap text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] data-[copy-error=true]:border-red-600 data-[copy-error=true]:text-red-700 dark:data-[copy-error=true]:text-red-300"
            _data("on:click", "window.fsharpDocsCopy(evt.currentTarget)")
            raw """<svg class="size-4 group-data-[copied=true]:hidden group-data-[copy-error=true]:hidden" data-docs-copy-icon="copy" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M5.5 2.75A2.75 2.75 0 0 0 2.75 5.5v7A2.75 2.75 0 0 0 5.5 15.25h1.75a.75.75 0 0 0 0-1.5H5.5c-.69 0-1.25-.56-1.25-1.25v-7c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v1.75a.75.75 0 0 0 1.5 0V5.5a2.75 2.75 0 0 0-2.75-2.75h-5Z"/><path d="M9.5 8.25A2.75 2.75 0 0 0 6.75 11v4.5a2.75 2.75 0 0 0 2.75 2.75h5A2.75 2.75 0 0 0 17.25 15.5V11a2.75 2.75 0 0 0-2.75-2.75h-5Zm-1.25 2.75c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v4.5c0 .69-.56 1.25-1.25 1.25h-5c-.69 0-1.25-.56-1.25-1.25V11Z"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copied=true]:block" data-docs-copy-icon="success" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.051l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.479-9.816a.75.75 0 0 1 1.051-.143Z" clip-rule="evenodd"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copy-error=true]:block" data-docs-copy-icon="error" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm0-12.75a.75.75 0 0 1 .75.75v4a.75.75 0 0 1-1.5 0V6a.75.75 0 0 1 .75-.75ZM10 14a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z" clip-rule="evenodd"/></svg>"""
            span {
                _data("docs-copy-label", "true")
                _class "sr-only"
                _role "status"
            }
        }

    let render (className:string) (codeClass:string) (label:string) (source:string) =
        div {
            _class "relative min-w-0"
            _data("docs-copyable-code", "true")
            renderButton $"Copy {label}"
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
        CopyableCode.render ($"{ApiStyles.code} language-{prismLanguage}") ($"language-{prismLanguage}") "code" block.source

module private ApiReferenceView =
    let endpoint (endpoint:DocsEndpoint) =
        let methodName = DocsHttpMethod.value endpoint.method'
        div {
            _class "border-b border-[var(--fve-border)] pb-4"
            _data("http-method", methodName)
            div {
                _class "flex items-center gap-3"
                span { _class $"inline-flex rounded-md px-2 py-1 text-xs font-bold tracking-wide {DocsHttpMethod.className endpoint.method'}"; methodName }
                code { _class "overflow-wrap-anywhere text-sm font-semibold text-[var(--fve-text)]"; endpoint.path }
            }
            match endpoint.description with
            | Some description -> p { _class "mt-3 text-sm leading-relaxed text-[var(--fve-muted-text)]"; description }
            | None -> ()
        }

    let compactParameter (parameter:DocsParameter) =
        div {
            _class ApiStyles.parameter
            div {
                _class "flex items-center gap-2"
                code { _class "text-sm font-bold text-[var(--fve-text)]"; parameter.name }
                span { _class "text-xs text-[var(--fve-muted-text)]"; parameter.typeName }
                if parameter.required then span { _class "rounded-full bg-red-50 px-1.5 py-0.5 text-xs font-bold uppercase text-red-700 dark:bg-red-950 dark:text-red-200"; "Required" }
            }
            p { _class ApiStyles.parameterDescription; parameter.description }
        }

    let parameter (parameter:DocsApiParameter) =
        div {
            _class ApiStyles.parameter
            _data("parameter-location", DocsParameterLocation.value parameter.location)
            div {
                _class "flex items-center gap-2"
                code { _class "text-sm font-bold text-[var(--fve-text)]"; parameter.name }
                span { _class "text-xs text-[var(--fve-muted-text)]"; parameter.typeName }
                span { _class "text-xs text-[var(--fve-muted-text)]"; DocsParameterLocation.value parameter.location }
                if parameter.required then span { _class "rounded-full bg-red-50 px-1.5 py-0.5 text-xs font-bold uppercase text-red-700 dark:bg-red-950 dark:text-red-200"; "Required" }
            }
            match parameter.description with
            | Some description -> p { _class ApiStyles.parameterDescription; description }
            | None -> ()
            match parameter.defaultValue with
            | Some value -> p { _class ApiStyles.parameterDescription; "Default: "; code { value } }
            | None -> ()
            if not parameter.enumValues.IsEmpty then
                p { _class ApiStyles.parameterDescription; "Values: "; String.concat ", " parameter.enumValues }
            match parameter.example with
            | Some value -> p { _class ApiStyles.parameterDescription; "Example: "; code { value } }
            | None -> ()
        }

    let codeExample (title:string) (language:string) (source:string) =
        let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        section {
            _class ApiStyles.panel
            _data("docs-code-panel", "true")
            div { _class ApiStyles.panelHeader; span { title }; span { _class "text-[var(--fve-muted-text)] uppercase"; normalized } }
            CopyableCode.render ($"{ApiStyles.code} rounded-none border-0 shadow-none language-{prismLanguage}") ($"language-{prismLanguage}") title source
        }

    let response (response:DocsApiResponse) =
        section {
            _class ApiStyles.panel
            div {
                _class ApiStyles.panelHeader
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
            _class "flex flex-col gap-4"
            Endpoint.render operation.endpoint
            if operation.deprecated then span { _class "inline-flex w-fit rounded-full bg-amber-100 px-2 py-1 text-xs font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-200"; "Deprecated" }
            match operation.authentication with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3 [&>p]:mt-1 [&>p]:text-sm [&>p]:text-[var(--fve-muted-text)]"; strong { "Authentication" }; p { value } }
            | None -> ()
            match operation.apiVersion with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3 [&>code]:mt-1 [&>code]:block [&>code]:text-sm [&>code]:text-[var(--fve-muted-text)]"; strong { "API version" }; code { value } }
            | None -> ()
            match operation.idempotency with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3 [&>p]:mt-1 [&>p]:text-sm [&>p]:text-[var(--fve-muted-text)]"; strong { "Idempotency" }; p { value } }
            | None -> ()
            if not operation.parameters.IsEmpty then
                div { _class "overflow-hidden border-y border-[var(--fve-border)]"; for parameter in operation.parameters do Parameter.render parameter }
            if not operation.responses.IsEmpty then
                div { _class "flex flex-col gap-3"; for response in operation.responses do Response.render response }
            if not operation.errors.IsEmpty then
                div {
                    _class "overflow-hidden border-y border-[var(--fve-border)]"
                    for error in operation.errors do
                        div { _class ApiStyles.parameter; code { _class "text-sm font-bold text-[var(--fve-text)]"; error.code }; p { _class ApiStyles.parameterDescription; error.description } }
                }
        }

module ApiReference =
    /// Compatibility render helper. Prefer Endpoint.create |> Endpoint.withDescription |> Endpoint.render.
    let endpoint method' path description = Endpoint.create method' path |> Endpoint.withDescription description |> Endpoint.render

    /// Compact parameter data for short reference tables. Prefer Parameter for operation parameters.
    let parameter name typeName required description : DocsParameter =
        { name = name; typeName = typeName; required = required; description = description }

    let parameters values =
        div { _class "overflow-hidden border-y border-[var(--fve-border)]"; for parameter in values do ApiReferenceView.compactParameter parameter }

    let codeExample = ApiReferenceView.codeExample

    let responseExample (status:string) (language:string) (source:string) =
        section {
            _class ApiStyles.panel
            div { _class ApiStyles.panelHeader; span { "Response" }; code { status } }
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
