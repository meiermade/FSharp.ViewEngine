#!/usr/bin/env bash
set -euo pipefail

contract_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
base_output="$(mktemp)"
trap 'rm -f "$base_output"' EXIT

tailwindcss \
  --input "$contract_dir/consumer.css" \
  --output "$base_output" \
  --minify

assert_output() {
  local expected="$1"
  if ! grep -Fq -- "$expected" "$base_output"; then
    echo "Components Tailwind contract did not emit: $expected" >&2
    exit 1
  fi
}

assert_base_excludes() {
  local unexpected="$1"
  if grep -Fq -- "$unexpected" "$base_output"; then
    echo "Base Components Tailwind contract unexpectedly emitted: $unexpected" >&2
    exit 1
  fi
}

assert_output '.bg-\[var\(--fve-brand-solid\)\]'
assert_output '.hover\:bg-\[var\(--fve-brand-hover\)\]'
assert_output '.hover\:bg-\[var\(--fve-brand-subtle\)\]'
assert_output '.active\:bg-\[var\(--fve-brand-active\)\]'
assert_output '.focus-visible\:ring-2'
assert_output '.focus-visible\:outline-2'
assert_output '.aria-selected\:font-semibold'
assert_output '.forced-colors\:border-\[CanvasText\]'
assert_output '.forced-colors\:focus-visible\:outline-\[Highlight\]'
assert_output '.has-\[input\:focus-visible\]\:outline-2'
assert_output 'background-color:var(--fve-brand-solid)'
assert_output 'background-color:var(--fve-brand-active)'
assert_output ':has(:is(input:focus-visible))'
assert_output 'max-width:40%'
assert_output '.read-only\:bg-\[var\(--fve-neutral-subtle\)\]'
assert_output '.disabled\:invisible'
assert_output '.\[overflow-wrap\:anywhere\]'
assert_output '.focus-visible\:ring-inset'
assert_output '::-webkit-search-cancel-button'
assert_output '::-webkit-search-cancel-button'
assert_output '--fve-control-min-height:2.5rem'
assert_output '--fve-navigation-min-height:2rem'
assert_output 'padding-block:max(0px, calc((var(--fve-control-min-height) - var(--fve-control-line-height) - 2px) / 2))'
assert_output '.-ml-1'
assert_output '.overflow-x-auto'
assert_output '.indeterminate\:bg-\[var\(--fve-brand-solid\)\]'
assert_output '.forced-colors\:appearance-auto'
assert_output '.fve-table-records'
assert_output '.bg-\[var\(--fve-table-background\)\]'
assert_output '--fve-table-background:var(--fve-page)'
assert_output '--fve-table-background:var(--fve-surface)'
assert_output '@container fve-table'
assert_output 'var(--fve-table-padding-block-compact,'
assert_output '.fve-table-mobile-label'
assert_output ':not(:has(.fve-table-selection))'
assert_output '@container fve-table not (min-width:16rem)'
assert_output '.fixed'
assert_output '.sticky'
assert_output '.w-px'
assert_output '.min-h-\[var\(--fve-shell-bar-min-height\)\]'
assert_output '.max-w-full'
assert_output '.content-start'
assert_output '.contents'
assert_output '.grid-cols-1'
assert_output '.border-b-2'
assert_output '.border-transparent'
assert_output '.aria-selected\:bg-\[var\(--fve-surface\)\]'
assert_output '.aria-selected\:border-\[var\(--fve-brand-solid\)\]'
assert_output '.aria-selected\:shadow-sm'
assert_output '.backdrop\:bg-\[var\(--fve-overlay-backdrop\)\]'
assert_output '.w-\[min\(24rem\,calc\(100\%-3rem\)\)\]'
assert_output '.sm\:w-96'
assert_output '.sm\:hidden'
assert_output '.sm\:flex'
assert_output '.sm\:truncate'
assert_output '.\@container'
assert_output '@container (min-width:280px)'
assert_output '.md\:visible'
assert_output '.md\:w-60'
assert_output '.lg\:visible'
assert_output '.lg\:w-64'
assert_output '.-translate-x-full'
assert_output '.opacity-100'
assert_output '.max-w-4xl'
assert_output '.max-w-none'
assert_output '.lg\:grid-cols-3'
assert_output '.xl\:grid-cols-4'
assert_output '.gap-y-4'
assert_output '.uppercase'
assert_output '.tracking-wide'
assert_output '.font-normal'
assert_output '.size-9'
assert_output '.ml-2'
assert_output '.ml-auto'
assert_output '.peer-focus-visible\:ring-\[var\(--fve-critical-ring\)\]'
assert_output '.cursor-not-allowed'
assert_output '--fve-brand-solid:oklch(59.6% .145 163.225)'
assert_output '.acme-theme'
assert_output '--fve-shell-bar-min-height:'
assert_output '--fve-brand-solid:oklch(58% .18 264)'
assert_output '--fve-brand-active:oklch(44% .18 264)'
assert_output '.bg-red-400'
assert_output '.h-\[40rem\]'
assert_output '@container fve-calendar (min-width:48rem)'
assert_output 'grid-template-rows:repeat(calc(var(--fve-calendar-hours) * 60),.133333rem)'
assert_output 'background-image:repeating-linear-gradient(to bottom,var(--fve-border) 0 1px,transparent 1px 8rem)'
assert_base_excludes '.spec-browser-frame'
assert_base_excludes '.fve-app-mode-launch'
assert_base_excludes 'data-fve-app-mode-root'
assert_base_excludes 'data-fve-app-mode-controls'
echo "Components base Tailwind clean-consumer contract passed."
