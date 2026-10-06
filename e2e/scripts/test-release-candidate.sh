#!/usr/bin/env bash
set -euo pipefail

: "${EXPECTED_COMMIT:?EXPECTED_COMMIT is required}"
: "${EXPECTED_IMAGE:?EXPECTED_IMAGE is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"
: "${DOCS_EXPECTED_IMAGE:?DOCS_EXPECTED_IMAGE is required}"

test "$DOCS_EXPECTED_COMMIT" = "$EXPECTED_COMMIT"
test "$DOCS_EXPECTED_IMAGE" = "$EXPECTED_IMAGE"

test "$DOCS_E2E_BASE_URL" = 'https://fve.meiermade.net'

# Full Docs regressions run in PR CI. Release proves the deployed boundary
# without repeating gallery geometry and every documentation workflow.
unset E2E_SHARD
E2E_BROWSER=all E2E_CROSS_BROWSER_MODE=focused bash scripts/test-published-ci.sh tests/staging-smoke.spec.ts
