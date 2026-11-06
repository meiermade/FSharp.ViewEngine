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
fsharp_core_version="$(grep -Eo '<FveFSharpCorePackageVersion>[^<]+' "$sln_dir/src/FSharp.ViewEngine.Cli/FSharp.ViewEngine.Cli.fsproj" | cut -d'>' -f2)"
nugets_dir="$repo_dir/nugets"
tailwindcss_bin="${TAILWINDCSS_BIN:-$(command -v tailwindcss || true)}"
[[ -n "$tailwindcss_bin" ]] || { echo 'tailwindcss is required.' >&2; exit 1; }
# Reuse the repository's pinned browser-build tooling for this installed-source fixture.
npm ci --prefix "$sln_dir/src/Docs"
esbuild_bin="$sln_dir/src/Docs/node_modules/.bin/esbuild"
rm -rf "$output_dir"
mkdir -p "$output_dir/.nuget/packages"
(
  cd "$sln_dir"
  PACKAGE_ID=FSharp.ViewEngine PACKAGE_VERSION="$core_version" ./fake.sh Pack --single-target
  PACKAGE_ID=FSharp.ViewEngine.Cli PACKAGE_VERSION="$cli_version" CORE_PACKAGE_VERSION="$core_version" ./fake.sh Pack --single-target
)
# Isolate local-tool resolution as well as packages: a cached same-version tool can otherwise run stale source.
export DOTNET_CLI_HOME="$output_dir/.dotnet-home" NUGET_PACKAGES="$output_dir/.nuget/packages" DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
(
  cd "$output_dir"
  dotnet new tool-manifest >/dev/null
  dotnet tool install FSharp.ViewEngine.Cli --version "$cli_version" --add-source "$nugets_dir" --local >/dev/null
)
components=()
while IFS= read -r component; do components+=("$component"); done < <(grep 'FveName=' "$registry" | grep -v 'FveInternal="true"' | grep -Eo 'FveName="[^"]+"' | cut -d'"' -f2)
[[ ${#components[@]} -gt 0 ]] || { echo 'No components found.' >&2; exit 1; }

restore_build() {
  dotnet restore "$1" --source "$nugets_dir" --source https://api.nuget.org/v3/index.json
  dotnet build "$1" --configuration Release --no-restore
}
for framework in net8.0 net9.0 net10.0; do
  consumer_dir="$output_dir/$framework"
  sdk_major="${framework#net}"; sdk_major="${sdk_major%%.*}"
  sdk_version="$(dotnet --list-sdks | awk -v prefix="$sdk_major." '$1 ~ "^" prefix { version = $1 } END { print version }')"
  [[ -n "$sdk_version" ]] || { echo ".NET SDK $sdk_major is required." >&2; exit 1; }
  mkdir -p "$consumer_dir"
  printf '{"sdk":{"version":"%s","rollForward":"disable"}}\n' "$sdk_version" > "$consumer_dir/global.json"
  cp "$fixture_dir/ExistingProjectConsumer.fs" "$consumer_dir/ExistingProjectConsumer.fs"
  cat > "$consumer_dir/Acme.Components.fsproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>$framework</TargetFramework><RootNamespace>Acme.Components</RootNamespace><FSharpCoreImplicitPackageVersion>$fsharp_core_version</FSharpCoreImplicitPackageVersion><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><PackageReference Include="FSharp.ViewEngine" Version="$core_version" /></ItemGroup>
  <ItemGroup><Compile Include="ExistingProjectConsumer.fs" /></ItemGroup>
</Project>
EOF
  (
    cd "$consumer_dir"
    test "$(dotnet --version)" = "$sdk_version"
    dotnet tool restore >/dev/null
    dotnet fve init Acme.Components.fsproj --namespace Acme.Components --framework "$framework"
    dotnet fve add "${components[@]}" --config fve.json
    generated_line="$(grep -n 'Components/Foundation.fs' Acme.Components.fsproj | cut -d: -f1)"
    consumer_line="$(grep -n 'ExistingProjectConsumer.fs' Acme.Components.fsproj | cut -d: -f1)"
    [[ -n "$generated_line" && "$generated_line" -lt "$consumer_line" ]] || { echo 'Generated definitions must precede consumer source.' >&2; exit 1; }
    restore_build Acme.Components.fsproj
  )

  # These controls have their own closure, not a Documentation aggregate.
  documentation_dir="$output_dir/documentation-$framework"
  mkdir -p "$documentation_dir"
  cp "$consumer_dir/global.json" "$documentation_dir/global.json"
  cp "$fixture_dir/DocumentationConsumer.fs" "$documentation_dir/DocumentationConsumer.fs"
  cat > "$documentation_dir/Acme.Documentation.fsproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>$framework</TargetFramework><FSharpCoreImplicitPackageVersion>$fsharp_core_version</FSharpCoreImplicitPackageVersion><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><PackageReference Include="FSharp.ViewEngine" Version="$core_version" /></ItemGroup>
  <ItemGroup><Compile Include="DocumentationConsumer.fs" /></ItemGroup>
</Project>
EOF
  (
    cd "$documentation_dir"
    dotnet fve init Acme.Documentation.fsproj --namespace Acme.Documentation --framework "$framework"
    dotnet fve add code-block callout example mermaid fsharp-api-reference --config fve.json
    if find Components -type d -name Templates | grep -q .; then echo 'Framework support was distributed.' >&2; exit 1; fi
    restore_build Acme.Documentation.fsproj
  )

done

# Build the checked-in consumer as-is; no namespace rewriting or generated ZIP tree.
template_dir="$output_dir/templates-net10.0"
example_sources="$sln_dir/src/Example"
mkdir -p "$template_dir"
tar --exclude=bin --exclude=obj --exclude=node_modules -C "$example_sources" -cf - . | tar -C "$template_dir" -xf -
cp "$output_dir/net10.0/global.json" "$template_dir/global.json"
(
  cd "$template_dir"
  dotnet fve add button --config Example.Components/fve.json --overwrite
  restore_build Example.Server/Example.Server.fsproj
)

visual_dir="$output_dir/net10.0"
mkdir -p "$visual_dir/App/wwwroot/scripts"
for asset in prism.1.29.0.min.js prism-fsharp.1.29.0.min.js prism-python.1.29.0.min.js prism-typescript.1.29.0.min.js prism-bash.1.29.0.min.js prism-sql.1.29.0.min.js; do
  cp "$sln_dir/src/Docs/wwwroot/scripts/$asset" "$visual_dir/App/wwwroot/scripts/$asset"
done
cp "$fixture_dir/GeneratedConsumer.fsproj" "$visual_dir/App/GeneratedConsumer.fsproj"
cp "$fixture_dir/Program.fs" "$visual_dir/App/Program.fs"
cp "$fixture_dir/input.css" "$visual_dir/App/input.css"
mkdir -p "$visual_dir/App/js"
cp "$fixture_dir/app.js" "$visual_dir/App/js/app.js"
"$esbuild_bin" "$visual_dir/App/js/app.js" --bundle --format=esm --splitting --outdir="$visual_dir/App/wwwroot/assets" --minify
"$tailwindcss_bin" --input "$visual_dir/App/input.css" --output "$visual_dir/App/wwwroot/output.css" --minify
(cd "$visual_dir/App"; restore_build GeneratedConsumer.fsproj)
(cd "$visual_dir"; dotnet new sln --name GeneratedConsumer --format slnx --force >/dev/null; dotnet sln GeneratedConsumer.slnx add Acme.Components.fsproj App/GeneratedConsumer.fsproj >/dev/null)
printf 'Generated consumer solution is ready at %s/GeneratedConsumer.slnx\n' "$visual_dir"
