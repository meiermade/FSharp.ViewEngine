namespace Example.Server

open System
open FSharp.ViewEngine
open Example.Components
open type Html
open type Datastar
open Model
open Ledger.Domain

open SpecificationPages.Workflow

module Specification =
    let private workflows =
        [ SpecificationPages.Home.workflow
          SpecificationPages.ViewAccounts.workflow
          SpecificationPages.ViewAccount.workflow
          SpecificationPages.CreateAccount.workflow
          SpecificationPages.UpdateAccount.workflow
          SpecificationPages.DeleteAccount.workflow
          SpecificationPages.ViewTransactions.workflow
          SpecificationPages.ViewTransaction.workflow
          SpecificationPages.DeleteTransaction.workflow
          SpecificationPages.Workspace.workflow
          SpecificationPages.Settings.workflow
          SpecificationPages.Profile.workflow ]
        |> List.map (fun workflow ->
            { workflow with
                rules = shellRules @ workflow.rules })

    let workflowPages = workflows |> List.map _.key
    let pages = workflowPages @ (Architecture.navigation |> List.map fst)

    let navigation =
        [ "/examples/specification", "Overview"
          yield!
              workflows
              |> List.map (fun workflow -> "/examples/specification/" + workflow.key, workflow.label)
          yield!
              Architecture.navigation
              |> List.map (fun (key, label) -> Architecture.href key, label) ]

    let title page =
        workflows
        |> List.tryFind (fun workflow -> workflow.key = page)
        |> Option.map _.label
        |> Option.defaultWith (fun () ->
            Architecture.navigation
            |> List.tryFind (fst >> (=) page)
            |> Option.map snd
            |> Option.defaultValue "Geldos specification")

    let private specUrl page (query: Query) state appMode =
        specificationHref page state { query with appMode = appMode }

    let private resolve page (query: Query) =
        let workflow =
            workflows
            |> List.find (fun workflow -> workflow.key = (if page = "" then "home" else page))

        let selected =
            workflow.states
            |> List.tryFind (fun state -> state.key = query.specState)
            |> Option.defaultValue workflow.states.Head

        workflow, selected

    let private resourcePage (query: Query) (state: State) =
        match query.resource, state.page with
        | id, ApplicationPage.Environment _ when tryEnvironment id |> Option.isSome -> ApplicationPage.Environment id
        | id, ApplicationPage.EditEnvironment _ when tryEnvironment id |> Option.isSome ->
            ApplicationPage.EditEnvironment id
        | _ ->
            match Int32.TryParse query.resource, state.page with
            | (true, id), ApplicationPage.Account _ when tryAccount id |> Option.isSome -> ApplicationPage.Account id
            | (true, id), ApplicationPage.EditAccount _ when tryAccount id |> Option.isSome ->
                ApplicationPage.EditAccount id
            | (true, id), ApplicationPage.DeleteAccount _ when tryAccount id |> Option.isSome ->
                ApplicationPage.DeleteAccount id
            | (true, id), ApplicationPage.Transaction _ when tryTransaction id |> Option.isSome ->
                ApplicationPage.Transaction id
            | (true, id), ApplicationPage.DeleteTransaction _ when tryTransaction id |> Option.isSome ->
                ApplicationPage.DeleteTransaction id
            | _ -> state.page

    let productPage page query =
        let _, selected = resolve page query in resourcePage query selected

    let private productQuery (query: Query) (state: State) =
        let parameter key fallback =
            state.parameters
            |> List.tryPick (fun (name, value) -> if name = key then Some value else None)
            |> Option.defaultValue fallback

        let authored = queryFromValues (fun key -> parameter key "")

        let choose value defaultValue fixtureValue =
            if value = defaultValue then fixtureValue else value

        { query with
            specification = true
            embedded = false
            specState = state.key
            workspace =
                workspaceFromStrings
                    (parameter "organization" query.workspace.organization.id)
                    (parameter "environment" query.workspace.environment.id)
                    (parameter "ledger" query.workspace.ledger.id)
            search = choose query.search "" authored.search
            accountType = choose query.accountType "all" authored.accountType
            filters =
                (if query.filters.IsEmpty then
                     authored.filters
                 else
                     query.filters)
            sort = choose query.sort "name" authored.sort
            state = choose query.state "" authored.state
            account = choose query.account "all" authored.account
            range = choose query.range "90" authored.range
            comparison = choose query.comparison "expected" authored.comparison
            status = choose query.status "all" authored.status }

    let productContext page query =
        let _, selected = resolve page query
        productQuery query selected

    let private currentUrl (workflow: Workflow) (state: State) (query: Query) appMode =
        specUrl workflow.key query state.key appMode
        + querySuffix
            [ if query.search <> "" then
                  yield "search", query.search
              if query.accountType <> "all" then
                  yield "accountType", query.accountType
              if query.sort <> "name" then
                  yield "sort", query.sort
              if query.state <> "" then
                  yield "state", query.state
              if query.account <> "all" then
                  yield "account", query.account
              if query.range <> "90" then
                  yield "range", query.range
              if query.comparison <> "expected" then
                  yield "comparison", query.comparison
              if query.startDate <> "" then
                  yield "startDate", query.startDate
              if query.endDate <> "" then
                  yield "endDate", query.endDate
              if query.status <> "all" then
                  yield "status", query.status
              if not query.filters.IsEmpty then
                  yield "filters", String.concat "," query.filters ]

    let private diagram label (source: string) =
        div {
            _role "region"
            _ariaLabel label
            _tabindex 0
            _class "min-w-0 overflow-x-auto focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            fveMermaid { source }
        }

    let private productAddress productPage requested =
        let addressQuery =
            { requested with
                specification = false
                appMode = false }

        applicationHref addressQuery productPage
        + querySuffix
            [ for key, value in collectionQueryPairs addressQuery do
                  if value <> "" && value <> "all" && not (key = "sort" && value = "name") then
                      yield key, value
              if addressQuery.state <> "" then
                  yield "state", addressQuery.state
              if addressQuery.range <> "90" then
                  yield "range", addressQuery.range
              if addressQuery.comparison <> "expected" then
                  yield "comparison", addressQuery.comparison
              if addressQuery.startDate <> "" then
                  yield "startDate", addressQuery.startDate
              if addressQuery.endDate <> "" then
                  yield "endDate", addressQuery.endDate
              if addressQuery.overlayFrom <> "" then
                  yield "from", addressQuery.overlayFrom ]


    /// A complete product document uses the same views and forms as the Application example.
    let renderProduct page (query: Query) =
        let workflow, selected = resolve page query

        let requested =
            { productQuery query selected with
                appMode = false
                fixture = true
                previewId = ""
                topBarActions = [] }

        let productPage = resourcePage query selected

        div {
            _id "spec-product-document"
            _data ("fixture-href", currentUrl workflow selected { query with fixture = false } false)
            _class "h-dvh min-h-0 overflow-hidden"
            Application.render productPage requested
        }

    let private wireframe (query: Query) (workflow: Workflow) (selected: State) =
        let id = "spec-" + workflow.key.Replace('/', '-')
        // This ordered journey is consumer-authored, not catalog page order or state order.
        let journey =
            [ "home"
              "accounts/view-accounts"
              "accounts/view-account"
              "accounts/update-account"
              "transactions/view-transactions"
              "transactions/view-transaction" ]

        let destination index =
            let step = workflows |> List.find (fun candidate -> candidate.key = journey[index])

            step.label,
            specUrl
                step.key
                { defaultQuery with
                    workspace = query.workspace
                    returnWorkspace = query.returnWorkspace }
                step.states.Head.key
                query.appMode

        let previous, next =
            match journey |> List.tryFindIndex ((=) workflow.key) with
            | Some index ->
                (if index = 0 then None else Some(destination (index - 1))),
                (if index = journey.Length - 1 then
                     None
                 else
                     Some(destination (index + 1)))
            | None -> None, None

        let states =
            workflow.states
            |> List.map (fun state ->
                let requested =
                    if state.key = selected.key then
                        query
                    elif backgroundPage query state.page <> state.page then
                        { query with
                            state = ""
                            editorDraft = None
                            resource = (if state.page = selected.page then query.resource else "") }
                    else
                        { defaultQuery with
                            workspace = query.workspace
                            appMode = query.appMode
                            returnWorkspace = query.returnWorkspace
                            overlayFrom = query.overlayFrom }

                state.label, currentUrl workflow state requested query.appMode, state.key = selected.key)

        let label = workflow.label + " · " + selected.label

        let toggleHref =
            if query.appMode then
                currentUrl workflow selected query false + querySuffix [ "fveAppReturn", id ]
            else
                currentUrl workflow selected query true

        let window = AppMode.windowControl id label query.appMode toggleHref
        let controls = AppMode.controls id label states previous next

        let address =
            productAddress (resourcePage query selected) (productQuery query selected)

        let source = currentUrl workflow selected { query with fixture = true } false

        fveBrowser {
            _id "spec-product-browser"
            _expanded query.appMode
            _address address

            div {
                _class (
                    if query.appMode then
                        "h-full min-h-0"
                    else
                        "h-[40rem] min-h-0 overflow-hidden"
                )

                iframe {
                    _id "spec-product-frame"
                    _title (workflow.label + " product")
                    _src source
                    _data ("spec-fixture", "product")
                    _data ("fixture-source", source)
                    _dataPreserveAttr "src"
                    _class "block h-full w-full border-0 bg-[var(--fve-background)]"
                }
            }

            fveBrowserWindowControls {
                _class "min-w-0 max-w-full"

                div {
                    _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"
                    window
                    controls
                }
            }
        }
        |> AppMode.fixture id query.appMode

    let private sidebar query =
        let item key (label: string) =
            fveSideNavItem {
                _href (
                    workspaceUrl
                        (if key = "" then
                             "/examples/specification"
                         else
                             "/examples/specification/" + key)
                        query.workspace
                )

                label
            }

        let group label children =
            fveSideNavGroup {
                _label label
                yield! (children |> List.map (fun (key, label) -> item key label))
            }

        [ item "" "Overview"
          item "home" "View home"
          group
              "Accounts"
              [ for workflow in workflows do
                    if workflow.key.StartsWith("accounts/", StringComparison.Ordinal) then
                        yield workflow.key, workflow.label ]
          group
              "Transactions"
              [ for workflow in workflows do
                    if workflow.key.StartsWith("transactions/", StringComparison.Ordinal) then
                        yield workflow.key, workflow.label ]
          item "workspace" "Select workspace"
          item "settings" "View settings"
          item "profile" "View profile"
          fveSideNavGroup {
              _label "Architecture"

              item "architecture" "System context"

              group
                  "Solution"
                  [ "architecture/solution", "Overview"
                    "architecture/solution/domain", "Example.Domain"
                    "architecture/solution/application", "Example.Application"
                    "architecture/solution/components", "Example.Components"
                    "architecture/solution/server", "Example.Server" ]
          } ]

    let render (page: string) (query: Query) =
        let content =
            if page.StartsWith("architecture", StringComparison.Ordinal) then
                Architecture.render page
            elif page = "" then
                SpecificationPages.Overview.render workflows query
            else
                let workflow, selected = resolve page query

                div {
                    _class "grid min-w-0 grid-cols-1 gap-12"
                    Layout.documentationSection "wireframe" "Wireframe" (wireframe query workflow selected)

                    if not query.appMode then
                        Layout.documentationSection
                            "sequence"
                            "Sequence"
                            (diagram (workflow.label + " sequence") workflow.sequence)

                        Layout.documentationSection
                            "rules"
                            "Rules"
                            (Layout.prose (
                                ul {
                                    for rule in workflow.rules do
                                        li { rule }
                                }
                            ))
                }

        let current =
            workspaceUrl
                (if page = "" then
                     "/examples/specification"
                 else
                     "/examples/specification/" + page)
                query.workspace

        let href key =
            workspaceUrl
                (if key = "" then
                     "/examples/specification"
                 else
                     "/examples/specification/" + key)
                query.workspace

        let crumbs =
            [ fveBreadcrumbItem {
                  _href (href "")
                  "Overview"
              }
              if page.StartsWith("architecture/", StringComparison.Ordinal) then
                  fveBreadcrumbItem {
                      _href (href "architecture")
                      "Architecture"
                  }
              if page.StartsWith("architecture/solution/", StringComparison.Ordinal) then
                  fveBreadcrumbItem {
                      _href (href "architecture/solution")
                      "Solution"
                  }
              if page.StartsWith("accounts/", StringComparison.Ordinal) then
                  fveBreadcrumbItem { "Accounts" }
              if page.StartsWith("transactions/", StringComparison.Ordinal) then
                  fveBreadcrumbItem { "Transactions" }
              if page <> "" then
                  fveBreadcrumbItem {
                      _href current
                      title page
                  } ]

        let workflow = workflows |> List.tryFind (fun workflow -> workflow.key = page)

        let contents =
            if page = "" then
                [ "financial-workspace", "Financial workspace"
                  "start-here", "Start here"
                  "system", "System" ]
            elif page.StartsWith("architecture", StringComparison.Ordinal) then
                Architecture.contents page
            else
                []

        let description =
            workflow
            |> Option.map _.description
            |> Option.defaultValue "Financial workspace workflows and project contracts"

        Layout.documentationShell
            "Geldos specification"
            current
            (sidebar query)
            crumbs
            (Some(title page, description))
            (fragment {
                Layout.documentationSearch "Search specification"
                    [ "Specification", navigation |> List.map (fun (href, label) -> workspaceUrl href query.workspace, label) ]
                yield! query.topBarActions
            })
            contents
            workflow.IsSome
            content
