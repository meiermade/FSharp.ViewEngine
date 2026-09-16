# Unified Components coverage

Working inventory for [MEIER-752](https://www.notion.so/3c878df1e7ed816595fcf35cc37394e6). This is not a declaration that every listed pattern is implemented or release-accepted.

## Sources reviewed

The executable Specs, not accidentally incomplete production applications, define the product UI to support. Source review currently covers workflow declarations and representative shared shell, public-site, product/cart, and viewer implementations; the full route/state acceptance inventory is still being filled in.

| Product | Source checkout HEAD | Relevant source |
| --- | --- | --- |
| Enslie | `f704b586fff4be5cda3f77ffb89e25ad3e0e6904` | `sln/src/Spec/src/{Public,Web,Phone,AppMode}.fs` |
| ACKMX | `726dbeeb66365e245f18e94f2364c570154ff370` | `sln/src/Spec/src/Common/{RiderApp,Admin}.fs` |
| Funktos | `5bc24ba2d3ab6509a0b315015d11489847232eb6` | `sln/src/Spec/src/Common/Wireframe.fs`; compare the newer `revise-executable-spec` worktree before fixing acceptance scope |
| Geldos | `71beb14d2cd0cec9e72c7341c7e1cf7b905476d2` | `sln/src/Spec/src/Common/Wireframe.fs` |

These are local checkouts, not immutable snapshots of all working files. At inventory start, Enslie had generated CSS changes; Funktos had Common/View and asset changes; Geldos had Common/Handler and test changes. Nothing here changes, restarts, or migrates those projects.

## Public organization

`FSharp.ViewEngine` remains the engine. The UI assembly is `FSharp.ViewEngine.Components`, organized into `Primitives`, `Application`, `Marketing`, `Ecommerce`, and `Documentation`. These are not separate UI packages. Common controls and tokens belong to Primitives; the remaining areas compose them. Documentation owns the viewer, not consumer journeys or domain state.

## Coverage and remaining deltas

“Local” below means implemented in the preserved candidate, not published or accepted. The catalog's existing 79 copied examples are the starting point, not the final required count.

| Product workflow / pattern | Owner | Current public coverage | Remaining delta / proving example |
| --- | --- | --- | --- |
| All four: native fields, choices, errors, feedback and overlays | Primitives | Local Input, Textarea, Checkbox, RadioGroup, Select, searchable Select, ErrorSummary, Notice, native dialogs/popovers | Multiple Select/searchable Select; retain actual FormData, disabled/mixed state, remote result and morph checks |
| Geldos Accounts/Transactions; Funktos Functions/Runs; ACKMX Team/equipment | Primitives + Application | Local Table, DescriptionList, Metric, Pagination, Collection, Detail | Server sorting; complete populated/empty/loading/no-match/permission states and connected form actions |
| All four: navigation and responsive identity | Primitives + Application | Local Breadcrumbs, SideNav, action clusters, PageHeader/Page/AppShell | Consolidate API ownership; retain one navigation tree, genuine record destinations and one main landmark |
| Geldos ViewHome/Accounts/ViewAccount; Funktos console/settings; ACKMX administrative resources | Application | Local financial workspace and record fixtures | Connected shell → collection → matching detail → edit/action → settings example, without private duplicate components |
| Enslie Landing/HowItWorks/Help/Contact; Geldos ViewLandingPage/Pricing; Funktos public site; ACKMX Home/Ride/About | Marketing | Bespoke product source, no public Marketing family yet | Site header/footer, hero, feature, pricing, FAQ, CTA, content/contact sections; coherent home/features/pricing/contact site |
| ACKMX Shop/Product; Enslie Products/Shop/ProductPage | Ecommerce | Bespoke product grids/cards and detail source | Product card/grid, category/filter presentation, image/detail layout and finite variant selection |
| ACKMX Cart/CartEmpty/CheckoutShopOnly; Enslie Cart/Checkout/Orders | Ecommerce | Bespoke cart, summary and checkout source | Quantity/removal/empty states, validation, consistent demo totals and selected variants; matching explicitly simulated order/detail/history |
| All four: executable Specs and reference material | Documentation | Article/reference/canvas/gallery/diagram/frame APIs now compile inside Components; shared assembly and optional-asset boundary tested | Shared public renderers; close landmark and outstanding loader/readiness gaps |
| Enslie browser/phone journey and other products' browser frames | Documentation | Stable frame/journey patterns in siblings; local FVE App mode not implemented | One typed App mode, authored frame identity, Previous/State/Next, dock placement and Exit; real navigation/forms/history |
| Local developer loop | Repository host/build | One WatchDocs loop, shared stylesheet watching, five-family navigation and documented focused catalog checks | Complete connected examples and resettable provider-free demo journeys |

## Product-owned boundaries

- Geldos owns accounting invariants, ledgers, transaction semantics, formatting and financial provider operations.
- Funktos owns dependency execution, scheduling policy, run lifecycle and worker orchestration.
- ACKMX owns availability, staffing, booking/participant/waiver rules, inventory, custody, payments and fulfillment.
- Enslie owns group participation, consent, messaging, recommendation policy and merchant integrations.
- Product-specific phone content, maps, graphs, rich editors and specialized visualizations remain consumer-owned unless a concrete reusable UI need is separately agreed. This is not permission to defer an ordinary missing control.

## Local consolidation checkpoint

- The maintained Documentation source and optional stylesheet now live under `sln/src/FSharp.ViewEngine.Components/Documentation`; the separate Docs project is removed. Existing controls use `.Primitives`; page/shell/collection/detail compositions use `.Application`. Shared actions and Section remain in Primitives to avoid upward dependencies.
- The host and all 79 independent snippets use the migrated APIs. Documentation overview, navigation and installation guidance no longer advertise a new Docs release. Historical changelog/package versions are retained.
- Release selection is engine, Components, or both (engine first, with Components depending on that exact selected engine version). Legacy Docs publication is rejected. Preview/container verification follows the unified package; no workflow was executed remotely.
- Local evidence: 53 catalog tests (including all copied snippets and single-assembly/selective-asset assertions), 85 engine/Documentation tests, 24 build tests, isolated shared/Documentation Tailwind checks and Actionlint. Focused Chromium/Firefox/WebKit coverage checks copying, table variants, Documentation routes/navigation/unique headings, and long-namespace wrapping at 320px/200% in light/dark. A failing 80px paragraph-overflow regression was fixed in shared paragraph/list styling, not by shortening fixture content.
- A real isolated `.NET 10.0.5` consumer restores the locally packed Components package, renders controls and Documentation, and passes dependency/asset/symbol checks. This is **not** .NET 8/9 evidence or final release acceptance. The full retry-free browser/lifecycle and packaged-framework matrix remains open.
- The candidate watcher serves `http://127.0.0.1:5054`; only this repository's watcher was restarted after the project graph changed. Nothing is committed, pushed, deployed, published or deprecated. Marketing, Ecommerce, App mode, the full Spec route/state inventory and remaining controls/journeys are still implementation work, not accepted merely because their planned family names appear above.

## Five-area catalog checkpoint

The host now exposes peer **Primitives, Application, Marketing, Ecommerce and Documentation** navigation. `/components` is the shared directory; `/components/primitives` owns existing controls and Section; `/components/application` links the six existing shell/page/collection/detail galleries. Marketing and Ecommerce have explicit in-development indexes rather than fake previews. Documentation remains at `/docs`; every old component route and copied-example fragment is retained.

`Catalog.fs` owns family organization, and the host registry drives navigation, search, breadcrumbs and previous/next ordering. Tests check unique route ownership and every registered pager edge. Catalog cards use Documentation/Datastar navigation, retaining the document through browsing and back/forward. Unit coverage remains 79 compiled examples; focused browser coverage includes real clipboard writes, family browsing/search/history, mobile navigation dismissal/focus, light/dark, 1440px/390px/320px, 200% text and Axe.

The larger catalog regression exposed an unconditional Prism-promise wait on code-free indexes. The helper now waits for DOM parsing, then requires Prism readiness only when code exists; a dedicated regression confirms these indexes do not request Prism. This resolves that specific helper failure, not the unrelated historical full-suite/loader or inline AppShell landmark gaps. No timeouts were raised or failures excluded from final acceptance.

README now documents the single watch command, family URLs, project-graph restart boundary and focused tests. The complete Application form/action/settings journey, Marketing/Ecommerce implementation, shared App mode and full product coverage inventory remain unfinished within MEIER-752. Navigation implementation is not a claim that all five families or release requirements are complete.

## Forms gallery refinement checkpoint

Input now has twelve single-control examples: label, help, required, optional, validation, leading icon, prefix, suffix, clearable search, read-only, disabled and pending. `Input.withLeadingIcon`, `Input.withPrefix` and `Input.withSuffix` are shared Primitives APIs; prefix/suffix are accessible non-editable context, not submitted values. Textarea has basic/help/error and editing-state examples. Select, Checkbox, Switch and Radio group now begin with basic/help examples before their retained submission/validation and state demonstrations. searchable Select, ToggleButton and ErrorSummary already followed the focused/state pattern and remain supported rather than gaining redundant variants.

Application → Forms → `/components/form-layouts` owns three complete contact forms (stacked, two-column, sectioned), each with independent IDs, a real demo validation endpoint and preserved values/error focus after morphs. The prior complete Input validation workflow and account-result search are there; the original contact endpoint and default field IDs remain at the new page. Existing primitive page/example-root routes remain. Layouts use native HTML and the public controls/Section APIs; there is no new form framework or domain policy in Components. Icon redistribution attribution is in `THIRD-PARTY-NOTICES.md`.

Local evidence: 56 catalog tests compile all **103** copied examples across **35** galleries; 85 Core/Documentation and 24 build tests pass. 48 forms checks plus 45 broader catalog/copy/navigation/choice regressions passed retry-free across Chromium/Firefox/WebKit. Actual Input and complete-form desktop/mobile/dark/200% screenshots were inspected. Regression-driven fixes addressed suffix-only description association and Switch wrapping at 320px/200%. Isolated Tailwind verification includes adorned input focus selectors and sizing; a local `0.0.4-forms` package consumer exercised the public APIs on genuine .NET 10.0.5. This is focused local proof, not final release or .NET 8/9 acceptance. All unrelated inventory, connected-journey, multiple-choice, sorting, App-mode, lifecycle and delivery obligations remain open.

## Multiple-selection checkpoint

`Select.multiple` / `Select.withSelectedMany` and `Select.multiple` / `Select.withSelectedMany` now provide compile-time-distinct multiple modes through the existing shared renderers. Single-mode source calls remain supported. Multiple Select has a focused multiple listbox, separate active/selected state, bounded arrows/Home/End/typeahead, Space/Enter toggling, continued opening, dismissal/focus return, summary and clear. Multiple searchable Select retains ordered identities/labels outside its editable query, provides named removal and separate search/selection clearing, and never removes selections through Backspace.

Static and remote selections produce repeated native hidden values. The host validates the actual URL-encoded form POST, including a demonstrated one-to-three-member policy; Components does not own validation or persistence. `Select.withQuery` associates remote results with their request. Per-instance abort controllers protect newer requests and clean up when fields are removed/disabled. Selected presentation survives result and whole-field morphs, including selections absent from the current results, while the surrounding validation/disabled state stays server-owned. Initial values require supplied labels and unique encoded keys. The README documents explicit authoritative signal resets.

Eleven focused examples were added across the existing Select/searchable Select galleries (now **114 snippets / 35 galleries**), including preselection, native validation, static/remote search, loading, disabled and pending. Remote examples are provider-free, support empty/error/retry and whole-field refresh, and preserve the query/focus/selection. Narrow field previews use compact padding; shared choice grids have shrinkable tracks and readable wrapping options. Scrollable searchable Select popups and selected-item actions have explicit keyboard access.

Local evidence: **58** catalog tests compile every copied snippet and reject mixed-mode setters; **85** Core/Documentation and **24** build tests pass, as does isolated Tailwind verification. The local **0.0.5-multiple** package passes the genuine **.NET 10.0.5** consumer, including both multiple APIs and Documentation. All **33** new Chromium/Firefox/WebKit checks pass without retries. Inspected desktop and narrow/enlarged dark rendering. In the broader 75-check run, 69 passed and six failed: three are the previously recorded inline AppShell landmark failures; the other three were an old single-Select assertion ambiguously matching new selection-count announcements. That assertion now targets its exact existing result, and its full three-browser test passes **3/3**. The broader run is not claimed wholly green.

Diagnosed/fixed failures included internal-record serialization producing empty selected objects, native URL-encoded versus assumed multipart test data, grid min-content overflow, managed selected children being overwritten by a whole-field morph, scrollable popup keyboard access, explicit WebKit action tab stops, and an off-screen test `fill()` fixed by a real initial click. No forced clicks, raised timeouts, assertion exclusions or hidden retries were used. The existing AppShell and historical loader/full-suite gaps remain, along with genuine .NET 8/9, exact Spec coverage, connected Application/Marketing/Ecommerce journeys, sorting, App mode and delivery. The earlier Input/form alignment and gallery-copy discussion is not claimed completed by this multiple-selection checkpoint.

## Local popup-focus refinement

After review against the licensed Tailwind Plus Select Menu source, Select triggers and searchable Select inputs use a 2px inset brand outline for keyboard-visible focus only; their normal background/text stay unchanged. Active popup rows use solid brand fills, while selected inactive rows keep subtle tint, checkmark and stronger weight. Dark active rows use bright brand text with dark surface text to satisfy both text and panel-boundary contrast. DropdownMenu, clear/remove/retry and sticky-table overflow controls remain background-focused without a normal outline. Floating panels have no ordinary outline/border/ring; surface, radius and elevation remain. System-color panel boundaries and actual focus/active-descendant outlines remain in forced-colors mode. Gallery frames and unrelated control focus styles are unchanged.

Evidence: **60** retry-free Chromium/Firefox/WebKit checks (**18** focus/contrast/forced-colors/sticky-table checks, **33** multiple-choice regressions and **9** existing menu/searchable Select/anchoring regressions); **58** catalog tests still compile all **114** snippets; isolated Tailwind and genuine **.NET 10.0.5** package **0.0.7-field-focus** verification pass. Desktop light/dark and 320px/200%-text popup screenshots plus focused Select/searchable Select field states were inspected. Regression-driven fixes removed unsafe color interpolation, restored field-only keyboard outlines after review, corrected dark active-row foreground/boundary contrast, and retained the table overflow trigger fill. Earlier full-suite, AppShell, runtime-matrix and integrated delivery limitations remain unchanged. Nothing committed, pushed, published or deployed.

Everything remains in the preserved uncommitted candidate. No commit, push, PR update, sibling change, publication, deployment or Docs deprecation occurred. Watcher: `/tmp/fve-multiple-watchdocs.log`; evidence: `/tmp/fve-multiple-{unit,core,build-tests,tailwind,package-verification,final-browser,single-regression}.log`.

## Acceptance rules

Every relevant reusable row must end with its actual API, example route/state and focused test evidence. Add missing rows discovered during route/state review; do not mark the map complete from this initial inventory. Complete examples use public APIs and coherent resettable demo state, not inert buttons or unrelated success screens. The application, marketing and ecommerce examples all use the same Documentation App mode.

Use the existing local candidate at `http://127.0.0.1:5054`, without live payment/customer operations. Inspect light/dark, desktop, 390px/320px, enlarged text, keyboard/focus, forms and navigation/history. Preserve the final genuine framework/package and retry-free three-browser release gates. No CLI, separate family releases, sibling migration or delivery is authorized by this inventory.
