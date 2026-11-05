module Docs.Tests.ComponentDslTests

open System.Text.RegularExpressions
open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar

type private Status =
    | Active
    | Paused

type private Destination =
    | ButtonPage
    | ExamplesPage

let private encodeStatus =
    function
    | Active -> "active"
    | Paused -> "paused"

let tests =
    testList
        "Component DSL"
        [ test "distinct component IDs cannot share generated state or anchor identities" {
              let ids = ["review-start-date"; "review_start_date"; "Review-start-date"; "review--start-date"; "1-start"; "é"; "漢字"]
              let calendars =
                  ids |> List.mapi (fun index id ->
                      fveMonthCalendarSelection {
                          _id id; _name ("date" + string index); _ariaLabel ("Date " + string index)
                          _value "2026-11-12"
                      } |> Render.toString)
              let signals = calendars |> List.map (fun html -> Regex.Match(html, "data-signals=\"\\{([^:]+):").Groups[1].Value)
              Expect.isTrue (signals |> List.forall (System.String.IsNullOrEmpty >> not)) "every calendar declares its local state"
              Expect.equal (List.distinct signals).Length ids.Length "legal distinct IDs retain independent state"
          }

          test "readonly native controls retain persistent guards alongside caller events" {
              let attributes =
                  [ _id "readonly-event-contract"; _name "value"; _ariaLabel "Readonly value"; _readonly true
                    _dataOn ("click", "el.dataset.clicked = 'yes'")
                    _attr ("data-on:click__once", "el.dataset.once = 'yes'")
                    _attr ("data-on:click__capture", "el.dataset.captured = 'yes'") ]
              for render in [ fveCheckbox; fveSwitch; fveRadio; fveFileSelection ] do
                  let html = render { for attribute in attributes do attribute } |> Render.toString
                  let control = Regex.Match(html, "<input[^>]*>").Value
                  Expect.stringContains control "data-on:click__capture=\"if (el.readOnly) evt.preventDefault();" "a persistent capture guard cannot be overwritten"
                  Expect.stringContains control "el.dataset.captured" "the caller capture callback is retained"
                  Expect.stringContains control "data-on:click__once=" "caller once behavior remains independent"
                  Expect.stringContains control "data-on:click=\"el.dataset.clicked" "ordinary callbacks still run"
          }

          test "breadcrumb overflow preserves link behavior without duplicating IDs" {
              let html =
                  fveBreadcrumbs {
                      _id "link-attribute-contract"; _ariaLabel "Breadcrumb"
                      fveBreadcrumbItem {
                          _id "ancestor-link"; _href "/components"; _target "_blank"; _rel "noopener"; _download "source.zip"
                          _dataOn ("click", "el.dataset.activated = 'yes'")
                          "Components"
                      }
                      fveBreadcrumbItem { "Current" }
                  } |> Render.toString
              let overflow = Regex.Match(html, "<a(?=[^>]*role=\"menuitem\")[^>]*>").Value
              for expected in ["href=\"/components\""; "target=\"_blank\""; "rel=\"noopener\""; "download=\"source.zip\""; "el.dataset.activated"] do
                  Expect.stringContains overflow expected "overflow retains the same link semantics"
              Expect.equal (Regex.Matches(html, "id=\"ancestor-link\"").Count) 1 "the native ancestor ID stays unique"
          }

          test "native presentation elements consume only their component configuration" {
              let examples =
                  [ fveCard { _size Size.Small; _data ("consumer", "card"); "Body" }, ["size"]
                    fveBadge { _variant BadgeVariant.Outline; _color Color.Success; "Ready" }, ["variant"; "color"]
                    fveLink { _href "/components"; _variant LinkVariant.Outline; _size Size.Small; _color Color.Primary; "Components" }, ["variant"; "size"; "color"]
                    fveField { _layout FieldLayout.Horizontal; "Field" }, ["layout"] ]
              for element, consumed in examples do
                  let html = Render.toString element
                  for name in consumed do
                      Expect.isFalse (Regex.IsMatch(html, "\\s" + name + "=")) "component configuration does not become invalid native attributes"
              Expect.stringContains (Render.toString (fst examples.Head)) "data-consumer=\"card\"" "ordinary caller data attributes remain intact"
          }

          test "migrated component Usage sections teach the DSL rather than the old pipeline" {
              for key, builder, legacy in
                  [ "button", "fveButton", "Button.create"
                    "badge", "fveBadge", "Badge.create"
                    "kbd", "fveKbd", "Kbd.create"
                    "separator", "fveSeparator", "Separator.create"
                    "skeleton", "fveSkeleton", "Skeleton.create"
                    "avatar", "fveAvatar", "Avatar.create"
                    "progress", "fveProgress", "Progress.create"
                    "loading-indicator", "fveLoadingIndicator", "LoadingIndicator.create"
                    "empty-state", "fveEmptyState", "EmptyState.create"
                    "metric", "fveMetric", "Metric.create"
                    "callout", "fveCallout", "Callout.create"
                    "table", "fveTable", "Table.create"
                    "button-group", "fveButtonGroup", "ButtonGroup.create"
                    "link", "fveLink", "Button.renderLink"
                    "card", "fveCard", "Card.create"
                    "input", "fveInput", "Input.create"
                    "checkbox", "fveCheckbox", "Checkbox.create"
                    "choice-cards", "fveChoiceCards", "ChoiceCards.single"
                    "tag-input", "fveTagInput", "TagInput.create"
                    "copy-reveal", "fveCopyReveal", "CopyReveal.create"
                    "page-top-bar", "fvePageTopBar", "PageTopBar.create"
                    "page-header", "fvePageHeader", "PageHeader.create"
                    "section-header", "fveSectionHeader", "SectionHeader.create"
                    "description-list", "fveDescriptionList", "DescriptionList.create"
                    "pagination", "fvePagination", "Pagination.create"
                    "file-selection", "fveFileSelection", "FileSelection.create"
                    "error-summary", "fveErrorSummary", "ErrorSummary.create"
                    "notice", "fveNotice", "Notice.create"
                    "notification", "fveNotification", "Notification.create"
                    "message", "fveMessage", "Message.create"
                    "browser", "fveBrowser", "Browser.create"
                    "phone", "fvePhone", "Phone.create"
                    "code-block", "fveCodeBlock", "CodeBlock.create"
                    "mermaid", "fveMermaid", "Mermaid.create"
                    "fsharp-api-reference", "fveFSharpApiReference", "FSharpApiReference.create"
                    "tooltip", "fveTooltip", "Tooltip.create"
                    "breadcrumbs", "fveBreadcrumbs", "Breadcrumbs.create"
                    "resizable", "fveResizable", "Resizable.create"
                    "tabs", "fveTabs", "Tabs.create"
                    "popover", "fvePopover", "Popover.create"
                    "floating-panel", "fveFloatingPanel", "FloatingPanel.create"
                    "command", "fveCommand", "Command.create"
                    "example", "fveExample", "Example.gallery"
                    "chart", "fveChart", "Chart.create"
                    "date-picker", "fveDatePicker", "DatePicker.create"
                    "day-calendar", "fveDayCalendar", "DayCalendar.create"
                    "week-calendar", "fveWeekCalendar", "WeekCalendar.create"
                    "month-calendar", "fveMonthCalendar", "MonthCalendar.create"
                    "year-calendar", "fveYearCalendar", "YearCalendar.create"
                    "side-nav", "fveSideNav", "SideNav.create"
                    "bottom-navigation", "fveBottomNavigation", "BottomNavigation.create"
                    "dialog", "fveDialog", "Dialog.create"
                    "drawer", "fveDrawer", "Drawer.create"
                    "switch", "fveSwitch", "Switch.create"
                    "toggle-button", "fveToggleButton", "ToggleButton.create"
                    "radio-group", "fveRadioGroup", "RadioGroup.create"
                    "toggle-group", "fveToggleGroup", "ToggleGroup.single"
                    "field-group", "fveFieldGroup", "FieldGroup.create"
                    "select", "fveSelect", "Select.create"
                    "dropdown-menu", "fveDropdownMenu", "DropdownMenu.create" ] do
                  let path = "/components/" + key

                  let page =
                      Docs.Pages.Components.tryPage path
                      |> Option.orElseWith (fun () -> Docs.Pages.ComponentDocumentation.tryPage path)
                      |> Option.get

                  let content =
                      page.sections |> List.find (fun section -> section.id = "usage") |> _.content

                  let html = fragment { content } |> Render.toString
                  Expect.stringContains html (builder + " {") $"{key} Usage shows its argument-free installed builder"
                  Expect.isFalse (html.Contains legacy) $"{key} Usage does not teach the superseded entry point"

                  if key = "button" then
                      Expect.stringContains
                          html
                          "_dataIndicator"
                          "the working example uses the native request indicator"

                      Expect.stringContains html "_dataOn" "the initial button performs a real action"

                      Expect.equal
                          (Regex.Matches(html, "<p(?:>| )").Count)
                          0
                          "the example teaches usage without explanatory paragraphs"

              for example in Docs.Pages.Components.examplesFor "button" do
                  Expect.isFalse
                      (example.source.Contains "ButtonContent")
                      $"{example.title} composes ordinary children without content modes"

                  Expect.isFalse
                      (Regex.IsMatch(
                          example.source,
                          @"\bButton\.(?:create|render\w*|with\w+|color|variant|size|pending|content)\b"
                      ))
                      $"{example.title} teaches attributes rather than pipeline functions"

                  Expect.isFalse
                      (Regex.IsMatch(example.source, @"\b(buttonColor|buttonVariant|controlSize|ButtonColor)\b"))
                      $"{example.title} uses ordinary named Button attributes"

              for example in Docs.Pages.ComponentDocumentation.examples "card" do
                  Expect.isFalse
                      (Regex.IsMatch(example.source, @"\bCard\.(?:create|render|with\w+|header|footer|media|size)\b"))
                      $"{example.title} teaches scoped Card operations rather than option functions"

              for example in Docs.Pages.Components.examplesFor "select" do
                  Expect.isTrue
                      (Regex.IsMatch(example.source, @"\bfve(?:Multi)?Select\s*\{"))
                      $"{example.title} supplies a complete Select CE"

                  Expect.isFalse
                      (Regex.IsMatch(
                          example.source,
                          @"\bSelect\.(?:create|render\w*|with\w+|multiple|disabled|pending|loading|required)\b"
                      ))
                      $"{example.title} uses the CE for states, forms, and remote result patches"
          }

          test "native primitive builders compose attributes and content without runtime or legacy configuration" {
              let attributes =
                  [ _attr ("ID", "first"); _id "last"; _class "first-class"; _class "last-class" ]

              let badge =
                  fveBadge {
                      yield! attributes
                      _color Color.Success
                      _variant BadgeVariant.Outline
                      strong { "<Ready>" }
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(badge, "(?i)\\sid=").Count)
                  1
                  "scalar attributes are deduplicated case-insensitively"

              Expect.stringContains badge "id=\"last\"" "the last scalar wins"
              Expect.stringContains badge "first-class last-class" "classes compose"
              Expect.stringContains badge "<strong>&lt;Ready&gt;</strong>" "rich content keeps normal encoding"
              Expect.isFalse (badge.Contains "aria-live") "badges do not become live regions"

              let keys =
                  fveKbdShortcut {
                      _ariaLabel "Open search"

                      for key in [ "Control"; "K" ] do
                          fveKbd { key }
                  }
                  |> Render.toString

              Expect.equal (Regex.Matches(keys, "<kbd ").Count) 2 "shortcut children are native keys"
              Expect.equal (Regex.Matches(keys, ">\\+</span>").Count) 1 "keys have one presentational separator"

              let skeleton =
                  fveSkeletonRegion {
                      _ariaLabel "Loading records"

                      for _ in 1..2 do
                          fveSkeleton { _class "h-24 w-full rounded-xl" }
                  }
                  |> Render.toString

              Expect.equal (Regex.Matches(skeleton, "role=\"status\"").Count) 1 "the region alone owns status"

              Expect.equal
                  (Regex.Matches(skeleton, "aria-hidden=\"true\"").Count)
                  2
                  "placeholders remain presentational"

              let classSized = fveSkeleton { _class "h-4 w-64 rounded-full" } |> Render.toString
              Expect.isFalse (classSized.Contains "style=") "class sizing has no competing inline dimensions"

              let explicitSize =
                  fveSkeleton {
                      _class "h-8 w-full"
                      Html._width "16rem"
                      Html._height "1rem"
                  }
                  |> Render.toString

              Expect.stringContains
                  explicitSize
                  "width: 16rem; height: 1rem"
                  "explicit CSS dimensions are serialized as styles"

              Expect.isFalse
                  (Regex.IsMatch(explicitSize, @"\s(?:width|height)="))
                  "dimensions are not invalid span attributes"

              let widthOnly = fveSkeleton { Html._width "50%" } |> Render.toString
              Expect.isFalse (widthOnly.Contains "height:") "one explicit axis does not invent another"

              let progress =
                  fveProgress {
                      _ariaLabel "Import"
                      _value "1.5"
                      _max "2"
                      _state ProgressState.Failed
                      p { "Try again." }
                  }
                  |> Render.toString

              Expect.stringContains progress "<progress" "progress remains native"
              Expect.stringContains progress "75%" "fractional native values produce the correct percentage"
              Expect.stringContains progress "role=\"alert\"" "the explicit failure state is announced"
              Expect.stringContains progress "Try again." "supporting content is ordinary HTML"

              Expect.throws
                  (fun () ->
                      fveProgress {
                          _ariaLabel "Import"
                          _value "3"
                          _max "2"
                      }
                      |> ignore)
                  "invalid progress is rejected"

              Expect.throws
                  (fun () ->
                      fveProgress {
                          _ariaLabel "Import"
                          _state ProgressState.Complete
                      }
                      |> ignore)
                  "indeterminate completion is rejected"

              Expect.throws
                  (fun () -> fveSkeletonRegion { _ariaLabel " " } |> ignore)
                  "loading regions require a meaningful name"

              Expect.throws (fun () -> fveAvatar { "AM" } |> ignore) "non-decorative avatars require a name"

              let avatar =
                  fveAvatar {
                      _ariaHidden true
                      _size Size.Small
                      "AM"
                  }
                  |> Render.toString

              Expect.stringContains avatar "size-8" "Avatar shares the qualified Size attribute"
              Expect.stringContains avatar "aria-hidden=\"true\"" "decorative avatars stay hidden"
          }

          test "typed component attributes preserve their HTML wire values and native overloads" {
              for name, values in
                  [ "color",
                    [ _color Color.Primary, "primary"
                      _color Color.Secondary, "secondary"
                      _color Color.Success, "success"
                      _color Color.Warning, "warning"
                      _color Color.Error, "error"
                      _color Color.Info, "info"
                      _color Color.Neutral, "neutral"
                      _color Color.Custom, "custom" ]
                    "variant",
                    [ _variant ButtonVariant.Solid, "solid"
                      _variant ButtonVariant.Soft, "soft"
                      _variant ButtonVariant.Outline, "outline"
                      _variant ButtonVariant.Ghost, "ghost"
                      _variant LinkVariant.Text, "text"
                      _variant LinkVariant.Solid, "solid"
                      _variant LinkVariant.Soft, "soft"
                      _variant LinkVariant.Outline, "outline"
                      _variant LinkVariant.Ghost, "ghost"
                      _variant BadgeVariant.Solid, "solid"
                      _variant BadgeVariant.Soft, "soft"
                      _variant BadgeVariant.Outline, "outline"
                      _variant BadgeVariant.Ghost, "ghost" ]
                    "size",
                    [ _size Size.ExtraSmall, "extra-small"
                      _size Size.Small, "small"
                      _size Size.Medium, "medium"
                      _size Size.Large, "large"
                      _size Size.ExtraLarge, "extra-large" ]
                    "density",
                    [ _density Density.Compact, "compact"
                      _density Density.Comfortable, "comfortable" ]
                    "surface", [ _surface Surface.Panel, "panel"; _surface Surface.Plain, "plain" ]
                    "mobile-layout",
                    [ _mobileLayout MobileLayout.Scroll, "scroll"
                      _mobileLayout MobileLayout.Records, "records" ]
                    "data-mobile-cell",
                    [ _mobile Mobile.Field, "field"
                      _mobile Mobile.Primary, "primary"
                      _mobile Mobile.Summary, "summary"
                      _mobile Mobile.Actions, "actions" ] ] do
                  for attribute, wire in values do
                      Expect.equal attribute.Name name "typed helpers remain ordinary HTML attributes"

                      Expect.equal
                          attribute.Value
                          (ValueSome wire)
                          "union case names do not change the browser protocol"

              Expect.equal (Html._size 12).Value (ValueSome "12") "native HTML integer size remains available"

              Expect.equal
                  (Html._color "#ffffff").Value
                  (ValueSome "#ffffff")
                  "native HTML color strings remain available"
          }

          test "Table emits a thin host and inert sections containing native rows and cells" {
              let rows = [ "Alex & Jamie"; "<Riley>" ]

              let html =
                  fveTable {
                      _id "members"
                      _ariaLabel "Members"
                      _density Density.Comfortable
                      fveTableHeader { fveTableRow { fveTableHead { "Name" } } }

                      fveTableBody {
                          _id "member-rows"

                          for name in rows do
                              fveTableRow {
                                  fveTableCell {
                                      _colspan 2
                                      name
                                  }
                              }

                          fveTableRow {
                              _id "more-members"
                              _dataOnIntersect ([ "once" ], "@get(el.dataset.next)")
                              _dataAttr ("data-next", "$nextMembers")
                              fveTableCell { "Loading…" }
                          }
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "<fve-table"
                    "aria-label=\"Members\""
                    "density=\"comfortable\""
                    "<template data-fve-table-part=\"head\""
                    "<template data-fve-table-part=\"body\" id=\"member-rows\""
                    "<tr"
                    "<th"
                    "<td"
                    "colspan=\"2\""
                    "Alex &amp; Jamie"
                    "&lt;Riley&gt;"
                    "data-on-intersect__once" ] do
                  Expect.stringContains
                      html
                      expected
                      "attributes, typed F# loops and encoded rich content remain server-authored"

              for generated in
                  [ "<table "
                    "<thead"
                    "<tbody"
                    "<fve-table-row"
                    "<fve-table-cell"
                    "overflow-x-auto" ] do
                  Expect.isFalse
                      (html.Contains generated)
                      "the runtime owns the live table and its presentation, not custom row/cell wrappers"

              let row =
                  fveTableRow {
                      _id "new-member"
                      fveTableCell { "New member" }
                  }
                  |> Render.toString

              Expect.isTrue
                  (row.StartsWith "<tr ")
                  "the same row renderer is usable in a standalone Datastar replacement"

              Expect.isFalse (row.Contains "template") "row patches need no component-specific envelope"

              let repeated =
                  fveTable {
                      _ariaLabel "Members"
                      _density Density.Compact
                      _density Density.Comfortable
                      _class "border"
                      _class "h-64"
                  }
                  |> Render.toString

              Expect.equal (Regex.Matches(repeated, " density=").Count) 1 "scalar attributes have one owner"
              Expect.stringContains repeated "density=\"comfortable\"" "the last scalar attribute wins"

              Expect.stringContains
                  repeated
                  "class=\"border h-64\""
                  "authored classes compose without replacing earlier utilities"

              for invalid in
                  [ (fun () -> fveTable { () })
                    (fun () ->
                        fveTable {
                            _ariaLabel "Members"
                            _attr ("density", "tiny")
                        })
                    (fun () ->
                        fveTable {
                            _ariaLabel "Members"
                            _role "grid"
                        }) ] do
                  Expect.throwsT<System.ArgumentException>
                      (fun () -> invalid () |> ignore)
                      "labels and component attributes are validated"
          }

          test "nested composition mixes options, attributes, encoded content, loops, and conditionals" {
              let example includeExtras =
                  fveCard {
                      _size (if includeExtras then Size.Small else Size.Medium)
                      fveCardHeader { h2 { "Accounts" } }

                      fveCardFooter {
                          fveButton {
                              _variant ButtonVariant.Solid
                              _size Size.Small
                              _type "submit"
                              "Save"
                          }
                      }

                      fveCardMedia {
                          img {
                              _src "/social-card.png"
                              _alt "Card illustration"
                          }
                      }

                      _id "account-card"
                      _class "max-w-lg"

                      if includeExtras then
                          p { "Optional" }

                      for account in [ "Cash & savings"; "<Expenses>" ] do
                          p { account }

                      yield! [ p { "Last updated today" } ]
                  }

              let html = example true |> Render.toString

              for expected in
                  [ "id=\"account-card\""
                    "max-w-lg"
                    "--fve-card-inset:0.75rem"
                    "<h2>Accounts</h2>"
                    "alt=\"Card illustration\""
                    "Cash &amp; savings"
                    "&lt;Expenses&gt;"
                    "Last updated today"
                    "type=\"submit\""
                    ">Save</fve-button>" ] do
                  Expect.stringContains html expected "the public builder composes real HTML"

              let positions =
                  [ "<img"; "<h2>Accounts</h2>"; "Cash &amp; savings"; ">Save</fve-button>" ]
                  |> List.map (fun marker -> html.IndexOf(marker, System.StringComparison.Ordinal))

              Expect.equal
                  positions
                  (List.sort positions)
                  "regions render in media/header/body/footer order, regardless of operation order"

              for attribute in [ " header="; " footer="; " media="; " cardSize=" ] do
                  Expect.isFalse
                      (html.Contains attribute)
                      "Card configuration does not become fictional HTML attributes"

              let next = example false |> Render.toString
              Expect.isFalse (next.Contains "Optional") "conditional children do not persist between evaluations"
              let independent = fveCard { "Independent" } |> Render.toString

              for previous in
                  [ "account-card"
                    "max-w-lg"
                    "<h2>"
                    "<button"
                    "<img"
                    "p-3"
                    "Last updated today" ] do
                  Expect.isFalse
                      (independent.Contains previous)
                      "a reused builder does not retain regions, size, attributes, or children"

              let emptyParts =
                  fveCard {
                      fveCardHeader { () }
                      fveCardFooter { () }
                      fveCardMedia { () }
                  }
                  |> Render.toString

              for region in [ "data-fve-card-header"; "data-fve-card-footer"; "data-fve-card-media" ] do
                  Expect.isFalse (emptyParts.Contains region) "empty optional regions do not render wrappers"
          }

          test "button host owns authored attributes and request state without generating native internals" {
              let html =
                  fveButton {
                      _color Color.Primary
                      _variant ButtonVariant.Solid
                      _size Size.Small
                      _id "sync"
                      _type "submit"
                      _name "action"
                      _value "sync"
                      _ariaLabel "Sync accounts"
                      _class "w-full"
                      _dataIndicator "_sync_pending"
                      _dataOn ("click", "@get('/components/button/settled')")
                      "Sync"
                  }
                  |> Render.toString

              for expected in
                  [ "id=\"sync\""
                    "type=\"submit\""
                    "name=\"action\""
                    "value=\"sync\""
                    "aria-label=\"Sync accounts\""
                    "w-full"
                    "data-indicator:_sync_pending"
                    "data-attr:disabled=\"$_sync_pending\""
                    "data-attr:aria-busy"
                    "<fve-button"
                    "color=\"primary\""
                    "variant=\"solid\""
                    "size=\"small\"" ] do
                  Expect.stringContains html expected "the builder preserves native and Datastar behavior"

              Expect.equal (Regex.Matches(html, " type=").Count) 1 "native attributes have one authored owner"
              Expect.isFalse (html.Contains "<button") "Rocket, not F#, creates the native control"
              Expect.isFalse (html.Contains "data-fve-button-spinner") "Rocket owns the spinner markup"

              let disabled =
                  fveButton {
                      _dataIndicator "_busy"
                      _disabled true
                      "Save"
                  }
                  |> Render.toString

              Expect.stringContains
                  disabled
                  "data-attr:disabled=\"true\""
                  "pending completion cannot enable an explicitly disabled control"

              let icon =
                  fveButton {
                      _ariaLabel "Add account"

                      span {
                          _ariaHidden true
                          "+"
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  icon
                  "aria-label=\"Add account\""
                  "ordinary icon children use a native accessible name"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _type "link"
                          "Invalid"
                      }
                      |> ignore)
                  "unsupported native button types are rejected"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _dataIndicator "$invalid"
                          "Invalid"
                      }
                      |> ignore)
                  "indicators take signal names rather than expressions"

              for indicator, signal in
                  [ _dataIndicator "requests.sync-pending", "requests.syncPending"
                    _dataIndicator ("sync-pending", [ "case.snake" ]), "sync_pending"
                    _dataIndicator ("sync-pending", [ "case.pascal" ]), "SyncPending"
                    _attr ("data-indicator", "requests.syncPending"), "requests.syncPending" ] do
                  let html =
                      fveButton {
                          indicator
                          "Sync"
                      }
                      |> Render.toString

                  Expect.stringContains
                      html
                      ($"data-attr:disabled=\"${signal}\"")
                      "default bindings use Datastar's actual indicator path and casing"

              let external =
                  fveButton {
                      _dataAttr ("disabled", "$form.pending")
                      _dataAttr ("aria-busy", "$form.pending ? 'true' : null")
                      "Save"
                  }
                  |> Render.toString

              Expect.stringContains external "data-attr:disabled=\"$form.pending\"" "forms own their request tracking"

              Expect.isFalse
                  (external.Contains "data-fve-button-spinner")
                  "busy presentation remains entirely runtime-owned"

              Expect.isFalse
                  (external.Contains "data-indicator")
                  "an external binding does not attach a competing request indicator"

              let initial =
                  fveButton {
                      _ariaBusy true
                      _disabled true
                      "Save"
                  }
                  |> Render.toString

              Expect.stringContains initial "aria-busy=\"true\"" "static native busy state passes through"
              Expect.isFalse (initial.Contains "data-attr:aria-busy") "static busy presentation needs no client binding"

              let custom =
                  fveButton {
                      _dataIndicator "_request"
                      _dataAttr ("aria-busy", "$_request || $form.pending")
                      _dataAttr ("disabled", "$form.pending")
                      "Save"
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(custom, " data-attr:aria-busy=").Count)
                  1
                  "explicit native busy bindings replace defaults without duplicate attributes"

              Expect.stringContains custom "$_request || $form.pending" "consumer busy expressions remain intact"

              Expect.stringContains
                  custom
                  "data-attr:disabled=\"$form.pending\""
                  "explicit native disabled bindings replace defaults"
          }

          test "button real named attributes compose as ordinary F# values" {
              let named size =
                  fveButton {
                      if size = Size.Small then
                          _color Color.Primary

                      _variant ButtonVariant.Solid
                      _size size
                      _dataAttr ("variant", "'soft'")
                      _type "submit"
                      _name "action"
                      _value "save"
                      "Save"
                  }
                  |> Render.toString

              let html = named Size.Small
              Expect.stringContains html "<fve-button" "Rocket receives a real custom element"

              Expect.stringContains
                  html
                  "color=\"primary\" variant=\"solid\" size=\"small\""
                  "named helpers produce real observed attributes"

              Expect.isFalse (html.Contains "<button") "the renderer emits a thin custom-element host"

              Expect.stringContains
                  html
                  "name=\"action\" value=\"save\""
                  "the host carries the native submission contract"

              let hostTag =
                  System.Text.RegularExpressions.Regex.Match(html, "<fve-button[^>]*>").Value

              Expect.stringContains
                  hostTag
                  "data-attr:variant="
                  "appearance bindings target the observed component prop"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _attr ("size", "unsupported")
                          "Invalid"
                      }
                      |> ignore)
                  "raw attribute escape hatches remain validated"
          }

          test "button named attributes compose with conditional values and ordinary child control flow" {
              let render showExtras =
                  let color = Color.Primary
                  let variant = ButtonVariant.Soft
                  let size = Size.Small

                  fveButton {
                      _color (if showExtras then color else Color.Secondary)
                      _variant variant
                      _size size
                      _dataOn ("click", "@get('/components/button/settled')")
                      span { "Sync & review" }

                      if showExtras then
                          span { "Optional" }

                      for number in [ 1; 2 ] do
                          span { string number }

                      yield! [ span { "Extra" } ]
                  }
                  |> Render.toString

              let html = render true

              for expected in
                  [ "<span>Sync &amp; review</span>"
                    "<span>Optional</span>"
                    "<span>1</span>"
                    "<span>2</span>"
                    "<span>Extra</span>"
                    "data-on:click" ] do
                  Expect.stringContains html expected "configuration keywords do not restrict normal child composition"

              for attribute in [ " buttonColor="; " buttonVariant="; " controlSize="; " pendingSignal=" ] do
                  Expect.isFalse
                      (html.Contains attribute)
                      "component configuration is not emitted as fictional HTML attributes"

              Expect.isFalse ((render false).Contains "Optional") "a reused builder does not retain child state"

              Expect.isFalse
                  ((fveButton { "Idle" } |> Render.toString).Contains "data-attr:aria-busy=")
                  "a reused builder does not retain pending configuration"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _href "/examples"
                          "Invalid"
                      }
                      |> ignore)
                  "buttons reject destinations instead of changing elements or emitting invalid HTML"
          }

          test "button groups compose ordinary controls, strings, fragments, and conditional children" {
              let render includeExtras =
                  fveButtonGroup {
                      _orientation (
                          if includeExtras then
                              Orientation.Vertical
                          else
                              Orientation.Horizontal
                      )

                      _ariaLabel "Account actions"
                      _id "account-actions"
                      _class "consumer-group"
                      "Accounts & balances"

                      if includeExtras then
                          fveButton {
                              _name "action"
                              _value "archive"
                              "Archive"
                          }

                      for href, label in [ "/components/button", "Button"; "/components/link", "Link" ] do
                          fveLink {
                              _href href
                              label
                          }

                      fragment {
                          fveButton {
                              _type "submit"
                              "Save"
                          }
                      }
                  }
                  |> Render.toString

              let html = render true

              for expected in
                  [ "role=\"group\""
                    "aria-label=\"Account actions\""
                    "id=\"account-actions\""
                    "consumer-group"
                    "Accounts &amp; balances"
                    "name=\"action\""
                    "value=\"archive\""
                    "href=\"/components/link\""
                    "type=\"submit\""
                    "flex-col" ] do
                  Expect.stringContains html expected "group composition preserves native child semantics"

              Expect.equal
                  (Regex.Matches(html, "<fve-button ").Count)
                  2
                  "conditional and fragment children remain ordinary component hosts"

              Expect.equal (Regex.Matches(html, "<a ").Count) 2 "each loop iteration remains a native link"
              Expect.isFalse ((render false).Contains "Archive") "conditional controls do not persist"

              let independent =
                  fveButtonGroup {
                      _ariaLabel "Independent"
                      fveButton { "Next" }
                  }
                  |> Render.toString

              for previous in [ "account-actions"; "consumer-group"; "Archive"; "flex-col" ] do
                  Expect.isFalse (independent.Contains previous) "each group has fresh state"

              Expect.throws
                  (fun () -> fveButtonGroup { fveButton { "Unnamed" } } |> ignore)
                  "a group needs an accessible label"

              Expect.throws (fun () -> fveButtonGroup { _ariaLabel "Empty" } |> ignore) "an empty group is rejected"

              Expect.isFalse
                  (html.Contains " buttonGroupOrientation=")
                  "configuration does not become a fictional HTML attribute"
          }

          test "links own native destinations, attributes, and encoded content without button behavior" {
              let html =
                  fveLink {
                      _href "/examples?from=components&sort=name"
                      _target "_blank"
                      _rel "noopener"
                      _attr ("download", "examples.html")
                      _class "consumer-link"
                      _style "letter-spacing:0.01em"
                      _ariaLabel "Browse examples"
                      "Examples & guides"
                  }
                  |> Render.toString

              for expected in
                  [ "<a href=\"/examples?from=components&amp;sort=name\""
                    "target=\"_blank\""
                    "rel=\"noopener\""
                    "download=\"examples.html\""
                    "consumer-link"
                    "letter-spacing:0.01em"
                    "aria-label=\"Browse examples\""
                    "Examples &amp; guides"
                    "underline" ] do
                  Expect.stringContains
                      html
                      expected
                      "text links preserve native attributes and consumer-owned presentation"

              for unsupported in
                  [ "<button"
                    "type=\"button\""
                    "data-on:"
                    "data-attr:disabled"
                    "aria-busy=\"" ] do
                  Expect.isFalse (html.Contains unsupported) "a Link does not inherit button behavior"

              Expect.throws
                  (fun () -> fveLink { "Missing destination" } |> ignore)
                  "a link needs an explicit destination"

              for href in [ ""; " " ] do
                  Expect.throws
                      (fun () ->
                          fveLink {
                              _href href
                              "Invalid"
                          }
                          |> ignore)
                      "blank destinations do not silently fall back to buttons"

              Expect.throws
                  (fun () ->
                      fveLink {
                          _href "/examples"
                          _href "/components"
                          "Ambiguous"
                      }
                      |> ignore)
                  "there is one owner for the destination"

              for attribute in
                  [ _disabled true
                    _type "submit"
                    _type "reset"
                    _attr ("formaction", "/examples") ] do
                  Expect.throws
                      (fun () ->
                          fveLink {
                              _href "/examples"
                              attribute
                              "Invalid"
                          }
                          |> ignore)
                      "links reject button-only attributes"
          }

          test "link variants remain anchors and compose without leaking configuration" {
              let render (treatment: LinkVariant) showExtras =
                  fveLink {
                      _variant treatment
                      _color Color.Error
                      _size Size.Small
                      _href "/examples"

                      if showExtras then
                          strong { "Optional" }

                      for number in [ 1; 2 ] do
                          span { string number }

                      yield! [ span { "More & examples" } ]
                  }
                  |> Render.toString

              for treatment in
                  [ LinkVariant.Text
                    LinkVariant.Solid
                    LinkVariant.Soft
                    LinkVariant.Outline
                    LinkVariant.Ghost ] do
                  let html = render treatment true

                  for expected in
                      [ "<a href=\"/examples\""
                        "--fve-error-text"
                        "<strong>Optional</strong>"
                        "<span>1</span>"
                        "<span>2</span>"
                        "More &amp; examples" ] do
                      Expect.stringContains
                          html
                          expected
                          "the variant changes presentation, not semantics or content composition"

                  for attribute in [ " linkVariant="; " linkColor="; " controlSize=" ] do
                      Expect.isFalse
                          (html.Contains attribute)
                          "typed configuration does not become fictional HTML attributes"

              Expect.isFalse
                  ((render LinkVariant.Outline false).Contains "Optional")
                  "child state is fresh per evaluation"

              let plain =
                  fveLink {
                      _href "/components"
                      "Independent"
                  }
                  |> Render.toString

              for previous in [ "--fve-error-text"; "Optional"; "More &amp; examples"; "min-h-" ] do
                  Expect.isFalse
                      (plain.Contains previous)
                      "a subsequent text link does not retain control styling, palette, or children"
          }

          test "Chart serializes ECharts options at a thin editable host boundary" {
              let options =
                  {| xAxis =
                      {| ``type`` = "category"
                         data = [| "September" |] |}
                     yAxis = {| ``type`` = "value" |}
                     series =
                      [| {| name = "Actual <balance>"
                            ``type`` = "bar"
                            data = [| 20676M |] |} |] |}

              let render options =
                  fveChart {
                      _id "typed-chart"
                      _ariaLabel "Balances"
                      _options options
                      _events [ "click"; "datazoom" ]
                      _empty false
                      _class "consumer-chart"
                  }

              let html = render options |> Render.toString
              Expect.stringContains html "<fve-chart" "the custom element owns chart rendering and lifecycle"
              Expect.stringContains html "consumer-chart" "native host classes compose"
              Expect.isFalse (html.Contains "<script") "the application owns all script delivery"
              let encoded = Regex.Match(html, " options=\"([^\"]+)\"").Groups[1].Value

              use document =
                  System.Text.Json.JsonDocument.Parse(System.Net.WebUtility.HtmlDecode encoded)

              let series = document.RootElement.GetProperty("series").[0]
              Expect.equal (series.GetProperty("type").GetString()) "bar" "the caller chooses the ECharts series type"

              Expect.equal
                  (series.GetProperty("name").GetString())
                  "Actual <balance>"
                  "text survives JSON and HTML encoding once"

              Expect.equal (series.GetProperty("data").[0].GetDecimal()) 20676M "decimals remain JSON numbers"

              Expect.equal
                  (document.RootElement.GetProperty("xAxis").GetProperty("type").GetString())
                  "category"
                  "ECharts property names retain their case"

              for invalid in [ box [| 1; 2 |]; box "not an option object"; null ] do
                  Expect.throws (fun () -> render invalid |> ignore) "only option objects cross the host boundary"

              Expect.throws
                  (fun () ->
                      fveChart {
                          _id "invalid-event-chart"
                          _ariaLabel "Balances"
                          _options options
                          _events [ "click onclick" ]
                      }
                      |> ignore)
                  "forwarded event names must be valid ECharts event identifiers"
          }

          test "Date controls distinguish read-only, disabled and busy without owning labels" {
              let calendar attributes =
                  fveMonthCalendarSelection {
                      _id "native-date"
                      _name "date"
                      _ariaLabel "Date"
                      _value "2026-09-17"
                      _displayMonth (System.DateOnly(2026, 9, 1))
                      yield! (attributes: HtmlAttribute list)
                  }
                  |> Render.toString

              let readOnly = calendar [ _readonly true ]

              Expect.stringContains
                  readOnly
                  "aria-disabled=\"true\""
                  "read-only dates expose their unavailable editing action"

              Expect.isFalse (Regex.IsMatch(readOnly, "<input[^>]* disabled")) "read-only values remain successful"
              let disabled = calendar [ _disabled true ]
              Expect.isTrue (Regex.IsMatch(disabled, "<input[^>]* disabled")) "disabled values are not submitted"
              let busy = calendar [ _ariaBusy true ]
              Expect.stringContains busy "aria-busy=\"true\"" "busy state is exposed independently"

              Expect.isFalse
                  (Regex.IsMatch(busy, "<(?:input|button)[^>]*\\sdisabled(?:\\s|=|>)"))
                  "busy does not implicitly disable values or date choices"

              let picker =
                  fveDatePicker {
                      _id "due-date"
                      _name "due"
                      _value "2026-09-17"
                      _form "details"
                      _readonly true
                  }
                  |> Render.toString

              Expect.isTrue
                  (Regex.IsMatch(picker, "<button[^>]*id=\"due-date\""))
                  "the public ID belongs to the labelable native button"

              Expect.isFalse (picker.Contains "<label") "visible labels remain caller-owned"
              Expect.stringContains picker "form=\"details\"" "native values retain external form association"

              Expect.stringContains
                  picker
                  "value=\"2026-09-17\""
                  "date-only form values retain their canonical wire representation"
          }

          test "Calendar builders preserve native attributes and explicit date destinations" {
              let date = System.DateOnly(2026, 9, 17)

              let html =
                  fveMonthCalendar {
                      _id "native-calendar"
                      _label "Schedule"
                      _date date
                      _layout MonthCalendarLayout.Compact
                      _class "consumer-calendar"

                      fveCalendarPrevious {
                          _id "previous-month"
                          _href "/components/month-calendar"
                          _class "consumer-previous"
                      }

                      fveCalendarDate {
                          _date date
                          _href "/components/day-calendar"
                          _class "consumer-date"
                      }

                      CalendarEvent.create "session" "Session" date "/components/day-calendar"
                  }
                  |> Render.toString

              Expect.stringContains html "id=\"native-calendar\"" "the native root owns its ID"
              Expect.stringContains html "consumer-calendar" "native classes compose with the calendar layout"
              Expect.stringContains html "id=\"previous-month\"" "navigation parts preserve native attributes"
              Expect.stringContains html "consumer-previous" "navigation classes compose with owned presentation"
              Expect.stringContains html "consumer-date" "date links preserve their native attributes"

              Expect.equal
                  (Regex.Matches(html, "href=\"/components/day-calendar\"").Count)
                  1
                  "only explicitly supplied dates become links in compact calendars"

              Expect.equal
                  (Calendar.compactMonthDates date |> List.length)
                  42
                  "standalone compact months expose six weeks for application URL mapping"

              let duplicateDates () =
                  fveDayCalendar {
                      _label "Duplicate date"
                      _date date

                      fveCalendarDate {
                          _date date
                          _href "/components/day-calendar"
                      }

                      fveCalendarDate {
                          _date date
                          _href "/components/week-calendar"
                      }
                  }
                  |> ignore

              Expect.throws duplicateDates "date destinations cannot silently overwrite one another"
          }

          test "Code and diagram builders preserve literal source and native root attributes" {
              let source = "<tag>&\r\n    exact indentation"

              let code =
                  fveCodeBlock {
                      _id "literal-source"
                      _class "custom-code"
                      _language "fs"
                      source
                  }
                  |> Render.toString

              for expected in
                  [ "id=\"literal-source\""
                    "custom-code"
                    "language-fsharp"
                    "&lt;tag&gt;&amp;&#13;&#10;    exact indentation" ] do
                  Expect.stringContains code expected "source is encoded once without normalizing whitespace"

              let diagram =
                  fveMermaid {
                      _id "literal-diagram"
                      "flowchart LR\n  A[<Input>] --> B[Result]"
                  }
                  |> Render.toString

              Expect.stringContains diagram "id=\"literal-diagram\"" "native identity is retained"
              Expect.stringContains diagram "&lt;Input&gt;" "diagram source is encoded"

              Expect.throws
                  (fun () -> fveCodeBlock { p { "Not source text" } } |> ignore)
                  "code does not silently serialize HTML children"

              Expect.throws (fun () -> fveMermaid { " " } |> ignore) "empty diagrams are rejected"

              let reference =
                  fveFSharpApiReference {
                      _id "size-api"
                      typeof<Size>
                  }
                  |> Render.toString

              Expect.stringContains reference "id=\"size-api\"" "API-reference roots accept ordinary attributes"

              Expect.stringContains
                  reference
                  "type Size"
                  "typed declarations, not serialized type names, supply reflected source"
          }

          test "Tooltip relationships remain explicit and theme switchers take ordinary attributes" {
              let tooltip =
                  fveTooltip {
                      _id "review-help"
                      _description "Review the values before saving."
                      _side TooltipSide.Bottom
                      _delay 150

                      button {
                          _type "button"
                          _ariaDescribedby "existing-help review-help-content"
                          "Review"
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "id=\"review-help-content\""
                    "role=\"tooltip\""
                    "aria-describedby=\"existing-help review-help-content\""
                    "position-area:bottom" ] do
                  Expect.stringContains tooltip expected "the caller owns the focusable control and its description IDs"

              let setup =
                  ColorMode.assetsWithNonce "product-appearance" ColorMode.Dark (Some "page-nonce")
                  |> Render.toString

              Expect.stringContains setup "nonce=\"page-nonce\"" "the host owns the CSP nonce"

              Expect.stringContains
                  setup
                  "storageKey: \"product-appearance\""
                  "the document initializer explicitly configures the browser storage key"

              Expect.stringContains
                  setup
                  "defaultMode: \"dark\""
                  "the initializer configures the fallback preference, not a control"

              Expect.isFalse (setup.Contains "<button") "appearance setup has no prescribed UI"
          }

          test "Field composes native labels, descriptions and errors without control callbacks" {
              let html =
                  fveField {
                      fveLabel {
                          _for "billing-email"
                          "Email address"
                      }

                      fveInput {
                          _id "billing-email"
                          _name "email"
                          _type InputType.Email
                          _value "<customer@fve.meiermade.com>"
                          _required true
                          _autocomplete "email"
                          _ariaInvalid true
                          _ariaDescribedby "billing-email-description billing-email-validation"
                      }

                      fveFieldDescription {
                          _id "billing-email-description"
                          "For billing notices."
                      }

                      fveFieldError {
                          _id "billing-email-validation"
                          "Check the address."
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "id=\"billing-email\""
                    "for=\"billing-email\""
                    "name=\"email\""
                    "type=\"email\""
                    "value=\"&lt;customer@fve.meiermade.com&gt;\""
                    "required"
                    "aria-invalid=\"true\""
                    "billing-email-description billing-email-validation"
                    "role=\"alert\"" ] do
                  Expect.stringContains html expected "ordinary attributes own native relationships"

              Expect.equal (Regex.Matches(html, "<label\\b").Count) 1 "Input does not add a second field label"

              let readonly =
                  fveInput {
                      _name "reference"
                      _readonly true
                      _required true
                      _value "INV-2048"
                  }
                  |> Render.toString

              Expect.stringContains readonly "readonly" "read-only values still submit"
              Expect.isFalse (readonly.Contains "aria-busy") "read-only does not mean a request is running"

              Expect.isFalse
                  (Regex.IsMatch(readonly, "\\srequired(?:\\s|>)"))
                  "read-only controls do not carry required"

              Expect.throws
                  (fun () -> fveInput { "Unsupported child" } |> ignore)
                  "void controls reject unused children"

              let notes =
                  fveTextarea {
                      _name "notes"
                      _rows 3
                      "<Notes>"
                  }
                  |> Render.toString

              Expect.stringContains
                  notes
                  "&lt;Notes&gt;</textarea>"
                  "textarea values are encoded text, not a value attribute"

              Expect.throws
                  (fun () ->
                      fveTextarea {
                          _value "one"
                          "two"
                      }
                      |> ignore)
                  "textarea cannot have competing value sources"
          }

          test "Select consumes string-valued items and keeps native successful form values" {
              let items () =
                  fveSelectContent {
                      for status in [ Active; Paused ] do
                          fveSelectItem {
                              _value (encodeStatus status)
                              string status
                          }
                  }

              let single =
                  fveField {
                      fveLabel {
                          _for "account-status"
                          "Status"
                      }

                      fveSelect {
                          _id "account-status"
                          _name "status"
                          _value (encodeStatus Paused)
                          _required true
                          items ()
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "name=\"status\""
                    "value=\"paused\""
                    "id=\"account-status\""
                    "for=\"account-status\""
                    "role=\"combobox\""
                    "aria-required=\"true\"" ] do
                  Expect.stringContains single expected "application encoders meet an ordinary HTML boundary"

              let trigger = Regex.Match(single, "<button[^>]*role=\"combobox\"[^>]*>").Value

              Expect.isFalse
                  (Regex.IsMatch(trigger, "\\srequired(?:=|\\s|>)"))
                  "required is not an invalid native button attribute"

              let multiple =
                  fveMultiSelect {
                      _name "statuses"
                      _ariaLabel "Statuses"
                      _values ([ Active; Paused; Active ] |> List.map encodeStatus)
                      _search SelectSearch.Static
                      items ()
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(multiple, "name=\"statuses\"").Count)
                  2
                  "duplicates do not create duplicate submitted values"

              for value in [ "active"; "paused" ] do
                  Expect.stringContains multiple ($"value=\"{value}\"") "each selected item submits separately"
          }

          test "remote Select retains selected labels independently of its current result page" {
              let field =
                  fveMultiSelect {
                      _id "remote-statuses"
                      _name "statuses"
                      _ariaLabel "Account statuses"
                      _search (SelectSearch.Remote "/components/members/search")
                      _values [ "active" ]

                      fveSelectSelectedItem {
                          _value "active"
                          "Active account"
                      }

                      fveSelectContent {
                          _query "Paused"

                          fveSelectItem {
                              _value "paused"
                              "Paused account"
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains field "value=\"active\"" "the omitted selection remains a successful control"
              Expect.stringContains field "Active account" "the selected label does not disappear with remote filtering"

              Expect.isFalse
                  (Regex.IsMatch(field, "data-fve-option-value=\"active\""))
                  "selection metadata does not invent a popup result"

              let popup =
                  fveSelectContent {
                      _id "fve-select-remote_statuses-popup"
                      _query "Paused"

                      fveSelectItem {
                          _value "paused"
                          "Paused account"
                      }
                  }
                  |> Render.toString

              Expect.stringContains popup "role=\"listbox\"" "a popup patch is independently renderable"
              Expect.isFalse (popup.Contains "name=\"statuses\"") "popup patches do not duplicate form state"
          }

          test "Select preserves rich item labels, groups and disabled identities" {
              let html =
                  fveSelect {
                      _name "status"
                      _ariaLabel "Status"
                      _value "active"

                      fveSelectContent {
                          fveSelectGroup {
                              _label "Statuses"

                              fveSelectItem {
                                  _value "active"
                                  _label "Active"
                                  strong { "Active" }
                              }

                              fveSelectItem {
                                  _value "paused"
                                  _disabled true
                                  "Paused"
                              }
                          }
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "<strong>Active</strong>"
                    "role=\"group\""
                    "aria-label=\"Statuses\""
                    "fve-select-choice" ] do
                  Expect.stringContains html expected "rich content stays native and events stay root-owned"

              let disabled =
                  Regex.Match(html, "<button[^>]*data-fve-option-value=\"paused\"[^>]*>").Value

              Expect.isTrue (Regex.IsMatch(disabled, "\\sdisabled(?:\\s|>|=)")) "disabled items cannot be chosen"

              Expect.stringContains
                  (System.Net.WebUtility.HtmlDecode html)
                  "\"blocked\":[\"paused\"]"
                  "the owner guards disabled identities too"

              Expect.throws
                  (fun () ->
                      fveSelectItem {
                          _value "rich"
                          strong { "Rich" }
                      }
                      |> ignore)
                  "rich items need an explicit plain label"
          }

          test "Select keeps an empty selection and validates wire identities and cardinality" {
              let render (selection: string option) =
                  fveSelect {
                      _name "status"
                      _ariaLabel "Status"

                      match selection with
                      | Some value -> _value value
                      | None -> ()

                      fveSelectContent {
                          fveSelectItem {
                              _value "active"
                              "Active"
                          }

                          fveSelectItem {
                              _value "paused"
                              "Paused"
                          }
                      }
                  }
                  |> Render.toString

              let valueInput html =
                  Regex.Match(html, "<input[^>]*name=\"status\"[^>]*>").Value

              Expect.stringContains
                  (render None |> valueInput)
                  "value=\"\""
                  "an empty value does not invent a first selection"

              Expect.stringContains
                  (render (Some "paused") |> valueInput)
                  "value=\"paused\""
                  "server selections render back"

              for build in
                  [ (fun () -> fveSelect { _name "missing" })
                    (fun () ->
                        fveSelect {
                            _name "duplicate"

                            fveSelectContent {
                                fveSelectItem {
                                    _value "same"
                                    "One"
                                }

                                fveSelectItem {
                                    _value "same"
                                    "Two"
                                }
                            }
                        })
                    (fun () ->
                        fveSelect {
                            _name "unknown"
                            _value "unknown"
                            fveSelectContent { () }
                        })
                    (fun () ->
                        fveSelect {
                            _name "single"
                            _values [ "one" ]
                            fveSelectContent { () }
                        })
                    (fun () ->
                        fveMultiSelect {
                            _name "multiple"
                            _value "one"
                            fveSelectContent { () }
                        }) ] do
                  Expect.throwsT<System.ArgumentException>
                      (fun () -> build () |> ignore)
                      "invalid HTML selection configurations are rejected"
          }

          test "TagInput validates string values and encodes initial native controls" {
              let html =
                  fveTagInput {
                      _id "tags"
                      _name "tags"
                      _values [ "<draft>"; "a/b" ]
                      _readonly true
                      _ariaLabel "Tags"
                  }
                  |> Render.toString

              Expect.stringContains html "&lt;draft&gt;" "values are text, never raw HTML"
              Expect.stringContains html "value=\"a/b\"" "wire identities are not lossy tokens"

              Expect.isFalse
                  (Regex.IsMatch(html, "<input[^>]*type=\"hidden\"[^>]* disabled"))
                  "read-only tag values remain successful"

              for values in [ [ "" ]; [ " " ]; [ "same"; "same" ] ] do
                  Expect.throwsT<System.ArgumentException>
                      (fun () ->
                          fveTagInput {
                              _id "tags"
                              _name "tags"
                              _values values
                          }
                          |> ignore)
                      "empty and duplicate initial values are rejected"
          }

          test "Toggle groups validate string identities and render only selected native form values" {
              let html =
                  fveMultiToggleGroup {
                      _id "formats"
                      _name "format"
                      _ariaLabel "Formatting"
                      _values [ "bold"; "italic" ]
                      _readonly true

                      fveToggleGroupItem {
                          _value "bold"
                          "Bold"
                      }

                      fveToggleGroupItem {
                          _value "italic"
                          _label "Italic"
                          em { "Italic" }
                      }

                      fveToggleGroupItem {
                          _value "strike"
                          _disabled true
                          "Strike"
                      }
                  }
                  |> Render.toString

              let inputs =
                  Regex.Matches(html, "<input[^>]*name=\"format\"[^>]*>")
                  |> Seq.cast<Match>
                  |> Seq.map _.Value
                  |> Seq.toList

              Expect.equal inputs.Length 2 "only selected identities are submitted"

              Expect.isTrue
                  (inputs |> List.forall (fun input -> not (input.Contains "disabled")))
                  "read-only values remain successful"

              Expect.stringContains html "<em>Italic</em>" "rich item content stays server-rendered"

              for build in
                  [ (fun () ->
                        fveToggleGroup {
                            _id "g"
                            _name "v"
                            _ariaLabel "Group"

                            fveToggleGroupItem {
                                _value "same"
                                "One"
                            }

                            fveToggleGroupItem {
                                _value "same"
                                "Two"
                            }
                        })
                    (fun () ->
                        fveToggleGroup {
                            _id "g"
                            _name "v"
                            _ariaLabel "Group"
                            _value "unknown"

                            fveToggleGroupItem {
                                _value "a"
                                "A"
                            }
                        })
                    (fun () ->
                        fveToggleGroup {
                            _id "g"
                            _name "v"
                            _ariaLabel "Group"
                            _values [ "a" ]

                            fveToggleGroupItem {
                                _value "a"
                                "A"
                            }
                        })
                    (fun () ->
                        fveMultiToggleGroup {
                            _id "g"
                            _name "v"
                            _ariaLabel "Group"
                            _value "a"

                            fveToggleGroupItem {
                                _value "a"
                                "A"
                            }
                        }) ] do
                  Expect.throwsT<System.ArgumentException>
                      (fun () -> build () |> ignore)
                      "invalid identities or cardinality are rejected"

              Expect.throws
                  (fun () ->
                      fveToggleGroupItem {
                          _value "rich"
                          strong { "Rich" }
                      }
                      |> ignore)
                  "rich items need a plain label"
          }

          test "overlay triggers compose caller controls without wrappers or cached-control mutation" {
              let control (attributes: HtmlAttribute list) =
                  button {
                      for attribute in attributes do
                          attribute
                          _class "account-trigger"
                          span { "OL" }
                          span { "Operating ledger" }
                  }

              let menu id =
                  fveDropdownMenu {
                      _id id

                      fveDropdownMenuTrigger {
                          _dataOn ("click", "console.log('caller')")
                          control
                      }

                      fveDropdownMenuItem {
                          _href "/components/dropdown-menu"
                          "Guidance"
                      }
                  }
                  |> Render.toString

              let first = menu "first"
              let second = menu "second"

              Expect.equal
                  (Regex.Matches(first, "<button[ >]").Count)
                  1
                  "the authored native control is the only trigger button"

              Expect.stringContains first "class=\"account-trigger\"" "the trigger does not invent presentation"
              Expect.stringContains first "console.log(&#39;caller&#39;)" "caller handlers compose with menu behavior"
              Expect.stringContains first "id=\"first-trigger\"" "the actual control receives relationships"
              Expect.stringContains second "id=\"second-trigger\"" "reuse gets fresh relationships"

              Expect.isFalse
                  ((Render.toString (control [])).Contains "aria-haspopup")
                  "cached controls remain caller-owned"

              let rocket =
                  fveButton {
                      _id "custom-action"
                      _color Color.Primary
                      _variant ButtonVariant.Soft
                      _ariaLabel "Account actions"
                      span { "OL" }
                  }

              let html =
                  fveDropdownMenu {
                      _id "rocket-actions"
                      fveDropdownMenuTrigger { rocket }

                      fveDropdownMenuItem {
                          _href "/components/button"
                          "Buttons"
                      }
                  }
                  |> Render.toString

              Expect.equal (Regex.Matches(html, "<fve-button[ >]").Count) 1 "one Rocket host owns the button"
              Expect.isFalse (html.Contains "<button") "no second SSR Button implementation is introduced"

              Expect.stringContains
                  html
                  "data-fve-control-id=\"custom-action\""
                  "the explicit contract names Rocket's native control"

              Expect.stringContains html "color=\"primary\"" "the child owns presentation"

              Expect.isFalse
                  ((Render.toString rocket).Contains "data-fve-control-id")
                  "a reused Rocket host does not acquire another component's bindings"

              Expect.throws
                  (fun () ->
                      fveDropdownMenu {
                          _id "invalid-trigger"
                          fveDropdownMenuTrigger { div { "Not a control" } }
                          fveDropdownMenuItem { "Action" }
                      }
                      |> ignore)
                  "a noninteractive wrapper is not silently promoted to a button"
          }

          test "dropdown CE takes native links and owns its focus and keyboard attributes" {
              let destinationUrl =
                  function
                  | ButtonPage -> "/components"
                  | ExamplesPage -> "/examples"

              let html =
                  fveDropdownMenu {
                      fveDropdownMenuTrigger { fveButton { "Explore" } }

                      fveDropdownMenuItem {
                          _href (destinationUrl ButtonPage)
                          "Components"
                      }

                      fveDropdownMenuItem {
                          _href (destinationUrl ExamplesPage)
                          "Examples"
                      }

                      _id "explore"
                      _class "shrink-0"
                      _data ("test-menu", "catalog")
                  }
                  |> Render.toString

              for expected in
                  [ "id=\"explore\""
                    "id=\"explore-trigger\""
                    "id=\"explore-menu\""
                    "shrink-0"
                    "data-test-menu=\"catalog\""
                    "role=\"menu\""
                    "href=\"/components\""
                    "href=\"/examples\""
                    "data-on:keydown"
                    "aria-controls=\"explore-menu\"" ] do
                  Expect.stringContains html expected "the builder retains the menu's native and Datastar contract"

              Expect.throws
                  (fun () -> fveDropdownMenu { _id "missing" } |> ignore)
                  "missing trigger/items remain validation errors"
          }

          test "Command items take native destinations, actions and search metadata" {
              let html =
                  fveCommand {
                      _id "native-commands"
                      _ariaLabel "Find documentation"
                      _class "custom-command"

                      fveCommandGroup {
                          _label "Documentation"

                          fveCommandItem {
                              _id "home-command"
                              _href "/"
                              _keywords [ "start"; "overview" ]
                              _description "Documentation home"
                              _class "custom-item"
                              "Home"
                          }

                          fveCommandItem {
                              _readonly true
                              _dataOn ("click__prevent", "$blocked++")
                              "Unavailable action"
                          }
                      }
                  }
                  |> Render.toString

              let link = Regex.Match(html, "<a[^>]*id=\"home-command\"[^>]*>").Value

              for expected in
                  [ "href=\"/\""
                    "role=\"option\""
                    "data-fve-command-keywords=\"start overview\""
                    "custom-item" ] do
                  Expect.stringContains link expected "native attributes compose with bounded command matching"

              Expect.stringContains html "custom-command" "root classes compose"
              Expect.isFalse (html.Contains "$blocked++") "read-only actions cannot retain modified click handlers"

              Expect.throws
                  (fun () -> fveCommandItem { "No destination or action" } |> ignore)
                  "commands must have an actual effect"

              Expect.stringContains
                  (Command.triggerAttributes "native-commands"
                   |> List.map (fun attribute -> attribute.Name)
                   |> String.concat " ")
                  "aria-controls"
                  "native-button helpers take an ID, not a configuration object"
          }

          test "Popover parts own native attributes and infer naming from visible triggers" {
              let html =
                  fvePopover {
                      _id "details-popover"

                      fvePopoverTrigger {
                          _class
                              "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                          _title "More detail"

                          fun (triggerAttributes: HtmlAttribute list) ->
                              button {
                                  for attribute in triggerAttributes do
                                      attribute

                                  "Details"
                              }

                      }

                      fvePopoverContent {
                          _class "custom-content"
                          p { "Supporting content" }
                      }
                  }
                  |> Render.toString

              let trigger = Regex.Match(html, "<button[^>]*>").Value

              for expected in [ "title=\"More detail\""; "popovertarget=\"details-popover-content\"" ] do
                  Expect.stringContains trigger expected "ordinary trigger attributes belong to the native button"

              Expect.isFalse (trigger.Contains "aria-label") "visible content does not require a duplicate ARIA label"

              Expect.stringContains
                  html
                  "aria-labelledby=\"details-popover-trigger\""
                  "the rich popup derives its name from the actual trigger"

              Expect.stringContains html "custom-content" "popup presentation composes with ordinary classes"

              let floating =
                  fveFloatingPanel {
                      _id "guide"
                      _label "Guide"
                      _class "custom-guide"
                      _state FloatingPanelState.Minimized
                      p { "Help" }
                      fveFloatingPanelFooter { "Supporting action" }
                  }
                  |> Render.toString

              Expect.stringContains floating "custom-guide" "floating panel attributes belong to its native section"
              Expect.stringContains floating "FloatingPanelState" "local UI state is private to the component"
              Expect.stringContains floating "Supporting action" "the footer is ordinary server-rendered content"
          }

          test "Tabs keep native trigger and panel attributes on their owning elements" {
              let html =
                  fveTabs {
                      _id "details"
                      _ariaLabel "Detail views"
                      _value "activity"

                      fveTabsItem {
                          _value "summary"
                          _id "summary-panel"
                          _class "custom-panel"

                          fveTabsTrigger {
                              _id "summary-tab"
                              _title "Summary details"
                              _dataOn ("click", "$reviews++")
                              "Summary"
                          }

                          p { "Summary content" }
                      }

                      fveTabsItem {
                          _value "activity"
                          fveTabsTrigger { "Activity" }
                          p { "Activity content" }
                      }
                  }
                  |> Render.toString

              let trigger = Regex.Match(html, "<button[^>]*id=\"summary-tab\"[^>]*>").Value

              for expected in
                  [ "role=\"tab\""
                    "title=\"Summary details\""
                    "aria-controls=\"summary-panel\""
                    "$reviews++" ] do
                  Expect.stringContains trigger expected "native trigger attributes compose with selection behavior"

              let panel = Regex.Match(html, "<div[^>]*id=\"summary-panel\"[^>]*>").Value

              for expected in
                  [ "role=\"tabpanel\""
                    "aria-labelledby=\"summary-tab\""
                    "custom-panel"
                    "hidden" ] do
                  Expect.stringContains panel expected "item attributes belong to its native panel"

              Expect.throws
                  (fun () ->
                      fveTabsItem {
                          _value "missing"
                          "No trigger"
                      }
                      |> ignore)
                  "a tab cannot omit its accessible trigger"
          }

          test "argument-free builders do not leak values or attributes between evaluations" {
              let first =
                  fveSelect {
                      _name "format"
                      _ariaLabel "Format"
                      _value "csv"
                      _nativeFallback true
                      _required true

                      fveSelectContent {
                          fveSelectItem {
                              _value "csv"
                              "CSV"
                          }

                          fveSelectItem {
                              _value "json"
                              "JSON"
                          }
                      }
                  }
                  |> Render.toString

              Expect.isTrue
                  (Regex.IsMatch(Regex.Match(first, "<select[^>]*>").Value, "\\srequired(?:=|\\s|>)"))
                  "native fallback preserves required"

              let second =
                  fveSelect {
                      _name "empty"
                      _ariaLabel "Empty"
                      fveSelectContent { () }
                  }
                  |> Render.toString

              for previous in [ "CSV"; "JSON"; "name=\"format\""; "<select" ] do
                  Expect.isFalse (second.Contains previous) "a singleton builder retains no earlier state"

              let input = fveInput { _name "fresh" } |> Render.toString
              Expect.isFalse (input.Contains "billing-email") "field identity belongs to the caller"
              Expect.isFalse (input.Contains "<label") "a control does not introduce field chrome"
          } ]
