namespace Ledger

open System
open System.Globalization
open Domain

/// The server supplies URL values; the use case owns one coherent chart/table read model.
module Home =
    type ReadHomeRequest =
        { records:Records; account:string; period:string; comparison:string; startDate:string; endDate:string }
    type ReadHomeResponse =
        { account:Account; accountIds:Set<int>; currentBalance:decimal; periodDays:int; comparison:string
          startDate:DateOnly; endDate:DateOnly; visibleStart:DateOnly; visibleEnd:DateOnly
          actual:(DateOnly * decimal) list; scenario:(DateOnly * decimal) list
          transactions:Transaction list }

    let readHome (request:ReadHomeRequest) : ReadHomeResponse =
        let account = request.records.accounts |> List.tryFind (fun account -> string account.id=request.account) |> Option.defaultValue request.records.accounts.Head
        let period = match request.period with "7" -> 7 | "30" -> 30 | _ -> 90
        let comparison = if List.contains request.comparison ["optimistic";"pessimistic"] then request.comparison else "expected"
        let endDate = DateOnly(2026,9,18)
        let parse value = DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None)
        let selection =
            match parse request.startDate, parse request.endDate with
            | (true, first), (true, last) when first >= endDate.AddDays(-89) && last <= endDate && first <= last -> Some(first, last)
            | _ -> None
        let period =
            match selection with
            | Some(first, _) when first < endDate.AddDays(1 - period) -> 90
            | _ -> period
        let startDate = endDate.AddDays(1 - period)
        let visibleStart,visibleEnd = selection |> Option.defaultValue (startDate, endDate)
        let scope = subtree request.records account
        let accountIds = scope |> List.map _.id |> Set.ofList
        let activity = request.records.transactions |> List.filter (involves accountIds)
        let opening = scope |> List.sumBy _.openingBalance
        let balanceAt date = opening + (activity |> List.filter (fun row -> row.date<=date) |> List.sumBy (change accountIds))
        let factor = match comparison with "optimistic" -> 1.5M | "pessimistic" -> -0.5M | _ -> 1M
        { account=account; accountIds=accountIds; currentBalance=totalBalance request.records account
          periodDays=period; comparison=comparison; startDate=startDate; endDate=endDate
          visibleStart=visibleStart; visibleEnd=visibleEnd
          actual=[for offset in 0..period-1 do let date = startDate.AddDays offset in yield date,balanceAt date]
          scenario=[for offset in 0..period-1 -> startDate.AddDays offset,balanceAt startDate + decimal offset*12M*factor]
          transactions=activity |> List.filter (fun row -> row.date>=visibleStart && row.date<=visibleEnd) |> List.sortByDescending (fun row -> row.date,row.id) }
