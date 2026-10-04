namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

open System.Reflection
open System.Xml.Linq
open Microsoft.FSharp.Reflection

/// <category>fsharp-api-reference</category>
[<NoEquality; NoComparison>]
type FSharpApiReferenceConfig = private { types:Type list }

module private FSharpApiReflection =
    let private simpleName (value:string) =
        let index = value.IndexOf('`')
        if index < 0 then value else value.Substring(0, index)

    let rec typeName (type':Type) =
        if type'.IsGenericParameter then "'" + type'.Name.ToLowerInvariant()
        elif type' = typeof<unit> then "unit"
        elif type' = typeof<bool> then "bool"
        elif type' = typeof<byte> then "byte"
        elif type' = typeof<int> then "int"
        elif type' = typeof<int64> then "int64"
        elif type' = typeof<decimal> then "decimal"
        elif type' = typeof<float> then "float"
        elif type' = typeof<string> then "string"
        elif type'.IsArray then typeName (type'.GetElementType()) + " array"
        elif FSharpType.IsTuple type' then
            FSharpType.GetTupleElements type' |> Array.map typeName |> String.concat " * " |> sprintf "(%s)"
        elif type'.IsGenericType then
            let definition = type'.GetGenericTypeDefinition()
            let arguments = type'.GetGenericArguments() |> Array.map typeName
            if definition = typedefof<option<_>> then arguments[0] + " option"
            elif definition = typedefof<list<_>> then arguments[0] + " list"
            elif definition = typedefof<Set<_>> then arguments[0] + " Set"
            elif definition = typedefof<Map<_,_>> then $"Map<{arguments[0]}, {arguments[1]}>"
            elif definition = typedefof<Microsoft.FSharp.Core.FSharpFunc<_,_>> then $"({arguments[0]} -> {arguments[1]})"
            else simpleName type'.Name + "<" + String.concat ", " arguments + ">"
        else simpleName type'.Name

    let private documentation (anchor:Type) =
        let path = IO.Path.ChangeExtension(anchor.Assembly.Location, ".xml")
        if IO.File.Exists path then Some(XDocument.Load path) else None

    let remarks (anchor:Type) =
        documentation anchor
        |> Option.toList
        |> List.collect (fun document ->
            document.Descendants(XName.Get "member")
            |> Seq.choose (fun member' ->
                let name = member'.Attribute(XName.Get "name")
                let remarks = member'.Element(XName.Get "remarks")
                if isNull name || isNull remarks || not (name.Value.StartsWith("T:", StringComparison.Ordinal)) then None
                else
                    let comments =
                        remarks.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        |> Array.map _.Trim()
                        |> Array.filter (String.IsNullOrWhiteSpace >> not)
                        |> Array.map (fun line -> "/// " + line)
                        |> String.concat "\n"
                    Some(name.Value.Substring(2), comments))
            |> Seq.toList)
        |> Map.ofList

    let private opaqueRecord (type':Type) =
        FSharpType.IsRecord(type', true)
        && (FSharpType.GetRecordFields(type', true) |> Array.forall (fun field -> not field.GetMethod.IsPublic))

    let typesInCategory (owner:string) (anchor:Type) : Type list =
        let assembly = anchor.Assembly
        let typesByName = assembly.GetExportedTypes() |> Seq.map (fun type' -> type'.FullName, type') |> Map.ofSeq
        let categoryName = XName.Get "category"
        let memberName = XName.Get "member"
        documentation anchor
        |> Option.toList
        |> List.collect (fun document ->
            document.Descendants(memberName)
            |> Seq.choose (fun member' ->
                let name = member'.Attribute(XName.Get "name")
                let owned =
                    member'.Elements(categoryName)
                    |> Seq.exists (fun category -> String.Equals(category.Value.Trim(), owner, StringComparison.Ordinal))
                if isNull name || not owned || not (name.Value.StartsWith("T:", StringComparison.Ordinal)) then None
                else typesByName |> Map.tryFind (name.Value.Substring(2)))
            |> Seq.toList)
        |> List.distinct
        |> List.sortBy (fun type' -> (if type'.IsAbstract && type'.IsSealed then 2 elif type'.Name.EndsWith("Config", StringComparison.Ordinal) then 1 else 0), type'.MetadataToken)

    let private unionDeclaration (type':Type) =
        let cases =
            FSharpType.GetUnionCases type'
            |> Array.map (fun case ->
                let fields = case.GetFields()
                if Array.isEmpty fields then "    | " + case.Name
                else "    | " + case.Name + " of " + (fields |> Array.map (fun field -> typeName field.PropertyType) |> String.concat " * "))
        "type " + typeName type' + " =\n" + String.concat "\n" cases

    let private recordDeclaration (type':Type) =
        if type'.Name.EndsWith("Config", StringComparison.Ordinal) || opaqueRecord type' then "type " + typeName type'
        else
            let fields = FSharpType.GetRecordFields(type', true)
            let body =
                fields
                |> Array.mapi (fun index field -> (if index = 0 then "    { " else "      ") + field.Name + ": " + typeName field.PropertyType)
                |> String.concat "\n"
            "type " + typeName type' + " =\n" + body + " }"

    let private methodDeclaration (method':MethodInfo) =
        let arguments =
            method'.GetParameters()
            |> Array.map (fun parameter ->
                let name = if String.IsNullOrWhiteSpace parameter.Name then "value" else parameter.Name
                name + ":" + typeName parameter.ParameterType)
            |> Array.toList
        let signature = arguments @ [ typeName method'.ReturnType ] |> String.concat " -> "
        "    val " + method'.Name + " : " + signature

    let declaration (type':Type) =
        if type'.IsAbstract && type'.IsSealed then
            let methods =
                type'.GetMethods(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly)
                |> Array.filter (fun method' -> not method'.IsSpecialName)
                |> Array.sortBy _.MetadataToken
                |> Array.map methodDeclaration
            let moduleName = simpleName type'.Name
            let sourceName = if moduleName.EndsWith("Module", StringComparison.Ordinal) then moduleName.Substring(0, moduleName.Length - "Module".Length) else moduleName
            "module " + sourceName + " =\n" + String.concat "\n" methods
        elif FSharpType.IsUnion type' then unionDeclaration type'
        elif FSharpType.IsRecord(type', true) then recordDeclaration type'
        else "type " + typeName type'

/// <category>fsharp-api-reference</category>
[<RequireQualifiedAccess>]
module FSharpApiReference =
    /// Uses exactly the supplied compiled declarations. Referenced component types remain on their own pages.
    let create (types:Type list) : FSharpApiReferenceConfig =
        if List.isEmpty types then invalidArg (nameof types) "At least one public owning type is required."
        let assembly = (List.head types).Assembly
        if types |> List.exists (fun type' -> type'.Assembly <> assembly) then invalidArg (nameof types) "Owning types must come from one assembly."
        { types = types |> List.distinct }

    /// Selects declarations explicitly assigned to the category in their XML documentation.
    let forCategory owner (anchor:Type) : FSharpApiReferenceConfig =
        if String.IsNullOrWhiteSpace owner then invalidArg (nameof owner) "An API-reference category is required."
        match FSharpApiReflection.typesInCategory owner anchor with
        | [] -> invalidArg (nameof owner) $"No public F# declarations are documented for the '{owner}' category."
        | types -> create types

    /// Renders compiled signatures and declaration-owned XML usage remarks without exposing private config fields.
    let render (config:FSharpApiReferenceConfig) =
        let remarks = FSharpApiReflection.remarks (List.head config.types)
        let source =
            config.types
            |> List.map (fun type' ->
                let comments = remarks |> Map.tryFind type'.FullName |> Option.map (fun value -> value + "\n") |> Option.defaultValue ""
                comments + FSharpApiReflection.declaration type')
            |> String.concat "\n\n"
        div {
            _attr ("data-fve-api-reference", "true")
            _class "grid min-w-0 gap-4"
            CodeBlock.create "fsharp" source |> CodeBlock.render
        }
