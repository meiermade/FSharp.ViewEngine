namespace FSharp.ViewEngine.Cli

open System
open System.IO
open System.Reflection
open System.Xml.Linq

[<RequireQualifiedAccess>]
module Registry =
    let private assembly = Assembly.GetExecutingAssembly()

    let private resource name =
        use stream = assembly.GetManifestResourceStream name
        if isNull stream then invalidOp $"Embedded registry resource '{name}' was not found."
        use reader = new StreamReader(stream)
        reader.ReadToEnd() |> Text.normalize

    let private version () =
        let attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        if isNull attribute || String.IsNullOrWhiteSpace attribute.InformationalVersion then "0.0.0-local"
        else attribute.InformationalVersion.Split('+')[0]

    let files (components:RegistryComponent list) = components |> List.collect _.Files

    let load () : ComponentRegistry =
        let document = resource "FSharp.ViewEngine.Cli.Registry.props" |> XDocument.Parse
        let entries =
            document.Descendants()
            |> Seq.filter (fun element -> element.Name.LocalName = "FveComponent" || element.Name.LocalName = "FveAsset")
            |> Seq.toList
        let attribute name (element:XElement) =
            let value = element.Attribute(XName.Get name)
            if isNull value then "" else value.Value
        let owner element =
            match attribute "FveName" element with
            | "" -> attribute "FveOwner" element
            | name -> name
        let components =
            entries
            |> List.filter (attribute "FveName" >> String.IsNullOrWhiteSpace >> not)
            |> List.map (fun definition ->
                let name = attribute "FveName" definition
                { Name = name
                  Dependencies =
                    (attribute "FveDependencies" definition).Split(';', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
                    |> Array.toList
                  Internal = attribute "FveInternal" definition = "true"
                  Files =
                    entries
                    |> List.filter (fun entry -> owner entry = name)
                    |> List.map (fun entry ->
                        let fileName = attribute "FveFile" entry
                        { Component = name
                          FileName = fileName
                          Compile = entry.Name.LocalName = "FveComponent"
                          Module = attribute "FveModule" entry = "true"
                          Source = resource $"FSharp.ViewEngine.Cli.Registry.{Path.GetFileName fileName}" }) })
        let names = components |> List.map _.Name |> Set.ofList
        if names.Count <> components.Length then invalidOp "Registry component names must be unique."
        for entry in entries do
            let fileName = attribute "FveFile" entry
            if not (Set.contains (owner entry) names) then invalidOp $"Registry file '{fileName}' has no owning component."
        let fileNames = files components |> List.map (fun file -> Path.GetFileName file.FileName)
        if Set.count (Set.ofList fileNames) <> fileNames.Length then invalidOp "Registry resource filenames must be unique."
        for item in components do
            for dependency in item.Dependencies do
                if not (Set.contains dependency names) then
                    invalidOp $"Registry component '{item.Name}' references unknown dependency '{dependency}'."
        { Version = version (); Components = components }

    let resolve (registry:ComponentRegistry) (requested:string list) =
        let byName = registry.Components |> Seq.map (fun item -> item.Name, item) |> Map.ofSeq
        let unknown =
            requested |> List.distinct
            |> List.filter (fun name -> Map.tryFind name byName |> Option.forall _.Internal)
        if not unknown.IsEmpty then
            let unavailable = String.concat ", " unknown
            let available = registry.Components |> List.filter (fun item -> not item.Internal) |> List.map _.Name |> String.concat ", "
            Error $"Unknown component(s): {unavailable}. Available components: {available}."
        else
            let mutable selected = Set.empty
            let ordered = ResizeArray<RegistryComponent>()
            let rec visit visiting name =
                if Set.contains name visiting then invalidOp $"Registry dependency cycle at '{name}'."
                if not (Set.contains name selected) then
                    let item = Map.find name byName
                    for dependency in item.Dependencies do visit (Set.add name visiting) dependency
                    selected <- Set.add name selected
                    ordered.Add item
            for name in requested do visit Set.empty name
            Ok (List.ofSeq ordered)

    let sourceFor (namespaceName:string) (item:RegistryFile) =
        item.Source.Replace("FSharp.ViewEngine.Components", namespaceName, StringComparison.Ordinal)
