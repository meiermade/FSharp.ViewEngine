#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
playwright_image="$(< "$e2e_dir/playwright-image.txt")"
cross_browser_mode="${E2E_CROSS_BROWSER_MODE:-focused}"

: "${DOCS_E2E_BASE_URL:?DOCS_E2E_BASE_URL is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"

case "$cross_browser_mode" in
  focused)
    project_args=(--project=chromium)
    retry_args=(--retries=0)
    ;;
  full)
    project_args=(--project=chromium --project=firefox --project=webkit)
    retry_args=(--retries=1)
    ;;
  *)
    echo "Unsupported E2E_CROSS_BROWSER_MODE: $cross_browser_mode" >&2
    exit 2
    ;;
esac

docker run --rm --init \
  --env CI=true \
  --env E2E_START_LOCAL=0 \
  --env E2E_CROSS_BROWSER_MODE="$cross_browser_mode" \
  --env DOCS_E2E_BASE_URL \
  --env DOCS_EXPECTED_COMMIT \
  --volume "$e2e_dir:/work" \
  --workdir /work \
  "$playwright_image" \
  npx playwright test "${project_args[@]}" "${retry_args[@]}"
