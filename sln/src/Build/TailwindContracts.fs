module TailwindContracts

open System
open System.IO
open System.Text.RegularExpressions

let checkOutput name (required:string list) (forbidden:string list) (css:string) =
    for expected in required do
        if not (css.Contains(expected, StringComparison.Ordinal)) then
            failwith $"{name} Tailwind contract did not emit: {expected}"
    for unexpected in forbidden do
        if css.Contains(unexpected, StringComparison.Ordinal) then
            failwith $"{name} Tailwind contract unexpectedly emitted: {unexpected}"

let private componentSelectors = [
    @".bg-\[var\(--fve-brand-solid\)\]"
    @".enabled\:hover\:bg-\[var\(--fve-internal-color-hover\)\]"
    @".hover\:bg-\[var\(--fve-brand-subtle\)\]"
    @".enabled\:active\:bg-\[var\(--fve-internal-color-active\)\]"
    @".focus-visible\:ring-2"
    @".focus-visible\:outline-2"
    @".aria-selected\:font-semibold"
    @".forced-colors\:border-\[CanvasText\]"
    @".forced-colors\:focus-visible\:outline-\[Highlight\]"
    @".has-\[\>\[data-fve-input-control\]\:focus-visible\]\:outline-2"
    "background-color:var(--fve-brand-solid)"
    "background-color:var(--fve-internal-color-active)"
    "color:var(--fve-internal-color-on-solid)"
    "color-scheme:dark"
    ":has(>[data-fve-input-control]:focus-visible)"
    "flex:1 0 min(8rem,100%)"
    ".order-0"
    ".order-4"
    @".user-invalid\:border-\[var\(--fve-critical-ring\)\]"
    ":has(>[data-fve-input-control]:user-invalid)"
    @".read-only\:bg-\[var\(--fve-neutral-subtle\)\]"
    @".\[overflow-wrap\:anywhere\]"
    @".focus-visible\:ring-inset"
    "::-webkit-search-cancel-button"
    "--fve-control-min-height:2.5rem"
    "--fve-navigation-min-height:1.75rem"
    "padding-block:max(0px, calc((var(--fve-control-min-height) - var(--fve-control-line-height) - 2px) / 2))"
    ".-ml-1"
    ".overflow-x-auto"
    @".indeterminate\:bg-\[var\(--fve-brand-solid\)\]"
    @".forced-colors\:appearance-auto"
    ".fve-table-records"
    @".bg-\[var\(--fve-internal-table-background\)\]"
    "--fve-internal-table-background:var(--fve-background)"
    "--fve-internal-table-background:var(--fve-surface)"
    "@container fve-table"
    "var(--fve-table-padding-block-compact,"
    ".fve-table-mobile-label"
    ":not(:has(.fve-table-selection))"
    "@container fve-table not (min-width:16rem)"
    ".fixed"
    ".sticky"
    ".w-px"
    @".min-h-\[calc\(var\(--fve-shell-bar-min-height\)-1px\)\]"
    ".max-w-full"
    ".content-start"
    ".contents"
    ".grid-cols-1"
    ".border-b-2"
    ".border-transparent"
    @".aria-selected\:bg-\[var\(--fve-surface\)\]"
    @".aria-selected\:border-\[var\(--fve-brand-solid\)\]"
    @".aria-selected\:shadow-sm"
    @".backdrop\:bg-\[var\(--fve-overlay-backdrop\)\]"
    ".w-full"
    @".sm\:w-96"
    ".truncate"
    @".\@container"
    ".gap-y-4"
    ".uppercase"
    ".tracking-wide"
    ".font-normal"
    ".size-9"
    ".gap-2"
    ".ml-auto"
    @".aria-invalid\:border-\[var\(--fve-critical-ring\)\]"
    @".aria-invalid\:focus-visible\:outline-\[var\(--fve-critical-ring\)\]"
    ".cursor-not-allowed"
    "--fve-brand-solid:oklch(59.6% .145 163.225)"
    ".acme-theme"
    "--fve-shell-bar-min-height:"
    "--fve-brand-solid:oklch(58% .18 264)"
    "--fve-brand-active:oklch(44% .18 264)"
    @".flex-\[1_1_8rem\]"
    @".h-\[40rem\]"
    "min-width:56rem"
    "min-width:112rem"
    "grid-template-columns:3.5rem minmax(0,1fr)"
    @".aria-selected\:bg-\[var\(--fve-surface-active\)\]"
    "grid-template-rows:repeat(calc(var(--fve-internal-calendar-hours) * 60),var(--fve-internal-calendar-minute-height))"
    "background-image:repeating-linear-gradient(to bottom,color-mix(in srgb,var(--fve-border) 60%,transparent) 0 1px,transparent 1px var(--fve-internal-calendar-hour-height))"
    // Prism-generated spans must be styled by copied components, not the Docs host.
    ".token.keyword"
    ".token.string"
    "light-dark(var(--color-red-700),var(--color-red-300))"
    "color-mix(in oklch,var(--fve-background) 72%,var(--fve-surface-subtle))"
]

let private documentationSelectors = [
    ".fve-components"
    @".bg-\[var\(--fve-background\)\]"
    @".text-\[var\(--fve-text\)\]"
    @".border-\[var\(--fve-border\)\]"
    ".rounded-xl"
    ".outline-none"
    @".focus-visible\:outline-2"
    @".lg\:flex"
    @".xl\:hidden"
    @".bg-\[var\(--fve-brand-subtle\)\]"
]

let verify (compile:string -> string -> unit) componentsDirectory =
    let documentationDirectory = Path.Combine(componentsDirectory, "Documentation")
    for name, directory, required, forbidden in [
        "Components", componentsDirectory, componentSelectors, [".spec-browser-frame"; ".fve-app-mode-launch"; "data-fve-app-mode-root"; "data-fve-app-mode-controls"]
        "Documentation", documentationDirectory, documentationSelectors, ["--spec-"; ".spec-"]
    ] do
        let output = Path.GetTempFileName()
        try
            compile (Path.Combine(directory, "consumer.css")) output
            checkOutput name required forbidden (File.ReadAllText output)
            printfn $"{name} Tailwind clean-consumer contract passed."
        finally File.Delete output

    for file in Directory.EnumerateFiles(documentationDirectory, "*.fs", SearchOption.AllDirectories) do
        if Regex.IsMatch(File.ReadAllText file, """--spec-|class="[^"]*spec-|"spec-[a-z]""") then
            failwith $"Documentation source still references the retired spec design system: {file}"
