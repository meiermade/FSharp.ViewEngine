#!/usr/bin/env bash
set -euo pipefail

: "${DOCS_E2E_BASE_URL:?DOCS_E2E_BASE_URL is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"
: "${DOCS_EXPECTED_IMAGE:?DOCS_EXPECTED_IMAGE is required}"
: "${CF_ACCESS_CLIENT_ID:?CF_ACCESS_CLIENT_ID is required}"
: "${CF_ACCESS_CLIENT_SECRET:?CF_ACCESS_CLIENT_SECRET is required}"

if [[ "$DOCS_E2E_BASE_URL" != 'https://fve.meiermade.net' ]]; then
  echo 'Staging Access credentials may only target https://fve.meiermade.net.' >&2
  exit 2
fi

for attempt in $(seq 1 60); do
  health="$({
    curl --fail --silent --show-error \
      --header "CF-Access-Client-Id: $CF_ACCESS_CLIENT_ID" \
      --header "CF-Access-Client-Secret: $CF_ACCESS_CLIENT_SECRET" \
      "$DOCS_E2E_BASE_URL/health"
  } 2>/dev/null || true)"

  status="$(jq -r '.status // empty' <<< "$health" 2>/dev/null || true)"
  environment="$(jq -r '.environment // empty' <<< "$health" 2>/dev/null || true)"
  commit="$(jq -r '.commit // empty' <<< "$health" 2>/dev/null || true)"
  image="$(jq -r '.image // empty' <<< "$health" 2>/dev/null || true)"

  if [[ "$status" == 'ok' \
      && "$environment" == 'staging' \
      && "$commit" == "$DOCS_EXPECTED_COMMIT" \
      && "$image" == "$DOCS_EXPECTED_IMAGE" ]]; then
    echo "Staging reports commit $commit and the expected immutable image."
    exit 0
  fi

  echo "Waiting for staging release identity (attempt $attempt/60; commit=${commit:-unknown})."
  sleep 5
done

echo 'Staging did not report the expected release identity.' >&2
exit 1
