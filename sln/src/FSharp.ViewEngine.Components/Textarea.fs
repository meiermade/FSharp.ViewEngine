namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>textarea</category>
[<NoEquality; NoComparison>]
type TextareaConfig = private { field:TextFieldData; rows:int }

/// <category>textarea</category>
[<RequireQualifiedAccess>]
module Textarea =
    let create name label : TextareaConfig = { field = TextField.create "fve-textarea-" name label; rows = 4 }
    let withId id (config:TextareaConfig) = { config with field = { config.field with id = TextField.stableId id } }
    let id (config:TextareaConfig) = config.field.id
    /// Retain the associated accessible label without showing form-field chrome.
    let withVisuallyHiddenLabel (config:TextareaConfig) = { config with field = { config.field with labelVisuallyHidden = true } }
    let withValue value (config:TextareaConfig) = { config with field = { config.field with value = value } }
    let withRows rows (config:TextareaConfig) =
        if rows < 1 then invalidArg (nameof rows) "A textarea needs at least one row."
        { config with rows = rows }
    let withDescription text (config:TextareaConfig) = { config with field = { config.field with description = Some (TextField.requiredText (nameof text) text) } }
    let withValidation text (config:TextareaConfig) = { config with field = { config.field with validation = Some (TextField.requiredText (nameof text) text) } }
    let withAttributes attributes (config:TextareaConfig) = { config with field = { config.field with attributes = config.field.attributes @ attributes } }
    let required (config:TextareaConfig) = { config with field = { config.field with required = true } }
    let disabled (config:TextareaConfig) = { config with field = { config.field with disabled = true } }
    let pending (config:TextareaConfig) = { config with field = { config.field with pending = true } }
    let render (config:TextareaConfig) =
        let control =
            textarea {
                for attribute in TextField.attributes true (TextField.classes config.field) [] config.field do attribute
                _rows config.rows
                config.field.value
            }
        TextField.render config.field control
