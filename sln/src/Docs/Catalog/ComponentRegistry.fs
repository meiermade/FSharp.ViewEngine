namespace Docs.Catalog

open Docs.Common
open Docs.Common.Registration
open Docs.ComponentPages.Installation.View
open Docs.ComponentPages.Button.View
open Docs.ComponentPages.Icon.View
open Docs.ComponentPages.Link.View
open Docs.ComponentPages.ButtonGroup.View
open Docs.ComponentPages.Kbd.View
open Docs.ComponentPages.Badge.View
open Docs.ComponentPages.LoadingIndicator.View
open Docs.ComponentPages.Progress.View
open Docs.ComponentPages.EmptyState.View
open Docs.ComponentPages.Skeleton.View
open Docs.ComponentPages.Separator.View
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
open Docs.ComponentPages.Section.View
open Docs.ComponentPages.Disclosure.View
open Docs.ComponentPages.Slide.View
open Docs.ComponentPages.Presentation.View
open Docs.ComponentPages.Browser.View
open Docs.ComponentPages.Resizable.View
open Docs.ComponentPages.Phone.View
open Docs.ComponentPages.Page.View
open Docs.ComponentPages.Collection.View
open Docs.ComponentPages.Detail.View
open Docs.ComponentPages.AppShell.View
open Docs.ComponentPages.BottomNavigation.View
open Docs.ComponentPages.Message.View
open Docs.ComponentPages.DayCalendar.View
open Docs.ComponentPages.WeekCalendar.View
open Docs.ComponentPages.MonthCalendar.View
open Docs.ComponentPages.YearCalendar.View
open Docs.ComponentPages.MediaLibrary.View
open Docs.ComponentPages.AccountManagement.View
open Docs.Guides.ComponentPages

module ComponentRegistry =
    let overviewRegistration =
        packageRegistration "components-overview" "/components" "Overview" "Components"

    let actionRegistrations =
        [ buttonRegistration
          linkRegistration
          buttonGroupRegistration
          dropdownMenuRegistration
          toggleButtonRegistration
          toggleGroupRegistration ]

    let feedbackRegistrations =
        [ badgeRegistration
          noticeRegistration
          notificationRegistration
          loadingIndicatorRegistration
          progressRegistration
          emptyStateRegistration
          skeletonRegistration ]

    let dataDisplayRegistrations =
        [ iconRegistration
          tableRegistration
          chartRegistration
          descriptionListRegistration
          metricRegistration
          avatarRegistration
          dayCalendarRegistration
          weekCalendarRegistration
          monthCalendarRegistration
          yearCalendarRegistration
          kbdRegistration
          separatorRegistration ]

    let formControlRegistrations =
        [ inputRegistration
          inputGroupRegistration
          fieldRegistration
          textareaRegistration
          fileSelectionRegistration
          tagInputRegistration
          errorSummaryRegistration
          selectRegistration
          timePickerRegistration
          datePickerRegistration
          checkboxRegistration
          switchRegistration
          radioGroupRegistration
          choiceCardsRegistration ]

    let navigationRegistrations =
        [ breadcrumbsRegistration
          sideNavRegistration
          tabsRegistration
          paginationRegistration
          commandRegistration ]

    let overlayRegistrations =
        [ dialogRegistration
          drawerRegistration
          floatingPanelRegistration
          popoverRegistration
          tooltipRegistration ]

    let compositionRegistrations = [ pageTopBarRegistration; pageHeaderRegistration; sectionRegistration; disclosureRegistration ]

    let frameRegistrations =
        [ browserRegistration; phoneRegistration; resizableRegistration; slideRegistration; presentationRegistration ]

    let applicationNavigationRegistrations = [ bottomNavigationRegistration ]

    let applicationWorkflowRegistrations = [ messageRegistration ]

    let applicationResourceRegistrations: DocPage list = []

    let pageExampleRegistrations: DocPage list = []

    let guideRegistrations = [ interactionRegistration; themingRegistration ]

    let allRegistrations =
        [ overviewRegistration; installationRegistration ]
        @ actionRegistrations
        @ feedbackRegistrations
        @ dataDisplayRegistrations
        @ formControlRegistrations
        @ navigationRegistrations
        @ overlayRegistrations
        @ compositionRegistrations
        @ frameRegistrations
        @ applicationNavigationRegistrations
        @ applicationWorkflowRegistrations
        @ applicationResourceRegistrations
        @ pageExampleRegistrations
        @ guideRegistrations

    let page = overviewRegistration
