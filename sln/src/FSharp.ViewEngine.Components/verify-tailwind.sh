#!/usr/bin/env bash
set -euo pipefail

contract_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="$(mktemp)"
trap 'rm -f "$output"' EXIT

tailwindcss \
  --input "$contract_dir/consumer.css" \
  --output "$output" \
  --minify

assert_output() {
  local expected="$1"
  if ! grep -Fq -- "$expected" "$output"; then
    echo "Components Tailwind contract did not emit: $expected" >&2
    exit 1
  fi
}

assert_output '.bg-\[var\(--fve-brand-solid\)\]'
assert_output '.hover\:bg-\[var\(--fve-brand-hover\)\]'
assert_output '.hover\:bg-\[var\(--fve-brand-subtle\)\]'
assert_output '.active\:bg-\[var\(--fve-brand-active\)\]'
assert_output '.focus-visible\:ring-2'
assert_output '.focus-visible\:ring-inset'
assert_output '::-webkit-search-cancel-button'
assert_output '.overflow-x-auto'
assert_output '.max-w-full'
assert_output '.border-b-2'
assert_output '.border-transparent'
assert_output '.aria-selected\:bg-\[var\(--fve-surface\)\]'
assert_output '.aria-selected\:border-\[var\(--fve-brand-solid\)\]'
assert_output '.aria-selected\:shadow-sm'
assert_output '.backdrop\:bg-\[var\(--fve-overlay-backdrop\)\]'
assert_output '.w-\[min\(24rem\,calc\(100\%-3rem\)\)\]'
assert_output '.sm\:w-96'
assert_output '.sm\:hidden'
assert_output '.sm\:w-auto'
assert_output '.sm\:truncate'
assert_output '.\@container'
assert_output '@container (min-width:280px)'
assert_output '.gap-y-3'
assert_output '.lg\:visible'
assert_output '.lg\:w-64'
assert_output '.-translate-x-full'
assert_output '.opacity-100'
assert_output '.max-w-4xl'
assert_output '.max-w-none'
assert_output '.lg\:grid-cols-3'
assert_output '.size-9'
assert_output '.ml-2'
assert_output '.ml-auto'
assert_output '.peer-focus-visible\:ring-\[var\(--fve-critical-ring\)\]'
assert_output '.cursor-not-allowed'
assert_output '.fve-theme-emerald'
assert_output '.acme-theme'
assert_output '--fve-brand-solid:oklch(58% .18 264)'
assert_output '--fve-brand-active:oklch(44% .18 264)'

echo "Components Tailwind clean-consumer contract passed."
