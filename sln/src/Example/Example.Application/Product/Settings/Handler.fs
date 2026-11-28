module Example.Application.Product.Settings.Handler

open Example.UI.Views.Model
open Ledger.Domain

let tryPage = function
    | ["settings"] -> Some ApplicationPage.Settings
    | ["settings"; "general"; "edit"] -> Some ApplicationPage.EditOrganization
    | ["settings"; "environments"; id] when tryEnvironment id |> Option.isSome -> Some(ApplicationPage.Environment id)
    | ["settings"; "environments"; id; "edit"] when tryEnvironment id |> Option.isSome -> Some(ApplicationPage.EditEnvironment id)
    | ["settings"; key] when settingsSections |> List.exists (fst >> (=) key) -> Some(ApplicationPage.SettingsSection key)
    | _ -> None
