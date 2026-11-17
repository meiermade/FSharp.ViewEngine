namespace Docs.Common

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar

module SourceListsExamples =
    let shellSource =
        [ "ShellDestination"; "shellDestinationKey"; "shellDestinationUrl" ]

    let formSource = [ "choiceSubmitButton"; "choiceResult" ]

    let selectSource = [ "selectStatusOptions" ]

    let comboboxSource = [ "searchItems"; "itemOptions" ]

    let sideNavSource = [ "nestedSideNav"; "sideNavExample" ]
