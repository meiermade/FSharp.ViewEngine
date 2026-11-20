namespace Ledger

open System

/// Immutable USD financial fixtures and workspace facts shared by the Geldos examples.
module Domain =
    [<RequireQualifiedAccess>]
    type AccountType = Asset | Liability | Equity | Revenue | Expense | Placeholder
    type Account = { id:int; name:string; parentId:int option; accountType:AccountType; currency:string; openingBalance:decimal }
    [<RequireQualifiedAccess>]
    type TransactionStatus = Verified | NeedsReview
    type Transaction = { id:int; date:DateOnly; description:string; fromAccountId:int; toAccountId:int; amount:decimal; status:TransactionStatus }

    let accounts =
        let account id name parentId kind opening =
            { id=id; name=name; parentId=parentId; accountType=kind; currency="USD"; openingBalance=opening }
        [ account 101 "Operating checking" (Some 1) AccountType.Asset 12500M
          account 102 "Tax reserve" (Some 1) AccountType.Asset 8400M
          account 103 "Business credit card" (Some 2) AccountType.Liability -650M
          account 104 "Savings" (Some 1) AccountType.Asset 5000M
          account 105 "Unassigned expense" (Some 5) AccountType.Expense 0M
          account 1 "Assets" None AccountType.Asset 0M
          account 2 "Liabilities" None AccountType.Liability 0M
          account 3 "Equity" None AccountType.Equity -25250M
          account 4 "Revenue" None AccountType.Revenue 0M
          account 5 "Expenses" None AccountType.Expense 0M
          account 6 "Placeholder" None AccountType.Placeholder 0M ]

    let transactions =
        let transfer id month day description fromId toId amount status =
            { id=id; date=DateOnly(2026,month,day); description=description; fromAccountId=fromId; toAccountId=toId; amount=amount; status=status }
        let verified id month day description fromId toId amount =
            transfer id month day description fromId toId amount TransactionStatus.Verified
        [ verified 201 9 18 "Client payment" 4 101 3200M
          verified 202 9 17 "Cloud hosting" 101 5 89M
          transfer 203 9 16 "Office supplies" 103 5 125M TransactionStatus.NeedsReview
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
    type Records = { accounts:Account list; transactions:Transaction list }
    let initialRecords = { accounts=accounts; transactions=transactions }
    let tryAccount id = accounts |> List.tryFind (fun account -> account.id=id)
    let tryTransaction id = transactions |> List.tryFind (fun transaction -> transaction.id=id)
    let findAccount id = tryAccount id |> Option.get
    let findTransaction id = tryTransaction id |> Option.get
    let isRoot (account:Account) = account.parentId.IsNone
    let children (records:Records) id = records.accounts |> List.filter (fun row -> row.parentId = Some id)
    let rec subtree records (account:Account) =
        account :: (children records account.id |> List.collect (subtree records))

    let involves (ids:Set<int>) (transaction:Transaction) =
        ids.Contains transaction.fromAccountId || ids.Contains transaction.toAccountId

    let change (ids:Set<int>) (transaction:Transaction) =
        (if ids.Contains transaction.toAccountId then transaction.amount else 0M)
        - (if ids.Contains transaction.fromAccountId then transaction.amount else 0M)

    let balance (records:Records) (account:Account) =
        account.openingBalance + (records.transactions |> List.sumBy (change (Set.singleton account.id)))

    let totalBalance records account = subtree records account |> List.sumBy (balance records)

    let accountTypes = [AccountType.Asset; AccountType.Liability; AccountType.Equity; AccountType.Revenue; AccountType.Expense; AccountType.Placeholder]
    let canDeleteAccount (records:Records) id =
        records.accounts |> List.tryFind (fun account -> account.id=id)
        |> Option.exists (fun account ->
            not (isRoot account) && List.isEmpty (children records id) && balance records account = 0M
            && not (records.transactions |> List.exists (involves (Set.singleton id))))
    type Organization = { id:string; name:string; role:string }
    type Environment = { id:string; name:string; sandbox:bool }
    type Ledger = { id:string; name:string; organizationId:string }
    type Workspace = { organization:Organization; environment:Environment; ledger:Ledger }
    let organizations = [{id="example-household";name="Example Household";role="Admin"};{id="client-organization";name="Client Organization";role="Member"}]
    let environments = [{id="production";name="Production";sandbox=false};{id="staging";name="Staging";sandbox=true};{id="review-testing";name="Review testing";sandbox=true}]
    let tryEnvironment id = environments |> List.tryFind (fun environment -> environment.id=id)
    let findEnvironment id = tryEnvironment id |> Option.get
    let ledgers =
        [ for id,name in ["example-household","Example Household";"alex","Alex";"jordan","Jordan";"casey","Casey";"riley","Riley";"example-business","Example Business, LLC"] do
              yield {id=id;name=name;organizationId="example-household"}
          yield {id="operating-company";name="Operating Company";organizationId="client-organization"} ]
    let defaultWorkspace = {organization=organizations.Head;environment=environments.Head;ledger=ledgers[1]}
    let workspaceFromStrings organization environment ledger =
        let organization = organizations |> List.tryFind (fun item -> item.id=organization) |> Option.defaultValue organizations.Head
        let environment = environments |> List.tryFind (fun item -> item.id=environment) |> Option.defaultValue environments.Head
        let available = ledgers |> List.filter (fun item -> item.organizationId=organization.id)
        let ledger = available |> List.tryFind (fun item -> item.id=ledger) |> Option.defaultValue (if organization.id="example-household" then ledgers[1] else available.Head)
        {organization=organization;environment=environment;ledger=ledger}
