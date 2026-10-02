namespace Docs.Examples

open System
open System.Globalization
open Ledger.Domain

/// HTTP destinations, display formatting and per-render context for the financial examples.
module Model =
    let money (value:decimal) = value.ToString("C2", CultureInfo.GetCultureInfo "en-US")
    let date (value:DateOnly) = value.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo "en-US")
    let workspaceQuery (workspace:Workspace) =
        $"organization={Uri.EscapeDataString workspace.organization.id}&environment={Uri.EscapeDataString workspace.environment.id}&ledger={Uri.EscapeDataString workspace.ledger.id}"
    let workspaceUrl path workspace = path+"?"+workspaceQuery workspace
    let settingsSections = ["organizations","Organizations";"environments","Environments";"users","Users";"api-clients","API clients";"ledgers","Ledgers";"billing","Billing"]
    [<RequireQualifiedAccess>]
    type ApplicationPage = Home | Accounts | Account of int | CreateAccount | EditAccount of int | DeleteAccount of int | Transactions | Transaction of int | Settings | SettingsSection of string | Profile
    let applicationUrl = function
        | ApplicationPage.Home -> "/examples/application"
        | ApplicationPage.Accounts -> "/examples/application/accounts"
        | ApplicationPage.Account id -> $"/examples/application/accounts/{id}"
        | ApplicationPage.CreateAccount -> "/examples/application/accounts/new"
        | ApplicationPage.EditAccount id -> $"/examples/application/accounts/{id}/edit"
        | ApplicationPage.DeleteAccount id -> $"/examples/application/accounts/{id}/delete"
        | ApplicationPage.Transactions -> "/examples/application/transactions"
        | ApplicationPage.Transaction id -> $"/examples/application/transactions/{id}"
        | ApplicationPage.Settings -> "/examples/application/settings"
        | ApplicationPage.SettingsSection key -> "/examples/application/settings/"+key
        | ApplicationPage.Profile -> "/examples/application/profile"
    type Query = { search:string; accountType:string; sort:string; state:string; embedded:bool; workspace:Workspace; account:string; range:string; comparison:string; status:string; appMode:bool; specState:string; resource:string; dock:string; specification:bool; previewId:string }
    let defaultQuery = { search=""; accountType="all"; sort="name"; state=""; embedded=false; workspace=defaultWorkspace; account="all"; range="90"; comparison="expected"; status="all"; appMode=false; specState=""; resource=""; dock="bottom"; specification=false; previewId="" }
    let queryFromValues (value:string -> string) =
        let fallback key defaultValue = if value key="" then defaultValue else value key
        { search=value "search"; accountType=fallback "accountType" "all"; sort=fallback "sort" "name"; state=value "state"; embedded=value "embedded"="1"
          workspace=workspaceFromStrings (value "organization") (value "environment") (value "ledger")
          account=fallback "account" "all"; range=fallback "range" "90"; comparison=fallback "comparison" "expected"; status=fallback "status" "all"; appMode=(value "appMode"="1" || value "fveAppMode"="app"); specState=value "specState"; resource=value "resource"; dock=(if value "fveAppDock"="top" then "top" else "bottom"); specification=false; previewId="" }
    let elementId (query:Query) id = if query.previewId="" then id else query.previewId+"-"+id
    let querySuffix pairs = pairs |> List.map (fun (key,value:string) -> "&"+key+"="+Uri.EscapeDataString value) |> String.concat ""
    let specificationDestination page outcome =
        match page with
        | ApplicationPage.Home -> "home","default",""
        | ApplicationPage.Accounts -> "accounts/view-accounts",(if outcome="selection-valid" then "selection" elif outcome="selection-invalid" then "selection-invalid" else "default"),""
        | ApplicationPage.Account id -> "accounts/view-account","default",string id
        | ApplicationPage.EditAccount id -> "accounts/update-account",(if List.contains outcome ["invalid";"invalid-type";"invalid-details";"validated"] then outcome else "default"),string id
        | ApplicationPage.DeleteAccount id -> "accounts/delete-account",(match outcome with "deleted" -> "validated" | "delete-blocked" -> "blocked" | _ -> "default"),string id
        | ApplicationPage.CreateAccount -> "accounts/create-account",(if List.contains outcome ["invalid";"invalid-type";"invalid-details";"validated"] then outcome else "default"),""
        | ApplicationPage.Transactions -> "transactions/view-transactions",(if List.contains outcome ["selection-invalid";"selection-valid"] then outcome else "default"),""
        | ApplicationPage.Transaction id -> "transactions/view-transaction","default",string id
        | ApplicationPage.Settings -> "settings",(if List.contains outcome ["invalid";"validated"] then outcome else "organizations"),""
        | ApplicationPage.SettingsSection key -> "settings",(if key="organizations" && List.contains outcome ["invalid";"validated"] then outcome else key),""
        | ApplicationPage.Profile -> "profile",(if List.contains outcome ["invalid";"validated"] then outcome else "default"),""
    let specificationHref workflow state (query:Query) =
        let pairs =
            [ yield "specState",state
              if query.resource<>"" then yield "resource",query.resource
              if query.appMode then yield "appMode","1"
              yield "fveAppDock",query.dock ]
        workspaceUrl ("/examples/specification"+(if workflow="" then "" else "/"+workflow)) query.workspace + querySuffix pairs
    let applicationHref (query:Query) page =
        if query.specification then
            let workflow,state,resource = specificationDestination page ""
            specificationHref workflow state {query with resource=resource}
        else workspaceUrl (applicationUrl page) query.workspace+(if query.embedded then "&embedded=1" else "")
    let private jsonOptions = System.Text.Json.JsonSerializerOptions(WriteIndented=true)
    let accountPayload (account:Account) =
        System.Text.Json.JsonSerializer.Serialize ({| id=account.id; name=account.name; accountType=string account.accountType; currency=account.currency; balance=balance account |},jsonOptions)
    let transactionPayload (transaction:Transaction) =
        System.Text.Json.JsonSerializer.Serialize ({| id=transaction.id; date=transaction.date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture); description=transaction.description; accountId=transaction.accountId; amount=transaction.amount; status=string transaction.status |},jsonOptions)
