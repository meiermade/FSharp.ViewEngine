# Reference-backed application page examples

App shell remains a minimal layout gallery. Application **Page examples** demonstrate coherent, connected workspaces built with the shared components, not new public graph, finance or messaging engines.

## Composition sources

| Example | Reference composition | Adapted behavior |
| --- | --- | --- |
| Dependency graph | Funktos Spec, `sln/src/Spec/src/Common/Wireframe.fs` | Positioned node cards, directional edges, selected dependency, search/list alternative and zoom/reset |
| Execution detail | Funktos execution workflow | Matching execution identity, status/metadata, timed spans, inspection and logs |
| Financial reporting and account management | Geldos Spec, `sln/src/Spec/src/Common/Wireframe.fs` | Actual/plan series, axes, periods, table data, linked accounts/transactions, create and settings forms |
| Messaging | Enslie Spec, `sln/src/Spec/src/Lab.fs` | Conversation rail, attributed bubbles, scrollable history, anchored composer and conversation-local replies |
| Operations, scheduling and media | ACKMX Spec, `sln/src/Spec/src/Common/Admin.fs` | Operational summaries, setup guidance, date-aware Month/Week/Day/Year calendar with matching session details, repository-owned color-background media, retained selection, metadata editing and drawer-based upload |

These are bounded adaptations, not copies of each product's full domain or promises of all its functionality. Product repositories were read, not modified. Navigation and the primary actions operate on realistic fixtures; setup/populated/loading/empty/error scenarios are controlled outside the product UI. Ordinary Docs previews expose shareable, URL-backed review tabs, while App mode uses the shared Fixture state selector. Operations alone adds a Setup tab for its recoverable First steps panel; Populated keeps onboarding guidance out of the working dashboard.

## Source and consumer ownership

- `sln/src/Docs/src/Pages/PageExamples.fs`: typed fixture data, queries and pure page compositions.
- `sln/src/Docs/src/Pages/Components.fs`: account workflow, catalog registration and copied source.
- `sln/src/Docs/src/Common/PageExampleSession.fs`: bounded in-memory demonstration storage.
- `sln/src/Docs/src/Common/Handler.fs`: form/query adapters, response rendering and isolated uploaded-image delivery.

Copied examples include their composition declarations and fixture data, with a concrete Browser invocation. Consumers must supply their own routes, handlers, authentication, authorization, persistence and transport. Compile these consumer-authored utilities with Tailwind alongside the package manifest; the examples do not require the optional Documentation manifest unless the consumer also uses Documentation components.

Graph edges, trace bars and financial series use native SVG/HTML. No specialist runtime is added. A consumer replacing them with a graph layout, chart, messaging or storage provider owns that provider's installation, accessibility alternatives, security and license obligations.

## Temporary demonstration state

Messages, created accounts, settings, media metadata and uploaded images are cookie-isolated server memory. They are not production services and must not receive private/customer data.

- HttpOnly, SameSite=Strict cookie, HTTPS Secure flag, scoped to `/components/page-examples`.
- Thirty-minute expiry, restart reset, maximum 32 concurrent sessions with oldest-session eviction.
- Maximum 100 messages per session, 2,000 characters per message.
- Maximum 20 created accounts; names/types and workspace settings are validated.
- PNG/JPEG/WebP signature checks; no SVG uploads. Maximum 2 MB per image, four new photos and 8 MB stored image bytes per session. Replacements reclaim the old image; image signatures are not a production image-decoding/sanitization pipeline.
- Form requests are capped at 3 MB, reject a supplied foreign Origin, and never write uploads to disk or a provider. Private example/image responses are not cacheable.
- Native forms retain the same-origin App-mode envelope on redirect; Datastar message submissions replace the conversation region.

Start a fresh browser context to reset immediately. Static component-gallery interaction demos retain their existing browser-local behavior.

## Repository-owned media fixtures

The six static media fixtures are generated solid-color PNG backgrounds owned by this repository. They replace the earlier ACKMX-origin photographs, carry no third-party redistribution dependency, and remain Docs-only rather than Components package assets. Uploaded JPEG, PNG and WebP files continue to exercise the bounded session workflow; consumers supply and license their own production media.

## Verification

`Docs.Tests` compiles all 165 copied examples across 61 galleries and checks the catalog and session validation/quota boundaries. Browser coverage is in `calendar.spec.ts`, `workspace-pages.spec.ts`, `page-examples.spec.ts`, `component-examples.spec.ts`, `app-mode.spec.ts`, and the affected catalog/layout suites. The current media follow-up passes all 93 `workspace-pages.spec.ts` checks across Chromium, Firefox and WebKit, including retained selection/count, drawer focus, upload, editing and replacement. The broader suites exercise actual mutations, isolation, matching destinations, recovery, App mode, keyboard navigation, light/dark/narrow layouts and accessibility. Text-resize checks are not represented as native browser-zoom evidence.

Local work is not a release: commit, push, review resubmission, exact-head CI, merge, publication and deployment remain separate approval gates.
