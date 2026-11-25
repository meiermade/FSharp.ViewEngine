namespace Docs.ComponentPages.Progress

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let progressRegistration =
        registration "components-progress" "/components/progress" "Progress" "Progress"

    let progressExampleRegion percentage =
        fveProgress {
            _id "progress-example-value"
            _ariaLabel "Example task"
            _value (string percentage)
            _max "100"

            if percentage = 100 then
                _state ProgressState.Complete
        }

    let operationalProgressExample =
        div {
            _class "grid gap-4"
            _attr ("data-signals__root__ifmissing", "{components_progress_pending: false, components_progress_done: false, components_progress_failed: false}")
            progressExampleRegion 0

            // The trigger stays mounted while the server patches the progress region.
            fveButton {
                _id "progress-example-trigger"
                _class "justify-self-start"
                _color Color.Primary
                _variant ButtonVariant.Solid
                _dataAttr ("disabled", "$components_progress_pending")
                _dataAttr ("aria-busy", "$components_progress_pending ? 'true' : null")

                _dataOn (
                    "click",
                    "if (!$components_progress_pending) { $components_progress_pending = true; $components_progress_failed = false; @post('/components/progress/run', {filterSignals: {exclude: /.*/}, retry: 'never'}).catch(() => { $components_progress_failed = true }).finally(() => { $components_progress_pending = false }) }"
                )

                span {
                    _dataText
                        "$components_progress_pending ? 'Running…' : $components_progress_done ? 'Run again' : 'Run example'"

                    "Run example"
                }
            }
            p {
                _hidden true
                _role "alert"
                _dataAttr ("hidden", "!$components_progress_failed")
                "Progress could not be loaded. Try again."
            }
        }

    let indeterminateProgressExample =
        fveProgress {
            _ariaLabel "Preparing files"
            _ariaValuetext "Checking source files"

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "No completion percentage is available yet."
            }
        }

    let completedProgressExample =
        fveProgress {
            _ariaLabel "File upload"
            _value "100"
            _max "100"
            _ariaValuetext "100%"
            _state ProgressState.Complete
        }

    let failedProgressExample =
        fveProgress {
            _ariaLabel "File upload"
            _value "68"
            _max "100"
            _ariaValuetext "Stopped at 68%"
            _state ProgressState.Failed

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "The upload stopped. Try again."
            }
        }

    let examples () =
            [ serverSample
                  "progress-active"
                  "Default"
                  [ "progressExampleRegion"
                    "operationalProgressExample"
                    "progressExampleHandler"
                    "progressExampleRoutes" ]
                  (fieldSurface operationalProgressExample)
              |> note
                  "Run a short example task. One SSE response updates the native progress bar through completion. The copied view, handler and routes use services.AddDatastar(); the delay simulates work."
              sample
                  "progress-indeterminate"
                  "Indeterminate"
                  [ "indeterminateProgressExample" ]
                  (fieldSurface indeterminateProgressExample)
              sample
                  "progress-complete"
                  "Complete"
                  [ "completedProgressExample" ]
                  (fieldSurface completedProgressExample)
              sample "progress-failed" "Failed" [ "failedProgressExample" ] (fieldSurface failedProgressExample) ]


    let private galleryContent : Content =
        { description = "Communicate determinate, indeterminate, complete, and failed progress."
          usage = []
          sections = []
          apiCategory = Some "progress" }

    let progressPage = gallery progressRegistration galleryContent (examples ())
