#!/usr/bin/env bash
set -euo pipefail

: "${EXPECTED_COMMIT:?EXPECTED_COMMIT is required}"
: "${EXPECTED_IMAGE:?EXPECTED_IMAGE is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"
: "${DOCS_EXPECTED_IMAGE:?DOCS_EXPECTED_IMAGE is required}"

test "$DOCS_EXPECTED_COMMIT" = "$EXPECTED_COMMIT"
test "$DOCS_EXPECTED_IMAGE" = "$EXPECTED_IMAGE"

case "${DOCS_EXPECTED_ENVIRONMENT:-staging}" in
  staging)
    test "$DOCS_E2E_BASE_URL" = 'https://fve.meiermade.net'
    : "${CF_ACCESS_CLIENT_ID:?CF_ACCESS_CLIENT_ID is required for protected release smoke}"
    : "${CF_ACCESS_CLIENT_SECRET:?CF_ACCESS_CLIENT_SECRET is required for protected release smoke}"
    export DOCS_EXPECTED_ENVIRONMENT=staging
    export CORE_PACKAGE_VERSION=unreleased CLI_PACKAGE_VERSION=unreleased
    ;;
  production)
    test "$DOCS_E2E_BASE_URL" = 'https://fve.meiermade.com'
    test -z "${CF_ACCESS_CLIENT_ID:-}${CF_ACCESS_CLIENT_SECRET:-}"
    : "${CORE_PACKAGE_VERSION:?CORE_PACKAGE_VERSION is required}"
    : "${CLI_PACKAGE_VERSION:?CLI_PACKAGE_VERSION is required}"
    export DOCS_EXPECTED_ENVIRONMENT=production
    ;;
  *) echo 'Release smoke requires staging or production identity' >&2; exit 2 ;;
esac

# Full regressions and form submissions belong to PR coverage, not deployed smoke.
unset E2E_SHARD
E2E_BROWSER=all E2E_CROSS_BROWSER_MODE=focused bash scripts/test-published-ci.sh tests/release-smoke.spec.ts
