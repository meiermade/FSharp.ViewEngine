namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>message</category>
[<RequireQualifiedAccess>]
type MessageSide = Sender | Receiver

/// <category>message</category>
[<AutoOpen>]
module MessageAttributes =
    type Html with
        static member _side(value:MessageSide) = Html._attr("side", if value = MessageSide.Sender then "sender" else "receiver")

module internal Message =
    let part name tag (classes:string) attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("Message", name, ComponentElement.render tag [_class classes] attributes children) :> HtmlElement

    let render attributes children =
        let sender = ComponentElement.required "label" attributes
        let sent = ComponentElement.choice "side" "receiver" ["sender"; "receiver"] attributes = "sender"
        let parts, body = ComponentParts.split "Message" children
        ComponentElement.render "article" [_ariaLabel ("Message from " + sender); _class ("flex min-w-0 items-end gap-3 " + if sent then "ml-auto flex-row-reverse" else "mr-auto")]
            (ComponentHtml.safeAttributes ["label"; "side"] attributes) [
                match Map.tryFind "avatar" parts with Some avatar -> yield avatar | None -> ()
                yield div {
                    _class ("grid min-w-0 max-w-[min(42rem,85%)] gap-1" + if sent then " justify-items-end" else "")
                    div {
                        _class ("min-w-0 [overflow-wrap:anywhere] rounded-2xl px-4 py-2.5 text-sm leading-6 shadow-sm " + if sent then "rounded-br-sm bg-[var(--fve-brand-solid)] text-white" else "rounded-bl-sm bg-[var(--fve-surface-subtle)] text-[var(--fve-text)] ring-1 ring-[var(--fve-border)]")
                        body
                    }
                    if ["metadata"; "status"; "actions"] |> List.exists (fun key -> Map.containsKey key parts) then
                        footer {
                            _class ("flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1 px-1 text-xs text-[var(--fve-muted-text)]" + if sent then " justify-end" else "")
                            for key in ["metadata"; "status"; "actions"] do
                                match Map.tryFind key parts with Some part -> part | None -> ()
                        }
                } ]

/// <category>message</category>
[<AutoOpen>]
module MessageDsl =
    let fveMessage = ComponentElementBuilder(Message.render)
    let fveMessageAvatar = ComponentElementBuilder(Message.part "avatar" "div" "shrink-0 self-start")
    let fveMessageMetadata = ComponentElementBuilder(Message.part "metadata" "span" "")
    let fveMessageStatus = ComponentElementBuilder(Message.part "status" "span" "")
    let fveMessageActions = ComponentElementBuilder(Message.part "actions" "div" "flex items-center gap-1")
