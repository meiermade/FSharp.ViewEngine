module internal Example.Application.Web.Projection

module Domain = Ledger.Domain
module Operations = Ledger.Operations
open Example.UI.Views.ViewData

let private organization (row: Domain.Organization) : Organization =
    { id = row.id; name = row.name; role = row.role }

let private environment (row: Domain.Environment) : Environment =
    { id = row.id; name = row.name; sandbox = row.sandbox }

let private ledger (row: Domain.Ledger) : Ledger =
    { id = row.id; name = row.name; organizationId = row.organizationId }

let catalog =
    let workspace = Domain.defaultWorkspace
    { organizations = List.map organization Domain.organizations
      environments = List.map environment Domain.environments
      ledgers = List.map ledger Domain.ledgers
      defaultWorkspace =
        { organization = organization workspace.organization; environment = environment workspace.environment; ledger = ledger workspace.ledger }
      accountTypes = List.map string Domain.accountTypes }

let private transaction (row: Domain.Transaction) : Transaction =
    { id = row.id; date = row.date; description = row.description; fromAccountId = row.fromAccountId
      toAccountId = row.toAccountId; amount = row.amount
      status = if row.status = Domain.TransactionStatus.Verified then Verified else NeedsReview }

let private account records (row: Domain.Account) : Account =
    { id = row.id; name = row.name; parentId = row.parentId; accountType = string row.accountType; currency = row.currency
      balance = Domain.balance records row; totalBalance = Domain.totalBalance records row
      canDelete = Domain.canDeleteAccount records row.id }

let records (source: Domain.Records) : Records =
    { accounts = List.map (account source) source.accounts
      transactions = List.map transaction source.transactions
      unchanged = source = Domain.initialRecords }

let home records (source: Ledger.Home.ReadHomeResponse) : Home =
    { account = account records source.account; accountIds = source.accountIds; currentBalance = source.currentBalance
      periodDays = source.periodDays; comparison = source.comparison; startDate = source.startDate; endDate = source.endDate
      visibleStart = source.visibleStart; visibleEnd = source.visibleEnd; actual = source.actual; scenario = source.scenario
      transactions = List.map transaction source.transactions }

let validateAccount records (draft: AccountDraft) =
    Operations.validateAccount records
        { existingAccountId = draft.existingAccountId; name = draft.name; accountType = draft.accountType
          parentId = draft.parentId; currency = draft.currency; subtype = draft.subtype; observedBalance = draft.observedBalance }

let profileError (draft: ProfileDraft) =
    match Operations.validateProfile
        { name = draft.name; email = draft.email; timeZone = draft.timeZone; dailySummaryTime = draft.dailySummaryTime } with
    | Ok _ -> None
    | Error Operations.ValidateProfileError.InvalidName -> Some ProfileError.InvalidName
    | Error Operations.ValidateProfileError.InvalidEmail -> Some ProfileError.InvalidEmail
    | Error Operations.ValidateProfileError.InvalidTimeZone -> Some ProfileError.InvalidTimeZone
    | Error Operations.ValidateProfileError.InvalidDailySummaryTime -> Some ProfileError.InvalidDailySummaryTime

let organizationError (draft: OrganizationDraft) =
    match Operations.validateOrganization { name = draft.name; currency = draft.currency } with
    | Ok _ -> None
    | Error Operations.ValidateOrganizationError.InvalidName -> Some OrganizationError.InvalidName
    | Error Operations.ValidateOrganizationError.InvalidCurrency -> Some OrganizationError.InvalidCurrency

let environmentError (draft: EnvironmentDraft) =
    match Operations.validateEnvironment { environmentId = draft.environmentId; name = draft.name } with
    | Ok _ -> None
    | Error _ -> Some EnvironmentError.InvalidName
