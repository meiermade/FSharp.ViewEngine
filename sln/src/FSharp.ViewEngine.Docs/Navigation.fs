namespace FSharp.ViewEngine.Docs

open System

[<NoEquality; NoComparison>]
type NavPage<'destination> =
    { id:string
      label:string
      href:string
      destination:'destination }

[<NoEquality; NoComparison>]
type NavGroup<'destination> =
    { id:string
      label:string
      defaultOpen:bool
      breadcrumbHref:string option
      children:NavNode<'destination> list }

and [<NoEquality; NoComparison>] NavNode<'destination> =
    | Page of NavPage<'destination>
    | Group of NavGroup<'destination>

type Breadcrumb =
    { label:string
      href:string option }

module Nav =
    let page id label href destination =
        Page
            { id = id
              label = label
              href = href
              destination = destination }

    let group id label defaultOpen children =
        Group
            { id = id
              label = label
              defaultOpen = defaultOpen
              breadcrumbHref = None
              children = children }

    let groupWithBreadcrumb id label breadcrumbHref defaultOpen children =
        Group
            { id = id
              label = label
              defaultOpen = defaultOpen
              breadcrumbHref = Some breadcrumbHref
              children = children }

module NavNode =
    let id = function
        | Page page -> page.id
        | Group group -> group.id

    let label = function
        | Page page -> page.label
        | Group group -> group.label

    let href = function
        | Page page -> Some page.href
        | Group _ -> None

    let breadcrumbHref = function
        | Page page -> Some page.href
        | Group group -> group.breadcrumbHref

    let destination = function
        | Page page -> Some page.destination
        | Group _ -> None

    let children = function
        | Page _ -> []
        | Group group -> group.children

    let defaultOpen = function
        | Page _ -> false
        | Group group -> group.defaultOpen

    let rec containsActive activeId node =
        id node = activeId || (children node |> List.exists (containsActive activeId))

    let rec collectGroups nodes =
        nodes
        |> List.collect (fun node ->
            match node with
            | Page _ -> []
            | Group group -> node :: collectGroups group.children)

    let rec collectPages nodes =
        nodes
        |> List.collect (fun node ->
            match node with
            | Page _ -> [ node ]
            | Group group -> collectPages group.children)

    let rec private tryPath activeId node =
        if id node = activeId then
            Some [ node ]
        else
            children node
            |> List.tryPick (tryPath activeId)
            |> Option.map (fun path -> node :: path)

    let tryFindPath activeId nodes =
        nodes |> List.tryPick (tryPath activeId)

module Navigation =
    let breadcrumbs nodes homeId activeId =
        let homeHref =
            nodes
            |> NavNode.collectPages
            |> List.tryFind (NavNode.id >> (=) homeId)
            |> Option.bind NavNode.href
            |> Option.defaultValue "/"

        let home = { label = "Home"; href = Some homeHref }

        match NavNode.tryFindPath activeId nodes with
        | Some [ Page page ] when page.id = homeId -> [ home ]
        | Some path ->
            home
            :: (path
                |> List.filter (NavNode.id >> (<>) homeId)
                |> List.map (fun node ->
                    { label = NavNode.label node
                      href = NavNode.breadcrumbHref node }))
        | None -> [ home ]

    let validate nodes =
        let validPath (href:string) =
            not (String.IsNullOrWhiteSpace href) && href.StartsWith('/')

        let rec validateNode node =
            let commonIssues =
                [ if String.IsNullOrWhiteSpace(NavNode.id node) then
                      issue "navigation.missing-id" "Navigation node IDs cannot be empty."
                  if String.IsNullOrWhiteSpace(NavNode.label node) then
                      issue "navigation.missing-label" $"Navigation node '{NavNode.id node}' must have a label." ]

            match node with
            | Page page ->
                commonIssues
                @ [ if not (validPath page.href) then
                        issue "navigation.invalid-href" $"Navigation page '{page.id}' must use an absolute application path." ]
            | Group group ->
                commonIssues
                @ [ if List.isEmpty group.children then
                        issue "navigation.empty-group" $"Navigation group '{group.id}' must contain at least one child."
                    if group.breadcrumbHref |> Option.exists (validPath >> not) then
                        issue "navigation.invalid-breadcrumb-href" $"Navigation group '{group.id}' must use an absolute application path for its breadcrumb." ]
                @ (group.children |> List.collect validateNode)

        let allNodes =
            let rec collect current =
                current @ (current |> List.collect (NavNode.children >> collect))
            collect nodes

        let duplicateIssues code description values =
            values
            |> List.countBy id
            |> List.choose (fun (value, count) ->
                if count > 1 then Some(issue code $"Duplicate {description}: {value}.") else None)

        let duplicateIds =
            allNodes
            |> List.map NavNode.id
            |> duplicateIssues "navigation.duplicate-id" "navigation node ID"

        let duplicateHrefs =
            allNodes
            |> List.choose NavNode.href
            |> duplicateIssues "navigation.duplicate-href" "navigation page href"

        (nodes |> List.collect validateNode) @ duplicateIds @ duplicateHrefs
