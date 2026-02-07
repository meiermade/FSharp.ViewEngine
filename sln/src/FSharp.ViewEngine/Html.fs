namespace FSharp.ViewEngine

open JetBrains.Annotations

type Html =
    static member val EmptyAttr = { Name = null; Value = ValueNone } with get
    static member val empty = NoopElement() :> HtmlElement with get
    static member raw ([<LanguageInjection("html")>]v: string) = RawElement(v) :> HtmlElement
    static member js ([<LanguageInjection("javascript")>]v: string) = RawElement(v) :> HtmlElement
    static member text (v: string) = TextElement(v) :> HtmlElement
    static member val html = TagBuilder("html") with get
    static member val head = TagBuilder("head") with get
    static member title (value: string) =
        let el = RegularElement("title")
        el.AddChild(TextElement(value) :> HtmlElement)
        el :> HtmlElement
    static member val script = TagBuilder("script") with get
    static member val body = TagBuilder("body") with get
    static member val main = TagBuilder("main") with get
    static member val header = TagBuilder("header") with get
    static member val footer = TagBuilder("footer") with get
    static member val nav = TagBuilder("nav") with get
    static member val h1 = TagBuilder("h1") with get
    static member val h2 = TagBuilder("h2") with get
    static member val h3 = TagBuilder("h3") with get
    static member val h4 = TagBuilder("h4") with get
    static member val h5 = TagBuilder("h5") with get
    static member val h6 = TagBuilder("h6") with get
    static member val div = TagBuilder("div") with get
    static member val p = TagBuilder("p") with get
    static member val span = TagBuilder("span") with get
    static member val a = TagBuilder("a") with get
    static member val button = TagBuilder("button") with get
    static member val code = TagBuilder("code") with get
    static member val pre = TagBuilder("pre") with get
    static member val ul = TagBuilder("ul") with get
    static member val ol = TagBuilder("ol") with get
    static member val li = TagBuilder("li") with get
    static member val blockquote = TagBuilder("blockquote") with get
    static member val article = TagBuilder("article") with get
    static member val dialog = TagBuilder("dialog") with get
    static member val time = TagBuilder("time") with get
    static member val form = TagBuilder("form") with get
    static member val label = TagBuilder("label") with get
    static member val textarea = TagBuilder("textarea") with get
    static member val select = TagBuilder("select") with get
    static member val option = TagBuilder("option") with get
    static member val table = TagBuilder("table") with get
    static member val thead = TagBuilder("thead") with get
    static member val tr = TagBuilder("tr") with get
    static member val th = TagBuilder("th") with get
    static member val tbody = TagBuilder("tbody") with get
    static member val td = TagBuilder("td") with get
    static member val dl = TagBuilder("dl") with get
    static member val dt = TagBuilder("dt") with get
    static member val dd = TagBuilder("dd") with get
    static member val template = TagBuilder("template") with get
    static member val iframe = TagBuilder("iframe") with get
    static member val section = TagBuilder("section") with get
    static member val aside = TagBuilder("aside") with get
    static member val figure = TagBuilder("figure") with get
    static member val figcaption = TagBuilder("figcaption") with get
    static member val details = TagBuilder("details") with get
    static member val summary = TagBuilder("summary") with get
    static member val strong = TagBuilder("strong") with get
    static member val em = TagBuilder("em") with get
    static member val b = TagBuilder("b") with get
    static member val i = TagBuilder("i") with get
    static member val u = TagBuilder("u") with get
    static member val s = TagBuilder("s") with get
    static member val small = TagBuilder("small") with get
    static member val mark = TagBuilder("mark") with get
    static member val sub = TagBuilder("sub") with get
    static member val sup = TagBuilder("sup") with get
    static member val abbr = TagBuilder("abbr") with get
    static member val cite = TagBuilder("cite") with get
    static member val q = TagBuilder("q") with get
    static member val dfn = TagBuilder("dfn") with get
    static member val var = TagBuilder("var") with get
    static member val samp = TagBuilder("samp") with get
    static member val kbd = TagBuilder("kbd") with get
    static member val ins = TagBuilder("ins") with get
    static member val del = TagBuilder("del") with get
    static member val address = TagBuilder("address") with get
    static member val hgroup = TagBuilder("hgroup") with get
    static member val search = TagBuilder("search") with get
    static member val noscript = TagBuilder("noscript") with get
    static member val slot = TagBuilder("slot") with get
    static member val data = TagBuilder("data") with get
    static member val video = TagBuilder("video") with get
    static member val audio = TagBuilder("audio") with get
    static member val picture = TagBuilder("picture") with get
    static member val canvas = TagBuilder("canvas") with get
    static member val object = TagBuilder("object") with get
    static member val fieldset = TagBuilder("fieldset") with get
    static member val legend = TagBuilder("legend") with get
    static member val datalist = TagBuilder("datalist") with get
    static member val output = TagBuilder("output") with get
    static member val progress = TagBuilder("progress") with get
    static member val meter = TagBuilder("meter") with get
    static member val caption = TagBuilder("caption") with get
    static member val colgroup = TagBuilder("colgroup") with get
    static member val tfoot = TagBuilder("tfoot") with get
    static member val map = TagBuilder("map") with get
    static member val br = VoidElement("br") :> HtmlElement with get
    static member val hr = VoidElement("hr") :> HtmlElement with get
    static member val wbr = VoidElement("wbr") :> HtmlElement with get
    static member val meta = VoidBuilder("meta") with get
    static member val link = VoidBuilder("link") with get
    static member val img = VoidBuilder("img") with get
    static member val input = VoidBuilder("input") with get
    static member val source = VoidBuilder("source") with get
    static member val track = VoidBuilder("track") with get
    static member val col = VoidBuilder("col") with get
    static member val area = VoidBuilder("area") with get
    static member val embed = VoidBuilder("embed") with get

    static member inline _id (v: string) = { Name = "id"; Value = ValueSome v }
    static member inline _class (v: string) = { Name = "class"; Value = ValueSome v }
    static member inline _class (v: string seq) = { Name = "class"; Value = ValueSome(v |> String.concat " ") }
    static member inline _style (v: string) = { Name = "style"; Value = ValueSome v }
    static member inline _lang (v: string) = { Name = "lang"; Value = ValueSome v }
    static member inline _charset (v: string) = { Name = "charset"; Value = ValueSome v }
    static member inline _name (v: string) = { Name = "name"; Value = ValueSome v }
    static member inline _content (v: string) = { Name = "content"; Value = ValueSome v }
    static member inline _href (v: string) = { Name = "href"; Value = ValueSome v }
    static member inline _rel (v: string) = { Name = "rel"; Value = ValueSome v }
    static member inline _src (v: string) = { Name = "src"; Value = ValueSome v }
    static member inline _async (v: bool) = if v then { Name = "async"; Value = ValueNone } else Html.EmptyAttr
    static member inline _defer (v: bool) = if v then { Name = "defer"; Value = ValueNone } else Html.EmptyAttr
    static member inline _action (v: string) = { Name = "action"; Value = ValueSome v }
    static member inline _method (v: string) = { Name = "method"; Value = ValueSome v }
    static member inline _formmethod (v: string) = { Name = "formmethod"; Value = ValueSome v }
    static member inline _type (v: string) = { Name = "type"; Value = ValueSome v }
    static member inline _for (v: string) = { Name = "for"; Value = ValueSome v }
    static member inline _rows (v: int) = { Name = "rows"; Value = ValueSome(string v) }
    static member inline _cols (v: int) = { Name = "cols"; Value = ValueSome(string v) }
    static member inline _data (attr: string, ?v: string) =
        let key = $"data-{attr}"
        match v with
        | Some v -> { Name = key; Value = ValueSome v }
        | None -> { Name = key; Value = ValueNone }
    static member inline _datetime (v: string) = { Name = "datetime"; Value = ValueSome v }
    static member inline _width (v: string) = { Name = "width"; Value = ValueSome v }
    static member inline _height (v: string) = { Name = "height"; Value = ValueSome v }
    static member inline _value (v: string) = { Name = "value"; Value = ValueSome v }
    static member inline _hidden (v: bool) = if v then { Name = "hidden"; Value = ValueNone } else Html.EmptyAttr
    static member inline _required (v: bool) = if v then { Name = "required"; Value = ValueNone } else Html.EmptyAttr
    static member inline _disabled (v: bool) = if v then { Name = "disabled"; Value = ValueNone } else Html.EmptyAttr
    static member inline _readonly (v: bool) = if v then { Name = "readonly"; Value = ValueNone } else Html.EmptyAttr
    static member inline _multiple (v: bool) = if v then { Name = "multiple"; Value = ValueNone } else Html.EmptyAttr
    static member inline _selected (v: bool) = if v then { Name = "selected"; Value = ValueNone } else Html.EmptyAttr
    static member inline _min (v: string) = { Name = "min"; Value = ValueSome v }
    static member inline _min (v: float) = { Name = "min"; Value = ValueSome(string v) }
    static member inline _minlength (v: string) = { Name = "minlength"; Value = ValueSome v }
    static member inline _minlength (v: int) = { Name = "minlength"; Value = ValueSome(string v) }
    static member inline _max (v: string) = { Name = "max"; Value = ValueSome v }
    static member inline _max (v: float) = { Name = "max"; Value = ValueSome(string v) }
    static member inline _maxlength (v: string) = { Name = "maxlength"; Value = ValueSome v }
    static member inline _maxlength (v: int) = { Name = "maxlength"; Value = ValueSome(string v) }
    static member inline _step (v: string) = { Name = "step"; Value = ValueSome v }
    static member inline _step (v: float) = { Name = "step"; Value = ValueSome(string v) }
    static member inline _checked (v: bool) = if v then { Name = "checked"; Value = ValueNone } else Html.EmptyAttr
    static member inline _role (v: string) = { Name = "role"; Value = ValueSome v }
    static member inline _ariaLabelledby (v: string) = { Name = "aria-labelledby"; Value = ValueSome v }
    static member inline _ariaDescribedby (v: string) = { Name = "aria-describedby"; Value = ValueSome v }
    static member inline _ariaModal (v: string) = { Name = "aria-modal"; Value = ValueSome v }
    static member inline _placeholder (v: string) = { Name = "placeholder"; Value = ValueSome v }
    static member inline _autocomplete (v: string) = { Name = "autocomplete"; Value = ValueSome v }
    static member inline _pattern (v: string) = { Name = "pattern"; Value = ValueSome v }
    static member inline _accept (v: string) = { Name = "accept"; Value = ValueSome v }
    static member inline _title (v: string) = { Name = "title"; Value = ValueSome v }
    static member inline _wrap (v: string) = { Name = "wrap"; Value = ValueSome v }
    static member inline _size (v: int) = { Name = "size"; Value = ValueSome(string v) }
    static member inline _colspan (v: int) = { Name = "colspan"; Value = ValueSome(string v) }
    static member inline _onload (v: string) = { Name = "onload"; Value = ValueSome v }
    static member inline _crossorigin = { Name = "crossorigin"; Value = ValueNone }
    static member inline _alt (v: string) = { Name = "alt"; Value = ValueSome v }
    static member inline _target (v: string) = { Name = "target"; Value = ValueSome v }
    static member inline _tabindex (v: int) = { Name = "tabindex"; Value = ValueSome(string v) }
    static member inline _autofocus (v: bool) = if v then { Name = "autofocus"; Value = ValueNone } else Html.EmptyAttr
    static member inline _open (v: bool) = if v then { Name = "open"; Value = ValueNone } else Html.EmptyAttr
    static member inline _loading (v: string) = { Name = "loading"; Value = ValueSome v }
    static member inline _srcset (v: string) = { Name = "srcset"; Value = ValueSome v }
    static member inline _sandbox (v: string) = { Name = "sandbox"; Value = ValueSome v }
    static member inline _allow (v: string) = { Name = "allow"; Value = ValueSome v }
    static member inline _enctype (v: string) = { Name = "enctype"; Value = ValueSome v }
    static member inline _novalidate (v: bool) = if v then { Name = "novalidate"; Value = ValueNone } else Html.EmptyAttr
    static member inline _spellcheck (v: bool) = { Name = "spellcheck"; Value = ValueSome(if v then "true" else "false") }
    static member inline _draggable (v: bool) = { Name = "draggable"; Value = ValueSome(if v then "true" else "false") }
    static member inline _contenteditable (v: bool) = { Name = "contenteditable"; Value = ValueSome(if v then "true" else "false") }
    static member inline _accesskey (v: string) = { Name = "accesskey"; Value = ValueSome v }
    static member inline _dir (v: string) = { Name = "dir"; Value = ValueSome v }
    static member inline _translate (v: bool) = { Name = "translate"; Value = ValueSome(if v then "yes" else "no") }
    static member inline _inputmode (v: string) = { Name = "inputmode"; Value = ValueSome v }
    static member inline _enterkeyhint (v: string) = { Name = "enterkeyhint"; Value = ValueSome v }
    static member inline _list (v: string) = { Name = "list"; Value = ValueSome v }
    static member inline _form (v: string) = { Name = "form"; Value = ValueSome v }
    static member inline _formaction (v: string) = { Name = "formaction"; Value = ValueSome v }
    static member inline _formenctype (v: string) = { Name = "formenctype"; Value = ValueSome v }
    static member inline _formnovalidate (v: bool) = if v then { Name = "formnovalidate"; Value = ValueNone } else Html.EmptyAttr
    static member inline _formtarget (v: string) = { Name = "formtarget"; Value = ValueSome v }
    static member inline _ariaLabel (v: string) = { Name = "aria-label"; Value = ValueSome v }
    static member inline _ariaHidden (v: string) = { Name = "aria-hidden"; Value = ValueSome v }
    static member inline _ariaExpanded (v: string) = { Name = "aria-expanded"; Value = ValueSome v }
    static member inline _ariaControls (v: string) = { Name = "aria-controls"; Value = ValueSome v }
    static member inline _ariaLive (v: string) = { Name = "aria-live"; Value = ValueSome v }
    static member inline _ariaCurrent (v: string) = { Name = "aria-current"; Value = ValueSome v }
    static member inline _rowspan (v: int) = { Name = "rowspan"; Value = ValueSome(string v) }
    static member inline _scope (v: string) = { Name = "scope"; Value = ValueSome v }
    static member inline _headers (v: string) = { Name = "headers"; Value = ValueSome v }
    static member inline _download (v: string) = { Name = "download"; Value = ValueSome v }
    static member inline _download () = { Name = "download"; Value = ValueNone }
    static member inline _referrerpolicy (v: string) = { Name = "referrerpolicy"; Value = ValueSome v }
    static member inline _media (v: string) = { Name = "media"; Value = ValueSome v }
    static member inline _sizes (v: string) = { Name = "sizes"; Value = ValueSome v }
    static member inline _poster (v: string) = { Name = "poster"; Value = ValueSome v }
    static member inline _controls (v: bool) = if v then { Name = "controls"; Value = ValueNone } else Html.EmptyAttr
    static member inline _autoplay (v: bool) = if v then { Name = "autoplay"; Value = ValueNone } else Html.EmptyAttr
    static member inline _loop (v: bool) = if v then { Name = "loop"; Value = ValueNone } else Html.EmptyAttr
    static member inline _muted (v: bool) = if v then { Name = "muted"; Value = ValueNone } else Html.EmptyAttr
    static member inline _preload (v: string) = { Name = "preload"; Value = ValueSome v }
    static member inline _start (v: int) = { Name = "start"; Value = ValueSome(string v) }
    static member inline _reversed (v: bool) = if v then { Name = "reversed"; Value = ValueNone } else Html.EmptyAttr
    static member inline _cite (v: string) = { Name = "cite"; Value = ValueSome v }

