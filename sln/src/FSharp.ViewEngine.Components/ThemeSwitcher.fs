namespace FSharp.ViewEngine.Components

open System
open System.Text.Json
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>theme-switcher</category>
[<RequireQualifiedAccess>]
type ColorMode = System | Light | Dark

/// <category>theme-switcher</category>
[<RequireQualifiedAccess>]
module ColorMode =
    let value = function ColorMode.System -> "system" | ColorMode.Light -> "light" | ColorMode.Dark -> "dark"

/// <category>theme-switcher</category>
[<NoEquality; NoComparison>]
type ThemeSwitcherConfig = private { id:string; label:string; defaultMode:ColorMode }

/// <summary>
/// A document-wide preference; semantic colors and product branding remain in ComponentsTheme.
/// Include assets once in the document head, before the stylesheet, to avoid a wrong-theme first paint.
/// </summary>
/// <category>theme-switcher</category>
[<RequireQualifiedAccess>]
module ThemeSwitcher =
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable theme-switcher ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible theme-switcher label is required."
        { id = id; label = label; defaultMode = ColorMode.System }

    /// Initial radio state before hydration; use the same default in the document assets.
    let withDefaultMode defaultMode config = { config with defaultMode = defaultMode }

    let assetsWithNonce storageKey defaultMode nonce =
        if String.IsNullOrWhiteSpace storageKey then invalidArg (nameof storageKey) "A product-owned preference storage key is required."
        let source = """
(() => {
  const valid = new Set(['system', 'light', 'dark']);
  const media = matchMedia('(prefers-color-scheme: dark)');
  window.fveColorMode = {
    storageKey: __KEY__, defaultMode: __DEFAULT__,
    current() {
      try {
        if (parent !== window && parent.location.origin === location.origin && frameElement?.hasAttribute('data-docs-preview-src')) {
          return parent.fveColorMode?.current() ?? (parent.document.documentElement.classList.contains('dark') ? 'dark' : 'light');
        }
        const stored = localStorage.getItem(this.storageKey);
        return valid.has(stored) ? stored : this.defaultMode;
      } catch { return this.defaultMode; }
    },
    apply(mode) {
      const selected = valid.has(mode) ? mode : this.defaultMode;
      const dark = selected === 'dark' || (selected === 'system' && media.matches);
      document.documentElement.classList.toggle('dark', dark);
      document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
      document.documentElement.dataset.colorMode = selected;
      return selected;
    },
    set(mode) {
      const selected = this.apply(mode);
      try { localStorage.setItem(this.storageKey, selected); } catch {}
      window.dispatchEvent(new CustomEvent('fve-color-mode', {detail: {mode: selected}}));
      return selected;
    },
    refresh() {
      const selected = this.apply(this.current());
      window.dispatchEvent(new CustomEvent('fve-color-mode', {detail: {mode: selected}}));
    }
  };
  window.fveColorMode.apply(window.fveColorMode.current());
  media.addEventListener('change', () => { if (window.fveColorMode.current() === 'system') window.fveColorMode.refresh(); });
  window.addEventListener('storage', event => { if (event.key === null || event.key === window.fveColorMode.storageKey) window.fveColorMode.refresh(); });
})();
"""
        script {
            match nonce with Some value -> _attr("nonce", value) | None -> ()
            raw (source.Replace("__KEY__", JsonSerializer.Serialize(storageKey)).Replace("__DEFAULT__", JsonSerializer.Serialize(ColorMode.value defaultMode)))
        }

    let assets storageKey defaultMode = assetsWithNonce storageKey defaultMode None

    let render config =
        let signal = "_" + ComponentHtml.signalToken config.id + "_mode"
        let icon = raw """<svg class="size-4 dark:hidden" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"/></svg><svg class="hidden size-4 dark:block" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.718 9.718 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 2.25 12c0 5.385 4.365 9.75 9.75 9.75a9.753 9.753 0 0 0 9.752-6.748Z"/></svg>"""
        let items : DropdownMenuItem<unit> list =
            [ for mode, label in [ColorMode.System,"System"; ColorMode.Light,"Light"; ColorMode.Dark,"Dark"] do
                let value = ColorMode.value mode
                DropdownMenuItem.radio $"window.fveColorMode.set('{value}')" label
                |> DropdownMenuItem.withChecked (mode = config.defaultMode)
                |> DropdownMenuItem.withCheckedExpression $"${signal} == '{value}'" ]
        div {
            _class "relative shrink-0"
            _attr ("data-signals__ifmissing", $"{{{signal}: window.fveColorMode.current()}}")
            _dataOn ("fve-color-mode__window", $"${signal} = window.fveColorMode.current()")
            _dataInit $"${signal} = window.fveColorMode.current()"
            DropdownMenu.create config.id config.label
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon icon)
            |> DropdownMenu.withContent items
            |> DropdownMenu.render (fun () -> "")
        }
