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

    let load () : ComponentRegistry =
        let document = resource "FSharp.ViewEngine.Cli.Registry.props" |> XDocument.Parse
        let components =
            document.Descendants()
            |> Seq.filter (fun element -> element.Name.LocalName = "FveComponent" || element.Name.LocalName = "FveAsset")
            |> Seq.map (fun element ->
                let attribute name =
                    let value = element.Attribute(XName.Get name)
                    if isNull value then "" else value.Value
                let fileName = attribute "FveFile"
                { Name = attribute "FveName"
                  FileName = fileName
                  Dependencies =
                    (attribute "FveDependencies").Split(';', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
                    |> Array.toList
                  Compile = element.Name.LocalName = "FveComponent"
                  Source = resource $"FSharp.ViewEngine.Cli.Registry.{Path.GetFileName fileName}" })
            |> Seq.toList
        let names = components |> List.map _.Name |> Set.ofList
        for item in components do
            for dependency in item.Dependencies do
                if not (Set.contains dependency names) then
                    invalidOp $"Registry component '{item.Name}' references unknown dependency '{dependency}'."
        { Version = version ()
          Components = components }

    let resolve (registry:ComponentRegistry) (requested:string list) =
        let byName = registry.Components |> Seq.map (fun item -> item.Name, item) |> Map.ofSeq
        let unknown = requested |> List.distinct |> List.filter (fun name -> not (Map.containsKey name byName))
        if not unknown.IsEmpty then
            let unavailable = String.concat ", " unknown
            let available = registry.Components |> List.map _.Name |> String.concat ", "
            Error $"Unknown component(s): {unavailable}. Available components: {available}."
        else
            let rec includeDependencies selected name =
                let item = Map.find name byName
                (Set.add name selected, item.Dependencies)
                ||> List.fold includeDependencies
            let selected = (Set.empty, requested) ||> List.fold includeDependencies
            registry.Components |> List.filter (fun item -> Set.contains item.Name selected) |> Ok

    let sourceFor (namespaceName:string) (item:RegistryComponent) =
        item.Source.Replace("FSharp.ViewEngine.Components", namespaceName, StringComparison.Ordinal)
