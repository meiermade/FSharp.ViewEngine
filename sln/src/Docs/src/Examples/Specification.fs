namespace Docs.Examples

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open Model
open Ledger.Domain

module Specification =
    type private State = { key:string; label:string; page:ApplicationPage; parameters:(string*string) list }
    type private Workflow = { key:string; label:string; description:string; states:State list; sequence:string; rules:string list }
    let private state key label page parameters = {key=key;label=label;page=page;parameters=parameters}
    let private readSequence endpoint lookup response =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Domain as Ledger.Domain\n  Operator->>Browser: Open page or apply controls\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: {lookup}\n  Domain-->>Server: Immutable financial fixtures\n  Server-->>Browser: 200 HTML — {response}"
    let private validateSequence getEndpoint postEndpoint (operation:string) =
        let invalidResponse =
            if ["validateAccount";"validateProfile";"validateOrganization"] |> List.exists (fun name -> operation.StartsWith(name,StringComparison.Ordinal)) then
                "200 HTML — field errors and entered values (private, no-store)"
            else
                $"302 Location {getEndpoint}?state=invalid\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — validation outcome"
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Application as Ledger.Application\n  Operator->>Browser: Open form\n  Browser->>Server: GET {getEndpoint}\n  Server-->>Browser: 200 HTML — labelled form\n  Operator->>Browser: Submit values\n  Browser->>Server: POST {postEndpoint} (URL-encoded)\n  Server->>Application: {operation}\n  alt Invalid values\n    Application-->>Server: Typed validation error\n    Server-->>Browser: {invalidResponse}\n  else Valid values\n    Application-->>Server: Validated response\n    Server-->>Browser: 302 Location {getEndpoint}?state=validated\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — finite outcome (no persistence)\n  end"
    let private selectionSequence endpoint response resource field =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Application as Ledger.Application\n  participant Domain as Ledger.Domain\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: Read immutable collection facts\n  Domain-->>Server: Financial fixtures\n  Server->>Server: Apply URL search, filters and sort\n  Server-->>Browser: 200 HTML — {response}\n  Operator->>Browser: Change filters or submit Search on Enter\n  Browser->>Server: GET {endpoint} with collection controls\n  Server-->>Browser: 200 HTML — updated rows and applied filter groups\n  Operator->>Browser: Select keyed rows\n  Browser->>Browser: Show count and bulk commands in the toolbar\n  Operator->>Browser: Review selected rows\n  Browser->>Server: POST {endpoint} (action=review-selected, {field})\n  Server->>Application: reviewSelection ({resource}, keys)\n  alt Empty or unknown keys\n    Application-->>Server: Typed selection error\n    Server-->>Browser: 302 Location {endpoint}?state=selection-invalid\n  else Available keys\n    Application-->>Server: Reviewed selection count\n    Server-->>Browser: 302 Location {endpoint}?state=selection-valid\n  end"
    let private editorStates page = [state "default" "Editor" page [];state "invalid" "Name validation" page ["state","invalid"];state "invalid-type" "Type validation" page ["state","invalid-type"];state "invalid-details" "Details validation" page ["state","invalid-details"];state "validated" "Validated" page ["state","validated"]]
    let private accountRules = ["Names SHALL contain 1–80 characters and be unique ignoring case, excluding the account being updated.";"Account type SHALL be one of Asset, Liability, Equity, Revenue or Expense; parent SHALL match that type root.";"Commodity SHALL be USD, subtype SHALL be Generic and month-end observation SHALL be not required.";"Native required validation SHALL precede submission; server validation SHALL reject invalid requests independently.";"A successful submission SHALL validate only: seeded accounts and submitted private values SHALL NOT be stored.";"Create and Edit SHALL use a single-column contextual Drawer with a scrolling body and fixed header/footer. Save and Update SHALL submit; Cancel, close, Escape and backdrop SHALL ask before discarding changed values.";"A failed submission SHALL return the entered values in that response only, with field errors. Submitted values SHALL NOT enter URLs, cookies or durable storage.";"Opening an overlay SHALL preserve its originating collection/detail title, breadcrumbs, workspace, search, filters and sorting."]
    let private shellRules = [
        "Application, Specification and API documentation SHALL share the documentation's sky brand theme; success, warning and error colors SHALL remain semantic."
        "PageTopBar SHALL contain brand and breadcrumbs; a visible PageHeader SHALL contain the page title and page actions." ]
    let private collectionRules = [
        "Search and Add filter SHALL precede the table. Search SHALL submit a native GET on Enter; applied filters SHALL appear beneath as joined muted-label, Select-value and icon-remove ButtonGroups. Select SHALL own value-selection semantics and keyboard behavior; consumer-owned GET forms SHALL apply and remove filters, with a native Select and Apply fallback when JavaScript is unavailable."
        "Each structured filter SHALL appear at most once and filters SHALL compose using AND. Clear all SHALL appear only while structured filters are active and SHALL preserve Search."
        "Sorting SHALL use column-heading destinations and SHALL retain active search, filters and workspace."
        "Selecting rows SHALL replace Search and Add filter with X selected, Review selected and Clear selection in the same toolbar; applied filters SHALL remain visible. At zero selection, count and bulk commands SHALL be hidden."
        "Table SHALL own native keyed selection and a screen-reader announcement; the consumer SHALL own the visible count, commands and review feedback. Native fallback SHALL offer toolbar review for checked rows."
        "The toolbar SHALL remain above the row-scrolling viewport and column headings SHALL remain sticky. Counts, commands, review feedback and pagination controls SHALL NOT appear below the table."
        "This finite example SHALL NOT imply an infinite-scroll endpoint or all-matching selection. The composition SHALL allow a consumer to supply additional stable-keyed rows without a table footer."
        "Filter changes SHALL resolve a new server-rendered collection with selection cleared. Review outcomes SHALL retain public collection controls and workspace, without retaining submitted selected keys." ]
    let private workflows =
        [ {key="home";label="View home";description="Inspect one account's balance and related activity for an explicit period and illustrative scenario."
           states=[state "default" "Default" ApplicationPage.Home [];state "account" "Tax reserve" ApplicationPage.Home ["account","102"];state "period" "Last 30 days" ApplicationPage.Home ["account","101";"range","30"];state "scenario" "Pessimistic scenario" ApplicationPage.Home ["account","101";"comparison","pessimistic"]]
           sequence=readSequence "/examples/application?account=101&range=30&comparison=expected" "Resolve workspace, account, balance and dated activity" "chart and related transactions"
           rules=["Account, period, scenario and workspace SHALL be URL-backed.";"The chart and transaction list SHALL use the same selected account and finite period.";"Solid balances SHALL derive from opening balance and signed transactions. Dashed scenarios SHALL be labelled illustrative."]}
          {key="accounts/view-accounts";label="View accounts";description="Browse the type hierarchy, filter and sort accounts, and review a keyed selection."
           states=[state "default" "Hierarchy" ApplicationPage.Accounts [];state "filter-added" "Type filter added" ApplicationPage.Accounts ["filters","accountType"];state "assets" "Assets" ApplicationPage.Accounts ["accountType","Asset";"filters","accountType"];state "sorted" "Balance descending" ApplicationPage.Accounts ["sort","balance"];state "selected" "Rows selected" ApplicationPage.Accounts ["state","selected"];state "empty" "No matches" ApplicationPage.Accounts ["search","No matching account"];state "selection" "Selection reviewed" ApplicationPage.Accounts ["state","selection-valid"];state "selection-invalid" "No selection" ApplicationPage.Accounts ["state","selection-invalid"]]
           sequence=selectionSequence "/examples/application/accounts" "type roots and matching accounts" "Accounts" "accountIds"
           rules=collectionRules @ ["Type roots SHALL begin at hierarchy depth zero; accounts SHALL sit one level below their root.";"Search, type filter and sort SHALL be server-rendered URL controls.";"Record links SHALL identify the exact account and open View account.";"Table SHALL own native keyed checkboxes; the page SHALL own selection review and feedback.";"Review SHALL accept available account IDs and type-group keys, rejecting an empty or unknown selection without changing financial data."]}
          {key="accounts/view-account";label="View account";description="Inspect an account's balance, properties and related transactions, then choose an account operation."
           states=[state "default" "Active account" (ApplicationPage.Account 101) [];state "no-activity" "No activity" (ApplicationPage.Account 105) []]
           sequence=readSequence "/examples/application/accounts/101" "Find account 101, its balance and its transactions" "account detail with update and delete links"
           rules=["The resource ID SHALL select the matching seeded account.";"Balance SHALL include only the account's opening balance and signed activity.";"Related transaction links SHALL open their exact records.";"Update and Delete destinations SHALL retain the viewed account's ID and workspace."]}
          {key="accounts/create-account";label="Create account";description="Enter a new account's name, type and details and validate them on the server."
           states=editorStates ApplicationPage.CreateAccount
           sequence=validateSequence "/examples/application/accounts/new" "/examples/application/accounts/new" "validateAccount (existingAccountId=None)"
           rules=accountRules @ ["Cancel or Escape SHALL return to Accounts without submitting, after confirming any unsaved changes."]}
          {key="accounts/update-account";label="Update account";description="Edit an existing account's authored values and review finite validation outcomes."
           states=editorStates (ApplicationPage.EditAccount 101)
           sequence=validateSequence "/examples/application/accounts/101/edit" "/examples/application/accounts/101/edit" "validateAccount (existingAccountId=Some 101)"
           rules=accountRules @ ["The editor SHALL begin with the exact account's seeded values.";"Cancel or Escape SHALL return to the originating collection or account detail without submitting, after confirming any unsaved changes."]}
          {key="accounts/delete-account";label="Delete account";description="Confirm deletion eligibility, distinguishing an unused account from one with financial activity."
           states=[state "default" "Confirmation" (ApplicationPage.DeleteAccount 105) [];state "blocked" "Has activity" (ApplicationPage.DeleteAccount 101) ["state","delete-blocked"];state "validated" "Deletion validated" (ApplicationPage.DeleteAccount 105) ["state","deleted"]]
           sequence="sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Application as Ledger.Application\n  participant Domain as Ledger.Domain\n  Browser->>Server: GET /examples/application/accounts/105/delete\n  Server-->>Browser: 200 HTML — confirmation over account detail\n  Operator->>Browser: Confirm validation\n  Browser->>Server: POST /examples/application/accounts/105/delete (action=delete)\n  Server->>Application: validateDeletion (accountId=105)\n  Application->>Domain: Check balance and transaction history\n  alt Account has balance or transactions\n    Application-->>Server: HasActivity\n    Server-->>Browser: 302 Location /examples/application/accounts/105/delete?state=delete-blocked\n  else Zero balance and no transactions\n    Application-->>Server: Validated account ID\n    Server-->>Browser: 302 Location /examples/application/accounts/105/delete?state=deleted\n  end"
           rules=["Deletion SHALL require confirmation, an existing account, zero balance and no transactions.";"An account with activity SHALL display the blocking reason and SHALL NOT offer a confirmation submission.";"Account 105 SHALL be the eligible fixture; account 101 SHALL demonstrate the blocked case.";"Successful validation SHALL NOT remove seeded records.";"Delete SHALL be last and separated in collection overflow menus; detail overflow SHALL expose the same destination.";"Confirmation SHALL focus Cancel and keep the originating page title and breadcrumbs unchanged.";"Cancel or Escape SHALL return to the originating collection or account detail."]}
          {key="transactions/view-transactions";label="View transactions";description="Find dated activity by account, verification status and search, then review a selection."
           states=[state "default" "All transactions" ApplicationPage.Transactions ["sort","date-desc"];state "filter-added" "Verification filter added" ApplicationPage.Transactions ["filters","status"];state "review" "Needs review" ApplicationPage.Transactions ["status","unverified";"filters","status"];state "account" "Operating checking" ApplicationPage.Transactions ["account","101";"filters","account"];state "combined" "Account and verification" ApplicationPage.Transactions ["account","101";"status","verified";"filters","account,status"];state "sorted" "Date ascending" ApplicationPage.Transactions ["sort","date-asc"];state "selected" "Rows selected" ApplicationPage.Transactions ["state","selected"];state "empty" "No matches" ApplicationPage.Transactions ["search","No matching transaction"];state "selection-invalid" "No selection" ApplicationPage.Transactions ["state","selection-invalid"];state "selection-valid" "Selection reviewed" ApplicationPage.Transactions ["state","selection-valid"]]
           sequence=selectionSequence "/examples/application/transactions" "matching transactions or empty state" "Transactions" "transactionIds"
           rules=collectionRules @ ["Amounts SHALL be signed decimal USD values; dates SHALL be date-only values.";"Verification SHALL be independent of amount.";"Empty results SHALL preserve filter controls.";"Selection review SHALL validate available transaction IDs without changing verification or moving money."]}
          {key="transactions/view-transaction";label="View transaction";description="Inspect a specific transaction's date, signed amount, verification and related account."
           states=[state "default" "Verified" (ApplicationPage.Transaction 201) [];state "review" "Needs review" (ApplicationPage.Transaction 203) []]
           sequence=readSequence "/examples/application/transactions/201" "Find transaction 201 and its related account" "transaction detail"
           rules=["The resource ID SHALL select the matching transaction.";"Account links SHALL open that transaction's actual account.";"Date, amount and verification SHALL match the shared Domain fixture."]}
          {key="transactions/delete-transaction";label="Delete transaction";description="Confirm the exact transaction and validate deletion without changing fixture data."
           states=[state "default" "Confirmation" (ApplicationPage.DeleteTransaction 203) [];state "verified" "Verified transaction" (ApplicationPage.DeleteTransaction 201) [];state "validated" "Deletion validated" (ApplicationPage.DeleteTransaction 203) ["state","deleted"]]
           sequence="sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Application as Ledger.Application\n  Browser->>Server: GET /examples/application/transactions/203/delete\n  Server-->>Browser: 200 HTML — confirmation over originating page\n  Operator->>Browser: Delete\n  Browser->>Server: POST /examples/application/transactions/203/delete (action=delete)\n  Server->>Application: validateTransactionDeletion (transactionId=203)\n  Application-->>Server: Validated transaction ID\n  Server-->>Browser: 302 Location with state=deleted\n  Browser->>Server: GET Location\n  Server-->>Browser: 200 HTML — deletion validated (fixtures unchanged)"
           rules=["Transaction deletion SHALL identify an existing transaction and require a centered confirmation with Cancel initially focused and a destructive Delete button.";"Verification status SHALL NOT restrict deletion in this finite example.";"Deletion SHALL be last and separated in collection overflow menus; detail overflow SHALL expose the same destination.";"The originating page title, breadcrumbs, workspace and collection controls SHALL remain unchanged while confirmation is open.";"Cancel, Escape or backdrop SHALL return to the originating collection or detail.";"Successful validation SHALL NOT remove seeded transactions."]}
          {key="workspace";label="Select workspace";description="Choose an organization, environment and ledger from the full sidebar context row."
           states=[state "default" "Current workspace" ApplicationPage.Home [];state "sandbox" "Staging" ApplicationPage.Home ["environment","staging"];state "ledger" "Jordan ledger" ApplicationPage.Home ["organization","example-household";"ledger","jordan"];state "organization" "Client organization" ApplicationPage.Home ["organization","client-organization";"ledger","operating-company"]]
           sequence="sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Ledger.Server\n  participant Domain as Ledger.Domain\n  Operator->>Browser: Choose ledger, organization or environment\n  Browser->>Server: GET /examples/application?organization=example-household&environment=staging&ledger=jordan\n  Server->>Domain: Resolve available workspace context\n  Domain-->>Server: Organization, environment and matching ledger\n  Server-->>Browser: 200 HTML — current workspace and sandbox notice"
           rules=["The trigger SHALL fill the sidebar context row and the current ledger SHALL be marked.";"A ledger SHALL belong to the selected organization; unknown combinations SHALL resolve to available fixtures.";"Page links, filters, forms and redirects SHALL retain workspace context.";"Sandbox environments SHALL be clearly labelled; closing a selector dialog SHALL restore trigger focus."]}
          {key="settings";label="View settings";description="Review the selected organization's details, environments, users, API clients, ledgers and billing."
           states=[
               for key,label in settingsSections do yield state key label (ApplicationPage.SettingsSection key) []
               yield state "edit" "Edit organization" ApplicationPage.EditOrganization []
               yield state "invalid" "Name validation" ApplicationPage.EditOrganization ["state","invalid"]
               yield state "validated" "Validated" ApplicationPage.EditOrganization ["state","validated"] ]
           sequence=validateSequence "/examples/application/settings/general/edit" "/examples/application/settings/general/edit" "validateOrganization (name, currency)"
           rules=["Settings SHALL belong to the selected organization; its selector SHALL retain the current settings section and change only the organization context.";"General SHALL show readable details; Edit organization SHALL open a Drawer with Update and Cancel.";"Organization memberships SHALL belong to Profile → Organizations, not organization Settings.";"Return to Ledger SHALL preserve the prior workspace, including after changing organizations in Settings.";"Organization name SHALL contain 1–80 characters; reporting commodity SHALL be USD.";"A submission SHALL validate without saving settings; invalid values SHALL remain only in the private, no-store response with field errors.";"Application UI SHALL use ordinary product copy without template commentary or invented live services."]}
          {key="profile";label="View profile";description="Review identity and workspace access, validate profile fields and choose local appearance."
           states=[state "default" "Profile" ApplicationPage.Profile [];state "organizations" "Organizations" ApplicationPage.ProfileOrganizations [];state "edit" "Edit profile" ApplicationPage.EditProfile [];state "invalid" "Validation" ApplicationPage.EditProfile ["state","invalid"];state "validated" "Validated" ApplicationPage.EditProfile ["state","validated"]]
           sequence=validateSequence "/examples/application/profile/edit" "/examples/application/profile/edit" "validateProfile (name, email, timeZone)" + "\n  Operator->>Browser: Choose System, Light or Dark\n  Browser->>Browser: Apply and store local appearance preference"
           rules=["The complete Ledger footer row SHALL open a personal Profile shell containing only Profile and Organizations, with a return to the prior Ledger workspace.";"Profile SHALL show readable personal details; Edit profile SHALL open a Drawer with Update and Cancel.";"Appearance SHALL stay directly on Profile as compact System, Light and Dark radio choices styled like tabs, applying immediately and independently of the edit Drawer.";"Organization memberships and roles SHALL appear on Organizations, with real destinations for each organization's Settings.";"Name SHALL contain 1–80 characters; email SHALL be valid and time zone SHALL be available.";"Only appearance SHALL persist locally; profile submissions SHALL NOT retain private values outside a private, no-store field-error response.";"Dirty editor dismissal and navigation SHALL ask before discarding changes."]} ]
        |> List.map (fun workflow -> {workflow with rules=shellRules @ workflow.rules})

    let workflowPages = workflows |> List.map _.key
    let pages = workflowPages @ (Architecture.navigation |> List.map fst)
    let navigation = ["/examples/specification","Overview";yield! workflows |> List.map (fun workflow -> "/examples/specification/"+workflow.key,workflow.label);yield! Architecture.navigation |> List.map (fun (key,label) -> Architecture.href key,label)]
    let title page =
        workflows |> List.tryFind (fun workflow -> workflow.key=page) |> Option.map _.label
        |> Option.defaultWith (fun () -> Architecture.navigation |> List.tryFind (fst >> (=) page) |> Option.map snd |> Option.defaultValue "Financial specification")
    let private specUrl page (query:Query) state appMode = specificationHref page state {query with appMode=appMode}
    let private resolve page (query:Query) =
        let workflow = workflows |> List.find (fun workflow -> workflow.key=(if page="" then "home" else page))
        let selected = workflow.states |> List.tryFind (fun state -> state.key=query.specState) |> Option.defaultValue workflow.states.Head
        workflow,selected
    let private resourcePage (query:Query) (state:State) =
        match Int32.TryParse query.resource,state.page with
        | (true,id),ApplicationPage.Account _ when tryAccount id |> Option.isSome -> ApplicationPage.Account id
        | (true,id),ApplicationPage.EditAccount _ when tryAccount id |> Option.isSome -> ApplicationPage.EditAccount id
        | (true,id),ApplicationPage.DeleteAccount _ when tryAccount id |> Option.isSome -> ApplicationPage.DeleteAccount id
        | (true,id),ApplicationPage.Transaction _ when tryTransaction id |> Option.isSome -> ApplicationPage.Transaction id
        | (true,id),ApplicationPage.DeleteTransaction _ when tryTransaction id |> Option.isSome -> ApplicationPage.DeleteTransaction id
        | _ -> state.page
    let productPage page query = let _,selected = resolve page query in resourcePage query selected
    let private productQuery (query:Query) (state:State) =
        let parameter key fallback = state.parameters |> List.tryPick (fun (name,value) -> if name=key then Some value else None) |> Option.defaultValue fallback
        let authored = queryFromValues (fun key -> parameter key "")
        let choose value defaultValue fixtureValue = if value=defaultValue then fixtureValue else value
        { query with specification=true; embedded=false; specState=state.key
                     workspace=workspaceFromStrings (parameter "organization" query.workspace.organization.id) (parameter "environment" query.workspace.environment.id) (parameter "ledger" query.workspace.ledger.id)
                     search=choose query.search "" authored.search; accountType=choose query.accountType "all" authored.accountType
                     filters=(if query.filters.IsEmpty then authored.filters else query.filters)
                     sort=choose query.sort "name" authored.sort; state=choose query.state "" authored.state
                     account=choose query.account "all" authored.account; range=choose query.range "90" authored.range
                     comparison=choose query.comparison "expected" authored.comparison; status=choose query.status "all" authored.status }
    let private currentUrl (workflow:Workflow) (state:State) (query:Query) appMode =
        specUrl workflow.key query state.key appMode + querySuffix [
            if query.search<>"" then yield "search",query.search
            if query.accountType<>"all" then yield "accountType",query.accountType
            if query.sort<>"name" then yield "sort",query.sort
            if query.state<>"" then yield "state",query.state
            if query.account<>"all" then yield "account",query.account
            if query.range<>"90" then yield "range",query.range
            if query.comparison<>"expected" then yield "comparison",query.comparison
            if query.status<>"all" then yield "status",query.status
            if not query.filters.IsEmpty then yield "filters",String.concat "," query.filters ]
    let private diagram label source =
        div { _role "region"; _ariaLabel label; _tabindex 0; _class "min-w-0 overflow-x-auto focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; Mermaid.create source |> Mermaid.render }
    let private fixtureId (workflow:Workflow) (state:State) = "spec-"+workflow.key.Replace('/','-')+"-"+state.key
    let private appMode (query:Query) (workflow:Workflow) (selected:State) =
        let index = workflows |> List.findIndex (fun candidate -> candidate.key=workflow.key)
        let destination index = let workflow = workflows[index] in workflow.label,specUrl workflow.key {query with resource=""} workflow.states.Head.key true
        let previous = if index=0 then None else Some(destination (index-1))
        let next = if index=workflows.Length-1 then None else Some(destination (index+1))
        let states = workflow.states |> List.map (fun state -> state.label,specUrl workflow.key (if state.key=selected.key then query else {query with resource=""}) state.key true,state.key=selected.key)
        AppMode.render query (workflow.label+" · "+selected.label) states previous next (currentUrl workflow selected query false) (fun controls -> Application.renderWithReviewControls controls (resourcePage query selected) (productQuery query selected))
    let private wireframe (query:Query) (workflow:Workflow) (selected:State) =
        let items = workflow.states |> List.map (fun state ->
            let requested = if state.key=selected.key then query else {defaultQuery with workspace=query.workspace;dock=query.dock}
            let product = Application.render (resourcePage requested state) {productQuery requested state with previewId=fixtureId workflow state;topBarActions=[]}
            let href = currentUrl workflow state requested true
            let addressQuery = {productQuery requested state with specification=false}
            let address = collectionHref addressQuery (resourcePage requested state) + querySuffix ["state",addressQuery.state;"range",addressQuery.range;"comparison",addressQuery.comparison]
            let preview = Browser.create (div { _class "h-[40rem]"; product }) |> Browser.withAddress address |> Browser.render |> AppMode.fixture (workflow.label+" · "+state.label) href
            TabItem.create (fixtureId workflow state) state.label preview)
        div {
            _class "min-w-0 max-w-full"
            Tabs.create ("spec-"+workflow.key.Replace('/','-')+"-states") (workflow.label+" states") items |> Tabs.withVariant TabsVariant.Underlined |> Tabs.withSelected (fixtureId workflow selected) |> Tabs.render
            noscript { nav { _ariaLabel "Open workflow states"; _class "flex flex-wrap gap-3"; for state in workflow.states do Layout.link (specUrl workflow.key {query with resource=""} state.key true) state.label } }
        }
    let private sidebar query =
        let item key label = SideNavItem.create (workspaceUrl (if key="" then "/examples/specification" else "/examples/specification/"+key) query.workspace) label
        let group label children = SideNavGroup.create label (children |> List.map (fun (key,label) -> item key label))
        [
            item "" "Overview"
            item "home" "View home"
            group "Accounts" [for workflow in workflows do if workflow.key.StartsWith("accounts/",StringComparison.Ordinal) then yield workflow.key,workflow.label]
            group "Transactions" [for workflow in workflows do if workflow.key.StartsWith("transactions/",StringComparison.Ordinal) then yield workflow.key,workflow.label]
            item "workspace" "Select workspace"
            item "settings" "View settings"
            item "profile" "View profile"
            SideNavGroup.create "Architecture" [
                item "architecture" "System context"
                group "Solution" [
                    "architecture/solution","Overview"
                    "architecture/solution/domain","Ledger.Domain"
                    "architecture/solution/application","Ledger.Application"
                    "architecture/solution/components","Acme.Components"
                    "architecture/solution/server","Ledger.Server" ] ] ]
    let render page (query:Query) =
        if List.contains page workflowPages && query.appMode then
            let workflow,selected = resolve page query
            appMode query workflow selected
        else
            let content =
                if page.StartsWith("architecture",StringComparison.Ordinal) then Architecture.render page
                elif page="" then
                    div {
                        _class "grid gap-12"
                        Layout.documentationSection "financial-workspace" "Financial workspace" (Layout.prose (div {
                            p { "Ledger is a financial application for reviewing account balances and transactions within an organization, environment and ledger." }
                            p { "Choose a named workflow to review its Wireframe, Sequence and Rules. State tabs show variants of that one workflow; expand a preview to follow its connected product journey at full viewport size." } }))
                        Layout.documentationSection "start-here" "Start here" (div {
                            _class "grid gap-4 sm:grid-cols-2"
                            for workflow in workflows do
                                div {
                                    Layout.link (specUrl workflow.key query "" false) workflow.label
                                    p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; workflow.description }
                                }
                        })
                        Layout.documentationSection "system" "System" (Layout.prose (p { "Application, Specification and API reference share immutable USD fixtures. Native forms validate without saving financial or private submissions; appearance is the only locally persisted preference." }))
                        Layout.link (Architecture.href "architecture") "Explore the system context"
                    }
                else
                    let workflow,selected = resolve page query
                    div {
                        _class "grid min-w-0 grid-cols-1 gap-12"
                        Layout.documentationSection "wireframe" "Wireframe" (wireframe query workflow selected)
                        Layout.documentationSection "sequence" "Sequence" (diagram (workflow.label+" sequence") workflow.sequence)
                        Layout.documentationSection "rules" "Rules" (Layout.prose (ul { for rule in workflow.rules do li { rule } }))
                    }
            let current = workspaceUrl (if page="" then "/examples/specification" else "/examples/specification/"+page) query.workspace
            let href key = workspaceUrl (if key="" then "/examples/specification" else "/examples/specification/"+key) query.workspace
            let crumbs = [
                BreadcrumbItem.create (href "") "Overview"
                if page.StartsWith("architecture/",StringComparison.Ordinal) then BreadcrumbItem.create (href "architecture") "Architecture"
                if page.StartsWith("architecture/solution/",StringComparison.Ordinal) then BreadcrumbItem.create (href "architecture/solution") "Solution"
                if page.StartsWith("accounts/",StringComparison.Ordinal) then BreadcrumbItem.unlinked "Accounts"
                if page.StartsWith("transactions/",StringComparison.Ordinal) then BreadcrumbItem.unlinked "Transactions"
                if page<>"" then BreadcrumbItem.create current (title page) ]
            let workflow = workflows |> List.tryFind (fun workflow -> workflow.key=page)
            let contents = if page="" then ["financial-workspace","Financial workspace";"start-here","Start here";"system","System"] elif page.StartsWith("architecture",StringComparison.Ordinal) then Architecture.contents page else []
            let description = workflow |> Option.map _.description |> Option.defaultValue "Financial workspace workflows and project contracts"
            Layout.documentationShell "Ledger specification" current (sidebar query) crumbs (Some (title page,description)) (fragment { Layout.secondaryLink (applicationHref {query with specification=false} ApplicationPage.Home) "Open application"; yield! query.topBarActions }) contents workflow.IsSome content
