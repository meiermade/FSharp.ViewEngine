#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
playwright_image="$(< "$e2e_dir/playwright-image.txt")"
cross_browser_mode="${E2E_CROSS_BROWSER_MODE:-focused}"
browser="${E2E_BROWSER:-all}"

: "${DOCS_E2E_BASE_URL:?DOCS_E2E_BASE_URL is required}"
: "${DOCS_EXPECTED_COMMIT:?DOCS_EXPECTED_COMMIT is required}"

case "$cross_browser_mode" in
  focused|full) retry_args=(--retries=0) ;;
  *)
    echo "Unsupported E2E_CROSS_BROWSER_MODE: $cross_browser_mode" >&2
    exit 2
    ;;
esac

case "$browser" in
  all) project_args=(--project=chromium --project=firefox --project=webkit) ;;
  chromium|firefox|webkit) project_args=("--project=$browser") ;;
  *)
    echo "Unsupported E2E_BROWSER: $browser" >&2
    exit 2
    ;;
esac

docker_env=(
  --env CI=true
  --env E2E_START_LOCAL=0
  --env E2E_CROSS_BROWSER_MODE="$cross_browser_mode"
  --env E2E_BROWSER="$browser"
  --env DOCS_E2E_BASE_URL
  --env DOCS_EXPECTED_COMMIT
)

for optional_name in DOCS_EXPECTED_ENVIRONMENT DOCS_EXPECTED_IMAGE CORE_PACKAGE_VERSION COMPONENTS_PACKAGE_VERSION CF_ACCESS_CLIENT_ID CF_ACCESS_CLIENT_SECRET; do
  if [[ -n "${!optional_name:-}" ]]; then
    docker_env+=(--env "$optional_name")
  fi
done

auth_directory="$e2e_dir/.auth"
auth_state="$auth_directory/access.json"
mkdir -p "$auth_directory"
chmod u+rwx "$auth_directory"
rm -f "$auth_state"

cleanup_auth_state() {
  local test_status=$?
  trap - EXIT
  if ! rm -f "$auth_state"; then
    echo "Unable to remove Playwright Access state: $auth_state" >&2
    if [[ $test_status -eq 0 ]]; then test_status=1; fi
  fi
  exit "$test_status"
}
trap cleanup_auth_state EXIT

test_args=("$@" "${project_args[@]}" "${retry_args[@]}")
if [[ -n "${E2E_SHARD:-}" ]]; then
  test_args+=("--shard=$E2E_SHARD")
fi

docker run --rm --init \
  "${docker_env[@]}" \
  --volume "$e2e_dir:/work" \
  --workdir /work \
  "$playwright_image" \
  npx playwright test "${test_args[@]}"
