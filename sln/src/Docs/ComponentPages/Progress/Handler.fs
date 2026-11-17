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
open Docs.ComponentPages.Progress.View

module Handler =
    let progressExampleHandler: HttpHandler =
        fun _ context ->
            task {
                let datastar = context.RequestServices.GetRequiredService<IDatastarService>()

                try
                    do! datastar.PatchSignalsAsync({| components_progress_done = false |}, context.RequestAborted)

                    for percentage in 0..10..100 do
                        // This short delay simulates work; real work supplies the completed amount.
                        if percentage > 0 then
                            do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)

                        do!
                            datastar.PatchElementsAsync(
                                progressExampleRegion percentage |> Render.toString,
                                cancellationToken = context.RequestAborted
                            )

                    do! datastar.PatchSignalsAsync({| components_progress_done = true |}, context.RequestAborted)
                with :? OperationCanceledException when context.RequestAborted.IsCancellationRequested ->
                    ()

                return Some context
            }

    let progressExampleRoutes: HttpHandler =
        Giraffe.Core.POST
        >=> route "/components/progress/run"
        >=> progressExampleHandler

    let post : HttpHandler = choose [progressExampleRoutes]
