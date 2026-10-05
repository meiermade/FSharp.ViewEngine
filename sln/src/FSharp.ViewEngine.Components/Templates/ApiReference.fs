namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

open System
open System.Reflection
open System.Security.Cryptography
open System.Text
open System.Xml.Linq
open Microsoft.FSharp.Reflection
open FSharp.ViewEngine
open type Html

/// <category>api-reference</category>
type DocsHttpMethod =
    | GET
    | POST
    | PUT
    | PATCH
    | DELETE
    | OPTIONS
    | HEAD

/// <category>api-reference</category>
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

/// <category>api-reference</category>
[<NoEquality; NoComparison>]
type ApiEndpointConfig =
    private
        { method':DocsHttpMethod
          path:string
          description:string option }

/// <category>api-reference</category>
type DocsParameter =
    { name:string
      typeName:string
      required:bool
      description:string }

/// <category>api-reference</category>
type DocsParameterLocation =
    | Path
    | Query
    | Header
    | Body

/// <category>api-reference</category>
module DocsParameterLocation =
    let value = function Path -> "path" | Query -> "query" | Header -> "header" | Body -> "body"

/// <category>api-reference</category>
[<NoEquality; NoComparison>]
type ApiParameterConfig =
    private
        { name:string
          typeName:string
          location:DocsParameterLocation
          required:bool
          defaultValue:string option
          enumValues:string list
          example:string option
          description:string option }

/// <category>api-reference</category>
[<NoEquality; NoComparison>]
type ApiResponseConfig =
    private
        { status:string
          description:string option
          language:string option
          example:string option }

/// <category>api-reference</category>
[<NoEquality; NoComparison>]
type ApiErrorConfig =
    private
        { code:string
          description:string }

/// <category>api-reference</category>
[<NoEquality; NoComparison>]
type ApiOperationConfig =
    private
        { endpoint:ApiEndpointConfig
          authentication:string option
          parameters:ApiParameterConfig list
          responses:ApiResponseConfig list
          errors:ApiErrorConfig list
          idempotency:string option
          apiVersion:string option
          deprecated:bool }

module private ApiStyles =
    let code =
        "m-0! overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-docs-code-surface,var(--fve-background))]! p-4 font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! shadow-sm"
    let panel = "overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-neutral-subtle)] text-[var(--fve-text)]"
    let panelHeader = "flex items-center justify-between border-b border-[var(--fve-border)] px-3.5 py-3 text-xs font-semibold text-[var(--fve-text)]"
    let parameter = "py-4 [&+&]:border-t [&+&]:border-[var(--fve-border)]"
    let parameterDescription = "mt-2 text-sm leading-relaxed text-[var(--fve-muted-text)]"

module private ApiReferenceView =
    let endpoint (endpoint:ApiEndpointConfig) =
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

    let parameter (parameter:ApiParameterConfig) =
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
            CopyableCode.render ($"{ApiStyles.code} rounded-none border-0 shadow-none language-{prismLanguage}") ($"bg-transparent! font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! language-{prismLanguage}") title source
        }

    let response (response:ApiResponseConfig) =
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


/// <category>api-reference</category>
[<RequireQualifiedAccess>]
module ApiEndpoint =
    let create method' path : ApiEndpointConfig =
        if String.IsNullOrWhiteSpace path || not (path.StartsWith('/')) then
            invalidArg (nameof path) "An endpoint requires an absolute API path."
        { method' = method'; path = path; description = None }

    let withDescription description (endpoint:ApiEndpointConfig) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An endpoint description cannot be empty."
        { endpoint with description = Some description }

    let render (endpoint:ApiEndpointConfig) = ApiReferenceView.endpoint endpoint

/// <category>api-reference</category>
[<RequireQualifiedAccess>]
module ApiParameter =
    let create name typeName location : ApiParameterConfig =
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

    let required (parameter:ApiParameterConfig) = { parameter with required = true }
    let withDescription description (parameter:ApiParameterConfig) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API parameter description cannot be empty."
        { parameter with description = Some description }
    let withDefaultValue value (parameter:ApiParameterConfig) =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "An API parameter default cannot be empty."
        { parameter with defaultValue = Some value }
    let withEnumValues values (parameter:ApiParameterConfig) =
        if List.isEmpty values || values |> List.exists String.IsNullOrWhiteSpace then
            invalidArg (nameof values) "API parameter values must be non-empty."
        { parameter with enumValues = values }
    let withExample value (parameter:ApiParameterConfig) =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "An API parameter example cannot be empty."
        { parameter with example = Some value }
    let render (parameter:ApiParameterConfig) = ApiReferenceView.parameter parameter

/// <category>api-reference</category>
[<RequireQualifiedAccess>]
module ApiResponse =
    let create status : ApiResponseConfig =
        if String.IsNullOrWhiteSpace status then invalidArg (nameof status) "An API response status is required."
        { status = status; description = None; language = None; example = None }

    let withDescription description (response:ApiResponseConfig) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API response description cannot be empty."
        { response with description = Some description }
    let withExample language source (response:ApiResponseConfig) =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "An API response example cannot be empty."
        { response with language = Some language; example = Some source }
    let render (response:ApiResponseConfig) = ApiReferenceView.response response

/// <category>api-reference</category>
[<RequireQualifiedAccess>]
module ApiError =
    let create code description : ApiErrorConfig =
        if String.IsNullOrWhiteSpace code then invalidArg (nameof code) "An API error code is required."
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An API error description is required."
        { code = code; description = description }

/// <category>api-reference</category>
[<RequireQualifiedAccess>]
module ApiOperation =
    let create method' path : ApiOperationConfig =
        { endpoint = ApiEndpoint.create method' path
          authentication = None
          parameters = []
          responses = []
          errors = []
          idempotency = None
          apiVersion = None
          deprecated = false }

    let withDescription description (operation:ApiOperationConfig) = { operation with endpoint = operation.endpoint |> ApiEndpoint.withDescription description }
    let withAuthentication authentication (operation:ApiOperationConfig) =
        if String.IsNullOrWhiteSpace authentication then invalidArg (nameof authentication) "Authentication guidance cannot be empty."
        { operation with authentication = Some authentication }
    let withParameters parameters (operation:ApiOperationConfig) = { operation with parameters = parameters }
    let withResponses responses (operation:ApiOperationConfig) = { operation with responses = responses }
    let withErrors errors (operation:ApiOperationConfig) = { operation with errors = errors }
    let withIdempotency guidance (operation:ApiOperationConfig) =
        if String.IsNullOrWhiteSpace guidance then invalidArg (nameof guidance) "Idempotency guidance cannot be empty."
        { operation with idempotency = Some guidance }
    let withApiVersion version (operation:ApiOperationConfig) =
        if String.IsNullOrWhiteSpace version then invalidArg (nameof version) "An API version cannot be empty."
        { operation with apiVersion = Some version }
    let deprecated (operation:ApiOperationConfig) = { operation with deprecated = true }

    let render (operation:ApiOperationConfig) =
        section {
            _class "flex flex-col gap-4"
            ApiEndpoint.render operation.endpoint
            if operation.deprecated then span { _class "inline-flex w-fit rounded-full bg-amber-100 px-2 py-1 text-xs font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-200"; "Deprecated" }
            match operation.authentication with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3"; strong { "Authentication" }; p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; value } }
            | None -> ()
            match operation.apiVersion with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3"; strong { "API version" }; code { _class "mt-1 block text-sm text-[var(--fve-muted-text)]"; value } }
            | None -> ()
            match operation.idempotency with
            | Some value -> div { _class "border-l-2 border-[var(--fve-border)] pl-3"; strong { "Idempotency" }; p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; value } }
            | None -> ()
            if not operation.parameters.IsEmpty then
                div { _class "overflow-hidden border-y border-[var(--fve-border)]"; for parameter in operation.parameters do ApiParameter.render parameter }
            if not operation.responses.IsEmpty then
                div { _class "flex flex-col gap-3"; for response in operation.responses do ApiResponse.render response }
            if not operation.errors.IsEmpty then
                div {
                    _class "overflow-hidden border-y border-[var(--fve-border)]"
                    for error in operation.errors do
                        div { _class ApiStyles.parameter; code { _class "text-sm font-bold text-[var(--fve-text)]"; error.code }; p { _class ApiStyles.parameterDescription; error.description } }
                }
        }

/// <category>api-reference</category>
module ApiReference =
    /// Compatibility render helper. Prefer ApiEndpoint.create |> ApiEndpoint.withDescription |> ApiEndpoint.render.
    let endpoint method' path description = ApiEndpoint.create method' path |> ApiEndpoint.withDescription description |> ApiEndpoint.render

    /// Compact parameter data for short reference tables. Prefer Parameter for operation parameters.
    let parameter name typeName required description : DocsParameter =
        { name = name; typeName = typeName; required = required; description = description }

    let parameters values =
        div { _class "overflow-hidden border-y border-[var(--fve-border)]"; for parameter in values do ApiReferenceView.compactParameter parameter }

    let codeExample = ApiReferenceView.codeExample

    let responseExample (status:string) (language:string) (source:string) =
        ApiReferenceView.codeExample ("Response " + status) language source

    /// Compatibility render helper. Prefer ApiOperation.create with its named modifiers and ApiOperation.render.
    let operation method' path description authentication parameters responses errors idempotency apiVersion deprecated =
        let operation =
            ApiOperation.create method' path
            |> ApiOperation.withDescription description
            |> ApiOperation.withParameters parameters
            |> ApiOperation.withResponses responses
            |> ApiOperation.withErrors errors
        let operation = authentication |> Option.map (fun value -> ApiOperation.withAuthentication value operation) |> Option.defaultValue operation
        let operation = idempotency |> Option.map (fun value -> ApiOperation.withIdempotency value operation) |> Option.defaultValue operation
        let operation = apiVersion |> Option.map (fun value -> ApiOperation.withApiVersion value operation) |> Option.defaultValue operation
        let operation = if deprecated then ApiOperation.deprecated operation else operation
        ApiOperation.render operation
