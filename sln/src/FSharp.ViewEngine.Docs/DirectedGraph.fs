namespace FSharp.ViewEngine.Docs

[<NoEquality; NoComparison>]
type DirectedGraph<'node when 'node:comparison> =
    { nodes:'node list
      roots:'node list
      edges:('node * 'node) list }

module DirectedGraph =
    let validate (graph:DirectedGraph<'node>) =
        let knownNodes = Set.ofList graph.nodes

        let duplicateNodeIssues =
            graph.nodes
            |> List.countBy id
            |> List.choose (fun (node, count) ->
                if count > 1 then Some(issue "graph.duplicate-node" $"Graph node is declared more than once: {node}.") else None)

        let rootIssues =
            [ if List.isEmpty graph.roots then
                  issue "graph.missing-root" "A directed graph must declare at least one root."
              for root in graph.roots do
                  if not (Set.contains root knownNodes) then
                      issue "graph.unknown-root" $"Graph root is not declared: {root}." ]

        let edgeIssues =
            [ for source, target in graph.edges do
                  if not (Set.contains source knownNodes) then
                      issue "graph.unknown-source" $"Graph edge source is not declared: {source}."
                  if not (Set.contains target knownNodes) then
                      issue "graph.unknown-target" $"Graph edge target is not declared: {target}." ]

        let validEdges =
            graph.edges
            |> List.filter (fun (source, target) -> Set.contains source knownNodes && Set.contains target knownNodes)
            |> List.groupBy fst
            |> List.map (fun (source, edges) -> source, (edges |> List.map snd))
            |> Map.ofList

        let rec visit visited pending =
            match pending with
            | [] -> visited
            | node :: remaining when Set.contains node visited -> visit visited remaining
            | node :: remaining ->
                let destinations = validEdges |> Map.tryFind node |> Option.defaultValue []
                visit (Set.add node visited) (destinations @ remaining)

        let reachable =
            graph.roots
            |> List.filter (fun root -> Set.contains root knownNodes)
            |> visit Set.empty

        let unreachableIssues =
            Set.difference knownNodes reachable
            |> Set.toList
            |> List.map (fun node -> issue "graph.unreachable" $"Graph node is unreachable from every root: {node}.")

        duplicateNodeIssues @ rootIssues @ edgeIssues @ unreachableIssues
