namespace Docs.Pages

open System
open System.Net
open System.Text.RegularExpressions
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar

/// The owning Theming guide's reference data, derived from the installed theme and palette defaults.
module ThemeTokens =
    type Token = { name: string; purpose: string; light: string; dark: string; scope: string }

    let private arguments (value: string) =
        let parts = ResizeArray<string>()
        let mutable depth, start = 0, 0
        for i in 0 .. value.Length - 1 do
            match value[i] with
            | '(' -> depth <- depth + 1
            | ')' -> depth <- depth - 1
            | ',' when depth = 0 -> parts.Add(value[start .. i-1].Trim()); start <- i + 1
            | _ -> ()
        parts.Add(value[start ..].Trim())
        parts |> Seq.toList

    let private modes (value: string) =
        if value.StartsWith("light-dark(", StringComparison.Ordinal) then
            match arguments value[11 .. value.Length-2] with
            | [light; dark] -> light, dark
            | _ -> invalidOp "Expected paired light/dark palette defaults."
        else value, value

    let private token name purpose light dark scope =
        { name = name; purpose = purpose; light = light; dark = dark; scope = scope }

    let private roles =
        Map [ "solid", "Solid fill"; "hover", "Solid hover fill and light-mode quiet-action hover text"
              "active", "Solid pressed fill and light-mode quiet-action pressed text"; "on-solid", "Text/icons on a Solid fill"
              "soft", "Soft idle fill"; "soft-hover", "Soft and colored Outline/Ghost hover fill"
              "soft-active", "Soft and colored Outline/Ghost pressed fill"; "text", "Quiet-action idle text/icons"
              "border", "Outline border source (mixed to 20% alpha)"; "focus", "Keyboard focus ring" ]

    let private description name =
        match name with
        | "--fve-background" -> "Page/canvas background"
        | "--fve-surface" -> "Opaque field, panel and selected-tab surface"
        | "--fve-surface-subtle" -> "Opaque subdued surface"
        | "--fve-surface-hover" -> "Opaque highlight/surface fill; not the interactive alpha wash"
        | "--fve-surface-active" -> "Opaque active surface and presentation surround"
        | "--fve-text" -> "Default readable foreground"
        | "--fve-muted-text" -> "Secondary readable foreground"
        | "--fve-border" -> "Default dividers and control borders"
        | "--fve-overlay-backdrop" -> "Dialog/Drawer backdrop, including alpha"
        | "--fve-hover-wash" -> "Background-only neutral hover layer; text/icons stay opaque"
        | "--fve-pressed-wash" -> "Stronger background-only neutral pressed layer"
        | "--fve-radius-control" -> "Control corner radius"
        | "--fve-radius-panel" -> "Panel corner radius"
        | "--fve-popup-active-background" -> "Pointer/keyboard-active popup option fill (separate from selected values)"
        | "--fve-popup-active-text" -> "Foreground on the active popup option"
        | "--fve-control-min-height" -> "Minimum control height"
        | "--fve-control-icon-size" -> "Control icon/spinner size"
        | "--fve-control-padding-block" -> "Vertical inset including any border"
        | "--fve-control-padding-inline" -> "Horizontal inset including any border"
        | "--fve-control-font-size" -> "Control label font size"
        | "--fve-control-line-height" -> "Control label line height"
        | "--fve-navigation-min-height" -> "Navigation row minimum height"
        | "--fve-navigation-padding-block" -> "Navigation row vertical padding"
        | "--fve-shell-bar-min-height" -> "Application shell bar minimum height"
        | _ ->
            let role = name.Split('-') |> Array.last
            match role with
            | "subtle" -> "Subtle semantic fill"
            | "solid" -> "Strong semantic fill"
            | "hover" -> "Semantic hover shade"
            | "active" -> "Semantic pressed shade"
            | "text" -> "Readable semantic foreground"
            | "ring" -> "Semantic focus/validation ring"
            | _ -> invalidOp $"Missing CSS-variable description: {name}"

    // Read the actual baseline rather than copying a second set of Sky/size/density values.
    let theme =
        Regex.Matches(ComponentsTheme.className ComponentsTheme.sky, @"\[(--fve-[\w-]+):([^\]]+)\]")
        |> Seq.cast<Match>
        |> Seq.groupBy (fun m -> m.Groups[1].Value)
        |> Seq.map (fun (name, matches) ->
            let values = matches |> Seq.map (fun m -> m.Groups[2].Value.Replace('_', ' ')) |> Seq.toList
            let light, _ = modes values.Head
            let _, dark = modes (List.last values)
            let scope =
                if name.StartsWith("--fve-control-", StringComparison.Ordinal) then "Theme region; an explicit component _size takes local precedence"
                elif name.StartsWith("--fve-navigation-", StringComparison.Ordinal) || name = "--fve-shell-bar-min-height" then "Theme region; ComponentsTheme.withDensity controls these defaults"
                else "Owning theme region; a nearer theme or component override takes precedence"
            token name (description name) light dark scope)
        |> Seq.sortBy _.name |> Seq.toList

    let palettes =
        [ "primary", Color.Primary; "secondary", Color.Secondary; "success", Color.Success
          "warning", Color.Warning; "error", Color.Error; "info", Color.Info; "neutral", Color.Neutral ]
        |> List.collect (fun (name, color) ->
            let markup = fveLink { _href "/guides/theming"; _color color; "Palette" } |> Render.toString
            let style = Regex.Match(markup, "style=\"([^\"]+)\"").Groups[1].Value |> WebUtility.HtmlDecode
            style.Split(';', StringSplitOptions.RemoveEmptyEntries)
            |> Array.choose (fun entry ->
                let split = entry.IndexOf(':')
                if split < 0 || not (entry.StartsWith("--fve-internal-color-", StringComparison.Ordinal)) then None
                else
                    let role = entry["--fve-internal-color-".Length .. split-1]
                    let light, dark = modes entry[split+1 ..]
                    let fallback (value: string) =
                        if not (value.StartsWith("var(", StringComparison.Ordinal)) then invalidOp "Expected a named action token."
                        match arguments value[4 .. value.Length-2] with
                        | [variable; value] -> variable, value
                        | _ -> invalidOp "Expected a named action token and default."
                    let variable, light = fallback light
                    let _, dark = fallback dark
                    let light, dark =
                        match theme |> List.tryFind (fun row -> row.name = variable) with
                        | Some baseline -> baseline.light, baseline.dark
                        | None -> light, dark
                    Some (token variable roles[role] light dark $"{name} Button/Link and palette-aware controls; Custom uses the supplied ColorPalette instead"))
            |> Array.toList)

    let components =
        [ token "--fve-card-inset" "Card header, body and footer padding" "1.25rem" "1.25rem" "Card host via _style; _size supplies the local default, so inherited values do not override it"
          token "--fve-shell-sidebar-width" "PageTopBar brand-region width at wide shell sizes" "15rem" "15rem" "PageTopBar or surrounding region; match it to the consumer-owned sidebar width"
          token "--fve-table-padding-inline" "Cell horizontal padding" "0.75rem" "0.75rem" "Table or surrounding region"
          token "--fve-table-padding-block-compact" "Compact cell vertical padding" "0.25rem" "0.25rem" "Table; used by compact/default density"
          token "--fve-table-padding-block-comfortable" "Comfortable cell vertical padding" "0.75rem" "0.75rem" "Table; used by comfortable density"
          token "--fve-table-row-hover" "Opaque hover fill; sticky cells reuse it, selected rows keep their own fill" "Table background mixed with 5% black" "Table background mixed with 10% white" "Table or region; only applies when _hoverRows true"
          token "--fve-chart-height" "Plot height" "18rem" "18rem" "Chart host; inherited into its shadow-DOM plot"
          token "--fve-calendar-viewport-height" "Maximum timed Day/Week viewport height" "38rem" "38rem" "Calendar region; an explicit _viewportHeight writes a local value"
          token "--fve-slide-cover-background" "Cover canvas background" "#101828" "#101828" "Cover Slide or its containing region"
          token "--fve-slide-cover-text" "Cover foreground" "white" "white" "Cover Slide; also sets its local --fve-text"
          token "--fve-slide-cover-muted" "Cover secondary text" "#d1d5db" "#d1d5db" "Cover Slide; also sets its local --fve-muted-text"
          token "--fve-docs-code-surface" "CodeBlock/CodeFrame background (historical token name)" "color-mix(in oklch, var(--fve-background) 72%, var(--fve-surface-subtle))" "color-mix(in oklch, var(--fve-background) 72%, var(--fve-surface-subtle))" "CodeBlock/CodeFrame or surrounding region" ]

    // Some semantic tokens also serve an action palette. Keep one row, with both owning scopes.
    let all =
        theme @ palettes @ components
        |> List.groupBy _.name
        |> List.map (fun (_, rows) ->
            { rows.Head with
                purpose = rows |> List.map _.purpose |> List.distinct |> String.concat "; "
                scope = rows |> List.map _.scope |> List.distinct |> String.concat "; " })

    let groups = [ "all", "All"; "theme", "Theme"; "actions", "Action palettes"; "components", "Component overrides" ]
    let pageSize = 10
    type Reference = { search: string; group: string; page: int; pageCount: int; total: int; rows: Token list }

    let select (search: string) group requestedPage =
        let search = search.Trim() |> fun value -> if value.Length > 256 then value[..255] else value
        let group = if groups |> List.exists (fun (value, _) -> value = group) then group else "all"
        let inGroup =
            match group with
            | "theme" -> theme
            | "actions" -> palettes
            | "components" -> components
            | _ -> all
            |> List.map _.name |> Set.ofList
        let words = search.Split([|' '; '\t'; '\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
        let matches =
            all |> List.filter (fun row ->
                Set.contains row.name inGroup &&
                (words |> Array.forall (fun word ->
                    [row.name; row.purpose; row.scope] |> List.exists (fun value -> value.Contains(word, StringComparison.OrdinalIgnoreCase)))))
        let total = matches.Length
        let pageCount = max 1 ((total + pageSize - 1) / pageSize)
        let page = max 1 (min requestedPage pageCount)
        { search = search; group = group; page = page; pageCount = pageCount; total = total
          rows = matches |> List.skip ((page - 1) * pageSize) |> List.truncate pageSize }

    let href state page =
        $"/guides/theming?tokenSearch={Uri.EscapeDataString state.search}&tokenGroup={state.group}&tokenPage={page}"

    let private tableRows (rows: Token list) =
        div {
            _class "min-w-0 overflow-x-auto rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)]"
            _role "region"; _ariaLabel "CSS variables"; _tabindex 0
            table {
                _class "w-full border-collapse text-left text-sm [&_th]:p-3 [&_td]:p-3 [&_td]:align-top [&_tr]:border-b [&_tr]:border-[var(--fve-border)] [&_code]:[overflow-wrap:anywhere]"
                caption { _class "sr-only"; "CSS variables" }
                thead { tr { for title in ["Variable"; "Purpose / owning scope"; "Light default"; "Dark default"] do th { _scope "col"; title } } }
                tbody {
                    if rows.IsEmpty then
                        tr { td { _colspan 4; "No variables match. Try another search or group." } }
                    for row in rows do
                        tr {
                            _data("css-token", row.name)
                            th { _scope "row"; _class "min-w-48 max-w-64 align-top font-normal [overflow-wrap:anywhere]"; code { row.name } }
                            td { _class "min-w-56"; p { row.purpose }; p { _class "mt-2 text-[var(--fve-muted-text)]"; row.scope } }
                            td { _class "min-w-40 max-w-64"; code { row.light } }
                            td { _class "min-w-40 max-w-64"; code { row.dark } }
                        }
                }
            }
        }

    let reference state =
        div {
            // Row heights can change between pages; do not anchor the guide's scroll to a moving row/link.
            _id "theme-token-reference"; _class "grid min-w-0 gap-4 [overflow-anchor:none]"
            form {
                _id "theme-token-filter"; _method "get"; _action "/guides/theming"
                _data("fve-local-navigation", "true")
                _class "flex min-w-0 flex-wrap items-end gap-3"
                _dataOn("input", "if (evt.target.name === 'tokenSearch') window.fveNavigationController?.abort()")
                _dataOn("input", ["debounce.200ms"], "if (evt.target.name === 'tokenSearch') el.requestSubmit()")
                _dataOn("change", "if (evt.target.name === 'tokenGroup') el.requestSubmit()")
                fveField {
                    _class "min-w-0 basis-56 grow"
                    fveLabel { _for "theme-token-search"; "Search CSS variables" }
                    fveInput {
                        _id "theme-token-search"; _name "tokenSearch"; _type "search"
                        _value state.search; _maxlength "256"; _placeholder "Name, purpose or scope"
                    }
                }
                fveField {
                    _class "min-w-0 basis-48 grow"
                    fveLabel { _for "theme-token-group"; "Variable group" }
                    fveSelect {
                        _id "theme-token-group"; _name "tokenGroup"; _value state.group; _nativeFallback true
                        fveSelectContent {
                            for value, label in groups do
                                fveSelectItem { _value value; label }
                        }
                    }
                }
                fveButton { _type "submit"; _variant ButtonVariant.Outline; "Apply" }
                if state.search <> "" || state.group <> "all" then
                    fveLink { _href (href (select "" "all" 1) 1); _data("fve-local-navigation", "true"); "Clear filters" }
            }
            tableRows state.rows
            fvePagination {
                _ariaLabel "CSS variable pages"
                fvePaginationSummary {
                    p {
                        _role "status"; _ariaLive "polite"
                        let first = if state.total = 0 then 0 else (state.page - 1) * pageSize + 1
                        let last = min (state.page * pageSize) state.total
                        $"{first}–{last} of {state.total} variables"
                    }
                }
                fvePaginationPrevious {
                    if state.page > 1 then _href (href state (state.page - 1)); _data("fve-local-navigation", "true")
                }
                fvePaginationContent {
                    for number in 1 .. state.pageCount do
                        if number = 1 || number = state.pageCount || abs (number - state.page) <= 1 then
                            fvePaginationItem {
                                _page number
                                if number = state.page then _ariaCurrent "page"
                                else _href (href state number); _data("fve-local-navigation", "true")
                            }
                        elif number = state.page - 2 || number = state.page + 2 then
                            fvePaginationEllipsis { () }
                }
                fvePaginationNext {
                    if state.page < state.pageCount then _href (href state (state.page + 1)); _data("fve-local-navigation", "true")
                }
            }
        }
