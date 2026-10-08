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
    echo "Documentation Tailwind contract did not emit: $expected" >&2
    exit 1
  fi
}

assert_output '.fve-components'
assert_output '.bg-\[var\(--fve-background\)\]'
assert_output '.text-\[var\(--fve-text\)\]'
assert_output '.border-\[var\(--fve-border\)\]'
assert_output '.rounded-xl'
assert_output '.outline-none'
assert_output '.focus-visible\:outline-2'
assert_output '.lg\:flex'
assert_output '.xl\:hidden'
# Shared SideNav renders the current-page background directly on the active row.
assert_output '.bg-\[var\(--fve-brand-subtle\)\]'

if grep -Fq -- '--spec-' "$output" || grep -Fq -- '.spec-' "$output"; then
  echo "Documentation emitted the retired spec design system" >&2
  exit 1
fi

if grep -R -E -q --include='*.fs' -- '--spec-|class="[^"]*spec-|"spec-[a-z]' "$contract_dir"; then
  echo "Documentation source still references the retired spec design system" >&2
  exit 1
fi

echo "Documentation Tailwind source-scanning contract passed."
