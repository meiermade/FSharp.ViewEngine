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
assert_output '.active\:bg-\[var\(--fve-brand-active\)\]'
assert_output '.focus-visible\:ring-2'
assert_output '.fve-theme-emerald'
assert_output '.acme-theme'
assert_output '--fve-brand-solid:oklch(58% .18 264)'
assert_output '--fve-brand-active:oklch(44% .18 264)'

echo "Components Tailwind clean-consumer contract passed."
