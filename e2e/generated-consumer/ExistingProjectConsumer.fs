namespace Acme.Consumer

open FSharp.ViewEngine
open Acme.Components
open type Html

module ExistingProjectConsumer =
    let command =
        Command.create "consumer-command" "Find documentation" [
            CommandGroup.create "Documentation" [ CommandItem.link "/" "Home" ] ]
        |> Command.render id

    let calendars =
        let date = System.DateOnly(2026, 9, 17)
        let events = [ CalendarEvent.create "session" "Training" date "/" ]
        fragment {
            DayCalendar.create "Day" date events |> DayCalendar.render id
            WeekCalendar.create "Week" date events |> WeekCalendar.render id
            MonthCalendar.create "Month" date events |> MonthCalendar.render id
            MonthCalendar.create "Compact month" date events |> MonthCalendar.withLayout MonthCalendarLayout.Compact |> MonthCalendar.render id
            YearCalendar.create "Year" date events |> YearCalendar.render id
            MonthCalendar.createSelection "dates" "Date range" "startDate" (MonthCalendarSelection.Range(Some date, Some(date.AddDays 2)))
            |> MonthCalendar.withRangeEndName "endDate"
            |> MonthCalendar.renderSelection
            DatePicker.create "due" "dueDate" "Due date" (MonthCalendarSelection.Single(Some date)) |> DatePicker.render
        }

    let action = Button.create (ButtonContent.Text "Continue") |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render

    let confirmation =
        Dialog.create "consumer-confirmation" "Continue?" (
            form {
                _method "dialog"
                Button.create (ButtonContent.Text "Cancel")
                |> Button.asSubmit
                |> Button.withAttributes [ _id "consumer-confirmation-cancel"; _value "cancel" ]
                |> Button.render
                Button.create (ButtonContent.Text "Continue")
                |> Button.asSubmit
                |> Button.withAttributes [ _value "confirm" ]
                |> Button.render
            })
        |> Dialog.withDescription "Choose whether to continue."
        |> Dialog.withInitialFocus "consumer-confirmation-cancel"
        |> Dialog.withAttributes [ _role "alertdialog" ]
        |> Dialog.render

    let page =
        let actions = div { _class "flex gap-2"; action; a { _href "/"; "Home" } }
        main {
            PageHeader.create "Accounts" |> PageHeader.withActions actions |> PageHeader.render
            section {
                _class "grid gap-4"
                SectionHeader.create "Details" |> SectionHeader.render
                Card.create (p { "Consumer-owned content and page structure" }) |> Card.render
            }
        }
