namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type MediaAsset<'destination> =
    private
        { id:string
          name:string
          source:string
          altText:string
          detail:string option
          primary:bool
          destination:'destination
          disabled:bool }

[<RequireQualifiedAccess>]
module MediaAsset =
    let create id name source altText destination =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable media ID is required."
        if String.IsNullOrWhiteSpace name || String.IsNullOrWhiteSpace source then invalidArg (nameof name) "Media name and source are required."
        { id = id; name = name; source = source; altText = altText; detail = None; primary = false; destination = destination; disabled = false }
    let withDetail detail (asset:MediaAsset<'destination>) = { asset with detail = Some detail }
    let primary (asset:MediaAsset<'destination>) = { asset with primary = true }
    let disabled (asset:MediaAsset<'destination>) = { asset with disabled = true }

[<NoEquality; NoComparison>]
type MediaLibraryConfig<'destination> =
    private
        { id:string
          label:string
          formName:string
          assets:MediaAsset<'destination> list
          selected:Set<string>
          emptyState:HtmlElement }

[<RequireQualifiedAccess>]
module MediaLibrary =
    let create id label formName (assets:MediaAsset<'destination> list) =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable media-library ID is required."
        if String.IsNullOrWhiteSpace label || String.IsNullOrWhiteSpace formName then invalidArg (nameof label) "Media-library label and form name are required."
        let keys = assets |> List.map _.id
        if List.distinct keys |> List.length <> keys.Length then invalidArg (nameof assets) "Media IDs must be unique."
        { id = id; label = label; formName = formName; assets = assets; selected = Set.empty
          emptyState =
            p {
                _class "p-4 text-sm text-[var(--fve-muted-text)]"
                "No media assets."
            } }
    let withSelected ids (config:MediaLibraryConfig<'destination>) = { config with selected = Set.ofList ids }
    let withEmptyState emptyState (config:MediaLibraryConfig<'destination>) = { config with emptyState = emptyState }
    let render resolve (config:MediaLibraryConfig<'destination>) =
        let signalName = ComponentHtml.signalToken (config.id + "-selected")
        let selected = "$" + signalName
        let eligible = config.assets |> List.filter (fun asset -> not asset.disabled) |> List.map _.id
        let initial = eligible |> List.filter config.selected.Contains
        let notify = "el.closest('[data-fve-media-library]').dispatchEvent(new CustomEvent('fve-selection-change', { bubbles: true, detail: { keys: Array.from(" + selected + ") } }))"
        section {
            _id config.id
            _attr ("data-fve-media-library", "true")
            _ariaLabel config.label
            _dataSignals ("{" + signalName + ": " + ComponentHtml.javascriptString initial + "}")
            _dataOn ("fve-selection-clear", selected + " = []; " + notify)
            _class "grid gap-3"
            header {
                _class "flex items-center justify-between gap-3"
                h2 {
                    _class "text-base font-semibold text-[var(--fve-text)]"
                    config.label
                }
                output {
                    _role "status"
                    _ariaLive "polite"
                    _dataText (selected + ".length + ' selected'")
                    _class "text-sm text-[var(--fve-muted-text)]"
                    string initial.Length + " selected"
                }
            }
            if List.isEmpty config.assets then config.emptyState
            else
                ul {
                    _class "grid gap-3 sm:grid-cols-2 lg:grid-cols-3"
                    for asset in config.assets do
                        let key = ComponentHtml.javascriptString asset.id
                        let checkedExpression = selected + ".includes(" + key + ")"
                        li {
                            _dataAttr ("data-selected", checkedExpression + " ? 'true' : 'false'")
                            _class "group relative overflow-hidden rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)] data-[selected=true]:ring-2 data-[selected=true]:ring-[var(--fve-brand-ring)]"
                            img {
                                _src asset.source
                                _alt asset.altText
                                _class "aspect-video w-full object-cover"
                            }
                            div {
                                _class "grid gap-2 p-3"
                                div {
                                    _class "flex min-w-0 items-start justify-between gap-2"
                                    div {
                                        _class "min-w-0"
                                        a {
                                            _href (resolve asset.destination)
                                            _class "font-semibold text-[var(--fve-brand-text)] hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                            asset.name
                                        }
                                        match asset.detail with
                                        | Some detail ->
                                            p {
                                                _class "text-sm text-[var(--fve-muted-text)]"
                                                detail
                                            }
                                        | None -> ()
                                    }
                                    if asset.primary then
                                        Badge.create "Primary"
                                        |> Badge.withTone
                                            Tone.Positive
                                        |> Badge.render
                                }
                                label {
                                    _class "flex cursor-pointer items-center gap-2 text-sm has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                                    input {
                                        _type "checkbox"
                                        _name config.formName
                                        _value asset.id
                                        _checked (config.selected.Contains asset.id)
                                        _disabled asset.disabled
                                        _dataEffect ("el.checked = " + checkedExpression)
                                        _dataOn ("change", selected + " = el.checked ? [..." + selected + ".filter(key => key != " + key + "), " + key + "] : " + selected + ".filter(key => key != " + key + "); " + notify)
                                        _class "size-4 accent-[var(--fve-brand-solid)]"
                                    }
                                    "Select " + asset.name
                                }
                            }
                        }
                }
        }
