namespace FSharp.ViewEngine.Docs

open System

/// A canonical documentation page and its compatibility aliases.
type DocsRegisteredPage =
    { path:string
      aliases:string list
      page:DocsPage }

/// Whole-site validation across typed navigation and registered pages.
module DocsRegistry =
    let private validPath (path:string) =
        not (String.IsNullOrWhiteSpace path) && path.StartsWith('/')

    let validate (navigation:NavNode<'destination> list) (registered:DocsRegisteredPage list) =
        let navigationPages = navigation |> NavNode.collectPages
        let canonicalPaths = registered |> List.map _.path
        let aliases = registered |> List.collect (fun entry -> entry.aliases |> List.map (fun alias -> alias, entry.path))
        let knownPaths = Set.ofList (canonicalPaths @ (aliases |> List.map fst))
        let navigationIds = navigationPages |> List.map NavNode.id |> Set.ofList

        let duplicate code label values =
            values
            |> List.countBy id
            |> List.choose (fun (value, count) -> if count > 1 then Some(issue code $"Duplicate {label}: {value}.") else None)

        let registrationIssues =
            registered
            |> List.collect (fun entry ->
                [ if not (validPath entry.path) then issue "registry.invalid-path" $"Registered path '{entry.path}' must be an absolute application path."
                  if not (Set.contains entry.page.activeId navigationIds) then issue "registry.unreachable-page" $"Registered page '{entry.page.activeId}' is not reachable from navigation."
                  for alias in entry.aliases do
                      if not (validPath alias) then issue "registry.invalid-alias" $"Alias '{alias}' must be an absolute application path."
                      if canonicalPaths |> List.contains alias then issue "registry.alias-collision" $"Alias '{alias}' collides with a canonical path." ]
                @ DocsPage.validate entry.page
                @ (match entry.page.pager with
                   | None -> []
                   | Some pager ->
                       [ pager.previousPage; pager.nextPage ]
                       |> List.choose id
                       |> List.choose (fun link ->
                           if Set.contains link.href knownPaths then None
                           else Some(issue "registry.invalid-pager-target" $"Pager destination '{link.href}' is not registered."))))

        let missingNavigation =
            navigationPages
            |> List.choose (fun node ->
                match NavNode.href node with
                | Some href when canonicalPaths |> List.contains href |> not ->
                    Some(issue "registry.missing-navigation-page" $"Navigation page '{NavNode.id node}' has no canonical registered page at '{href}'.")
                | _ -> None)

        let aliasCollisions =
            aliases
            |> List.map fst
            |> duplicate "registry.duplicate-alias" "page alias"

        Navigation.validate navigation
        @ registrationIssues
        @ missingNavigation
        @ (canonicalPaths |> duplicate "registry.duplicate-path" "registered path")
        @ aliasCollisions
