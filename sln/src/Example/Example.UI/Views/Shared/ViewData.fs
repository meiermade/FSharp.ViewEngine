namespace Example.UI.Views

open System

/// Data prepared for the shared screens by either host.
module ViewData =
    type Account =
        { id: int; name: string; parentId: int option; accountType: string; currency: string
          balance: decimal; totalBalance: decimal; canDelete: bool }

    type TransactionStatus = Verified | NeedsReview
    type Transaction =
        { id: int; date: DateOnly; description: string; fromAccountId: int; toAccountId: int
          amount: decimal; status: TransactionStatus }

    type Records = { accounts: Account list; transactions: Transaction list; unchanged: bool }

    type Organization = { id: string; name: string; role: string }
    type Environment = { id: string; name: string; sandbox: bool }
    type Ledger = { id: string; name: string; organizationId: string }
    type Workspace = { organization: Organization; environment: Environment; ledger: Ledger }
    type Catalog =
        { organizations: Organization list; environments: Environment list; ledgers: Ledger list
          defaultWorkspace: Workspace; accountTypes: string list }

    type Home =
        { account: Account; accountIds: Set<int>; currentBalance: decimal; periodDays: int; comparison: string
          startDate: DateOnly; endDate: DateOnly; visibleStart: DateOnly; visibleEnd: DateOnly
          actual: (DateOnly * decimal) list; scenario: (DateOnly * decimal) list; transactions: Transaction list }

    type AccountDraft =
        { existingAccountId: int option; name: string; accountType: string; parentId: string
          currency: string; subtype: string; observedBalance: string }
    type ProfileDraft = { name: string; email: string; timeZone: string; dailySummaryTime: string }
    type OrganizationDraft = { name: string; currency: string }
    type EnvironmentDraft = { environmentId: string; name: string }

    [<RequireQualifiedAccess>]
    type ProfileError = InvalidName | InvalidEmail | InvalidTimeZone | InvalidDailySummaryTime
    [<RequireQualifiedAccess>]
    type OrganizationError = InvalidName | InvalidCurrency
    [<RequireQualifiedAccess>]
    type EnvironmentError = InvalidName

    let tryAccount (records: Records) id = records.accounts |> List.tryFind (fun row -> row.id = id)
    let tryTransaction (records: Records) id = records.transactions |> List.tryFind (fun row -> row.id = id)
    let tryEnvironment (catalog: Catalog) id = catalog.environments |> List.tryFind (fun row -> row.id = id)
    let findAccount records id = tryAccount records id |> Option.get
    let findTransaction records id = tryTransaction records id |> Option.get
    let findEnvironment catalog id = tryEnvironment catalog id |> Option.get

    let workspaceFromStrings (catalog: Catalog) organization environment ledger =
        let org = catalog.organizations |> List.tryFind (fun row -> row.id = organization) |> Option.defaultValue catalog.defaultWorkspace.organization
        let env = tryEnvironment catalog environment |> Option.defaultValue catalog.defaultWorkspace.environment
        let ledgers = catalog.ledgers |> List.filter (fun row -> row.organizationId = org.id)
        let fallback = if org.id = catalog.defaultWorkspace.organization.id then catalog.defaultWorkspace.ledger else List.head ledgers
        { organization = org; environment = env; ledger = ledgers |> List.tryFind (fun row -> row.id = ledger) |> Option.defaultValue fallback }

    let isRoot (account: Account) = account.parentId.IsNone
    let children (records: Records) id = records.accounts |> List.filter (fun row -> row.parentId = Some id)
    let rec subtree records (account: Account) = account :: (children records account.id |> List.collect (subtree records))
    let involves (ids: Set<int>) (transaction: Transaction) = ids.Contains transaction.fromAccountId || ids.Contains transaction.toAccountId
    let signedAmount (ids: Set<int>) (transaction: Transaction) =
        match ids.Contains transaction.fromAccountId, ids.Contains transaction.toAccountId with
        | true, false -> -transaction.amount
        | false, true -> transaction.amount
        | _ -> 0M
