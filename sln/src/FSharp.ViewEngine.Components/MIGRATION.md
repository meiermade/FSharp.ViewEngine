# Migrating installed source

Run `dotnet fve diff --config ...` before updating the pinned CLI. Preserve local edits and migrate them manually before removing retired files, selectors, configuration entries, or compile items. `fve add --overwrite` explicitly replaces source; there are no compatibility aliases for retired selectors.

## Side nav

Replace required header/sections arguments with optional slots:

```fsharp
let navigation id label header nodes =
    SideNav.create id label
    |> SideNav.withHeader header
    |> SideNav.withContent (SideNavContent.create nodes)
```

- Omit `withHeader` rather than supplying a dummy header and calling `withoutHeader`.
- Replace static `SideNavSection.group` with `SideNavSection.create`. Former static uses of the interim `SideNavGroup.create` also become sections.
- Replace collapsible `SideNavItem.nested` with `SideNavGroup.create`; move expansion and identity options to the group.
- Pass ordered `SideNavNode` values directly to `SideNavContent.create`; remove the `ungrouped` wrapper.
- Context/footer accept lists of rendered elements without automatic padding. Use `SideNavRow` or pad custom content. Bound the parent height so only Content scrolls.

See [Side nav](https://fve.meiermade.com/components/side-nav) for current examples and APIs.

## Tooltip

`Tooltip.create` now accepts a trigger renderer receiving the description ID. Apply `_ariaDescribedby descriptionId` to the actual focusable control, retaining its accessible name and any existing description IDs. The former arbitrary-HTML overload is not retained. See [Tooltip](https://fve.meiermade.com/components/tooltip).

## Retired selectors and aggregates

| Previous boundary | Replacement |
| --- | --- |
| `text-field` | `input`, `textarea`, `error-summary` |
| `identity` | `avatar`, `copy-reveal` |
| `tags` | `tag-input` |
| Drawer bundled with Dialog | Install `drawer` and `dialog` separately |
| `upload-list` (`UploadState`, `UploadItem`, `UploadList`) | Consumer-owned queue/transport UI with ordinary HTML and selected controls |
| `first-steps` (`FirstStep`, `FirstSteps`) | Consumer-owned checklist inside `FloatingPanel` |
| `bulk-actions` (`BulkAction`, `BulkActions`) | `Table.withSelection` with native form values and ordinary Button/DropdownMenu controls |
| Page/Section/Collection/Detail/AppShell and Documentation aggregates | Consumer-authored layouts using independently installed components |

Applications own upload transport, checklist progress, batch processing, authorization, confirmation, and outcomes. The [Examples gallery](https://fve.meiermade.com/examples) supplies three complete compositions, not replacement framework APIs. Repository-only `Templates/` support is excluded from the CLI registry and copied examples.
