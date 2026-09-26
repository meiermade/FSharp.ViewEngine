# Repository Agent Instructions

## Component documentation

- Give every consumer-facing reusable component a dedicated documentation route and navigation entry.
- A component page owns that component's focused variants, states, interactions, and copyable F# examples.
- Composition pages may demonstrate several components together, but must never be the only documentation location for a component.
- Keep supporting data constructors and tightly coupled helpers on their owning component page; do not create pages for internal implementation modules.
- Keep layout-component galleries focused on structure and behavior with minimal illustrative content, not complete application workflows.
- Put assembled pages, connected workflows, and bounded consumer-authored recipes under **Page examples**, with links to their component documentation. Do not introduce a separate Integration examples category.
- When adding or promoting a component, update its page registration, route, catalog group, copied-example coverage, and route/navigation contract tests in the same change.

## Public documentation destinations

- Every user-facing link, form action, workflow destination, and displayed Browser address must resolve to a real same-origin catalog route or a genuine external resource.
- Connected examples must navigate between actual server-rendered states; never use invented hosts or dead placeholder endpoints as interactive destinations.
- Use relative paths or explicit variables such as `$API_ORIGIN` in hypothetical code snippets instead of fabricated domains.
- Reserved fake hosts are allowed only in non-rendered tests that specifically verify foreign-origin or encoding behavior.
