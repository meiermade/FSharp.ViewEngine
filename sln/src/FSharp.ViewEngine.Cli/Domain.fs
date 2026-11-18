namespace FSharp.ViewEngine.Cli

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions

[<CLIMutable>]
type InstalledFile =
    { Component:string
      Path:string
      RegistryVersion:string
      RegistryChecksum:string }

[<CLIMutable>]
type Configuration =
    { SchemaVersion:int
      RegistryVersion:string
      Project:string
      Namespace:string
      Components:string array
      Files:InstalledFile array
      Modules:string array }

[<NoEquality; NoComparison>]
type RegistryFile =
    { Component:string
      FileName:string
      Compile:bool
      Module:bool
      Source:string }

[<NoEquality; NoComparison>]
type RegistryComponent =
    { Name:string
      Dependencies:string list
      Internal:bool
      Files:RegistryFile list }

[<NoEquality; NoComparison>]
type ComponentRegistry =
    { Version:string
      Components:RegistryComponent list }

[<RequireQualifiedAccess>]
module Text =
    let normalize (value:string) = value.Replace("\r\n", "\n").Replace("\r", "\n")

    let checksum (value:string) =
        value
        |> normalize
        |> Encoding.UTF8.GetBytes
        |> SHA256.HashData
        |> Convert.ToHexString
        |> _.ToLowerInvariant()

    let writeAtomic (path:string) (content:string) =
        let directory = Path.GetDirectoryName path
        Directory.CreateDirectory directory |> ignore
        let temporary = path + "." + Guid.NewGuid().ToString("N") + ".fve-tmp"
        use staged = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)
        try
            staged.Write(Encoding.UTF8.GetBytes(normalize content))
            staged.Dispose()
            File.Move(temporary, path, true)
        finally File.Delete temporary

[<RequireQualifiedAccess>]
module Validation =
    let private namespacePattern = Regex("^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.CultureInvariant)

    let namespaceName (value:string) =
        if String.IsNullOrWhiteSpace value || not (namespacePattern.IsMatch value) then
            Error $"'{value}' is not a valid F# namespace."
        else Ok value

    /// Reject traversal and symbolic links below the selected consumer root.
    let consumerPaths (root:string) (paths:string seq) =
        try
            let root = Path.GetFullPath root
            let validate (value:string) =
                if String.IsNullOrWhiteSpace value then invalidArg "path" "Consumer paths cannot be empty."
                let parts = value.Split('/')
                // Manifests are portable forward-slash paths. Do not validate a normalized path and then use a different Unix filename.
                if Path.IsPathRooted value || value.Contains(':') || value.Contains('\\')
                   || (parts |> Array.exists (fun part -> part = "" || part = "." || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal))) then
                    invalidArg "path" $"Consumer path '{value}' must stay within the consumer directory."
                let mutable current = root
                for part in parts do
                    current <- Path.Combine(current, part)
                    let entry:FileSystemInfo =
                        if Directory.Exists current then DirectoryInfo(current) else FileInfo(current)
                    if not (isNull entry.LinkTarget) then
                        invalidArg "path" $"Consumer path '{value}' contains a symbolic link."
            for path in paths do validate path
            Ok ()
        with :? ArgumentException as error -> Error error.Message
           | :? IOException as error -> Error error.Message
           | :? UnauthorizedAccessException as error -> Error error.Message

    let projectPath (value:string) =
        if String.IsNullOrWhiteSpace value || not (List.exists (fun extension -> String.Equals(Path.GetExtension value, extension, StringComparison.OrdinalIgnoreCase)) [".fsproj"; ".fsx"]) then
            Error "A .fsproj or .fsx path is required."
        else Ok (Path.GetFullPath value)
