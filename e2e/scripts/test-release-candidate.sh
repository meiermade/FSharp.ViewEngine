#!/usr/bin/env bash
set -euo pipefail

: "${EXPECTED_COMMIT:?EXPECTED_COMMIT is required}"
: "${EXPECTED_IMAGE:?EXPECTED_IMAGE is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"
: "${DOCS_EXPECTED_IMAGE:?DOCS_EXPECTED_IMAGE is required}"

test "$DOCS_EXPECTED_COMMIT" = "$EXPECTED_COMMIT"
test "$DOCS_EXPECTED_IMAGE" = "$EXPECTED_IMAGE"

E2E_CROSS_BROWSER_MODE=full bash scripts/test-published-ci.sh
