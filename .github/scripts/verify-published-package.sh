#!/usr/bin/env bash
set -euo pipefail

package=${1:?package path is required}
published_package=${2:?published package path is required}
work_directory=${3:-"$RUNNER_TEMP/verify-published-package"}

rm -rf "$work_directory"
mkdir -p "$work_directory/expected" "$work_directory/published"
unzip -qq "$package" -d "$work_directory/expected"
unzip -qq "$published_package" -d "$work_directory/published"

# NuGet.org automatically repository-signs uploaded packages.
rm -f "$work_directory/published/.signature.p7s"

diff -qr "$work_directory/expected" "$work_directory/published"
