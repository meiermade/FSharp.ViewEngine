module Example.Application.Product.Profile.Handler

open Example.UI.Views.Model

let tryPage = function
    | ["profile"] -> Some ApplicationPage.Profile
    | ["profile"; "edit"] -> Some ApplicationPage.EditProfile
    | ["profile"; "organizations"] -> Some ApplicationPage.ProfileOrganizations
    | _ -> None
