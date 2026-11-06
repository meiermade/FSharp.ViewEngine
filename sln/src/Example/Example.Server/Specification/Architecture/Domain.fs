namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html

open Presentation

module internal Domain =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "rules", "Rules" ]

    let render () =
        contracts
            "Financial types"
            "Example.Domain/Domain.fs owns immutable financial fixtures, organization/environment/ledger context, balance calculations and deletion eligibility."
            [ "System and FSharp.Core only." ]
            "type Account = { id:int; name:string; accountType:AccountType; currency:string; openingBalance:decimal }\ntype Transaction = { id:int; date:DateOnly; description:string; accountId:int; amount:decimal; status:TransactionStatus }\ntype Workspace = { organization:Organization; environment:Environment; ledger:Ledger }\n// balance : Records -> Account -> decimal\n// canDeleteAccount : Records -> int -> bool"
            [ "Account balances SHALL equal opening balance plus the account's signed transactions."
              "Deletion eligibility SHALL require an existing account, zero balance and no transactions."
              "Every ledger SHALL belong to its selected organization. Unknown context values SHALL resolve to available fixtures." ]
