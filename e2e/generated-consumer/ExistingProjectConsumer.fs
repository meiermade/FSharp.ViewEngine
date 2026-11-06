namespace Acme.Consumer

open FSharp.ViewEngine
open Acme.Components
open type Html
open type Datastar

module ExistingProjectConsumer =
    type Currency =
        | USD
        | EUR

    type Destination = Home

    let encodeCurrency =
        function
        | USD -> "USD"
        | EUR -> "EUR"

    let destinationUrl =
        function
        | Home -> "/"

    let command =
        fveCommand {
            _id "consumer-command"
            _ariaLabel "Find documentation"

            fveCommandGroup {
                _label "Documentation"

                fveCommandItem {
                    _href "/"
                    "Home"
                }
            }
        }

    let calendars =
        let date = System.DateOnly(2026, 9, 17)
        let events = [ CalendarEvent.create "session" "Training" date "/" ]

        fragment {
            fveDayCalendar {
                _label "Day"
                _date date
                yield! (events: CalendarEvent<string> list)
            }

            fveWeekCalendar {
                _label "Week"
                _date date
                yield! (events: CalendarEvent<string> list)
            }

            fveMonthCalendar {
                _label "Month"
                _date date
                yield! (events: CalendarEvent<string> list)
            }

            fveMonthCalendar {
                _label "Compact month"
                _date date
                yield! (events: CalendarEvent<string> list)
                _layout MonthCalendarLayout.Compact
            }

            fveYearCalendar {
                _label "Year"
                _date date
                yield! (events: CalendarEvent<string> list)
            }

            fveMonthCalendarSelection {
                _id "dates"
                _name "startDate"
                _ariaLabel "Date range"
                _mode CalendarSelectionMode.Range

                _value (
                    (date: System.DateOnly)
                        .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                )

                _endValue (
                    ((date.AddDays 2): System.DateOnly)
                        .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                )

                _rangeEndName "endDate"
            }

            fveField {
                fveLabel {
                    _id ("due" + "-label")
                    _for "due"
                    "Due date"
                }

                fveFieldContent {
                    fveDatePicker {
                        _id "due"
                        _name "dueDate"

                        _value (
                            (date: System.DateOnly)
                                .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                        )
                    }
                }
            }
        }

    let chart =
        let options =
            {| xAxis =
                {| ``type`` = "category"
                   data = [| "Jan"; "Feb" |] |}
               yAxis = {| ``type`` = "value" |}
               series =
                [| {| ``type`` = "bar"
                      data = [| 1M; 2M |] |} |] |}

        fveChart {
            _id "consumer-chart"
            _ariaLabel "Values"
            _options options
        }

    let table =
        fveTable {
            _ariaLabel "Values"
            fveTableHeader { fveTableRow { fveTableHead { "Name" } } }

            fveTableBody {
                fveTableRow {
                    fveTableHead {
                        _scope "row"
                        _mobile Mobile.Primary
                        "First"
                    }
                }
            }
        }

    let navigation =
        fveSideNav {
            _id "consumer-navigation"
            _ariaLabel "Navigation"
            _value "/"

            fveSideNavContent {
                fveSideNavItem {
                    _href "/"
                    "Home"
                }
            }
        }

    let action =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _size Size.Small
            _dataIndicator "_consumer_pending"
            _dataOn ("click", "@get('/')")
            "Refresh"
        }

    let destination =
        fveLink {
            _variant LinkVariant.Outline
            _color Color.Neutral
            _size Size.Small
            _href "/"
            "Home"
        }

    // These expressions compile from installed, namespace-rewritten source, not a project reference.
    let accountDetails =
        fveCard {
            _size Size.Small
            _class "consumer-card"
            fveCardHeader { h2 { "Account details" } }

            fveField {
                fveLabel {
                    _for "accountName"
                    "Account name"
                }

                fveInput {
                    _id "accountName"
                    _name "accountName"
                    _required true
                    _value "Operating account"
                    _ariaDescribedby "accountName-description"
                }

                fveFieldDescription {
                    _id "accountName-description"
                    "Consumer-owned field content."
                }
            }

            fveField {
                fveLabel {
                    _for "currency"
                    "Currency"
                }

                fveSelect {
                    _id "currency"
                    _name "currency"
                    _value (encodeCurrency USD)

                    fveSelectContent {
                        fveSelectGroup {
                            _label "Currencies"

                            for currency, label in [ USD, "US Dollar"; EUR, "Euro" ] do
                                fveSelectItem {
                                    _value (encodeCurrency currency)
                                    label
                                }
                        }
                    }
                }
            }

            fveField {
                fveLabel {
                    _for "reviewers"
                    "Reviewers"
                }

                fveMultiSelect {
                    _id "reviewers"
                    _name "reviewers"
                    _values [ "alex" ]
                    _search SelectSearch.Static

                    fveSelectContent {
                        fveSelectItem {
                            _value "alex"
                            "Alex"
                        }

                        fveSelectItem {
                            _value "jamie"
                            "Jamie"
                        }
                    }
                }
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveSwitch {
                    _id "notifications"
                    _name "notifications"
                    _checked true
                }

                fveLabel {
                    _for "notifications"
                    "Notifications"
                }
            }

            fveRadioGroup {
                fveFieldLegend { "Delivery" }

                for value in [ "email"; "print" ] do
                    fveField {
                        _layout FieldLayout.Horizontal

                        fveRadio {
                            _id ("delivery-" + value)
                            _name "delivery"
                            _value value
                        }

                        fveLabel {
                            _for ("delivery-" + value)
                            value
                        }
                    }
            }

            fveField {
                fveLabel {
                    _for "account-tags"
                    "Tags"
                }

                fveTagInput {
                    _id "account-tags"
                    _name "tag"
                    _values [ "reviewed" ]
                }
            }

            fveChoiceCards {
                fveFieldLegend { "Plan" }

                fveChoiceCardsContent {
                    fveChoiceCard {
                        fveRadio {
                            _name "plan"
                            _value "basic"
                        }

                        fveChoiceCardTitle { "Basic" }
                    }

                    fveChoiceCard {
                        fveRadio {
                            _name "plan"
                            _value "team"
                        }

                        fveChoiceCardTitle { "Team" }
                    }
                }
            }

            fveField {
                fveLabel {
                    _for "demo-credential"
                    "Demo credential"
                }

                fveCopyReveal {
                    _id "demo-credential"
                    _value "safe-example"
                    _iconButtons true
                }
            }

            fveToggleButton { "Compact rows" }

            fveMultiToggleGroup {
                _id "formats"
                _name "format"
                _ariaLabel "Formatting"
                _values [ "bold" ]

                fveToggleGroupItem {
                    _value "bold"
                    "Bold"
                }

                fveToggleGroupItem {
                    _value "italic"
                    "Italic"
                }
            }

            fveDropdownMenu {
                _id "consumer-menu"
                fveDropdownMenuTrigger { fveButton { "Navigation" } }

                fveDropdownMenuItem {
                    _href (destinationUrl Home)
                    "Home"
                }
            }

            fveCardFooter { p { "Consumer-owned supporting content." } }
        }

    let currencyResults =
        fveSelectContent {
            _id "fve-select-currency-popup"

            fveSelectItem {
                _value (encodeCurrency EUR)
                "Euro"
            }
        }

    let confirmation =
        fveDialog {
            _id "consumer-confirmation"
            _label "Continue?"

            form {
                _method "dialog"

                fveButton {
                    _type "submit"
                    _id "consumer-confirmation-cancel"
                    _value "cancel"
                    "Cancel"
                }

                fveButton {
                    _type "submit"
                    _value "confirm"
                    "Continue"
                }
            }

            _description "Choose whether to continue."
            _initialFocus "consumer-confirmation-cancel"
            _role "alertdialog"
        }

    let page =
        let actions =
            fveButtonGroup {
                _orientation Orientation.Horizontal
                _ariaLabel "Account actions"
                "Accounts"
                action
                destination
            }

        main {
            fvePageHeader {
                _label "Accounts"
                fvePageHeaderActions { actions }
            }

            section {
                _class "grid gap-4"
                fveSectionHeader { _label "Details" }
                accountDetails
            }
        }
