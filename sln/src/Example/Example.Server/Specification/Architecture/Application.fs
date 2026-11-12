namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html

open Presentation

module internal Application =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "rules", "Rules" ]

    let render () =
        contracts
            "Operations"
            "Example.Application/Operations.fs validates account, organization, environment and profile submissions, prepares deletion confirmations and returns updated immutable workspace snapshots."
            [ "Example.Domain." ]
            "// validateAccount : ValidateAccountRequest -> Result<ValidateAccountResponse, ValidateAccountError>\n// prepareDeletion : PrepareDeletionRequest -> Result<PrepareDeletionResponse, PrepareDeletionError>\n// deleteSelection : DeleteSelectionRequest -> Result<DeleteSelectionResponse, DeleteSelectionError>\n// validateOrganization : ValidateOrganizationRequest -> Result<ValidateOrganizationResponse, ValidateOrganizationError>\n// validateEnvironment : ValidateEnvironmentRequest -> Result<ValidateEnvironmentResponse, ValidateEnvironmentError>\n// validateProfile : ValidateProfileRequest -> Result<ValidateProfileResponse, ValidateProfileError>"
            [ "Operations SHALL return typed success or validation errors without HTTP status codes or rendered HTML."
              "Account names SHALL contain 1–80 characters and be unique ignoring case."
              "Deletion SHALL reject empty, unknown or blocked selections atomically and return an updated snapshot rather than mutate shared fixtures."
              "Validation SHALL neither mutate seeded data nor retain submitted values." ]
