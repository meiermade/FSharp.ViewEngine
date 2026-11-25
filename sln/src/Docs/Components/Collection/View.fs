namespace Docs.ComponentPages.Collection

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.LedgerExamples
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let collectionRegistration =
        applicationRegistration "components-collection" "/components/collection" "Collection" "Collection"

    let defaultCollection =
        Collection.create
            "Items"
            (ul {
                li { "Project notes" }
                li { "Team guide" }
            })
        |> Collection.render

    let collectionExample actions toolbar table =
        Collection.create "Accounts" table
        |> Collection.withDescription "Review balances and posting availability."
        |> Collection.withActions actions
        |> Collection.withToolbar toolbar
        |> Collection.render

    let private collectionSelectionForm =
        form {
            _class "grid gap-3"
            filteredAccountTable

            fveButton {
                _dataOn (
                    "click",
                    "document.getElementById('account-bulk-audit').textContent = 'Selected account IDs: ' + new FormData(evt.currentTarget.form).getAll('accountIds').join(', ')"
                )

                "Inspect selected accounts"
            }
        }

    let collectionPage =
        div {
            _attr ("data-signals__root__ifmissing", "{collectionquery: '', collectiontype: 'all', collectionstate: 'ready'}")

            _dataInit
                "(() => { const params = new URL(window.location.href).searchParams; $collectionquery = params.get('query') || ''; $collectiontype = params.get('accountType') || 'all' })()"

            _dataEffect
                "(() => { const url = new URL(window.location.href); $collectionquery ? url.searchParams.set('query', $collectionquery) : url.searchParams.delete('query'); $collectiontype != 'all' ? url.searchParams.set('accountType', $collectiontype) : url.searchParams.delete('accountType'); window.history.replaceState(window.history.state, '', url) })()"

            collectionExample collectionActions toolbar collectionSelectionForm

            p {
                _dataShow $"!({collectionHasMatches})"
                _role "status"

                _class
                    "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                "No accounts match these filters. Clear filters to restore all accounts."
            }

            p {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"

                _dataText
                    "Array.from(document.querySelectorAll('#accounts-selection tbody tr')).filter(row => row.getClientRects().length).length + ' matching accounts'"

                "4 matching accounts"
            }

            p {
                _id "account-bulk-audit"
                _role "status"
                _ariaLive "polite"
                _class "text-sm text-[var(--fve-muted-text)]"
            }
        }

    let collectionPreview =
        div {
            _class "p-4 sm:p-6 lg:p-8"
            collectionPage
        }
        |> fullBleedThemedSurface

    // Supply your actions and rendered transaction content.

    let examples () =
            [ sample "collection-default" "Default" [ "defaultCollection" ] (detailsSurface defaultCollection) ]


    let private galleryContent : Content =
        { description = "Compose collection headings, controls, results, empty states, and pagination."
          usage = []
          sections = []
          apiCategory = Some "collection" }

    let collectionPageDocumentation =
        gallery collectionRegistration galleryContent (examples ())
