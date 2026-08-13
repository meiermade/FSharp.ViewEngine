namespace FSharp.ViewEngine

open System
open System.Buffers
open System.Runtime.CompilerServices
open System.IO
open System.Text

/// An HTML attribute name and optional encoded value. ValueNone renders a valueless boolean attribute.
[<Struct>]
type HtmlAttribute = { Name: string; Value: string voption }

/// Base type for renderable HTML nodes.
[<AbstractClass>]
type HtmlElement() =
    /// Appends this node's serialized HTML to the supplied StringBuilder.
    abstract Render: StringBuilder -> unit

module private HtmlEncoding =
    [<Literal>]
    let private UnicodeReplacementChar = '\uFFFD'

    let private htmlAsciiNonEncodingChars =
        SearchValues.Create(
            "\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !#$%()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f"
        )

    let rec private findEncodingCharLoop i (input: ReadOnlySpan<char>) =
        if i < input.Length then
            let ch = input[i]
            if ch <= '>' then
                if ch = '<' || ch = '>' || ch = '"' || ch = '\'' || ch = '&' then
                    i
                else
                    findEncodingCharLoop (i + 1) input
            elif Char.IsBetween(ch, '\u00A0', '\u00FF') || Char.IsSurrogate(ch) then
                i
            else
                findEncodingCharLoop (i + 1) input
        else
            -1

    let private indexOfHtmlEncodingChar (input: ReadOnlySpan<char>) =
        match input.IndexOfAnyExcept(htmlAsciiNonEncodingChars) with
        | -1 -> -1
        | index -> findEncodingCharLoop index input

    let private htmlEncodeInner (input: ReadOnlySpan<char>) (sb: StringBuilder) : unit =
        let mutable i = 0
        while i < input.Length do
            let ch = input[i]
            i <- i + 1
            if ch <= '>' then
                if '<' = ch then sb.Append "&lt;"
                elif '>' = ch then sb.Append "&gt;"
                elif '"' = ch then sb.Append "&quot;"
                elif '\'' = ch then sb.Append "&#39;"
                elif '&' = ch then sb.Append "&amp;"
                else sb.Append ch
            else if Char.IsBetween(ch, '\u00A0', '\u00FF') then
                sb.Append("&#").Append(int ch).Append(';')
            elif Char.IsSurrogate(ch) then
                if i < input.Length then
                    match Rune.TryCreate(ch, input[i]) with
                    | true, rune ->
                        i <- i + 1
                        sb.Append("&#").Append(rune.Value).Append(';')
                    | _ ->
                        sb.Append(UnicodeReplacementChar)
                else
                    sb.Append(UnicodeReplacementChar)
            else
                sb.Append(ch)
            |> ignore

    let htmlEncode (value: string) (sb: StringBuilder) =
        if isNull value then
            ()
        else
            let value = value.AsSpan()
            match indexOfHtmlEncodingChar value with
            | -1 -> sb.Append(value) |> ignore
            | index ->
                sb.Append(value.Slice(0, index)) |> ignore
                htmlEncodeInner (value.Slice index) sb

type TextElement(text: string) =
    inherit HtmlElement()
    override _.Render(sb) = HtmlEncoding.htmlEncode text sb

type RawElement(text: string) =
    inherit HtmlElement()
    override _.Render(sb) = sb.Append(text) |> ignore

type NoopElement() =
    inherit HtmlElement()
    override _.Render(_) = ()

/// A renderable sequence of sibling nodes.
type FragmentElement(elements:HtmlElement seq) =
    inherit HtmlElement()
    let elements = elements |> Seq.toArray
    override _.Render(sb) =
        for element in elements do element.Render(sb)

/// A validated HTML comment node.
type CommentElement(value:string) =
    inherit HtmlElement()
    do
        if isNull value || value.Contains("--") || value.EndsWith("-", StringComparison.Ordinal) then
            invalidArg (nameof value) "HTML comments cannot be null, contain '--', or end with '-'."
    override _.Render(sb) = sb.Append("<!--").Append(value).Append("-->") |> ignore

module private RenderHelpers =
    let inline renderAttr (sb: StringBuilder) (a: HtmlAttribute) =
        match a.Value with
        | ValueSome v ->
            sb.Append(' ').Append(a.Name).Append("=\"") |> ignore
            HtmlEncoding.htmlEncode v sb
            sb.Append('\"') |> ignore
        | ValueNone -> sb.Append(' ').Append(a.Name) |> ignore

type VoidElement(tag: string) =
    inherit HtmlElement()
    let mutable attrCount = 0
    let mutable attr0 = Unchecked.defaultof<HtmlAttribute>
    let mutable attr1 = Unchecked.defaultof<HtmlAttribute>
    let mutable attrRest: ResizeArray<HtmlAttribute> = null
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AddAttr(a: HtmlAttribute) =
        match attrCount with
        | 0 ->
            attr0 <- a
            attrCount <- 1
        | 1 ->
            attr1 <- a
            attrCount <- 2
        | _ ->
            if isNull attrRest then
                attrRest <- ResizeArray()
            attrRest.Add(a)
            attrCount <- attrCount + 1
    override _.Render(sb) =
        sb.Append('<').Append(tag) |> ignore
        match attrCount with
        | 0 -> ()
        | 1 -> RenderHelpers.renderAttr sb attr0
        | 2 ->
            RenderHelpers.renderAttr sb attr0
            RenderHelpers.renderAttr sb attr1
        | _ ->
            RenderHelpers.renderAttr sb attr0
            RenderHelpers.renderAttr sb attr1
            let rest = attrRest
            if not (isNull rest) then
                for i = 0 to rest.Count - 1 do
                    RenderHelpers.renderAttr sb rest[i]
        sb.Append('>') |> ignore

type RegularElement(tag: string) =
    inherit HtmlElement()
    let mutable attrCount = 0
    let mutable attr0 = Unchecked.defaultof<HtmlAttribute>
    let mutable attr1 = Unchecked.defaultof<HtmlAttribute>
    let mutable attrRest: ResizeArray<HtmlAttribute> = null
    let mutable childCount = 0
    let mutable child0 = Unchecked.defaultof<HtmlElement>
    let mutable child1 = Unchecked.defaultof<HtmlElement>
    let mutable childRest: ResizeArray<HtmlElement> = null
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AddAttr(a: HtmlAttribute) =
        match attrCount with
        | 0 ->
            attr0 <- a
            attrCount <- 1
        | 1 ->
            attr1 <- a
            attrCount <- 2
        | _ ->
            if isNull attrRest then
                attrRest <- ResizeArray()
            attrRest.Add(a)
            attrCount <- attrCount + 1
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AddChild(c: HtmlElement) =
        match childCount with
        | 0 ->
            child0 <- c
            childCount <- 1
        | 1 ->
            child1 <- c
            childCount <- 2
        | _ ->
            if isNull childRest then
                childRest <- ResizeArray()
            childRest.Add(c)
            childCount <- childCount + 1
    override _.Render(sb) =
        sb.Append('<').Append(tag) |> ignore
        match attrCount with
        | 0 -> ()
        | 1 -> RenderHelpers.renderAttr sb attr0
        | 2 ->
            RenderHelpers.renderAttr sb attr0
            RenderHelpers.renderAttr sb attr1
        | _ ->
            RenderHelpers.renderAttr sb attr0
            RenderHelpers.renderAttr sb attr1
            let rest = attrRest
            if not (isNull rest) then
                for i = 0 to rest.Count - 1 do
                    RenderHelpers.renderAttr sb rest[i]
        sb.Append('>') |> ignore
        match childCount with
        | 0 -> ()
        | 1 -> child0.Render(sb)
        | 2 ->
            child0.Render(sb)
            child1.Render(sb)
        | _ ->
            child0.Render(sb)
            child1.Render(sb)
            let rest = childRest
            if not (isNull rest) then
                for i = 0 to rest.Count - 1 do
                    rest[i].Render(sb)
        sb.Append("</").Append(tag).Append('>') |> ignore

type private StringBuilderPool =
    [<Literal>]
    static let MaximumRetainedCapacity = 262_144

    [<ThreadStatic; DefaultValue>]
    static val mutable private pooled: StringBuilder

    static member inline Rent() =
        let sb = StringBuilderPool.pooled
        if isNull sb then
            StringBuilder()
        else
            StringBuilderPool.pooled <- null
            sb

    static member inline Return(sb: StringBuilder) =
        if sb.Capacity <= MaximumRetainedCapacity then
            sb.Clear() |> ignore
            if isNull StringBuilderPool.pooled then
                StringBuilderPool.pooled <- sb

type TagBuilderCode = RegularElement -> unit
type VoidBuilderCode = VoidElement -> unit

/// Computation-expression builder for a regular element that accepts attributes, text, and child elements.
type TagBuilder(tag: string) =
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Yield(el: HtmlElement) : TagBuilderCode =
        fun st -> st.AddChild(el)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Yield(text: string) : TagBuilderCode =
        fun st -> st.AddChild(TextElement(text) :> HtmlElement)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Yield(attr: HtmlAttribute) : TagBuilderCode =
        fun st ->
            if not (isNull attr.Name) then
                st.AddAttr(attr)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Zero() : TagBuilderCode =
        fun _ -> ()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Combine([<InlineIfLambda>] f1: TagBuilderCode, [<InlineIfLambda>] f2: TagBuilderCode) : TagBuilderCode =
        fun st -> f1 st; f2 st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Delay([<InlineIfLambda>] f: unit -> TagBuilderCode) : TagBuilderCode =
        fun st -> (f ()) st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.For(xs: 'a seq, [<InlineIfLambda>] f: 'a -> TagBuilderCode) : TagBuilderCode =
        fun st ->
            for x in xs do
                (f x) st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member _.Run(f: TagBuilderCode) : HtmlElement =
        let el = RegularElement(tag)
        f el
        el :> HtmlElement


/// Computation-expression builder for a void element that accepts attributes and renders no closing tag.
type VoidBuilder(tag: string) =
    inherit VoidElement(tag)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Yield(attr: HtmlAttribute) : VoidBuilderCode =
        fun st ->
            if not (isNull attr.Name) then
                st.AddAttr(attr)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Zero() : VoidBuilderCode =
        fun _ -> ()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Combine([<InlineIfLambda>] f1: VoidBuilderCode, [<InlineIfLambda>] f2: VoidBuilderCode) : VoidBuilderCode =
        fun st -> f1 st; f2 st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.Delay([<InlineIfLambda>] f: unit -> VoidBuilderCode) : VoidBuilderCode =
        fun st -> (f ()) st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member inline _.For(xs: 'a seq, [<InlineIfLambda>] f: 'a -> VoidBuilderCode) : VoidBuilderCode =
        fun st ->
            for x in xs do
                (f x) st

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member _.Run(f: VoidBuilderCode) : HtmlElement =
        let el = VoidElement(tag)
        f el
        el :> HtmlElement

/// Functions for serializing completed element trees.
[<RequireQualifiedAccess>]
module Render =
    /// Appends an element directly to an existing StringBuilder.
    let writeToStringBuilder (builder:StringBuilder) (element:HtmlElement) =
        if isNull builder then nullArg (nameof builder)
        element.Render(builder)

    /// Writes an element to a TextWriter without closing the writer.
    let writeToTextWriter (writer:TextWriter) (element:HtmlElement) =
        if isNull writer then nullArg (nameof writer)
        let builder = StringBuilderPool.Rent()
        try
            element.Render(builder)
            writer.Write(builder.ToString())
        finally
            StringBuilderPool.Return(builder)

    /// Serializes an element without adding a document type declaration.
    let toString (element: HtmlElement) =
        let sb = StringBuilderPool.Rent()
        try
            element.Render(sb)
            sb.ToString()
        finally
            StringBuilderPool.Return(sb)

    /// Serializes an element as UTF-8 bytes without a byte-order mark.
    let toUtf8Bytes (element:HtmlElement) : byte array =
        element |> toString |> Encoding.UTF8.GetBytes

    /// Serializes a complete HTML document and prepends the HTML5 doctype.
    let toHtmlDocString (view: #HtmlElement) =
        let sb = StringBuilderPool.Rent()
        sb.AppendLine("<!DOCTYPE html>") |> ignore
        try
            view.Render(sb)
            sb.ToString()
        finally
            StringBuilderPool.Return(sb)
