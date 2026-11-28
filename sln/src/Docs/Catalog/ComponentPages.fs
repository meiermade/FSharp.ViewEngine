namespace Docs.Catalog

open System
open Docs.Common
open Docs.Pages
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
open Docs.Common.SamplesExamples
open Docs.Catalog.ComponentRegistry
open Docs.ComponentPages.Compositions.Page
open Docs.ComponentPages.Installation.View
open Docs.ComponentPages.Button.View
open Docs.ComponentPages.Icon.View
open Docs.ComponentPages.Link.View
open Docs.ComponentPages.ButtonGroup.View
open Docs.ComponentPages.Badge.View
open Docs.ComponentPages.Kbd.View
open Docs.ComponentPages.Separator.View
open Docs.ComponentPages.Skeleton.View
open Docs.ComponentPages.LoadingIndicator.View
open Docs.ComponentPages.Progress.View
open Docs.ComponentPages.EmptyState.View
open Docs.ComponentPages.Table.View
open Docs.ComponentPages.Chart.View
open Docs.ComponentPages.DescriptionList.View
open Docs.ComponentPages.Metric.View
open Docs.ComponentPages.Pagination.View
open Docs.ComponentPages.Avatar.View
open Docs.ComponentPages.Input.View
open Docs.ComponentPages.Field.View
open Docs.ComponentPages.InputGroup.View
open Docs.ComponentPages.FileSelection.View
open Docs.ComponentPages.TagInput.View
open Docs.ComponentPages.FormLayouts.View
open Docs.ComponentPages.Textarea.View
open Docs.ComponentPages.ErrorSummary.View
open Docs.ComponentPages.Notice.View
open Docs.ComponentPages.Notification.View
open Docs.ComponentPages.Select.View
open Docs.ComponentPages.TimePicker.View
open Docs.ComponentPages.DatePicker.View
open Docs.ComponentPages.Checkbox.View
open Docs.ComponentPages.Switch.View
open Docs.ComponentPages.ToggleButton.View
open Docs.ComponentPages.ToggleGroup.View
open Docs.ComponentPages.Breadcrumbs.View
open Docs.ComponentPages.SideNav.View
open Docs.ComponentPages.Tabs.View
open Docs.ComponentPages.RadioGroup.View
open Docs.ComponentPages.ChoiceCards.View
open Docs.ComponentPages.DropdownMenu.View
open Docs.ComponentPages.Command.View
open Docs.ComponentPages.Dialog.View
open Docs.ComponentPages.Drawer.View
open Docs.ComponentPages.FloatingPanel.View
open Docs.ComponentPages.Tooltip.View
open Docs.ComponentPages.Popover.View
open Docs.ComponentPages.PageTopBar.View
open Docs.ComponentPages.PageHeader.View
open Docs.ComponentPages.Slide.View
open Docs.ComponentPages.Presentation.View
open Docs.ComponentPages.Section.View
open Docs.ComponentPages.Disclosure.View
open Docs.ComponentPages.Browser.View
open Docs.ComponentPages.Resizable.View
open Docs.ComponentPages.Phone.View
open Docs.ComponentPages.Page.View
open Docs.ComponentPages.Collection.View
open Docs.ComponentPages.Detail.View
open Docs.ComponentPages.AppShell.View
open Docs.ComponentPages.AccountManagement.View
open Docs.ComponentPages.BottomNavigation.View
open Docs.ComponentPages.Message.View
open Docs.ComponentPages.DayCalendar.View
open Docs.ComponentPages.WeekCalendar.View
open Docs.ComponentPages.MonthCalendar.View
open Docs.ComponentPages.YearCalendar.View
open Docs.ComponentPages.MediaLibrary.View
open Docs.Guides.ComponentPages
open Docs.ComponentPages.Compositions.Page

/// Catalog composition and lookup only; variants, source and interactions belong to their component pages.
module ComponentCatalog =

    let examplesFor = function
        | "icon" -> Docs.ComponentPages.Icon.View.examples ()
        | "button" -> Docs.ComponentPages.Button.View.examples ()
        | "link" -> Docs.ComponentPages.Link.View.examples ()
        | "button-group" -> Docs.ComponentPages.ButtonGroup.View.examples ()
        | "badge" -> Docs.ComponentPages.Badge.View.examples ()
        | "kbd" -> Docs.ComponentPages.Kbd.View.examples ()
        | "separator" -> Docs.ComponentPages.Separator.View.examples ()
        | "skeleton" -> Docs.ComponentPages.Skeleton.View.examples ()
        | "resizable" -> Docs.ComponentPages.Resizable.View.examples ()
        | "tooltip" -> Docs.ComponentPages.Tooltip.View.examples ()
        | "popover" -> Docs.ComponentPages.Popover.View.examples ()
        | "message" -> Docs.ComponentPages.Message.View.examples ()
        | "loading-indicator" -> Docs.ComponentPages.LoadingIndicator.View.examples ()
        | "progress" -> Docs.ComponentPages.Progress.View.examples ()
        | "empty-state" -> Docs.ComponentPages.EmptyState.View.examples ()
        | "table" -> Docs.ComponentPages.Table.View.examples ()
        | "description-list" -> Docs.ComponentPages.DescriptionList.View.examples ()
        | "metric" -> Docs.ComponentPages.Metric.View.examples ()
        | "pagination" -> Docs.ComponentPages.Pagination.View.examples ()
        | "avatar" -> Docs.ComponentPages.Avatar.View.examples ()
        | "input" -> Docs.ComponentPages.Input.View.examples ()
        | "field" -> Docs.ComponentPages.Field.View.examples ()
        | "input-group" -> Docs.ComponentPages.InputGroup.View.examples ()
        | "form-layouts" -> Docs.ComponentPages.FormLayouts.View.examples ()
        | "file-selection" -> Docs.ComponentPages.FileSelection.View.examples ()
        | "tag-input" -> Docs.ComponentPages.TagInput.View.examples ()
        | "textarea" -> Docs.ComponentPages.Textarea.View.examples ()
        | "error-summary" -> Docs.ComponentPages.ErrorSummary.View.examples ()
        | "notice" -> Docs.ComponentPages.Notice.View.examples ()
        | "notification" -> Docs.ComponentPages.Notification.View.examples ()
        | "select" -> Docs.ComponentPages.Select.View.examples ()
        | "select-fixtures" -> Docs.ComponentPages.Select.View.fixtureExamples ()
        | "month-calendar" -> Docs.ComponentPages.MonthCalendar.View.examples ()
        | "chart" -> Docs.ComponentPages.Chart.View.examples ()
        | "time-picker" -> Docs.ComponentPages.TimePicker.View.examples ()
        | "date-picker" -> Docs.ComponentPages.DatePicker.View.examples ()
        | "checkbox" -> Docs.ComponentPages.Checkbox.View.examples ()
        | "switch" -> Docs.ComponentPages.Switch.View.examples ()
        | "toggle-button" -> Docs.ComponentPages.ToggleButton.View.examples ()
        | "toggle-group" -> Docs.ComponentPages.ToggleGroup.View.examples ()
        | "tabs" -> Docs.ComponentPages.Tabs.View.examples ()
        | "radio-group" -> Docs.ComponentPages.RadioGroup.View.examples ()
        | "choice-cards" -> Docs.ComponentPages.ChoiceCards.View.examples ()
        | "dropdown-menu" -> Docs.ComponentPages.DropdownMenu.View.examples ()
        | "command" -> Docs.ComponentPages.Command.View.examples ()
        | "dialog" -> Docs.ComponentPages.Dialog.View.examples ()
        | "drawer" -> Docs.ComponentPages.Drawer.View.examples ()
        | "floating-panel" -> Docs.ComponentPages.FloatingPanel.View.examples ()
        | "breadcrumbs" -> Docs.ComponentPages.Breadcrumbs.View.examples ()
        | "side-nav" -> Docs.ComponentPages.SideNav.View.examples ()
        | "page-top-bar" -> Docs.ComponentPages.PageTopBar.View.examples ()
        | "page-header" -> Docs.ComponentPages.PageHeader.View.examples ()
        | "section" -> Docs.ComponentPages.Section.View.examples ()
        | "disclosure" -> Docs.ComponentPages.Disclosure.View.examples ()
        | "slide" -> Docs.ComponentPages.Slide.View.examples ()
        | "presentation" -> Docs.ComponentPages.Presentation.View.examples ()
        | "browser" -> Docs.ComponentPages.Browser.View.examples ()
        | "phone" -> Docs.ComponentPages.Phone.View.examples ()
        | "page" -> Docs.ComponentPages.Page.View.examples ()
        | "collection" -> Docs.ComponentPages.Collection.View.examples ()
        | "detail" -> Docs.ComponentPages.Detail.View.examples ()
        | "app-shell" -> Docs.ComponentPages.AppShell.View.examples ()
        | "page-examples/account-management" -> Docs.ComponentPages.AccountManagement.View.examples ()
        | "bottom-navigation" -> Docs.ComponentPages.BottomNavigation.View.examples ()
        | "day-calendar" -> Docs.ComponentPages.DayCalendar.View.examples ()
        | "week-calendar" -> Docs.ComponentPages.WeekCalendar.View.examples ()
        | "year-calendar" -> Docs.ComponentPages.YearCalendar.View.examples ()
        | "media-library" -> Docs.ComponentPages.MediaLibrary.View.examples ()
        | value when value.StartsWith("page-examples/", StringComparison.Ordinal) ->
            let page =
                Docs.ComponentPages.Compositions.View.pages
                |> List.find (fun page -> value = "page-examples/" + Docs.ComponentPages.Compositions.View.slug page)
            [ { id = "components-" + Docs.ComponentPages.Compositions.View.slug page
                title = Docs.ComponentPages.Compositions.View.title page + " workspace"
                source = Docs.ComponentPages.Compositions.View.source page
                preview = connectedGallerySurface (Docs.ComponentPages.Compositions.View.preview page Docs.ComponentPages.Compositions.View.defaultQuery)
                note = None } ]
        | id -> invalidArg (nameof id) $"No component examples registered for '{id}'."

    let allExamples () =
        allRegistrations
        |> List.filter (fun page ->
            page.path.StartsWith("/components/", StringComparison.Ordinal)
            && page.path <> "/components/installation"
            && not (guideRegistrations |> List.exists (fun guide -> guide.path = page.path)))
        |> List.collect (fun page -> examplesFor (page.path.Substring("/components/".Length)))

    let private pages =
        [ installationRegistration.path, installationPage
          buttonRegistration.path, buttonPage
          iconRegistration.path, iconPage
          linkRegistration.path, linkPage
          buttonGroupRegistration.path, buttonGroupPage
          badgeRegistration.path, badgePage
          kbdRegistration.path, kbdPage
          separatorRegistration.path, separatorPage
          skeletonRegistration.path, skeletonPage
          loadingIndicatorRegistration.path, loadingIndicatorPage
          progressRegistration.path, progressPage
          emptyStateRegistration.path, emptyStatePage
          tableRegistration.path, tablePage
          chartRegistration.path, chartPage
          descriptionListRegistration.path, descriptionListPage
          metricRegistration.path, metricPage
          paginationRegistration.path, paginationPage
          avatarRegistration.path, avatarPage
          inputRegistration.path, inputPage
          fieldRegistration.path, fieldPage
          inputGroupRegistration.path, inputGroupPage
          fileSelectionRegistration.path, fileSelectionPage
          tagInputRegistration.path, tagInputPage
          formLayoutsRegistration.path, formLayoutsPage
          textareaRegistration.path, textareaPage
          errorSummaryRegistration.path, errorSummaryPage
          noticeRegistration.path, noticePage
          notificationRegistration.path, notificationPage
          selectRegistration.path, selectPage
          timePickerRegistration.path, timePickerPage
          datePickerRegistration.path, datePickerPage
          checkboxRegistration.path, checkboxPage
          switchRegistration.path, switchPage
          toggleButtonRegistration.path, toggleButtonPage
          toggleGroupRegistration.path, toggleGroupPage
          breadcrumbsRegistration.path, breadcrumbsPage
          sideNavRegistration.path, sideNavPage
          tabsRegistration.path, tabsPage
          radioGroupRegistration.path, radioGroupPage
          choiceCardsRegistration.path, choiceCardsPage
          dropdownMenuRegistration.path, dropdownMenuPage
          commandRegistration.path, commandPage
          dialogRegistration.path, dialogPage
          drawerRegistration.path, drawerPage
          floatingPanelRegistration.path, floatingPanelPage
          tooltipRegistration.path, tooltipPage
          popoverRegistration.path, popoverPage
          pageTopBarRegistration.path, pageTopBarPage
          pageHeaderRegistration.path, pageHeaderPage
          slideRegistration.path, slidePage
          presentationRegistration.path, presentationPage
          sectionRegistration.path, sectionPage
          disclosureRegistration.path, disclosurePage
          browserRegistration.path, browserPage
          resizableRegistration.path, resizablePage
          phoneRegistration.path, phonePage
          pageRegistration.path, pagePage
          collectionRegistration.path, collectionPageDocumentation
          detailRegistration.path, detailPageDocumentation
          appShellRegistration.path, appShellPage
          accountManagementRegistration.path, accountManagementPage
          bottomNavigationRegistration.path, bottomNavigationPage
          messageRegistration.path, messagePage
          dayCalendarRegistration.path, dayCalendarPage
          weekCalendarRegistration.path, weekCalendarPage
          monthCalendarRegistration.path, monthCalendarPage
          yearCalendarRegistration.path, yearCalendarPage
          mediaLibraryRegistration.path, mediaLibraryPage
          interactionRegistration.path, interactionPage
          themingRegistration.path, themingPage ]
        @ [ for page in Docs.ComponentPages.Compositions.View.pages -> Docs.ComponentPages.Compositions.View.url page, pageExamplePageFor page Docs.ComponentPages.Compositions.View.defaultQuery ]
        |> Map.ofList

    let tryPage path = Map.tryFind path pages
