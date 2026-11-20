namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html

open Presentation

module internal Domain =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "requirements", "Requirements" ]

    let render () =
        contracts
            "Financial types"
            "Example.Domain/Domain.fs owns immutable financial fixtures, organization/environment/ledger context, balance calculations and deletion eligibility."
            [ "System and FSharp.Core only." ]
            "type Account = { id:int; name:string; parentId:int option; accountType:AccountType; currency:string; openingBalance:decimal }\ntype Transaction = { id:int; date:DateOnly; description:string; fromAccountId:int; toAccountId:int; amount:decimal; status:TransactionStatus }\ntype Workspace = { organization:Organization; environment:Environment; ledger:Ledger }\n// balance : Records -> Account -> decimal\n// canDeleteAccount : Records -> int -> bool"
            [ "System roots SHALL have no parent and SHALL NOT be edited or deleted. Non-root accounts SHALL have a parent of the same type outside their own branch."
              "Transfers SHALL have distinct source and destination accounts and a positive amount. Direct balance SHALL equal opening balance plus incoming transfers minus outgoing transfers."
              "Total balance SHALL include each account in the subtree once. Transfers internal to the subtree SHALL appear once in its activity with zero net change."
              "Chart balances SHALL carry forward activity before the inclusive visible date range; scenario projections SHALL NOT replace recorded balances."
              "Account deletion SHALL require an existing non-root account, no children, zero balance and no transactions on either side. Transaction verification status SHALL NOT restrict transaction deletion."
              "Every ledger SHALL belong to its selected organization. Unknown context values SHALL resolve to available fixtures." ]
