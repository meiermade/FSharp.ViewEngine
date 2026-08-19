#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
server_port="${E2E_SERVER_PORT:-5054}"
health_url="http://127.0.0.1:${server_port}/health"
playwright_image="$(< "$e2e_dir/playwright-image.txt")"
server_pid=""

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  set +e
  if [[ -n "$server_pid" ]]; then
    kill "$server_pid" 2>/dev/null
    wait "$server_pid" 2>/dev/null
  fi
  exit "$exit_code"
}

trap cleanup EXIT INT TERM

bash "$script_dir/start-local.sh" &
server_pid=$!

ready=false
for _ in {1..300}; do
  if curl --fail --silent --show-error "$health_url" >/dev/null; then
    ready=true
    break
  fi

  if ! kill -0 "$server_pid" 2>/dev/null; then
    wait "$server_pid"
  fi

  sleep 2
done

if [[ "$ready" != true ]]; then
  echo "Docs image did not become healthy at $health_url" >&2
  exit 1
fi

docker run --rm --init --network host \
  --env CI=true \
  --env E2E_START_LOCAL=0 \
  --env DOCS_E2E_BASE_URL="http://127.0.0.1:${server_port}" \
  --volume "$e2e_dir:/work" \
  --workdir /work \
  "$playwright_image" \
  npx playwright test --project=chromium --project=firefox --project=webkit
