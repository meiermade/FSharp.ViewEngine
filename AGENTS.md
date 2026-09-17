# Repository Agent Instructions

## Component documentation

- Give every consumer-facing reusable component a dedicated documentation route and navigation entry.
- A component page owns that component's focused variants, states, interactions, and copyable F# examples.
- Composition pages may demonstrate several components together, but must never be the only documentation location for a component.
- Keep supporting data constructors and tightly coupled helpers on their owning component page; do not create pages for internal implementation modules.
- Put bounded consumer-authored recipes under **Integration examples**, not inside an unrelated component gallery and not among universal package components.
- When adding or promoting a component, update its page registration, route, catalog group, copied-example coverage, and route/navigation contract tests in the same change.
