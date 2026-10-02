namespace Ledger

open System
open Domain

/// Bounded application operations validate submissions without changing or retaining fixture data.
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
    type ReviewSelectionRequest = { resource:SelectionResource; keys:string list }
    type ReviewSelectionResponse = { count:int }
    [<RequireQualifiedAccess>]
    type ReviewSelectionError = EmptySelection | UnknownResource
    let reviewSelection (request:ReviewSelectionRequest) : Result<ReviewSelectionResponse,ReviewSelectionError> =
        let keys = request.keys |> List.distinct
        let available key =
            match request.resource with
            | SelectionResource.Accounts -> (accounts |> List.exists (fun row -> string row.id=key)) || (accountTypes |> List.exists (fun kind -> key="group-"+string kind))
            | SelectionResource.Transactions -> transactions |> List.exists (fun row -> string row.id=key)
        if keys.IsEmpty then Error ReviewSelectionError.EmptySelection
        elif keys |> List.exists (available >> not) then Error ReviewSelectionError.UnknownResource
        else Ok {count=keys.Length}

    type ValidateDeletionRequest = { accountId:int }
    type ValidateDeletionResponse = { accountId:int }
    [<RequireQualifiedAccess>]
    type ValidateDeletionError = NotFound | HasActivity
    let validateDeletion (request:ValidateDeletionRequest) : Result<ValidateDeletionResponse,ValidateDeletionError> =
        match tryAccount request.accountId with
        | None -> Error ValidateDeletionError.NotFound
        | Some _ when not (canDeleteAccount request.accountId) -> Error ValidateDeletionError.HasActivity
        | Some _ -> Ok {accountId=request.accountId}

    type ValidateOrganizationRequest = { name:string; currency:string }
    type ValidateOrganizationResponse = { accepted:bool }
    [<RequireQualifiedAccess>]
    type ValidateOrganizationError = InvalidOrganization
    let validateOrganization (request:ValidateOrganizationRequest) : Result<ValidateOrganizationResponse,ValidateOrganizationError> =
        if request.name.Trim().Length<1 || request.name.Trim().Length>80 || request.currency<>"USD" then Error ValidateOrganizationError.InvalidOrganization
        else Ok {accepted=true}

    type ValidateProfileRequest = { name:string; email:string; timeZone:string }
    type ValidateProfileResponse = { accepted:bool }
    [<RequireQualifiedAccess>]
    type ValidateProfileError = InvalidProfile
    let validateProfile (request:ValidateProfileRequest) : Result<ValidateProfileResponse,ValidateProfileError> =
        let validEmail =
            match System.Net.Mail.MailAddress.TryCreate(request.email) with
            | true,address -> address.Address=request.email && address.Address.Contains('@')
            | _ -> false
        if request.name.Trim().Length<1 || request.name.Trim().Length>80 || not validEmail || not (List.contains request.timeZone ["America/Chicago";"America/New_York";"UTC"]) then Error ValidateProfileError.InvalidProfile
        else Ok {accepted=true}
