# Changelog

For previous versions, see the [GitHub releases](https://github.com/meiermade/FSharp.ViewEngine/releases).

## Unreleased

- Replace `fveCopyReveal` with `_masked true` on `fveInput` for editable values, or on `fveDescriptionListItem` with `_value value` for read-only values with Show/Hide and Copy. Remove `copy-reveal` from `fve.json` before installing `input` and/or `description-list` with `dotnet fve add … --overwrite` (merge any local edits first). Replace the old browser module import with `Components/Assets/masked-value.js`. Remove the old `CopyReveal.fs` and `copy-reveal.js` files once their references are gone.
