namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal ChoiceCards =
    let render attributes children =
        FieldGroup.render attributes children

    let part tag (classes:string) defaults attributes children =
        ComponentElement.render tag (_class classes :: defaults) attributes children

/// <category>choice-cards</category>
[<AutoOpen>]
module ChoiceCardsDsl =
    /// Layout only. Native Radio/Checkbox children own names, values and cardinality.
    let fveChoiceCards = ComponentElementBuilder(ChoiceCards.render)
    let fveChoiceCardsContent = ComponentElementBuilder(ChoiceCards.part "div" "@container/choice-cards grid min-w-0 gap-3 sm:grid-cols-2" [])
    /// An explicit native label surrounding caller-authored controls and content.
    let fveChoiceCard = ComponentElementBuilder(ChoiceCards.part "label" "relative flex min-w-0 flex-col @min-[12rem]/choice-cards:flex-row cursor-pointer gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 sm:p-4 ring-1 ring-[var(--fve-border)] not-has-[:disabled]:not-has-[[readonly]]:not-has-[[aria-disabled=true]]:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] not-has-[:disabled]:not-has-[[readonly]]:not-has-[[aria-disabled=true]]:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] has-[:checked]:bg-[var(--fve-brand-subtle)] has-[:checked]:ring-2 has-[:checked]:ring-[var(--fve-brand-ring)] has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-[var(--fve-brand-ring)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50 [overflow-wrap:anywhere]" [])
    let fveChoiceCardContent = ComponentElementBuilder(ChoiceCards.part "span" "min-w-0 flex-1" [])
    let fveChoiceCardTitle = ComponentElementBuilder(ChoiceCards.part "strong" "block text-sm text-[var(--fve-text)]" [])
    let fveChoiceCardDescription = ComponentElementBuilder(ChoiceCards.part "span" "mt-1 block text-sm text-[var(--fve-muted-text)]" [])
    let fveChoiceCardMetadata = ComponentElementBuilder(ChoiceCards.part "span" "mt-2 block text-xs font-medium text-[var(--fve-muted-text)]" [])
    let fveChoiceCardMedia = ComponentElementBuilder(ChoiceCards.part "span" "flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-[var(--fve-radius-control)] bg-[var(--fve-neutral-subtle)]" [_ariaHidden true])
