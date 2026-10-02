namespace Docs.Examples

open System.Text.Json
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open Model
open Ledger.Domain

/// Consumer-authored API reference: resource navigation, readable contracts and cURL alongside JSON.
module ApiDocumentation =
    let navigation =
        [ "/examples/api-documentation", "Overview"
          "/examples/api-documentation/list-accounts", "List accounts"
          "/examples/api-documentation/create-account", "Create account"
          "/examples/api-documentation/update-account", "Update account"
          "/examples/api-documentation/delete-account", "Delete account"
          "/examples/api-documentation/list-transactions", "List transactions"
          "/examples/api-documentation/get-transaction", "Get transaction" ]
    let title = function
        | "list-accounts" -> "List accounts" | "create-account" -> "Create account" | "update-account" -> "Update account" | "delete-account" -> "Delete account" | "list-transactions" -> "List transactions" | "get-transaction" -> "Get transaction" | _ -> "Ledger API reference"
    let private pretty (source:string) =
        use document = JsonDocument.Parse source
        JsonSerializer.Serialize(document.RootElement,JsonSerializerOptions(WriteIndented=true))
    let private parameters (rows:(string*string*string) list) =
        dl {
            _class "divide-y divide-[var(--fve-border)]"
            for name,kind,description in rows do
                div {
                    _class "py-5 first:pt-0"
                    dt { _class "flex flex-wrap items-baseline gap-2"; code { _class "font-mono text-sm font-semibold"; name }; span { _class "text-xs text-[var(--fve-muted-text)]"; kind } }
                    dd { _class "mt-2 text-base leading-7 text-[var(--fve-muted-text)]"; description }
                }
        }
    let private methodPath (method':string) (path:string) =
        div {
            _class "flex min-w-0 flex-wrap items-center gap-3"
            Badge.create method' |> Badge.withColor (if method'="DELETE" then BadgeColor.Error elif method'="GET" then BadgeColor.Success else BadgeColor.Info) |> Badge.withVariant BadgeVariant.Soft |> Badge.render
            code { _class "break-all font-mono text-sm"; path }
        }
    let private split (narrative:HtmlElement) (examples:HtmlElement) =
        div {
            _class "grid min-w-0 xl:grid-cols-2"
            article { _class "min-w-0 px-4 py-8 sm:px-6 lg:px-8 xl:py-10"; div { _class "mx-auto grid max-w-2xl gap-8"; narrative } }
            aside {
                _ariaLabel "Request and response examples"
                _style "--fve-docs-code-surface:var(--fve-background)"
                _class ((ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" dark min-w-0 border-t border-[var(--fve-border)] bg-[var(--fve-background)] px-4 py-8 text-[var(--fve-text)] sm:px-6 lg:px-8 xl:border-t-0 xl:border-l xl:py-10")
                div { _class "mx-auto grid max-w-2xl gap-8"; examples }
            }
        }
    let private operation page method' path (description:string) request requestFields (returns:string) (responses:(string*string*string) list) =
        let narrative = div {
            _class "grid gap-8"
            header { _class "grid gap-4"; h1 { _class "text-2xl font-semibold tracking-tight sm:text-3xl"; title page }; p { _class "text-base leading-7 text-[var(--fve-muted-text)]"; description }; methodPath method' path }
            Layout.section "Parameters" (if List.isEmpty requestFields then p { _class "text-base text-[var(--fve-muted-text)]"; "No parameters." } else parameters requestFields)
            Layout.section "Returns" (p { _class "text-base leading-7 text-[var(--fve-muted-text)]"; returns })
            Layout.section "Response codes" (dl {
                _class "grid gap-4"
                for status,description,_ in responses do
                    div {
                        _class "grid grid-cols-[3rem_1fr] gap-3"
                        dt { _class "font-mono text-sm font-semibold"; status }
                        dd { _class "text-base leading-6 text-[var(--fve-muted-text)]"; description }
                    }
            })
            p { _class "border-t border-[var(--fve-border)] pt-5 text-sm leading-6 text-[var(--fve-muted-text)]"; "Illustrative API contract. Set "; code { "$API_ORIGIN" }; " to your own backend; this example does not host these endpoints." }
        }
        let examples = div {
            _class "grid gap-8"
            section {
                _ariaLabel "cURL request"; _class "grid gap-4"
                div { _class "flex items-center justify-between gap-3"; h2 { _class "text-sm font-semibold"; "Request" }; span { _class "rounded-md bg-[var(--fve-surface-subtle)] px-2 py-1 font-mono text-xs"; "cURL" } }
                methodPath method' path
                CodeBlock.create "shell" request |> CodeBlock.render
            }
            section {
                _ariaLabel "JSON response"; _class "grid gap-4"
                h2 { _class "text-sm font-semibold"; "Response" }
                Tabs.create "template-api-responses" "Response status" (responses |> List.map (fun (status,description,body) ->
                    TabItem.create status status (div { _class "grid gap-4"; p { _class "text-sm leading-6 text-[var(--fve-muted-text)]"; description }; if body<>"" then CodeBlock.create "json" (pretty body) |> CodeBlock.render else p { _class "rounded-xl border border-[var(--fve-border)] p-4 font-mono text-sm"; "No response body." } }))) |> Tabs.render
            }
        }
        split narrative examples
    let render page =
        let account = accounts.Head
        let transaction = transactions.Head
        let nameFields = ["name","string · required","Unique without regard to case. Must contain 1–80 characters."; "accountType","enum · required","One of Asset, Liability, Equity, Revenue or Expense. Reporting currency is USD."]
        let validation = "{\"error\":\"validation_failed\",\"fields\":{\"name\":\"Enter a unique account name of 1–80 characters.\"}}"
        let missing = "{\"error\":\"not_found\",\"message\":\"The resource does not exist.\"}"
        let collection payload records = "["+(records |> List.map payload |> String.concat ",")+"]"
        let content =
            match page with
            | "list-accounts" ->
                operation page "GET" "/api/accounts" "Returns the account collection. Filter by account type to inspect one part of the ledger's financial classification." "curl \"$API_ORIGIN/api/accounts?accountType=Asset\"" ["accountType","enum · query · optional","Restricts results to Asset, Liability, Equity, Revenue or Expense. Omit to return all accounts."] "An array of account objects. Each includes its identity, type, reporting currency and balance derived from opening balance plus transactions." ["200","Account collection",collection accountPayload (accounts |> List.filter (fun row -> row.accountType=AccountType.Asset))]
            | "create-account" ->
                let created = { id=106; name="Reserve fund"; accountType=AccountType.Asset; currency="USD"; openingBalance=0M }
                operation page "POST" "/api/accounts" "Creates a financial account with a unique name and an available account type. The new account starts with a zero balance in USD." "curl -X POST \"$API_ORIGIN/api/accounts\" \\\n  -H 'Content-Type: application/json' \\\n  -d '{\"name\":\"Reserve fund\",\"accountType\":\"Asset\"}'" nameFields "The created account object. The Location response header identifies /api/accounts/106. Invalid fields and duplicate names do not create a record." ["201","Created account · Location: /api/accounts/106",accountPayload created; "400","Invalid account fields",validation; "409","Duplicate account name","{\"error\":\"account_name_exists\"}"]
            | "update-account" ->
                operation page "PATCH" "/api/accounts/101" "Updates the account's name and type. Both fields are required by this illustrative contract; the account ID and existing balance remain unchanged." "curl -X PATCH \"$API_ORIGIN/api/accounts/101\" \\\n  -H 'Content-Type: application/json' \\\n  -d '{\"name\":\"Main operating account\",\"accountType\":\"Asset\"}'" (("id","integer · path · required","The identifier of an existing account.")::nameFields) "The updated account object, or a field-validation, missing-resource or duplicate-name error." ["200","Updated account",accountPayload { account with name="Main operating account" }; "400","Invalid fields",validation; "404","Unknown account",missing; "409","Duplicate name","{\"error\":\"account_name_exists\"}"]
            | "delete-account" ->
                operation page "DELETE" "/api/accounts/105" "Deletes an unused account. An account must have a zero balance and no transactions before it can be deleted." "curl -X DELETE \"$API_ORIGIN/api/accounts/105\"" ["id","integer · path · required","The identifier of an existing account. Unassigned expense (105) is the eligible seeded example."] "No response body on success. An unknown ID returns 404; an account with financial dependencies returns 409." ["204","Account deleted. No response body.",""; "404","Unknown account",missing; "409","Account has transactions or a non-zero balance","{\"error\":\"account_has_dependencies\"}"]
            | "list-transactions" ->
                operation page "GET" "/api/transactions" "Returns dated financial activity. Optionally narrow the collection to one account to reconcile its balance and inspect verification state." "curl \"$API_ORIGIN/api/transactions?accountId=101\"" ["accountId","integer · query · optional","Restricts results to transactions for an existing account. Omit to return all transactions."] "An array of transaction objects with a date, signed USD amount, account ID and verification status." ["200","Transactions for the requested account",collection transactionPayload (transactions |> List.filter (fun row -> row.accountId=101))]
            | "get-transaction" ->
                operation page "GET" "/api/transactions/201" "Retrieves one transaction by its stable identifier, including its associated account and verification state." "curl \"$API_ORIGIN/api/transactions/201\"" ["id","integer · path · required","The identifier of an existing transaction."] "The matching transaction object, or a not-found error. Dates use YYYY-MM-DD and amounts are signed decimal USD values." ["200","Transaction details",transactionPayload transaction; "404","Unknown transaction",missing]
            | _ ->
                split (div {
                    _class "grid gap-8"
                    header { _class "grid gap-4"; h1 { _class "text-2xl font-semibold tracking-tight sm:text-3xl"; title page }; p { _class "text-base leading-7 text-[var(--fve-muted-text)]"; "Accounts and transactions over JSON. Explore resource definitions, request parameters and response states alongside copyable cURL examples." } }
                    Layout.section "Getting started" (div { _class "grid gap-4 text-base leading-7 text-[var(--fve-muted-text)]"; p { "These pages describe an API you can implement using the same financial model as the Application and Specification." }; p { "Set "; code { "$API_ORIGIN" }; " to your backend's origin before using the requests. This catalog does not expose a financial API, issue credentials or run requests." } })
                    Layout.section "Resources" (div {
                        _class "grid gap-5"
                        for resource,description,url in ["Accounts","Financial classification, reporting currency and derived balances.","/examples/api-documentation/list-accounts";"Transactions","Dated signed activity, linked accounts and verification state.","/examples/api-documentation/list-transactions"] do
                            div {
                                Layout.link url resource
                                p { _class "px-3 text-base leading-7 text-[var(--fve-muted-text)]"; description }
                            }
                    })
                    Layout.section "Conventions" (parameters ["Content-Type","application/json","Requests and responses use JSON. Resource identifiers are integers.";"Dates and amounts","date-only · decimal","Transaction dates use YYYY-MM-DD. All signed amounts and account balances are USD.";"Errors","JSON object","Field validation uses 400; unknown resources use 404; duplicate names and deletion dependencies use 409."])
                    p { _class "text-sm leading-6 text-[var(--fve-muted-text)]"; "Application demo forms validate without persistence. API request examples describe an implementation contract, not operations performed by the preview." }
                }) (div {
                    _class "grid gap-8"
                    Layout.section "Your first request · cURL" (div { _class "grid gap-4"; methodPath "GET" "/api/accounts"; CodeBlock.create "shell" "curl \"$API_ORIGIN/api/accounts\"" |> CodeBlock.render })
                    Layout.section "Example response" (CodeBlock.create "json" (pretty (collection accountPayload accounts)) |> CodeBlock.render)
                    Layout.section "Account object" (CodeBlock.create "json" (accountPayload account) |> CodeBlock.render)
                })
        let current = if page="" then "/examples/api-documentation" else "/examples/api-documentation/"+page
        let groups = ["",[navigation.Head];"Accounts",navigation |> List.skip 1 |> List.take 4;"Transactions",navigation |> List.skip 5]
        Layout.shell "Ledger API" current groups (title page) "Accounts and transactions · JSON contract" (Layout.link "/examples/specification" "Read specification") content
