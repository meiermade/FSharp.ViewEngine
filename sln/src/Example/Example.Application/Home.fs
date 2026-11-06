namespace Ledger

open System
open System.Globalization
open Domain

/// The server supplies URL values; the use case owns one coherent chart/table read model.
module Home =
    type ReadHomeRequest =
        { records:Records; account:string; period:string; comparison:string; startDate:string; endDate:string }
    type ReadHomeResponse =
        { account:Account; periodDays:int; comparison:string
          startDate:DateOnly; endDate:DateOnly; visibleStart:DateOnly; visibleEnd:DateOnly
          actual:(DateOnly * decimal) list; scenario:(DateOnly * decimal) list
          transactions:Transaction list }

    let readHome (request:ReadHomeRequest) : ReadHomeResponse =
        let account = request.records.accounts |> List.tryFind (fun account -> string account.id=request.account) |> Option.defaultValue request.records.accounts.Head
        let period = match request.period with "7" -> 7 | "30" -> 30 | _ -> 90
        let comparison = if List.contains request.comparison ["optimistic";"pessimistic"] then request.comparison else "expected"
        let endDate = DateOnly(2026,9,18)
        let startDate = endDate.AddDays(1-period)
        let parse value = DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None)
        let visibleStart,visibleEnd =
            match parse request.startDate,parse request.endDate with
            | (true,first),(true,last) when first>=startDate && last<=endDate && first<=last -> first,last
            | _ -> startDate,endDate
        let activity = request.records.transactions |> List.filter (fun row -> row.accountId=account.id)
        let balanceAt date = account.openingBalance + (activity |> List.filter (fun row -> row.date<=date) |> List.sumBy _.amount)
        let factor = match comparison with "optimistic" -> 1.5M | "pessimistic" -> -0.5M | _ -> 1M
        { account=account; periodDays=period; comparison=comparison; startDate=startDate; endDate=endDate
          visibleStart=visibleStart; visibleEnd=visibleEnd
          actual=[for offset in 0..period-1 do let date = startDate.AddDays offset in yield date,balanceAt date]
          scenario=[for offset in 0..period-1 -> startDate.AddDays offset,balanceAt startDate + decimal offset*12M*factor]
          transactions=activity |> List.filter (fun row -> row.date>=visibleStart && row.date<=visibleEnd) |> List.sortByDescending (fun row -> row.date,row.id) }
