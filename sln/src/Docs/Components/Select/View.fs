namespace Docs.ComponentPages.Select

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.ChoicesExamples
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let selectRegistration =
        registration "components-select" "/components/select" "Select" "Select"

    let memberOptions =
        [ ("alex", "Alex Morgan", false)
          ("jamie", "Jamie Lee", false)
          ("riley", "Riley Chen", false)
          ("taylor", "Taylor Brooks", false)
          ("sam", "Sam Rivera", true) ]

    let multipleMembersSelect =
        fveField {
            fveLabel {
                _for "memberIds"
                "Team members"
            }

            fveMultiSelect {



                _placeholder "Choose members"
                _name "memberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "memberIds"
            }
        }

    let selectedMembersSelect =
        fveField {
            fveLabel {
                _for "assignedIds"
                "Assigned members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie"; "riley" ])
                _name "assignedIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "assignedIds"
            }
        }

    let disabledMembersSelect =
        fveField {
            fveLabel {
                _for "disabledMemberIds"
                "Disabled members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie" ])
                _name "disabledMemberIds"
                _disabled true

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "disabledMemberIds"
            }
        }

    let pendingMembersSelect =
        fveField {
            fveLabel {
                _for "pendingMemberIds"
                "Updating members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie" ])
                _readonly true
                _ariaBusy true
                _name "pendingMemberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "pendingMemberIds"
            }
        }

    let multipleSelectForm selection (validationMessage: string option) result =
        div {
            _id "components-multiple-select-form-region"
            _class "grid gap-3"

            form {
                _ariaLabel "Project reviewers"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-select', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-reviewers"
                        "Reviewers"
                    }

                    fveMultiSelect {



                        _values selection


                        _name "reviewerIds"
                        _id "components-reviewers"
                        _required true

                        fveSelectContent {
                            for choice in memberOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-reviewers") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-reviewers") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-reviewers" + "-description")
                        "Choose one to three reviewers."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-reviewers" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-reviewers-submit" "Validate reviewers"
            }

            choiceResult "components-reviewers-result" result
        }

    let multipleMembersCombobox =
        fveField {
            fveLabel {
                _for "searchMemberIds"
                "Search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _name "searchMemberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "searchMemberIds"
            }
        }

    let disabledMembersCombobox =
        fveField {
            fveLabel {
                _for "disabledSearchIds"
                "Disabled search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _name "disabledSearchIds"
                _disabled true

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "disabledSearchIds"
            }
        }

    let pendingMembersCombobox =
        fveField {
            fveLabel {
                _for "pendingSearchIds"
                "Updating search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _readonly true
                _ariaBusy true
                _name "pendingSearchIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "pendingSearchIds"
            }
        }

    let loadingMembersCombobox =
        fveField {
            fveLabel {
                _for "loadingSearchIds"
                "Loading members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex" ])
                _name "loadingSearchIds"

                fveSelectContent {
                    _loading true

                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "loadingSearchIds"
            }
        }

    let remoteMembersContent searchText choices (failure: string option) =
        fveSelectContent {
            _id "fve-select-components_remote_members-popup"
            _query searchText
            _error failure
            _retry "/components/members/search?retry=true"

            for choice in choices do
                fveSelectItem {
                    let value, label, disabled = choice
                    _value value
                    _label label
                    _disabled disabled
                }
        }

    let remoteMembersView searchText choices failure =
        fveField {
            fveLabel {
                _for "components-remote-members"
                "Remote members"
            }

            fveMultiSelect {


                _values (([ ("alex", "Alex Morgan", false) ]) |> List.map (fun (value, _, _) -> value))

                for value, label, _ in
                    (([ ("alex", "Alex Morgan", false) ])
                     |> List.distinctBy (fun (value, _, _) -> value)) do
                    fveSelectSelectedItem {
                        _value value
                        label
                    }

                _placeholder "Search members"

                _search (
                    SelectSearch.Remote(
                        if Option.isSome failure then
                            "/components/members/search?retry=true"
                        else
                            "/components/members/search"
                    )
                )

                _query searchText
                remoteMembersContent searchText choices failure
                _name "remoteMemberIds"
                _id "components-remote-members"

                _ariaDescribedby (String.concat " " [ ("components-remote-members") + "-description" ])
            }

            fveFieldDescription {
                _id ("components-remote-members" + "-description")
                "Try Jamie or Riley for matches, or error to test retry."
            }
        }

    let remoteMembersCombobox =
        div {
            _class "grid gap-3"
            remoteMembersView "" memberOptions None

            button {
                _type "button"
                _dataOn ("click", "document.getElementById('fve-select-components_remote_members-field').dispatchEvent(new CustomEvent('fve-select-refresh', {detail:{endpoint:'/components/members/field'}}))")
                _class "min-h-8 text-sm text-[var(--fve-brand-text)] underline underline-offset-2"
                "Refresh members"
            }
        }

    let searchMemberOptions (query: string) =
        [ "alex", "Alex Morgan"
          "jamie", "Jamie Lee"
          "riley", "Riley Chen"
          "taylor", "Taylor Brooks" ]
        |> List.filter (fun (_, label) -> label.Contains(query, StringComparison.OrdinalIgnoreCase))
        |> List.map (fun (value, label) -> (value, label, false))

    let remoteMembersField searchText =
        remoteMembersView searchText (searchMemberOptions searchText) None

    let remoteMembersOptions searchText retry =
        let isError = String.Equals(searchText, "error", StringComparison.OrdinalIgnoreCase)

        let choices =
            if isError then
                memberOptions
            else
                searchMemberOptions searchText

        let failure =
            if isError && not retry then
                Some "Members could not be loaded."
            else
                None

        remoteMembersContent searchText choices failure

    let multipleComboboxForm selection (validationMessage: string option) result =
        div {
            _id "components-multiple-combobox-form-region"
            _class "grid gap-3"

            form {
                _ariaLabel "Project assignees"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-combobox', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-assignees"
                        "Assignees"
                    }

                    fveMultiSelect {



                        _search SelectSearch.Static
                        _values selection


                        _name "assigneeIds"
                        _id "components-assignees"
                        _required true

                        fveSelectContent {
                            for choice in memberOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-assignees") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-assignees") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-assignees" + "-description")
                        "Choose one to three assignees."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-assignees" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-assignees-submit" "Validate assignees"
            }

            choiceResult "components-assignees-result" result
        }

    let frequencyOptions =
        [ "daily", "Daily"; "weekly", "Weekly"; "monthly", "Monthly" ]

    let defaultSelect =
        fveField {
            fveLabel {
                _for "updateFrequencyDefault"
                "Update frequency"
            }

            fveSelect {
                _id "updateFrequencyDefault"
                _name "updateFrequencyDefault"
                _value "daily"

                fveSelectContent {
                    for value, label in frequencyOptions do
                        fveSelectItem {
                            _value value

                            if value = "monthly" then
                                _disabled true

                            label
                        }
                }
            }
        }

    let nativeFallbackSelect =
        fveField {
            fveLabel {
                _for "nativeFrequency"
                "Update frequency"
            }

            fveSelect {



                _value "weekly"
                _nativeFallback true
                _name "nativeFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "nativeFrequency"
            }
        }

    let basicSelect =
        fveField {
            fveLabel {
                _for "updateFrequency"
                "Update frequency"
            }

            fveSelect {



                _value "weekly"
                _position SelectPosition.SelectedItem
                _name "updateFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "updateFrequency"
            }
        }

    let selectWithHelp =
        fveField {
            fveLabel {
                _for "digestFrequency"
                "Digest frequency"
            }

            fveSelect {




                _placeholder "Choose a frequency"
                _name "digestFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "digestFrequency"
                _ariaDescribedby (String.concat " " [ ("digestFrequency") + "-description" ])
            }

            fveFieldDescription {
                _id ("digestFrequency" + "-description")
                "Choose how often to receive a summary."
            }
        }

    let edgeAlignedSelect =
        fveField {
            fveLabel {
                _for "exportFormat"
                "Export format"
            }

            fveSelect {



                _value "json"
                _position SelectPosition.TriggerEnd
                _name "exportFormat"

                fveSelectContent {
                    for choice in ([ ("csv", "CSV", false); ("json", "JSON", false); ("pdf", "PDF", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "exportFormat"
            }
        }

    let selectFormRegion (selection: string option) (validationMessage: string option) result =
        div {
            _id "components-select-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _dataOn ("submit", "@post('/components/choices/select', {contentType:'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-status"
                        "Status"
                    }

                    fveSelect {



                        match selection with
                        | Some value -> _value value
                        | None -> ()

                        _placeholder "Choose a status"

                        _name "status"
                        _id "components-status"
                        _required true

                        fveSelectContent {
                            for choice in selectStatusOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-status") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-status") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-status" + "-description")
                        "Choose the availability of this item."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-status" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-select-submit" "Validate status"
            }

            choiceResult "components-select-result" result
        }

    let disabledStatusSelect =
        fveField {
            fveLabel {
                _for "components-disabled-status"
                "Disabled status"
            }

            fveSelect {



                _value "active"
                _name "status"
                _id "components-disabled-status"
                _disabled true

                fveSelectContent {
                    for choice in selectStatusOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let pendingStatusSelect =
        fveField {
            fveLabel {
                _for "components-pending-status"
                "Updating status"
            }

            fveSelect {



                _value "pending"
                _readonly true
                _ariaBusy true
                _name "status"
                _id "components-pending-status"

                fveSelectContent {
                    for choice in selectStatusOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let statusSelect = selectFormRegion None None None

    let private searchItems =
        [ "101", "Project notes"; "102", "Shared files"; "103", "Archived files" ]

    let private itemOptions values =
        values |> List.map (fun (value, label) -> (value, label, (value = "103")))

    let staticItemCombobox =
        fveField {
            fveLabel {
                _for "components-static-item"
                "Search items"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "101"

                _name "staticItem"
                _id "components-static-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _ariaDescribedby (String.concat " " [ ("components-static-item") + "-description" ])
            }

            fveFieldDescription {
                _id ("components-static-item" + "-description")
                "Type to filter the items."
            }
        }

    let itemContent contentId searchText (failure: string option) (choices: (string * string * bool) list) =
        fveSelectContent {
            _id contentId
            _query searchText
            _error failure
            _retry "/components/items/search?retry=true"
            _emptyMessage "No matching items"
            _loadingMessage "Loading items"

            for choice in choices do
                fveSelectItem {
                    let value, label, disabled = choice
                    _value value
                    _label label
                    _disabled disabled
                }
        }

    let itemComboboxView fieldName (caption: string) (choices: (string * string * bool) list) =
        fveField {
            fveLabel {
                _for fieldName
                caption
            }

            fveSelect {


                _placeholder "Search items"

                _search (SelectSearch.Remote "/components/items/search")
                itemContent ("fve-select-" + fieldName + "-popup") "" None choices
                _name fieldName

                _id fieldName
                _ariaDescribedby (String.concat " " [ (fieldName) + "-description" ])
            }

            fveFieldDescription {
                _id (fieldName + "-description")
                "Search for Project notes, or type error to try the retry action."
            }
        }

    let itemCombobox =
        let choices = itemOptions searchItems

        fveField {
            fveLabel {
                _for "item"
                "Choose an item"
            }

            fveSelect {


                _placeholder "Search items"

                _search (SelectSearch.Remote "/components/items/search")
                _name "item"
                itemContent "fve-select-item-popup" "" None choices

                _id "item"
                _ariaDescribedby (String.concat " " [ ("item") + "-description" ])
            }

            fveFieldDescription {
                _id ("item" + "-description")
                "Search for Project notes, or type error to try the retry action."
            }
        }

    let isolatedItemComboboxes =
        div {
            _class "grid gap-4 sm:grid-cols-2"
            itemComboboxView "firstitem" "First item" (itemOptions searchItems)
            itemComboboxView "seconditem" "Second item" (itemOptions searchItems)
        }

    let itemComboboxOptions contentId searchText retry =
        let failed =
            String.Equals(searchText, "error", StringComparison.OrdinalIgnoreCase)
            && not retry

        let choices =
            if failed then
                []
            else
                searchItems
                |> List.filter (fun (_, label) ->
                    String.IsNullOrWhiteSpace searchText
                    || label.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                |> itemOptions

        itemContent contentId searchText (if failed then Some "Items could not be loaded." else None) choices

    let itemSearchTargets =
        [ "item_query", "fve-select-item-popup"
          "firstitem_query", "fve-select-firstitem-popup"
          "seconditem_query", "fve-select-seconditem-popup"
          "components_loading_item_query", "fve-select-components_loading_item-popup" ]

    let composedStatusSelect =
        let icon (name: IconName) = fveIcon { _name name; _size Size.Small }

        let active = ("active", "Active", false)
        let pendingChoice = ("pending", "Pending", false)
        let scheduled = ("scheduled", "Scheduled", false)

        fveField {
            fveLabel {
                _for "components-composed-status"
                "Composed status"
            }

            fveSelect {


                _value "active"

                fveSelectContent {
                    _id "composed-status-content"

                    fveSelectGroup {
                        _label "Current"

                        fveSelectItem {
                            let value, label, disabled = active
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"
                                icon IconName.Bolt

                                span {
                                    _class "whitespace-nowrap"
                                    "Active"
                                }
                            }
                        }

                    }

                    fveSelectGroup {
                        _label "Upcoming"

                        fveSelectItem {
                            let value, label, disabled = pendingChoice
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"
                                icon IconName.Clock

                                span {
                                    _class "whitespace-nowrap"
                                    "Pending"
                                }
                            }
                        }

                        fveSelectItem {
                            let value, label, disabled = scheduled
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"

                                icon IconName.CalendarDays

                                span {
                                    _class "whitespace-nowrap"
                                    "Scheduled"
                                }
                            }
                        }

                    }
                }

                _name "composedStatus"
                _id "components-composed-status"
            }
        }

    let loadingItemCombobox =
        fveField {
            fveLabel {
                _for "components-loading-item"
                "Loading items"
            }

            fveSelect {



                _search (SelectSearch.Remote "/components/items/search")
                _name "loadingItem"
                _id "components-loading-item"

                fveSelectContent {
                    _loading true
                    _loadingMessage "Loading items"
                }
            }
        }

    let validationItemCombobox =
        fveField {
            fveLabel {
                _for "components-validated-item"
                "Item with validation"
            }

            fveSelect {



                _search SelectSearch.Static


                _name "validatedItem"
                _id "components-validated-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _ariaInvalid (Option.isSome (Some "Choose an available item."))

                _ariaDescribedby (
                    String.concat
                        " "
                        [ ("components-validated-item") + "-description"
                          if Option.isSome (Some("Choose an available item.")) then
                              ("components-validated-item") + "-validation" ]
                )
            }

            fveFieldDescription {
                _id ("components-validated-item" + "-description")
                "Choose an item before continuing."
            }

            match Some "Choose an available item." with
            | Some message ->
                fveFieldError {
                    _id ("components-validated-item" + "-validation")
                    message
                }
            | None -> ()
        }

    let disabledItemCombobox =
        fveField {
            fveLabel {
                _for "components-disabled-item"
                "Unavailable item"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "101"
                _name "disabledItem"
                _id "components-disabled-item"
                _disabled true

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let pendingItemCombobox =
        fveField {
            fveLabel {
                _for "components-pending-item"
                "Updating item"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "102"
                _readonly true
                _ariaBusy true
                _name "pendingItem"
                _id "components-pending-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let private selectSource = [ "selectStatusOptions" ]
    let private comboboxSource = [ "searchItems"; "itemOptions" ]

    let examples () =
            [ sample "select-default" "Basic" [ "frequencyOptions"; "defaultSelect" ] (fieldSurface defaultSelect)
              sample
                  "select-composed"
                  "Custom items and groups"
                  [ "composedStatusSelect" ]
                  (fieldSurface composedStatusSelect)
              sample
                  "select-multiple"
                  "Multiple selection"
                  [ "memberOptions"; "selectedMembersSelect" ]
                  (fieldSurface selectedMembersSelect)
              sample
                  "select-search"
                  "Local search"
                  (comboboxSource @ [ "staticItemCombobox" ])
                  (fieldSurface staticItemCombobox)
              serverSample
                  "select-search-remote"
                  "Remote search"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemCombobox"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchHandler"
                       "itemSearchRoutes" ])
                  (fieldSurface itemCombobox)
              sample
                  "select-validation"
                  "Form validation"
                  (formSource @ selectSource @ [ "selectFormRegion"; "statusSelect" ])
                  (fieldSurface statusSelect)
              sample
                  "select-disabled"
                  "Disabled"
                  (selectSource @ [ "disabledStatusSelect" ])
                  (fieldSurface disabledStatusSelect)
              sample
                  "select-pending"
                  "Pending"
                  (selectSource @ [ "pendingStatusSelect" ])
                  (fieldSurface pendingStatusSelect) ]

    let fixtureExamples () =
            [ sample "select-default" "Default" [ "frequencyOptions"; "defaultSelect" ] (fieldSurface defaultSelect)
              sample
                  "select-native-fallback"
                  "Native fallback"
                  [ "nativeFallbackSelect" ]
                  (fieldSurface nativeFallbackSelect)
              sample "select" "Selected-item alignment (default)" [ "basicSelect" ] (fieldSurface basicSelect)
              sample "select-help" "With help text" [ "selectWithHelp" ] (fieldSurface selectWithHelp)
              sample
                  "select-edge"
                  "Explicit trigger-edge position"
                  [ "edgeAlignedSelect" ]
                  (fieldSurface edgeAlignedSelect)
              sample
                  "select-validation"
                  "Required selection with validation"
                  (formSource @ selectSource @ [ "selectFormRegion"; "statusSelect" ])
                  (fieldSurface statusSelect)
              sample
                  "select-disabled"
                  "Disabled"
                  (selectSource @ [ "disabledStatusSelect" ])
                  (fieldSurface disabledStatusSelect)
              sample
                  "select-pending"
                  "Pending"
                  (selectSource @ [ "pendingStatusSelect" ])
                  (fieldSurface pendingStatusSelect)
              sample
                  "select-multiple"
                  "Multiple selection"
                  [ "memberOptions"; "multipleMembersSelect" ]
                  (fieldSurface multipleMembersSelect)
              sample
                  "select-multiple-selected"
                  "With several selected"
                  [ "memberOptions"; "selectedMembersSelect" ]
                  (fieldSurface selectedMembersSelect)
              sample
                  "select-multiple-validation"
                  "Multiple selection with validation"
                  (formSource @ [ "memberOptions"; "multipleSelectForm" ])
                  (fieldSurface (multipleSelectForm [] None None))
              sample
                  "select-multiple-disabled"
                  "Multiple selection, disabled"
                  [ "memberOptions"; "disabledMembersSelect" ]
                  (fieldSurface disabledMembersSelect)
              sample
                  "select-multiple-pending"
                  "Multiple selection, pending"
                  [ "memberOptions"; "pendingMembersSelect" ]
                  (fieldSurface pendingMembersSelect)
              sample
                  "select-search"
                  "Searchable"
                  (comboboxSource @ [ "staticItemCombobox" ])
                  (fieldSurface staticItemCombobox)
              sample
                  "select-composed"
                  "Composed groups and icon items"
                  [ "composedStatusSelect" ]
                  (fieldSurface composedStatusSelect)
              serverSample
                  "select-search-remote"
                  "Searchable with remote results"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemComboboxView"
                       "itemCombobox"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchTargets"
                       "itemSearchHandler"
                       "itemSearchRoutes" ])
                  (fieldSurface itemCombobox)
              serverSample
                  "select-search-isolation"
                  "Independent searches sharing one endpoint"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemComboboxView"
                       "isolatedItemComboboxes"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchTargets"
                       "itemSearchHandler"
                       "itemSearchRoutes"
                       "itemFixtureSearchRoutes" ])
                  (fieldSurface isolatedItemComboboxes)
              sample
                  "select-search-validation"
                  "Searchable with validation"
                  (comboboxSource @ [ "validationItemCombobox" ])
                  (fieldSurface validationItemCombobox)
              sample
                  "select-search-loading"
                  "Searchable, loading"
                  [ "loadingItemCombobox" ]
                  (fieldSurface loadingItemCombobox)
              sample
                  "select-search-disabled"
                  "Searchable, disabled"
                  (comboboxSource @ [ "disabledItemCombobox" ])
                  (fieldSurface disabledItemCombobox)
              sample
                  "select-search-pending"
                  "Searchable, pending"
                  (comboboxSource @ [ "pendingItemCombobox" ])
                  (fieldSurface pendingItemCombobox)
              sample
                  "select-search-multiple"
                  "Searchable multiple selection"
                  [ "memberOptions"; "multipleMembersCombobox" ]
                  (fieldSurface multipleMembersCombobox)
              serverSample
                  "select-search-multiple-remote"
                  "Searchable multiple selection with remote results"
                  [ "memberOptions"
                    "remoteMembersContent"
                    "remoteMembersView"
                    "remoteMembersCombobox"
                    "searchMemberOptions"
                    "remoteMembersField"
                    "remoteMembersOptions"
                    "selectSearchQuery"
                    "remoteMemberSearch"
                    "remoteMembersRoutes" ]
                  (fieldSurface remoteMembersCombobox)
              sample
                  "select-search-multiple-validation"
                  "Searchable multiple selection with validation"
                  (formSource @ [ "memberOptions"; "multipleComboboxForm" ])
                  (fieldSurface (multipleComboboxForm [] None None))
              sample
                  "select-search-multiple-loading"
                  "Searchable multiple selection, loading"
                  [ "memberOptions"; "loadingMembersCombobox" ]
                  (fieldSurface loadingMembersCombobox)
              sample
                  "select-search-multiple-disabled"
                  "Searchable multiple selection, disabled"
                  [ "memberOptions"; "disabledMembersCombobox" ]
                  (fieldSurface disabledMembersCombobox)
              sample
                  "select-search-multiple-pending"
                  "Searchable multiple selection, pending"
                  [ "memberOptions"; "pendingMembersCombobox" ]
                  (fieldSurface pendingMembersCombobox) ]


    let private galleryContent : Content =
        { description = "Choose single or multiple string values with search, keyboard, form and positioning behavior. Single selections update their named value input and emit a bubbling change event. Ordinary single selects can opt into a styled native fallback before Datastar initializes; only the active control submits a value."
          usage = [
              prose
                  "Compose fveSelectContent and fveSelectItem inside fveSelect or fveMultiSelect. The Select supplies its trigger and manages selection/form values; customize items with ordinary HTML."

              prose
                  "Give remote content a stable ID and render it with the same helper for initial results and responses. The remote example includes the running handlers and routes. Items supply their own string values and labels; serialize domain values explicitly at the HTML boundary."
          ]
          sections = []
          apiCategory = Some "select" }

    let selectPage = gallery selectRegistration galleryContent (examples ())

    let selectFixturePage = gallery selectRegistration galleryContent (fixtureExamples ())
