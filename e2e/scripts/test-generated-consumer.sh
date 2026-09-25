#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"

bash "$script_dir/prepare-generated-consumer.sh"
cd "$e2e_dir"
npx playwright test --config playwright.generated.config.ts
