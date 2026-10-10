# Repository Agent Instructions

## Local Docs development

- For local Docs development and review, run `./fake.sh WatchDocs` from the active worktree's `sln` directory. Use its single watcher at `http://127.0.0.1:5054`; do not start separate manual Docs hosts or use alternate ports. The target safely replaces the previous Docs watcher across worktrees.

## Component documentation

- Give every consumer-facing reusable component a dedicated documentation route and navigation entry.
- A component page owns that component's focused variants, states, interactions, and copyable F# examples.
- Composition pages may demonstrate several components together, but must never be the only documentation location for a component.
- Keep supporting data constructors and tightly coupled helpers on their owning component page; do not create pages for internal implementation modules.
- Keep layout-component galleries focused on structure and behavior with minimal illustrative content, not complete application workflows.
- Keep installable building blocks under **Components**. Put assembled pages and workflows in exactly three full-page **Examples** templates: Specification, Application, and API documentation, sharing the same financial example model. Examples is the last catalog navigation group, below Project. Its three-card gallery opens templates in new tabs; templates own their navigation, without Preview/Code chrome. The catalog host adds a source-ZIP download to their top bar, outside the copied source. Do not introduce Application/Documentation component categories or an Integration examples category.
- When adding or promoting a component, update its page registration, route, catalog group, copied-example coverage, and route/navigation contract tests in the same change.

## Public documentation destinations

- Every user-facing link, form action, workflow destination, and displayed Browser address must resolve to a real same-origin catalog route or a genuine external resource.
- Connected examples must navigate between actual server-rendered states; never use invented hosts or dead placeholder endpoints as interactive destinations.
- Use relative paths or explicit variables such as `$API_ORIGIN` in hypothetical code snippets instead of fabricated domains.
- Reserved fake hosts are allowed only in non-rendered tests that specifically verify foreign-origin or encoding behavior.
