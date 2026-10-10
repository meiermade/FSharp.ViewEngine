namespace Docs.Web

open FSharp.ViewEngine.Components.Templates

/// Adapt the consumer-owned navigation boundary to the catalog's retained shell.
module Navigation =
    let enhancement : DocsNavigationEnhancement =
        let shared = Docs.Examples.Navigation.enhancement
        { initialScript = shared.initialScript
          clickAction = shared.clickAction
          submitAction = shared.submitAction
          restoreAction = shared.restoreAction
          fetchLifecycle = shared.fetchLifecycle
          appModeExitAction = shared.appModeExitAction }

    let tryIntent = Docs.Examples.Navigation.tryIntent
    let respond = Docs.Examples.Navigation.respond
