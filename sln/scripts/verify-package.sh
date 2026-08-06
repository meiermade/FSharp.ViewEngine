#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: $0 path/to/FSharp.ViewEngine.<version>.nupkg" >&2
  exit 2
fi

package_path="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
package_dir="$(dirname "$package_path")"
package_name="$(basename "$package_path")"
symbols_package_path="${package_path%.nupkg}.snupkg"
version="${package_name#FSharp.ViewEngine.}"
version="${version%.nupkg}"
frameworks="${PACKAGE_TEST_FRAMEWORKS:-net8.0 net9.0 net10.0}"
work_dir="$(mktemp -d "${TMPDIR:-/tmp}/fsharp-viewengine-package.XXXXXX")"

cleanup() {
  rm -rf "$work_dir"
}
trap cleanup EXIT

packaged_frameworks="$(
  unzip -Z1 "$package_path" \
    | awk -F/ '/^lib\/net[0-9]+\.[0-9]+\/FSharp\.ViewEngine\.dll$/ { print $2 }' \
    | sort -u \
    | paste -sd ' ' -
)"

if [[ "$packaged_frameworks" != "net8.0" ]]; then
  echo "expected only lib/net8.0, found: ${packaged_frameworks:-none}" >&2
  exit 1
fi

if unzip -Z1 "$package_path" | grep -Eq '\.pdb$'; then
  echo "the main package must not contain PDB files" >&2
  exit 1
fi

if [[ ! -f "$symbols_package_path" ]]; then
  echo "missing symbol package: $symbols_package_path" >&2
  exit 1
fi

symbol_frameworks="$(
  unzip -Z1 "$symbols_package_path" \
    | awk -F/ '/^lib\/net[0-9]+\.[0-9]+\/FSharp\.ViewEngine\.pdb$/ { print $2 }' \
    | sort -u \
    | paste -sd ' ' -
)"

if [[ "$symbol_frameworks" != "net8.0" ]]; then
  echo "expected only lib/net8.0 symbols, found: ${symbol_frameworks:-none}" >&2
  exit 1
fi

nuspec="$(unzip -p "$package_path" '*.nuspec')"
repository_commit="$(sed -nE 's/.*commit="([0-9a-f]{40})".*/\1/p' <<<"$nuspec")"
if [[ -z "$repository_commit" ]] \
  || ! grep -Eq '<repository type="git" url="https://github\.com/meiermade/FSharp\.ViewEngine"' <<<"$nuspec"; then
  echo "package repository metadata is missing its GitHub URL or commit" >&2
  exit 1
fi

source_url="https://raw.githubusercontent.com/meiermade/FSharp.ViewEngine/$repository_commit/"
if ! unzip -p "$symbols_package_path" lib/net8.0/FSharp.ViewEngine.pdb \
  | grep -aFq "$source_url"; then
  echo "portable PDB does not map Source Link to the packaged repository commit" >&2
  exit 1
fi

for framework in $frameworks; do
  project_dir="$work_dir/$framework"
  dotnet new console --language F# --framework "$framework" --output "$project_dir" --no-restore >/dev/null

  cat >"$project_dir/Program.fs" <<'FSHARP'
open FSharp.ViewEngine
open type Html

let actual = div { _class "package-smoke"; "ok" } |> Render.toString
if actual <> "<div class=\"package-smoke\">ok</div>" then
    failwith $"unexpected render: {actual}"

printfn "FSharp.ViewEngine package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
FSHARP

  dotnet add "$project_dir" package FSharp.ViewEngine --version "$version" --source "$package_dir" --no-restore >/dev/null
  dotnet restore "$project_dir" --source "$package_dir" --source https://api.nuget.org/v3/index.json >/dev/null

  if ! grep -q 'lib/net8\.0/FSharp\.ViewEngine\.dll' "$project_dir/obj/project.assets.json"; then
    echo "$framework did not select the net8.0 compatibility asset" >&2
    exit 1
  fi

  dotnet run --project "$project_dir" --framework "$framework" --no-restore
done
