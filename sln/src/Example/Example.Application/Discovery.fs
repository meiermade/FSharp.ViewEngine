module Example.Application.Discovery

open Example.UI.Views.Model

let paths =
    [ yield! Marketing.View.paths
      yield! Example.UI.Views.Product.navigation |> List.map fst
      for account in Ledger.Domain.accounts do
          yield applicationUrl (ApplicationPage.Account account.id)
          if not (Ledger.Domain.isRoot account) then
              yield applicationUrl (ApplicationPage.EditAccount account.id)
              yield applicationUrl (ApplicationPage.DeleteAccount account.id)
      for environment in Ledger.Domain.environments do
          yield applicationUrl (ApplicationPage.Environment environment.id)
          yield applicationUrl (ApplicationPage.EditEnvironment environment.id)
      yield applicationUrl ApplicationPage.CreateAccount
      for transaction in Ledger.Domain.transactions do
          yield applicationUrl (ApplicationPage.Transaction transaction.id) ]
    |> List.distinct
