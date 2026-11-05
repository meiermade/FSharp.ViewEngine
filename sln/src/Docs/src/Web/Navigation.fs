namespace Docs.Web

open FSharp.ViewEngine.Components.Templates

/// Adapt the consumer-owned navigation boundary to the catalog's retained shell.
module Navigation =
    let enhancement : DocsNavigationEnhancement =
        let shared = Example.Server.Navigation.enhancement
        { initialScript = shared.initialScript
          clickAction = shared.clickAction
          submitAction = shared.submitAction
          restoreAction = shared.restoreAction
          fetchLifecycle = shared.fetchLifecycle
          appModeExitAction = shared.appModeExitAction }

    let tryIntent = Example.Server.Navigation.tryIntent
    let respond = Example.Server.Navigation.respond
