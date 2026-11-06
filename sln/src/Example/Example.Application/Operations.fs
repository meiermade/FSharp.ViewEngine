namespace Ledger

open System
open Domain

/// Application operations validate submissions and return immutable, browser-session record snapshots.
module Operations =
    type ValidateAccountRequest =
        { existingAccountId:int option; name:string; accountType:string; parentType:string
          currency:string; subtype:string; observedBalance:string }
    type ValidateAccountResponse = { existingAccountId:int option }
    [<RequireQualifiedAccess>]
    type ValidateAccountError = InvalidName | DuplicateName | InvalidType | InvalidDetails
    let validateAccount (request:ValidateAccountRequest) : Result<ValidateAccountResponse,ValidateAccountError> =
        let name = request.name.Trim()
        if name.Length<1 || name.Length>80 then Error ValidateAccountError.InvalidName
        elif accounts |> List.exists (fun account -> Some account.id<>request.existingAccountId && account.name.Equals(name,StringComparison.OrdinalIgnoreCase)) then Error ValidateAccountError.DuplicateName
        elif not (accountTypes |> List.exists (fun kind -> string kind=request.accountType)) then Error ValidateAccountError.InvalidType
        elif request.parentType<>request.accountType || request.currency<>"USD" || request.subtype<>"Generic" || request.observedBalance<>"not-required" then Error ValidateAccountError.InvalidDetails
        else Ok {existingAccountId=request.existingAccountId}

    [<RequireQualifiedAccess>]
    type SelectionResource = Accounts | Transactions
    type PrepareDeletionRequest = { records:Records; resource:SelectionResource; keys:string list }
    type PrepareDeletionResponse = { ids:int list; blockedIds:int list }
    [<RequireQualifiedAccess>]
    type PrepareDeletionError = EmptySelection | UnknownRecord
    let prepareDeletion (request:PrepareDeletionRequest) : Result<PrepareDeletionResponse,PrepareDeletionError> =
        let available =
            match request.resource with
            | SelectionResource.Accounts -> request.records.accounts |> List.map _.id
            | SelectionResource.Transactions -> request.records.transactions |> List.map _.id
        let keys = request.keys |> List.distinct
        let ids = keys |> List.choose (fun key -> available |> List.tryFind (fun id -> string id=key))
        if keys.IsEmpty then Error PrepareDeletionError.EmptySelection
        elif ids.Length<>keys.Length then Error PrepareDeletionError.UnknownRecord
        else
            Ok { ids=ids
                 blockedIds=if request.resource=SelectionResource.Accounts then ids |> List.filter (canDeleteAccount request.records >> not) else [] }

    type DeleteSelectionRequest = { records:Records; resource:SelectionResource; keys:string list }
    type DeleteSelectionResponse = { records:Records; count:int }
    [<RequireQualifiedAccess>]
    type DeleteSelectionError = EmptySelection | UnknownRecord | HasActivity
    let deleteSelection (request:DeleteSelectionRequest) : Result<DeleteSelectionResponse,DeleteSelectionError> =
        match prepareDeletion { records=request.records; resource=request.resource; keys=request.keys } with
        | Error PrepareDeletionError.EmptySelection -> Error DeleteSelectionError.EmptySelection
        | Error PrepareDeletionError.UnknownRecord -> Error DeleteSelectionError.UnknownRecord
        | Ok selected when not selected.blockedIds.IsEmpty -> Error DeleteSelectionError.HasActivity
        | Ok selected ->
            let ids = Set.ofList selected.ids
            let records =
                match request.resource with
                | SelectionResource.Accounts -> { request.records with accounts=request.records.accounts |> List.filter (fun row -> not (ids.Contains row.id)) }
                | SelectionResource.Transactions -> { request.records with transactions=request.records.transactions |> List.filter (fun row -> not (ids.Contains row.id)) }
            Ok { records=records; count=ids.Count }

    type ValidateOrganizationRequest = { name:string; currency:string }
    type ValidateOrganizationResponse = { accepted:bool }
    [<RequireQualifiedAccess>]
    type ValidateOrganizationError = InvalidName | InvalidCurrency
    let validateOrganization (request:ValidateOrganizationRequest) : Result<ValidateOrganizationResponse,ValidateOrganizationError> =
        if request.name.Trim().Length<1 || request.name.Trim().Length>80 then Error ValidateOrganizationError.InvalidName
        elif request.currency<>"USD" then Error ValidateOrganizationError.InvalidCurrency
        else Ok {accepted=true}

    type ValidateEnvironmentRequest = { environmentId:string; name:string }
    type ValidateEnvironmentResponse = { environmentId:string }
    [<RequireQualifiedAccess>]
    type ValidateEnvironmentError = NotFound | InvalidName
    let validateEnvironment (request:ValidateEnvironmentRequest) : Result<ValidateEnvironmentResponse,ValidateEnvironmentError> =
        if tryEnvironment request.environmentId |> Option.isNone then Error ValidateEnvironmentError.NotFound
        elif request.name.Trim().Length<1 || request.name.Trim().Length>80 then Error ValidateEnvironmentError.InvalidName
        else Ok {environmentId=request.environmentId}

    type ValidateProfileRequest = { name:string; email:string; timeZone:string }
    type ValidateProfileResponse = { accepted:bool }
    [<RequireQualifiedAccess>]
    type ValidateProfileError = InvalidName | InvalidEmail | InvalidTimeZone
    let validateProfile (request:ValidateProfileRequest) : Result<ValidateProfileResponse,ValidateProfileError> =
        let validEmail =
            match System.Net.Mail.MailAddress.TryCreate(request.email) with
            | true,address -> address.Address=request.email && address.Address.Contains('@')
            | _ -> false
        if request.name.Trim().Length<1 || request.name.Trim().Length>80 then Error ValidateProfileError.InvalidName
        elif not validEmail then Error ValidateProfileError.InvalidEmail
        elif not (List.contains request.timeZone ["America/Chicago";"America/New_York";"UTC"]) then Error ValidateProfileError.InvalidTimeZone
        else Ok {accepted=true}
