module internal Example.Specification.Web.Fixtures

open System
open Example.UI.Views.ViewData
open Example.UI.Views.Model

let organizations: Organization list = [{id="example-household";name="Example Household";role="Admin"};{id="client-organization";name="Client Organization";role="Member"}]
let environments: Environment list = [{id="production";name="Production";sandbox=false};{id="staging";name="Staging";sandbox=true};{id="review-testing";name="Review testing";sandbox=true}]
let ledgers: Ledger list =
    [ for id,name in ["example-household","Example Household";"alex","Alex";"jordan","Jordan";"casey","Casey";"riley","Riley";"example-business","Example Business, LLC"] do
          yield {id=id;name=name;organizationId="example-household"}
      yield {id="operating-company";name="Operating Company";organizationId="client-organization"} ]
let defaultWorkspace = {organization=organizations.Head;environment=environments.Head;ledger=ledgers[1]}

let catalog =
    { organizations = organizations; environments = environments; ledgers = ledgers; defaultWorkspace = defaultWorkspace
      accountTypes = [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense"; "Placeholder" ] }

// Authored review data: opening balances and dated transfers drive the chart fixtures.
let private account id name parentId kind opening =
    ({ id = id; name = name; parentId = parentId; accountType = kind; currency = "USD"
       balance = 0M; totalBalance = 0M; canDelete = id = 105 }, opening)

let private accountRows =
    [ account 101 "Operating checking" (Some 1) "Asset" 12500M
      account 102 "Tax reserve" (Some 1) "Asset" 8400M
      account 103 "Business credit card" (Some 2) "Liability" -650M
      account 104 "Savings" (Some 1) "Asset" 5000M
      account 105 "Unassigned expense" (Some 5) "Expense" 0M
      account 1 "Assets" None "Asset" 0M
      account 2 "Liabilities" None "Liability" 0M
      account 3 "Equity" None "Equity" -25250M
      account 4 "Revenue" None "Revenue" 0M
      account 5 "Expenses" None "Expense" 0M
      account 6 "Placeholder" None "Placeholder" 0M ]

let transactions =
    let transfer id month day description fromId toId amount status =
        { id=id; date=DateOnly(2026,month,day); description=description; fromAccountId=fromId; toAccountId=toId; amount=amount; status=status }
    let verified id month day description fromId toId amount =
        transfer id month day description fromId toId amount Verified
    [ verified 201 9 18 "Client payment" 4 101 3200M
      verified 202 9 17 "Cloud hosting" 101 5 89M
      transfer 203 9 16 "Office supplies" 103 5 125M NeedsReview
      verified 204 9 15 "Tax payment" 102 5 450M
      verified 205 6 25 "June consulting" 4 101 4200M
      verified 206 6 30 "June contractor" 101 5 1800M
      verified 207 7 3 "Team subscriptions" 101 5 129M
      verified 208 7 15 "July consulting" 4 101 2500M
      verified 209 7 20 "Office rent" 101 5 1200M
      verified 210 8 5 "August consulting" 4 101 3600M
      verified 211 8 9 "Software licenses" 101 5 220M
      verified 212 8 18 "August contractor" 101 5 1600M
      verified 213 8 29 "Travel reimbursement" 5 101 140M
      verified 214 9 2 "Office utilities" 101 5 115M
      verified 215 9 10 "September consulting" 4 101 2800M
      verified 216 9 14 "Design software" 101 5 79M
      verified 217 9 16 "Team lunch" 101 5 42M
      verified 218 7 10 "Savings transfer" 102 104 500M ]

let private openingById = accountRows |> List.map (fun (row, opening) -> row.id, opening) |> Map.ofList

let withActivity (records: Records) =
    let withBalance row =
        { row with balance = openingById[row.id] + (records.transactions |> List.sumBy (signedAmount (Set.singleton row.id))) }
    let records = { records with accounts = List.map withBalance records.accounts }
    { records with accounts = records.accounts |> List.map (fun row -> { row with totalBalance = subtree records row |> List.sumBy _.balance }) }

let initialRecords =
    { accounts = List.map fst accountRows; transactions = transactions; unchanged = true } |> withActivity

let query () = { defaultQuery catalog with records = initialRecords }

let home (query: Query) : Home =
    let account = query.records.accounts |> List.tryFind (fun row -> string row.id = query.account) |> Option.defaultValue query.records.accounts.Head
    let period = match query.range with "7" -> 7 | "30" -> 30 | _ -> 90
    let last = DateOnly(2026, 9, 18)
    let parse value = DateOnly.TryParseExact(value, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None)
    let selected =
        match parse query.startDate, parse query.endDate with
        | (true, first), (true, finish) when first >= last.AddDays(-89) && finish <= last && first <= finish -> Some(first, finish)
        | _ -> None
    let period = if selected |> Option.exists (fun (first, _) -> first < last.AddDays(1 - period)) then 90 else period
    let first = last.AddDays(1 - period)
    let visibleStart, visibleEnd = selected |> Option.defaultValue (first, last)
    let ids = subtree query.records account |> List.map _.id |> Set.ofList
    let activity = query.records.transactions |> List.filter (involves ids)
    let opening = ids |> Seq.sumBy (fun id -> openingById[id])
    let balance date = opening + (activity |> List.filter (fun row -> row.date <= date) |> List.sumBy (signedAmount ids))
    let comparison = if List.contains query.comparison ["optimistic"; "pessimistic"] then query.comparison else "expected"
    let slope = match comparison with "optimistic" -> 18M | "pessimistic" -> -6M | _ -> 12M
    { account = account; accountIds = ids; currentBalance = account.totalBalance; periodDays = period; comparison = comparison
      startDate = first; endDate = last; visibleStart = visibleStart; visibleEnd = visibleEnd
      actual = [ for offset in 0 .. period - 1 do let date = first.AddDays offset in yield date, balance date ]
      scenario = [ for offset in 0 .. period - 1 -> first.AddDays offset, balance first + decimal offset * slope ]
      transactions = activity |> List.filter (fun row -> row.date >= visibleStart && row.date <= visibleEnd) }
