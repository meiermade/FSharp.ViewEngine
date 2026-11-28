namespace Example.Specification.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html

open Presentation

module internal Application =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "requirements", "Requirements" ]

    let render () =
        contracts
            "Operations"
            "Example.Application/UseCases/Operations.fs validates account, organization, environment and profile submissions, prepares deletion confirmations and returns updated immutable workspace snapshots."
            [ "Example.Domain." ]
            "// validateAccount : ValidateAccountRequest -> Result<ValidateAccountResponse, ValidateAccountError>\n// prepareDeletion : PrepareDeletionRequest -> Result<PrepareDeletionResponse, PrepareDeletionError>\n// deleteSelection : DeleteSelectionRequest -> Result<DeleteSelectionResponse, DeleteSelectionError>\n// validateOrganization : ValidateOrganizationRequest -> Result<ValidateOrganizationResponse, ValidateOrganizationError>\n// validateEnvironment : ValidateEnvironmentRequest -> Result<ValidateEnvironmentResponse, ValidateEnvironmentError>\n// validateProfile : ValidateProfileRequest -> Result<ValidateProfileResponse, ValidateProfileError>"
            [ "Operations SHALL return typed success or validation errors without HTTP status codes or rendered HTML."
              "Account, organization, environment and profile names SHALL contain 1–80 characters. Account names SHALL be unique ignoring case, excluding the account being updated."
              "Account type SHALL be a supported type root and its parent SHALL match that root. This example accepts USD, Generic subtype and no required month-end observation; organization reporting commodity SHALL be USD."
              "Profile validation SHALL require a valid email, an available time zone and a local HH:mm daily summary time in 15-minute intervals. Server validation SHALL apply independently of native form validation."
              "Deletion SHALL reject empty, unknown or blocked selections atomically and return an updated snapshot rather than mutate shared fixtures."
              "Validation SHALL neither mutate seeded data nor retain submitted values." ]
