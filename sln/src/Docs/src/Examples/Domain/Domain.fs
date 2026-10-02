namespace Ledger

open System

/// Immutable USD financial fixtures and workspace facts shared by all three examples.
module Domain =
    [<RequireQualifiedAccess>]
    type AccountType = Asset | Liability | Equity | Revenue | Expense
    type Account = { id:int; name:string; accountType:AccountType; currency:string; openingBalance:decimal }
    [<RequireQualifiedAccess>]
    type TransactionStatus = Verified | NeedsReview
    type Transaction = { id:int; date:DateOnly; description:string; accountId:int; amount:decimal; status:TransactionStatus }
    let accounts =
        [ { id=101; name="Operating checking"; accountType=AccountType.Asset; currency="USD"; openingBalance=12500M }
          { id=102; name="Tax reserve"; accountType=AccountType.Asset; currency="USD"; openingBalance=8400M }
          { id=103; name="Business credit card"; accountType=AccountType.Liability; currency="USD"; openingBalance= -650M }
          { id=104; name="Savings"; accountType=AccountType.Asset; currency="USD"; openingBalance=5000M }
          { id=105; name="Unassigned expense"; accountType=AccountType.Expense; currency="USD"; openingBalance=0M } ]
    let transactions =
        [ { id=201; date=DateOnly(2026,9,18); description="Client payment"; accountId=101; amount=3200M; status=TransactionStatus.Verified }
          { id=202; date=DateOnly(2026,9,17); description="Cloud hosting"; accountId=101; amount= -89M; status=TransactionStatus.Verified }
          { id=203; date=DateOnly(2026,9,16); description="Office supplies"; accountId=103; amount= -125M; status=TransactionStatus.NeedsReview }
          { id=204; date=DateOnly(2026,9,15); description="Tax payment"; accountId=102; amount= -450M; status=TransactionStatus.Verified } ]
    let tryAccount id = accounts |> List.tryFind (fun account -> account.id=id)
    let tryTransaction id = transactions |> List.tryFind (fun transaction -> transaction.id=id)
    let findAccount id = tryAccount id |> Option.get
    let findTransaction id = tryTransaction id |> Option.get
    let balance (account:Account) = account.openingBalance + (transactions |> List.filter (fun row -> row.accountId=account.id) |> List.sumBy _.amount)
    let accountTypes = [AccountType.Asset; AccountType.Liability; AccountType.Equity; AccountType.Revenue; AccountType.Expense]
    let canDeleteAccount id =
        tryAccount id |> Option.exists (fun account -> balance account=0M && not (transactions |> List.exists (fun row -> row.accountId=id)))
    type Organization = { id:string; name:string; role:string }
    type Environment = { id:string; name:string; sandbox:bool }
    type Ledger = { id:string; name:string; organizationId:string }
    type Workspace = { organization:Organization; environment:Environment; ledger:Ledger }
    let organizations = [{id="example-household";name="Example Household";role="Admin"};{id="client-organization";name="Client Organization";role="Member"}]
    let environments = [{id="production";name="Production";sandbox=false};{id="staging";name="Staging";sandbox=true};{id="review-testing";name="Review testing";sandbox=true}]
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
