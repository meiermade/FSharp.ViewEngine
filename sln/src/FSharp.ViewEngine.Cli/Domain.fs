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
      Files:InstalledFile array }

[<NoEquality; NoComparison>]
type RegistryComponent =
    { Name:string
      FileName:string
      Dependencies:string list
      Compile:bool
      Source:string }

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
        let temporary = path + ".fve-tmp"
        File.WriteAllText(temporary, normalize content, UTF8Encoding(false))
        File.Move(temporary, path, true)

[<RequireQualifiedAccess>]
module Validation =
    let private namespacePattern = Regex("^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.CultureInvariant)

    let namespaceName (value:string) =
        if String.IsNullOrWhiteSpace value || not (namespacePattern.IsMatch value) then
            Error $"'{value}' is not a valid F# namespace."
        else Ok value

    let projectPath (value:string) =
        if String.IsNullOrWhiteSpace value || not (String.Equals(Path.GetExtension value, ".fsproj", StringComparison.OrdinalIgnoreCase)) then
            Error "A .fsproj path is required."
        else Ok (Path.GetFullPath value)
