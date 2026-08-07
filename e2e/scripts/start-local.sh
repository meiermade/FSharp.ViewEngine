#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
repo_dir="$(cd "$e2e_dir/.." && pwd)"
server_port="${E2E_SERVER_PORT:-5054}"
project_name="fsharp-viewengine-e2e-${server_port}"
log_dir="/tmp/fsharp-viewengine-e2e"
log_file="$log_dir/compose.log"

mkdir -p "$log_dir"
export E2E_SERVER_PORT="$server_port"

compose() {
  docker compose \
    --project-name "$project_name" \
    --project-directory "$repo_dir" \
    --file "$repo_dir/compose.yml" \
    "$@"
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  set +e
  compose logs --no-color >"$log_file" 2>&1
  compose down --volumes --remove-orphans
  exit "$exit_code"
}

trap cleanup EXIT INT TERM

compose up --build --detach --force-recreate --remove-orphans
compose logs --follow --no-color docs &
wait $!
