# Releasing

Core and CLI have independent NuGet release trains, managed by the **Publish packages** workflow:

- `FSharp.ViewEngine`: tags such as `v2026.8.1`.
- `FSharp.ViewEngine.Cli`: tags such as `cli/v2026.9.0`.

Dispatch Core, CLI, both, or Docs-only. CLI packages contain the exact canonical component registry compiled by the repository. Publish Core before CLI when both change. A Docs-only release requires package-contract checks proving the selected public Core and CLI versions already contain every contract change. Direct packing requires explicit package-version properties.

## Documentation promotion

Each successful push to `main` deploys an immutable candidate to protected staging at `https://fve.meiermade.net` and runs bounded, credential-scoped smoke checks. PR CI runs full Chromium and focused Firefox/WebKit checks; package verification and production safeguards remain required.

Production Docs at `https://fve.meiermade.com` change only through an explicit release promoting the exact staging-accepted digest with coherent package versions. An ordinary merge does not update production. Production infrastructure follows the release workflow revision, independently of the accepted image's source; rollback restores the previous image and package metadata on the canonical hostname, not retired infrastructure.

For infrastructure-only maintenance, dispatch **Deploy production infrastructure** (`deploy-infrastructure.yml`) from `main` after the PR's production preview is reviewed. It preserves the existing production digest, source commit and package versions and checks canonical health and a representative Docs route before and after the update. It does not build an image or publish packages. This maintenance workflow shares the production release concurrency lock.

The old production hostname and compatibility redirect are retired; old bookmarks are not supported. Use only the canonical production origin and protected staging at `https://fve.meiermade.net`.

Engine releases become GitHub's “Latest” release; CLI releases do not. Add versioned changelog entries in a follow-up PR only after publication, verification, and GitHub release reconciliation. Feature PRs must not claim unpublished versions or release dates.

## Package retirement

Deprecate a permanently replaced package only after its replacement and canonical production documentation are verified. In NuGet.org **Manage Packages → Deprecation**, select every version, choose **Legacy**, name the replacement, and leave the versions downloadable.

Run `node e2e/scripts/verify-package-retirement.mjs` from the repository root to verify public metadata and pinned downloads for every Components and Docs version returned by NuGet.org.
