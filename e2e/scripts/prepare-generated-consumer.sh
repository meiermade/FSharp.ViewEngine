#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(cd "$script_dir/.." && pwd)"
repo_dir="$(cd "$e2e_dir/.." && pwd)"
sln_dir="$repo_dir/sln"
registry="$sln_dir/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.Registry.props"
fixture_dir="$e2e_dir/generated-consumer"
output_dir="${GENERATED_CONSUMER_ROOT:-/tmp/fsharp-viewengine-generated-consumer}"
core_version="${GENERATED_CORE_VERSION:-0.0.0-generated}"
cli_version="${GENERATED_CLI_VERSION:-0.0.1-generated}"
nugets_dir="$repo_dir/nugets"
tailwindcss_bin="${TAILWINDCSS_BIN:-$(command -v tailwindcss || true)}"

if [[ -z "$tailwindcss_bin" ]]; then
  echo "tailwindcss is required. Set TAILWINDCSS_BIN or install the pinned standalone CLI." >&2
  exit 1
fi

rm -rf "$output_dir"
mkdir -p "$output_dir" "$output_dir/.nuget/packages"

(
  cd "$sln_dir"
  PACKAGE_ID=FSharp.ViewEngine PACKAGE_VERSION="$core_version" ./fake.sh Pack --single-target
  PACKAGE_ID=FSharp.ViewEngine.Cli PACKAGE_VERSION="$cli_version" CORE_PACKAGE_VERSION="$core_version" ./fake.sh Pack --single-target
)

export NUGET_PACKAGES="$output_dir/.nuget/packages"
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

(
  cd "$output_dir"
  dotnet new tool-manifest >/dev/null
  dotnet tool install FSharp.ViewEngine.Cli \
    --version "$cli_version" \
    --add-source "$nugets_dir" \
    --local >/dev/null
)

components=()
while IFS= read -r component; do
  components+=("$component")
done < <(grep -Eo 'FveName="[^"]+"' "$registry" | cut -d'"' -f2)

if [[ ${#components[@]} -eq 0 ]]; then
  echo "No registry components were found in $registry" >&2
  exit 1
fi

for framework in net8.0 net9.0 net10.0; do
  consumer_dir="$output_dir/$framework"
  sdk_major="${framework#net}"
  sdk_major="${sdk_major%%.*}"
  sdk_version="$(dotnet --list-sdks | awk -v prefix="$sdk_major." '$1 ~ "^" prefix { version = $1 } END { print version }')"
  if [[ -z "$sdk_version" ]]; then
    echo ".NET SDK $sdk_major is required to verify $framework." >&2
    exit 1
  fi
  mkdir -p "$consumer_dir"
  printf '{\n  "sdk": {\n    "version": "%s",\n    "rollForward": "disable"\n  }\n}\n' "$sdk_version" > "$consumer_dir/global.json"
  (
    cd "$consumer_dir"
    test "$(dotnet --version)" = "$sdk_version"
    dotnet fve init Acme.Components.fsproj --namespace Acme.Components --framework "$framework"
    dotnet fve add "${components[@]}" --config fve.json
    dotnet restore Acme.Components.fsproj \
      --source "$nugets_dir" \
      --source https://api.nuget.org/v3/index.json
    dotnet build Acme.Components.fsproj --configuration Release --no-restore
  )
done

visual_dir="$output_dir/net10.0"
mkdir -p "$visual_dir/App/wwwroot"
cp "$fixture_dir/GeneratedConsumer.fsproj" "$visual_dir/App/GeneratedConsumer.fsproj"
cp "$fixture_dir/Program.fs" "$visual_dir/App/Program.fs"
cp "$fixture_dir/input.css" "$visual_dir/App/input.css"

"$tailwindcss_bin" \
  --input "$visual_dir/App/input.css" \
  --output "$visual_dir/App/wwwroot/output.css" \
  --minify

(
  cd "$visual_dir/App"
  dotnet restore GeneratedConsumer.fsproj \
    --source "$nugets_dir" \
    --source https://api.nuget.org/v3/index.json
  dotnet build GeneratedConsumer.fsproj --configuration Release --no-restore
)

(
  cd "$visual_dir"
  dotnet new sln --name GeneratedConsumer --format slnx --force >/dev/null
  dotnet sln GeneratedConsumer.slnx add Acme.Components.fsproj App/GeneratedConsumer.fsproj >/dev/null
)

printf 'Generated consumer solution is ready at %s/GeneratedConsumer.slnx\n' "$visual_dir"
