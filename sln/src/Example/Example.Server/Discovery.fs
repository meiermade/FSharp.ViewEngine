module Example.Server.Discovery

open System
open System.Net
open Example.Components.Views.Model

let paths =
    [ yield! Marketing.View.paths
      yield! Product.Handler.navigation |> List.map fst
      yield "/examples/presentation"
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
          yield applicationUrl (ApplicationPage.Transaction transaction.id)
      yield! Specification.View.navigation |> List.map fst
      yield! ApiDocumentation.View.navigation |> List.map fst ]
    |> List.distinct

let sitemap origin =
    let urls = paths |> List.map (fun path -> $"  <url><loc>{WebUtility.HtmlEncode(origin + path)}</loc></url>")
    String.concat "\n" ([ "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"; "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" ] @ urls @ [ "</urlset>"; "" ])
