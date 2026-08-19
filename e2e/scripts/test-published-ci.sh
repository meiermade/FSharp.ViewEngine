#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
playwright_image="$(< "$e2e_dir/playwright-image.txt")"

: "${DOCS_E2E_BASE_URL:?DOCS_E2E_BASE_URL is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"

docker run --rm --init \
  --env CI=true \
  --env E2E_START_LOCAL=0 \
  --env DOCS_E2E_BASE_URL \
  --env DOCS_EXPECTED_COMMIT \
  --volume "$e2e_dir:/work" \
  --workdir /work \
  "$playwright_image" \
  npm run test:published
