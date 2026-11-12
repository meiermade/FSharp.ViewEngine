namespace Example.Server

open Example.Components.Views
open Example.Components.Views.Model

/// Both the app and Spec enter here; the host runs use cases and supplies one shared view.
module Application =
    let navigation = Product.navigation
    let title = Product.title
    let pageTitle = Product.pageTitle

    let render page (query: Query) =
        let background = backgroundPage query page
        let query =
            match background with
            | ApplicationPage.Account id -> { query with account = string id }
            | _ -> query
        let home =
            match background with
            | ApplicationPage.Home | ApplicationPage.Account _ ->
                Some(Ledger.Home.readHome
                    { records = query.records
                      account = query.account
                      period = query.range
                      comparison = query.comparison
                      startDate = query.startDate
                      endDate = query.endDate })
            | _ -> None
        let query =
            { query with
                submitAction = Navigation.formSubmitAction
                home = home
                profileValidation =
                    if page = ApplicationPage.EditProfile && query.state = "invalid" then
                        Some(Ledger.Operations.validateProfile (profileRequest query)) else None
                organizationValidation =
                    if page = ApplicationPage.EditOrganization && query.state = "invalid" then
                        Some(Ledger.Operations.validateOrganization (organizationRequest query)) else None
                environmentValidation =
                    match page with
                    | ApplicationPage.EditEnvironment id when query.state = "invalid" ->
                        Some(Ledger.Operations.validateEnvironment (environmentRequest query id))
                    | _ -> None }
        Product.render page query
