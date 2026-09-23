#!/usr/bin/env bash
set -euo pipefail

: "${EXPECTED_COMMIT:?EXPECTED_COMMIT is required}"
: "${EXPECTED_IMAGE:?EXPECTED_IMAGE is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"
: "${DOCS_EXPECTED_IMAGE:?DOCS_EXPECTED_IMAGE is required}"

test "$DOCS_EXPECTED_COMMIT" = "$EXPECTED_COMMIT"
test "$DOCS_EXPECTED_IMAGE" = "$EXPECTED_IMAGE"

release_specs=()
for spec in tests/*.spec.ts; do
  if [[ "$(basename "$spec")" != "production-smoke.spec.ts" ]]; then
    release_specs+=("$spec")
  fi
done

if [[ ${#release_specs[@]} -eq 0 ]]; then
  echo "No staging release acceptance specs found" >&2
  exit 2
fi

E2E_CROSS_BROWSER_MODE=focused bash scripts/test-published-ci.sh "${release_specs[@]}"
