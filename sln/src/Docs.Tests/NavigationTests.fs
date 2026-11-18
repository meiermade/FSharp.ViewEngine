module Docs.Tests.NavigationTests

open System
open Expecto
open Example.Server.Common

let tests = testList "Navigation sequences" [
    testCase "only the latest concurrent intent survives until absolute expiry" <| fun _ ->
        let mutable now = DateTimeOffset.UnixEpoch
        let tracker = NavigationSequences(3, TimeSpan.FromMinutes 10., fun () -> now)
        let client = Guid.NewGuid()
        System.Threading.Tasks.Parallel.For(1, 501, fun sequence -> tracker.Record(client, int64 sequence)) |> ignore
        Expect.isTrue (tracker.IsLatest(client, 500L)) "the maximum wins regardless of arrival order"
        Expect.isFalse (tracker.IsLatest(client, 499L)) "stale responses cannot patch the document"
        now <- now.AddMinutes 9.
        tracker.Record(client, 501L)
        Expect.isTrue (tracker.IsLatest(client, 501L)) "active entries still advance"
        now <- now.AddMinutes 1.
        Expect.isFalse (tracker.IsLatest(client, 501L)) "updates cannot extend storage indefinitely"
        tracker.Record(client, 1L)
        Expect.isTrue (tracker.IsLatest(client, 1L)) "a fresh intent can re-establish expired tracking"

    testCase "capacity evicts the oldest client without accumulating repeated-client entries" <| fun _ ->
        let tracker = NavigationSequences(3, TimeSpan.FromMinutes 10., fun () -> DateTimeOffset.UnixEpoch)
        let first, second, third, fourth = Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()
        for sequence in 1L .. 1000L do tracker.Record(first, sequence)
        tracker.Record(second, 1L)
        tracker.Record(third, 1L)
        tracker.Record(fourth, 1L)
        Expect.isFalse (tracker.IsLatest(first, 1000L)) "the fourth distinct client evicts the oldest"
        for client in [second; third; fourth] do
            Expect.isTrue (tracker.IsLatest(client, 1L)) "current clients remain usable at the capacity boundary"
]
