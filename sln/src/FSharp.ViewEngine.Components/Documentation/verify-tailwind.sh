#!/usr/bin/env bash
set -euo pipefail

contract_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="$(mktemp)"
without_docs_source="$(mktemp "$contract_dir/.consumer-without-docs.XXXXXX.css")"
without_docs_output="$(mktemp)"
without_components_source="$(mktemp "$contract_dir/.consumer-without-components.XXXXXX.css")"
without_components_output="$(mktemp)"
trap 'rm -f "$output" "$without_docs_source" "$without_docs_output" "$without_components_source" "$without_components_output"' EXIT

tailwindcss \
  --input "$contract_dir/consumer.css" \
  --output "$output" \
  --minify

assert_output() {
  local expected="$1"
  if ! grep -Fq -- "$expected" "$output"; then
    echo "Docs Tailwind contract did not emit: $expected" >&2
    exit 1
  fi
}

assert_output '.bg-\[var\(--fve-brand-solid\)\]'
assert_output '.fve-components'
assert_output '.spec-shell'
assert_output '.spec-content-link:focus-visible'
assert_output '.list-disc{list-style-type:disc}'
assert_output '.list-decimal{list-style-type:decimal}'
assert_output '.spec-document .token.keyword'
assert_output '.docs-reference-layout'
assert_output '.docs-canvas-layout .spec-main-inner'
assert_output '[data-fve-app-mode-root=true]'
assert_output '[data-fve-app-mode-controls=true]'
assert_output '--docs-text-ancillary:.75rem'
assert_output '--docs-text-reading:1rem'
assert_output '--docs-code-bg:#f6f8fa'
assert_output ':root.dark'
assert_output '@media (min-width:1024px)'

cat > "$without_docs_source" <<'CSS'
@import "tailwindcss" source(none);
@import "../FSharp.ViewEngine.Components.tailwind.css";
CSS

tailwindcss \
  --input "$without_docs_source" \
  --output "$without_docs_output" \
  --minify

if grep -Fq -- '.spec-shell' "$without_docs_output"; then
  echo "Docs selectors unexpectedly exist without Documentation.tailwind.css" >&2
  exit 1
fi

cat > "$without_components_source" <<'CSS'
@import "tailwindcss" source(none);
@import "./Documentation.tailwind.css";
CSS

tailwindcss \
  --input "$without_components_source" \
  --output "$without_components_output" \
  --minify

if grep -Fq -- '.bg-\[var\(--fve-brand-solid\)\]' "$without_components_output"; then
  echo "Components utilities unexpectedly exist without FSharp.ViewEngine.Components.tailwind.css" >&2
  exit 1
fi

for marker in '.list-disc{list-style-type:disc}' '.list-decimal{list-style-type:decimal}'; do
  if ! grep -Fq -- "$marker" "$without_components_output"; then
    echo "Docs list marker utility depends on consumer or Components scanning: $marker" >&2
    exit 1
  fi
done

echo "Docs Tailwind clean-consumer contract passed."
