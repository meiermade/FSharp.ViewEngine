#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
sln_dir="$(dirname "$script_dir")"
# Source commit behind the upstream v1.0.5 tag.
revision=fb03989a5fbb5584d15198e1a4bac151a848ef08
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

curl --fail --silent --show-error --location --max-time 60 \
  "https://codeload.github.com/starfederation/datastar/tar.gz/$revision" \
  | tar -xz -C "$work_dir" --strip-components=1
patch -d "$work_dir" -p1 -F0 < "$script_dir/datastar-rocket-scope.patch"

"$sln_dir/node_modules/.bin/esbuild" "$work_dir/library/src/bundles/datastar-rocket.ts" \
  --bundle --format=esm --target=es2021 --minify --define:ALIAS=null \
  --banner:js='// Datastar v1.0.5 + Rocket beta.3; FVE scope compatibility patch. Rebuild with sln/scripts/rebuild-datastar-rocket.sh.' \
  --outfile="$sln_dir/src/FSharp.ViewEngine.Components/Assets/datastar-rocket.1.0.5.js"
