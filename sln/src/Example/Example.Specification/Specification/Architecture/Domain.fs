namespace Example.Specification.ArchitecturePages

open System
open Microsoft.FSharp.Reflection
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html

open Presentation

module internal Domain =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "requirements", "Requirements" ]

    let rec private typeName (model: Type) =
        if model = typeof<int> then "int"
        elif model = typeof<string> then "string"
        elif model = typeof<decimal> then "decimal"
        elif model.IsGenericType && model.GetGenericTypeDefinition() = typedefof<option<_>> then
            typeName (model.GetGenericArguments()[0]) + " option"
        else model.Name

    let private declaration (model: Type) =
        let fields (properties: Reflection.PropertyInfo array) =
            properties |> Array.map (fun field -> $"{field.Name}: {typeName field.PropertyType}")
        if FSharpType.IsRecord model then
            let members = FSharpType.GetRecordFields model |> fields |> String.concat "\n      "
            $"type {model.Name} =\n    {{ {members} }}"
        else
            let cases =
                FSharpType.GetUnionCases model
                |> Array.map (fun case ->
                    let members = case.GetFields() |> fields
                    if Array.isEmpty members then case.Name
                    else case.Name + " of " + String.concat " * " members)
                |> String.concat "\n    | "
            $"type {model.Name} =\n    | {cases}"

    let private types =
        [ typeof<Ledger.Domain.AccountType>; typeof<Ledger.Domain.Account>
          typeof<Ledger.Domain.TransactionStatus>; typeof<Ledger.Domain.Transaction>; typeof<Ledger.Domain.Workspace> ]
        |> List.map declaration
        |> String.concat "\n\n"

    let render () =
        contracts
            "Financial types"
            "Example.Domain/Domain.fs owns immutable financial fixtures, organization/environment/ledger context, balance calculations and deletion eligibility."
            [ "System and FSharp.Core only." ]
            types
            [ "System roots SHALL have no parent and SHALL NOT be edited or deleted. Non-root accounts SHALL have a parent of the same type outside their own branch."
              "Transfers SHALL have distinct source and destination accounts and a positive amount. Direct balance SHALL equal opening balance plus incoming transfers minus outgoing transfers."
              "Total balance SHALL include each account in the subtree once. Transfers internal to the subtree SHALL appear once in its activity with zero net change."
              "Chart balances SHALL carry forward activity before the inclusive visible date range; scenario projections SHALL NOT replace recorded balances."
              "Account deletion SHALL require an existing non-root account, no children, zero balance and no transactions on either side. Transaction verification status SHALL NOT restrict transaction deletion."
              "Every ledger SHALL belong to its selected organization. Unknown context values SHALL resolve to available fixtures." ]
