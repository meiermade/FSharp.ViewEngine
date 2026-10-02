namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>error-summary</category>
[<NoEquality; NoComparison>]
type FieldError = private { fieldId:string; label:string; message:string }

/// <category>error-summary</category>
[<RequireQualifiedAccess>]
module FieldError =
    let create fieldId label message =
        { fieldId = TextField.stableId fieldId
          label = TextField.requiredText (nameof label) label
          message = TextField.requiredText (nameof message) message }

/// <category>error-summary</category>
[<NoEquality; NoComparison>]
type ErrorSummaryConfig = private { id:string; title:string; errors:FieldError list; focusOnMount:bool }

/// <category>error-summary</category>
[<RequireQualifiedAccess>]
module ErrorSummary =
    let create id title errors =
        if List.isEmpty errors then invalidArg (nameof errors) "An error summary needs at least one error."
        { id = TextField.stableId id; title = TextField.requiredText (nameof title) title; errors = errors; focusOnMount = false }
    /// Focus a newly inserted summary after a server validation response. Does not continually steal focus.
    let focusOnMount (config:ErrorSummaryConfig) = { config with focusOnMount = true }
    let render (config:ErrorSummaryConfig) =
        div {
            _id config.id
            _role "alert"
            _tabindex -1
            _ariaLabelledby (config.id + "-title")
            _class "[overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] border border-[var(--fve-critical-ring)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-critical-ring)]"
            if config.focusOnMount then _dataInit "el.focus()"
            p {
                _id (config.id + "-title")
                _class "font-semibold"
                config.title
            }
            ul {
                _class "mt-2 list-disc space-y-1 pl-5"
                for error in config.errors do
                    li {
                        a {
                            _href ("#" + Uri.EscapeDataString error.fieldId)
                            _class "underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-offset-2"
                            _dataOn ("click", $"document.getElementById({ComponentHtml.javascriptString error.fieldId})?.focus()")
                            error.label + ": " + error.message
                        }
                    }
            }
        }
