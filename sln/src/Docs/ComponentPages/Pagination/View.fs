namespace Docs.ComponentPages.Pagination

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
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let paginationRegistration =
        registration "components-pagination" "/components/pagination" "Pagination" "Pagination"

    let defaultPagination =
        fvePagination {
            _ariaLabel "Results pages"

            fvePaginationContent {
                fvePaginationItem {
                    _page 1
                    _ariaCurrent "page"
                }

                fvePaginationItem {
                    _page 2
                    _href "/components/pagination/page?page=2"
                }
            }
        }

    type PaginationDestination = PaginationPage of int

    let paginationDestinationUrl (PaginationPage page) =
        $"/components/pagination/page?page={page}"

    let paginationConfig (variant: PaginationVariant) requestedPage =
        let currentPage = Math.Clamp(requestedPage, 1, 8)

        let pageItem page =
            fvePaginationItem {
                _page page

                if page = currentPage then
                    _ariaCurrent "page"
                else
                    _href (paginationDestinationUrl (PaginationPage page))
            }

        let middlePages = [ max 2 (currentPage - 1) .. min 7 (currentPage + 1) ]
        let firstResult = (currentPage - 1) * 25 + 1
        let lastResult = min 184 (currentPage * 25)

        fvePagination {
            _ariaLabel "Results pages"
            _variant variant

            fvePaginationPrevious {
                if currentPage > 1 then
                    _href (paginationDestinationUrl (PaginationPage(currentPage - 1)))
            }

            fvePaginationContent {
                pageItem 1

                if List.head middlePages > 2 then
                    fvePaginationEllipsis { () }

                for page in middlePages do
                    pageItem page

                if List.last middlePages < 7 then
                    fvePaginationEllipsis { () }

                pageItem 8
            }

            fvePaginationNext {
                if currentPage < 8 then
                    _href (paginationDestinationUrl (PaginationPage(currentPage + 1)))
            }

            fvePaginationSummary { $"Showing {firstResult}–{lastResult} of 184 items" }
        }

    let paginationPreview requestedPage =
        paginationConfig PaginationVariant.Full requestedPage

    let paginationVariantPreview variant requestedPage = paginationConfig variant requestedPage

    let paginationPreviewRegion requestedPage =
        div {
            _id "components-pagination-region"
            paginationPreview requestedPage
        }

    let paginationExamples requestedPage =
        [ sample
              "pagination"
              "Page navigation"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationPreview" ]
              (centered (paginationPreviewRegion requestedPage))
          sample
              "pagination-numbers"
              "Numbers only"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationVariantPreview" ]
              (centered (paginationVariantPreview PaginationVariant.NumbersOnly requestedPage))
          sample
              "pagination-edges"
              "Previous and next only"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationVariantPreview" ]
              (centered (paginationVariantPreview PaginationVariant.PreviousNextOnly requestedPage)) ]

    let paginationGalleryExamples requestedPage =
        sample "pagination-default" "Default" [ "defaultPagination" ] (fieldSurface defaultPagination)
        :: paginationExamples requestedPage

    let examples () = paginationGalleryExamples 2


    let private galleryContent : Content =
        { description = "Navigate bounded result pages with current, previous, next, and compact range items."
          usage = []
          sections = []
          apiCategory = Some "pagination" }

    let paginationPageFor requestedPage =
        gallery paginationRegistration galleryContent (paginationGalleryExamples requestedPage)

    let paginationPage = paginationPageFor 2
