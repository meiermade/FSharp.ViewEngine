namespace FSharp.ViewEngine

open System
open System.Globalization
open JetBrains.Annotations

/// Standard HTML element builders, attributes, and explicit custom-markup escape hatches.
type Html =
    /// An ignored attribute value, useful for conditional attribute expressions.
    static member val EmptyAttr = { Name = null; Value = ValueNone } with get
    /// A renderable node that emits no output.
    static member val empty = NoopElement() :> HtmlElement with get
    /// Emits trusted HTML without encoding. Never pass user-controlled content.
    static member raw ([<LanguageInjection("html")>]v: string) = RawElement(v) :> HtmlElement
    /// Emits trusted JavaScript without encoding. Never pass user-controlled content.
    static member js ([<LanguageInjection("javascript")>]v: string) = RawElement(v) :> HtmlElement
    /// Creates an HTML-encoded text node.
    static member text (v: string) = TextElement(v) :> HtmlElement
    /// Creates a renderable sequence of sibling nodes.
    static member fragment (elements:HtmlElement seq) = FragmentElement(elements) :> HtmlElement
    /// Creates a validated HTML comment. Values containing `--` or ending in `-` are rejected.
    static member comment (value:string) = CommentElement(value) :> HtmlElement
    /// Creates a regular custom element. The developer-controlled name is emitted without validation.
    static member el (name: string) = TagBuilder(name)
    /// Creates a void custom element. The developer-controlled name is emitted without validation.
    static member elVoid (name: string) = VoidBuilder(name)
    static member val html = TagBuilder("html") with get
    static member val head = TagBuilder("head") with get
    static member title (value: string) =
        let el = RegularElement("title")
        el.AddChild(TextElement(value) :> HtmlElement)
        el :> HtmlElement
    static member val titleBuilder = TagBuilder("title") with get
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
    static member val selectedcontent = TagBuilder("selectedcontent") with get
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
    static member val ruby = TagBuilder("ruby") with get
    static member val rt = TagBuilder("rt") with get
    static member val rp = TagBuilder("rp") with get
    static member val bdi = TagBuilder("bdi") with get
    static member val bdo = TagBuilder("bdo") with get
    static member val optgroup = TagBuilder("optgroup") with get
    static member val menu = TagBuilder("menu") with get
    [<Obsolete("The portal element is not part of the HTML Living Standard. Use Html.el \"portal\" only when intentionally targeting a non-standard implementation.")>]
    static member val portal = TagBuilder("portal") with get
    static member val style = TagBuilder("style") with get
    static member val br = VoidBuilder("br") with get
    static member val hr = VoidBuilder("hr") with get
    static member val wbr = VoidBuilder("wbr") with get
    static member val meta = VoidBuilder("meta") with get
    static member val link = VoidBuilder("link") with get
    static member val img = VoidBuilder("img") with get
    static member val input = VoidBuilder("input") with get
    static member val source = VoidBuilder("source") with get
    static member val track = VoidBuilder("track") with get
    static member val col = VoidBuilder("col") with get
    static member val area = VoidBuilder("area") with get
    static member val embed = VoidBuilder("embed") with get
    static member val ``base`` = VoidBuilder("base") with get

    // Custom attributes
    static member inline _attr (name: string) = { Name = name; Value = ValueNone }
    static member inline _attr (name: string, v: string) = { Name = name; Value = ValueSome v }

    // Global attributes
    static member inline _id (v: string) = { Name = "id"; Value = ValueSome v }
    static member inline _class (v: string) = { Name = "class"; Value = ValueSome v }
    static member inline _class (v: string seq) = { Name = "class"; Value = ValueSome(v |> String.concat " ") }
    static member inline _style (v: string) = { Name = "style"; Value = ValueSome v }
    static member inline _title (v: string) = { Name = "title"; Value = ValueSome v }
    static member inline _lang (v: string) = { Name = "lang"; Value = ValueSome v }
    static member inline _dir (v: string) = { Name = "dir"; Value = ValueSome v }
    static member inline _hidden (v: bool) = if v then { Name = "hidden"; Value = ValueNone } else Html.EmptyAttr
    static member inline _tabindex (v: int) = { Name = "tabindex"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _accesskey (v: string) = { Name = "accesskey"; Value = ValueSome v }
    static member inline _autocapitalize (v: string) = { Name = "autocapitalize"; Value = ValueSome v }
    static member inline _autocorrect (v: string) = { Name = "autocorrect"; Value = ValueSome v }
    static member inline _headingoffset (v: int) = { Name = "headingoffset"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _headingreset (v: bool) = if v then { Name = "headingreset"; Value = ValueNone } else Html.EmptyAttr
    static member inline _writingsuggestions (v: bool) = { Name = "writingsuggestions"; Value = ValueSome(if v then "true" else "false") }
    static member inline _translate (v: bool) = { Name = "translate"; Value = ValueSome(if v then "yes" else "no") }
    static member inline _spellcheck (v: bool) = { Name = "spellcheck"; Value = ValueSome(if v then "true" else "false") }
    static member inline _draggable (v: bool) = { Name = "draggable"; Value = ValueSome(if v then "true" else "false") }
    static member inline _contenteditable (v: bool) = { Name = "contenteditable"; Value = ValueSome(if v then "true" else "false") }
    static member inline _autofocus (v: bool) = if v then { Name = "autofocus"; Value = ValueNone } else Html.EmptyAttr
    static member inline _inert (v: bool) = if v then { Name = "inert"; Value = ValueNone } else Html.EmptyAttr
    static member inline _inputmode (v: string) = { Name = "inputmode"; Value = ValueSome v }
    static member inline _enterkeyhint (v: string) = { Name = "enterkeyhint"; Value = ValueSome v }
    static member inline _is (v: string) = { Name = "is"; Value = ValueSome v }
    static member inline _slot (v: string) = { Name = "slot"; Value = ValueSome v }
    static member inline _part (v: string) = { Name = "part"; Value = ValueSome v }
    static member inline _nonce (v: string) = { Name = "nonce"; Value = ValueSome v }
    static member inline _popover (v: string) = { Name = "popover"; Value = ValueSome v }
    static member inline _data (attr: string, ?v: string) =
        let key = $"data-{attr}"
        match v with
        | Some v -> { Name = key; Value = ValueSome v }
        | None -> { Name = key; Value = ValueNone }

    // Document and meta attributes
    static member inline _charset (v: string) = { Name = "charset"; Value = ValueSome v }
    static member inline _name (v: string) = { Name = "name"; Value = ValueSome v }
    static member inline _content (v: string) = { Name = "content"; Value = ValueSome v }
    static member inline _property (v: string) = { Name = "property"; Value = ValueSome v }
    static member inline _httpEquiv (v: string) = { Name = "http-equiv"; Value = ValueSome v }

    // Link and resource attributes
    static member inline _href (v: string) = { Name = "href"; Value = ValueSome v }
    static member inline _rel (v: string) = { Name = "rel"; Value = ValueSome v }
    static member inline _as (v: string) = { Name = "as"; Value = ValueSome v }
    static member inline _blocking (v: string) = { Name = "blocking"; Value = ValueSome v }
    static member inline _color (v: string) = { Name = "color"; Value = ValueSome v }
    static member inline _hreflang (v: string) = { Name = "hreflang"; Value = ValueSome v }
    static member inline _ping (v: string) = { Name = "ping"; Value = ValueSome v }
    static member inline _src (v: string) = { Name = "src"; Value = ValueSome v }
    static member inline _srcset (v: string) = { Name = "srcset"; Value = ValueSome v }
    static member inline _sizes (v: string) = { Name = "sizes"; Value = ValueSome v }
    static member inline _imagesrcset (v: string) = { Name = "imagesrcset"; Value = ValueSome v }
    static member inline _imagesizes (v: string) = { Name = "imagesizes"; Value = ValueSome v }
    static member inline _media (v: string) = { Name = "media"; Value = ValueSome v }
    static member inline _type (v: string) = { Name = "type"; Value = ValueSome v }
    static member inline _target (v: string) = { Name = "target"; Value = ValueSome v }
    static member inline _download (v: string) = { Name = "download"; Value = ValueSome v }
    static member inline _download () = { Name = "download"; Value = ValueNone }
    static member inline _referrerpolicy (v: string) = { Name = "referrerpolicy"; Value = ValueSome v }
    static member inline _crossorigin = { Name = "crossorigin"; Value = ValueNone }
    static member inline _integrity (v: string) = { Name = "integrity"; Value = ValueSome v }
    static member inline _fetchpriority (v: string) = { Name = "fetchpriority"; Value = ValueSome v }
    static member inline _async (v: bool) = if v then { Name = "async"; Value = ValueNone } else Html.EmptyAttr
    static member inline _defer (v: bool) = if v then { Name = "defer"; Value = ValueNone } else Html.EmptyAttr
    static member inline _nomodule (v: bool) = if v then { Name = "nomodule"; Value = ValueNone } else Html.EmptyAttr

    // Image and media attributes
    static member inline _alt (v: string) = { Name = "alt"; Value = ValueSome v }
    static member inline _width (v: string) = { Name = "width"; Value = ValueSome v }
    static member inline _height (v: string) = { Name = "height"; Value = ValueSome v }
    static member inline _loading (v: string) = { Name = "loading"; Value = ValueSome v }
    static member inline _decoding (v: string) = { Name = "decoding"; Value = ValueSome v }
    static member inline _usemap (v: string) = { Name = "usemap"; Value = ValueSome v }
    static member inline _ismap (v: bool) = if v then { Name = "ismap"; Value = ValueNone } else Html.EmptyAttr
    static member inline _poster (v: string) = { Name = "poster"; Value = ValueSome v }
    static member inline _controls (v: bool) = if v then { Name = "controls"; Value = ValueNone } else Html.EmptyAttr
    static member inline _autoplay (v: bool) = if v then { Name = "autoplay"; Value = ValueNone } else Html.EmptyAttr
    static member inline _loop (v: bool) = if v then { Name = "loop"; Value = ValueNone } else Html.EmptyAttr
    static member inline _muted (v: bool) = if v then { Name = "muted"; Value = ValueNone } else Html.EmptyAttr
    static member inline _playsinline (v: bool) = if v then { Name = "playsinline"; Value = ValueNone } else Html.EmptyAttr
    static member inline _preload (v: string) = { Name = "preload"; Value = ValueSome v }
    static member inline _kind (v: string) = { Name = "kind"; Value = ValueSome v }
    static member inline _srclang (v: string) = { Name = "srclang"; Value = ValueSome v }
    static member inline _label (v: string) = { Name = "label"; Value = ValueSome v }
    static member inline _default (v: bool) = if v then { Name = "default"; Value = ValueNone } else Html.EmptyAttr
    static member inline _coords (v: string) = { Name = "coords"; Value = ValueSome v }
    static member inline _shape (v: string) = { Name = "shape"; Value = ValueSome v }

    // Form attributes
    static member inline _action (v: string) = { Name = "action"; Value = ValueSome v }
    static member inline _acceptCharset (v: string) = { Name = "accept-charset"; Value = ValueSome v }
    static member inline _method (v: string) = { Name = "method"; Value = ValueSome v }
    static member inline _enctype (v: string) = { Name = "enctype"; Value = ValueSome v }
    static member inline _novalidate (v: bool) = if v then { Name = "novalidate"; Value = ValueNone } else Html.EmptyAttr
    static member inline _for (v: string) = { Name = "for"; Value = ValueSome v }
    static member inline _value (v: string) = { Name = "value"; Value = ValueSome v }
    static member inline _placeholder (v: string) = { Name = "placeholder"; Value = ValueSome v }
    static member inline _autocomplete (v: string) = { Name = "autocomplete"; Value = ValueSome v }
    static member inline _pattern (v: string) = { Name = "pattern"; Value = ValueSome v }
    static member inline _accept (v: string) = { Name = "accept"; Value = ValueSome v }
    static member inline _alpha (v: bool) = if v then { Name = "alpha"; Value = ValueNone } else Html.EmptyAttr
    static member inline _colorspace (v: string) = { Name = "colorspace"; Value = ValueSome v }
    static member inline _required (v: bool) = if v then { Name = "required"; Value = ValueNone } else Html.EmptyAttr
    static member inline _disabled (v: bool) = if v then { Name = "disabled"; Value = ValueNone } else Html.EmptyAttr
    static member inline _readonly (v: bool) = if v then { Name = "readonly"; Value = ValueNone } else Html.EmptyAttr
    static member inline _multiple (v: bool) = if v then { Name = "multiple"; Value = ValueNone } else Html.EmptyAttr
    static member inline _selected (v: bool) = if v then { Name = "selected"; Value = ValueNone } else Html.EmptyAttr
    static member inline _checked (v: bool) = if v then { Name = "checked"; Value = ValueNone } else Html.EmptyAttr
    static member inline _rows (v: int) = { Name = "rows"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _cols (v: int) = { Name = "cols"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _wrap (v: string) = { Name = "wrap"; Value = ValueSome v }
    static member inline _size (v: int) = { Name = "size"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _list (v: string) = { Name = "list"; Value = ValueSome v }
    static member inline _dirname (v: string) = { Name = "dirname"; Value = ValueSome v }
    static member inline _min (v: string) = { Name = "min"; Value = ValueSome v }
    static member inline _min (v: float) = { Name = "min"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _minlength (v: string) = { Name = "minlength"; Value = ValueSome v }
    static member inline _minlength (v: int) = { Name = "minlength"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _max (v: string) = { Name = "max"; Value = ValueSome v }
    static member inline _max (v: float) = { Name = "max"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _maxlength (v: string) = { Name = "maxlength"; Value = ValueSome v }
    static member inline _maxlength (v: int) = { Name = "maxlength"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _step (v: string) = { Name = "step"; Value = ValueSome v }
    static member inline _step (v: float) = { Name = "step"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _command (v: string) = { Name = "command"; Value = ValueSome v }
    static member inline _commandfor (v: string) = { Name = "commandfor"; Value = ValueSome v }
    static member inline _form (v: string) = { Name = "form"; Value = ValueSome v }
    static member inline _formaction (v: string) = { Name = "formaction"; Value = ValueSome v }
    static member inline _formmethod (v: string) = { Name = "formmethod"; Value = ValueSome v }
    static member inline _formenctype (v: string) = { Name = "formenctype"; Value = ValueSome v }
    static member inline _formnovalidate (v: bool) = if v then { Name = "formnovalidate"; Value = ValueNone } else Html.EmptyAttr
    static member inline _formtarget (v: string) = { Name = "formtarget"; Value = ValueSome v }
    static member inline _popovertarget (v: string) = { Name = "popovertarget"; Value = ValueSome v }
    static member inline _popovertargetaction (v: string) = { Name = "popovertargetaction"; Value = ValueSome v }

    // Table attributes
    static member inline _abbr (v: string) = { Name = "abbr"; Value = ValueSome v }
    static member inline _colspan (v: int) = { Name = "colspan"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _rowspan (v: int) = { Name = "rowspan"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _span (v: int) = { Name = "span"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _scope (v: string) = { Name = "scope"; Value = ValueSome v }
    static member inline _headers (v: string) = { Name = "headers"; Value = ValueSome v }

    // Details and dialog attributes
    static member inline _open (v: bool) = if v then { Name = "open"; Value = ValueNone } else Html.EmptyAttr
    static member inline _closedby (v: string) = { Name = "closedby"; Value = ValueSome v }
    static member inline _cite (v: string) = { Name = "cite"; Value = ValueSome v }
    static member inline _datetime (v: string) = { Name = "datetime"; Value = ValueSome v }

    // List attributes
    static member inline _start (v: int) = { Name = "start"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _reversed (v: bool) = if v then { Name = "reversed"; Value = ValueNone } else Html.EmptyAttr

    // Meter attributes
    static member inline _high (v: float) = { Name = "high"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _low (v: float) = { Name = "low"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }
    static member inline _optimum (v: float) = { Name = "optimum"; Value = ValueSome(v.ToString(CultureInfo.InvariantCulture)) }

    // Iframe attributes
    static member inline _sandbox (v: string) = { Name = "sandbox"; Value = ValueSome v }
    static member inline _allow (v: string) = { Name = "allow"; Value = ValueSome v }
    static member inline _allowfullscreen (v: bool) = if v then { Name = "allowfullscreen"; Value = ValueNone } else Html.EmptyAttr
    static member inline _srcdoc (v: string) = { Name = "srcdoc"; Value = ValueSome v }

    // Object attributes
    static member inline _objectData (v: string) = { Name = "data"; Value = ValueSome v }

    // Template attributes
    static member inline _shadowrootclonable (v: bool) = if v then { Name = "shadowrootclonable"; Value = ValueNone } else Html.EmptyAttr
    static member inline _shadowrootcustomelementregistry (v: bool) = if v then { Name = "shadowrootcustomelementregistry"; Value = ValueNone } else Html.EmptyAttr
    static member inline _shadowrootdelegatesfocus (v: bool) = if v then { Name = "shadowrootdelegatesfocus"; Value = ValueNone } else Html.EmptyAttr
    static member inline _shadowrootmode (v: string) = { Name = "shadowrootmode"; Value = ValueSome v }
    static member inline _shadowrootserializable (v: bool) = if v then { Name = "shadowrootserializable"; Value = ValueNone } else Html.EmptyAttr
    static member inline _shadowrootslotassignment (v: string) = { Name = "shadowrootslotassignment"; Value = ValueSome v }

    // Microdata attributes
    static member inline _itemscope (v: bool) = if v then { Name = "itemscope"; Value = ValueNone } else Html.EmptyAttr
    static member inline _itemtype (v: string) = { Name = "itemtype"; Value = ValueSome v }
    static member inline _itemprop (v: string) = { Name = "itemprop"; Value = ValueSome v }
    static member inline _itemid (v: string) = { Name = "itemid"; Value = ValueSome v }
    static member inline _itemref (v: string) = { Name = "itemref"; Value = ValueSome v }

    // ARIA attributes
    static member inline _role (v: string) = { Name = "role"; Value = ValueSome v }
    static member inline _aria (name: string, v: string) = { Name = $"aria-{name}"; Value = ValueSome v }
    static member inline _ariaActivedescendant (v: string) = { Name = "aria-activedescendant"; Value = ValueSome v }
    static member inline _ariaAtomic (v: string) = { Name = "aria-atomic"; Value = ValueSome v }
    static member inline _ariaAtomic (v: bool) = { Name = "aria-atomic"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaAutocomplete (v: string) = { Name = "aria-autocomplete"; Value = ValueSome v }
    static member inline _ariaBusy (v: string) = { Name = "aria-busy"; Value = ValueSome v }
    static member inline _ariaBusy (v: bool) = { Name = "aria-busy"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaChecked (v: string) = { Name = "aria-checked"; Value = ValueSome v }
    static member inline _ariaChecked (v: bool) = { Name = "aria-checked"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaColcount (v: string) = { Name = "aria-colcount"; Value = ValueSome v }
    static member inline _ariaColindex (v: string) = { Name = "aria-colindex"; Value = ValueSome v }
    static member inline _ariaColspan (v: string) = { Name = "aria-colspan"; Value = ValueSome v }
    static member inline _ariaControls (v: string) = { Name = "aria-controls"; Value = ValueSome v }
    static member inline _ariaCurrent (v: string) = { Name = "aria-current"; Value = ValueSome v }
    static member inline _ariaCurrent (v: bool) = { Name = "aria-current"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaDescribedby (v: string) = { Name = "aria-describedby"; Value = ValueSome v }
    static member inline _ariaDetails (v: string) = { Name = "aria-details"; Value = ValueSome v }
    static member inline _ariaDisabled (v: string) = { Name = "aria-disabled"; Value = ValueSome v }
    static member inline _ariaDisabled (v: bool) = { Name = "aria-disabled"; Value = ValueSome(if v then "true" else "false") }
    [<Obsolete("aria-dropeffect is deprecated in WAI-ARIA 1.2.")>]
    static member inline _ariaDropeffect (v: string) = { Name = "aria-dropeffect"; Value = ValueSome v }
    static member inline _ariaErrormessage (v: string) = { Name = "aria-errormessage"; Value = ValueSome v }
    static member inline _ariaExpanded (v: string) = { Name = "aria-expanded"; Value = ValueSome v }
    static member inline _ariaExpanded (v: bool) = { Name = "aria-expanded"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaFlowto (v: string) = { Name = "aria-flowto"; Value = ValueSome v }
    [<Obsolete("aria-grabbed is deprecated in WAI-ARIA 1.2.")>]
    static member inline _ariaGrabbed (v: string) = { Name = "aria-grabbed"; Value = ValueSome v }
    static member inline _ariaHaspopup (v: string) = { Name = "aria-haspopup"; Value = ValueSome v }
    static member inline _ariaHaspopup (v: bool) = { Name = "aria-haspopup"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaHidden (v: string) = { Name = "aria-hidden"; Value = ValueSome v }
    static member inline _ariaHidden (v: bool) = { Name = "aria-hidden"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaInvalid (v: string) = { Name = "aria-invalid"; Value = ValueSome v }
    static member inline _ariaInvalid (v: bool) = { Name = "aria-invalid"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaKeyshortcuts (v: string) = { Name = "aria-keyshortcuts"; Value = ValueSome v }
    static member inline _ariaLabel (v: string) = { Name = "aria-label"; Value = ValueSome v }
    static member inline _ariaLabelledby (v: string) = { Name = "aria-labelledby"; Value = ValueSome v }
    static member inline _ariaLevel (v: string) = { Name = "aria-level"; Value = ValueSome v }
    static member inline _ariaLive (v: string) = { Name = "aria-live"; Value = ValueSome v }
    static member inline _ariaModal (v: string) = { Name = "aria-modal"; Value = ValueSome v }
    static member inline _ariaModal (v: bool) = { Name = "aria-modal"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaMultiline (v: string) = { Name = "aria-multiline"; Value = ValueSome v }
    static member inline _ariaMultiline (v: bool) = { Name = "aria-multiline"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaMultiselectable (v: string) = { Name = "aria-multiselectable"; Value = ValueSome v }
    static member inline _ariaMultiselectable (v: bool) = { Name = "aria-multiselectable"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaOrientation (v: string) = { Name = "aria-orientation"; Value = ValueSome v }
    static member inline _ariaOwns (v: string) = { Name = "aria-owns"; Value = ValueSome v }
    static member inline _ariaPlaceholder (v: string) = { Name = "aria-placeholder"; Value = ValueSome v }
    static member inline _ariaPosinset (v: string) = { Name = "aria-posinset"; Value = ValueSome v }
    static member inline _ariaPressed (v: string) = { Name = "aria-pressed"; Value = ValueSome v }
    static member inline _ariaPressed (v: bool) = { Name = "aria-pressed"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaReadonly (v: string) = { Name = "aria-readonly"; Value = ValueSome v }
    static member inline _ariaReadonly (v: bool) = { Name = "aria-readonly"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaRelevant (v: string) = { Name = "aria-relevant"; Value = ValueSome v }
    static member inline _ariaRequired (v: string) = { Name = "aria-required"; Value = ValueSome v }
    static member inline _ariaRequired (v: bool) = { Name = "aria-required"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaRoledescription (v: string) = { Name = "aria-roledescription"; Value = ValueSome v }
    static member inline _ariaRowcount (v: string) = { Name = "aria-rowcount"; Value = ValueSome v }
    static member inline _ariaRowindex (v: string) = { Name = "aria-rowindex"; Value = ValueSome v }
    static member inline _ariaRowspan (v: string) = { Name = "aria-rowspan"; Value = ValueSome v }
    static member inline _ariaSelected (v: string) = { Name = "aria-selected"; Value = ValueSome v }
    static member inline _ariaSelected (v: bool) = { Name = "aria-selected"; Value = ValueSome(if v then "true" else "false") }
    static member inline _ariaSetsize (v: string) = { Name = "aria-setsize"; Value = ValueSome v }
    static member inline _ariaSort (v: string) = { Name = "aria-sort"; Value = ValueSome v }
    static member inline _ariaValuemax (v: string) = { Name = "aria-valuemax"; Value = ValueSome v }
    static member inline _ariaValuemin (v: string) = { Name = "aria-valuemin"; Value = ValueSome v }
    static member inline _ariaValuenow (v: string) = { Name = "aria-valuenow"; Value = ValueSome v }
    static member inline _ariaValuetext (v: string) = { Name = "aria-valuetext"; Value = ValueSome v }

    // Event handler attributes
    static member inline _onclick ([<LanguageInjection("javascript")>]v: string) = { Name = "onclick"; Value = ValueSome v }
    static member inline _ondblclick ([<LanguageInjection("javascript")>]v: string) = { Name = "ondblclick"; Value = ValueSome v }
    static member inline _onchange ([<LanguageInjection("javascript")>]v: string) = { Name = "onchange"; Value = ValueSome v }
    static member inline _oninput ([<LanguageInjection("javascript")>]v: string) = { Name = "oninput"; Value = ValueSome v }
    static member inline _onbeforeinput ([<LanguageInjection("javascript")>]v: string) = { Name = "onbeforeinput"; Value = ValueSome v }
    static member inline _onsubmit ([<LanguageInjection("javascript")>]v: string) = { Name = "onsubmit"; Value = ValueSome v }
    static member inline _onreset ([<LanguageInjection("javascript")>]v: string) = { Name = "onreset"; Value = ValueSome v }
    static member inline _oninvalid ([<LanguageInjection("javascript")>]v: string) = { Name = "oninvalid"; Value = ValueSome v }
    static member inline _onselect ([<LanguageInjection("javascript")>]v: string) = { Name = "onselect"; Value = ValueSome v }
    static member inline _onfocus ([<LanguageInjection("javascript")>]v: string) = { Name = "onfocus"; Value = ValueSome v }
    static member inline _onblur ([<LanguageInjection("javascript")>]v: string) = { Name = "onblur"; Value = ValueSome v }
    static member inline _onkeydown ([<LanguageInjection("javascript")>]v: string) = { Name = "onkeydown"; Value = ValueSome v }
    static member inline _onkeyup ([<LanguageInjection("javascript")>]v: string) = { Name = "onkeyup"; Value = ValueSome v }
    static member inline _onkeypress ([<LanguageInjection("javascript")>]v: string) = { Name = "onkeypress"; Value = ValueSome v }
    static member inline _onmousedown ([<LanguageInjection("javascript")>]v: string) = { Name = "onmousedown"; Value = ValueSome v }
    static member inline _onmouseup ([<LanguageInjection("javascript")>]v: string) = { Name = "onmouseup"; Value = ValueSome v }
    static member inline _onmouseover ([<LanguageInjection("javascript")>]v: string) = { Name = "onmouseover"; Value = ValueSome v }
    static member inline _onmouseout ([<LanguageInjection("javascript")>]v: string) = { Name = "onmouseout"; Value = ValueSome v }
    static member inline _onmousemove ([<LanguageInjection("javascript")>]v: string) = { Name = "onmousemove"; Value = ValueSome v }
    static member inline _onmouseenter ([<LanguageInjection("javascript")>]v: string) = { Name = "onmouseenter"; Value = ValueSome v }
    static member inline _onmouseleave ([<LanguageInjection("javascript")>]v: string) = { Name = "onmouseleave"; Value = ValueSome v }
    static member inline _oncontextmenu ([<LanguageInjection("javascript")>]v: string) = { Name = "oncontextmenu"; Value = ValueSome v }
    static member inline _onwheel ([<LanguageInjection("javascript")>]v: string) = { Name = "onwheel"; Value = ValueSome v }
    static member inline _onscroll ([<LanguageInjection("javascript")>]v: string) = { Name = "onscroll"; Value = ValueSome v }
    static member inline _onresize ([<LanguageInjection("javascript")>]v: string) = { Name = "onresize"; Value = ValueSome v }
    static member inline _oncopy ([<LanguageInjection("javascript")>]v: string) = { Name = "oncopy"; Value = ValueSome v }
    static member inline _oncut ([<LanguageInjection("javascript")>]v: string) = { Name = "oncut"; Value = ValueSome v }
    static member inline _onpaste ([<LanguageInjection("javascript")>]v: string) = { Name = "onpaste"; Value = ValueSome v }
    static member inline _ondrag ([<LanguageInjection("javascript")>]v: string) = { Name = "ondrag"; Value = ValueSome v }
    static member inline _ondragstart ([<LanguageInjection("javascript")>]v: string) = { Name = "ondragstart"; Value = ValueSome v }
    static member inline _ondragend ([<LanguageInjection("javascript")>]v: string) = { Name = "ondragend"; Value = ValueSome v }
    static member inline _ondragover ([<LanguageInjection("javascript")>]v: string) = { Name = "ondragover"; Value = ValueSome v }
    static member inline _ondragenter ([<LanguageInjection("javascript")>]v: string) = { Name = "ondragenter"; Value = ValueSome v }
    static member inline _ondragleave ([<LanguageInjection("javascript")>]v: string) = { Name = "ondragleave"; Value = ValueSome v }
    static member inline _ondrop ([<LanguageInjection("javascript")>]v: string) = { Name = "ondrop"; Value = ValueSome v }
    static member inline _ontouchstart ([<LanguageInjection("javascript")>]v: string) = { Name = "ontouchstart"; Value = ValueSome v }
    static member inline _ontouchmove ([<LanguageInjection("javascript")>]v: string) = { Name = "ontouchmove"; Value = ValueSome v }
    static member inline _ontouchend ([<LanguageInjection("javascript")>]v: string) = { Name = "ontouchend"; Value = ValueSome v }
    static member inline _onanimationstart ([<LanguageInjection("javascript")>]v: string) = { Name = "onanimationstart"; Value = ValueSome v }
    static member inline _onanimationend ([<LanguageInjection("javascript")>]v: string) = { Name = "onanimationend"; Value = ValueSome v }
    static member inline _onanimationiteration ([<LanguageInjection("javascript")>]v: string) = { Name = "onanimationiteration"; Value = ValueSome v }
    static member inline _ontransitionend ([<LanguageInjection("javascript")>]v: string) = { Name = "ontransitionend"; Value = ValueSome v }
    static member inline _onload ([<LanguageInjection("javascript")>]v: string) = { Name = "onload"; Value = ValueSome v }
    static member inline _onerror ([<LanguageInjection("javascript")>]v: string) = { Name = "onerror"; Value = ValueSome v }
    static member inline _onabort ([<LanguageInjection("javascript")>]v: string) = { Name = "onabort"; Value = ValueSome v }
    static member inline _ontoggle ([<LanguageInjection("javascript")>]v: string) = { Name = "ontoggle"; Value = ValueSome v }
    static member inline _onplay ([<LanguageInjection("javascript")>]v: string) = { Name = "onplay"; Value = ValueSome v }
    static member inline _onpause ([<LanguageInjection("javascript")>]v: string) = { Name = "onpause"; Value = ValueSome v }
    static member inline _onended ([<LanguageInjection("javascript")>]v: string) = { Name = "onended"; Value = ValueSome v }

