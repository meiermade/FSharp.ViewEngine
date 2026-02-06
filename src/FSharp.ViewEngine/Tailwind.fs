namespace FSharp.ViewEngine

type Tailwind =
    static member val elAutocomplete = TagBuilder("el-autocomplete") with get
    static member val elOptions = TagBuilder("el-options") with get
    static member val elOption = TagBuilder("el-option") with get
    static member val elSelect = TagBuilder("el-select") with get
    static member val elSelectedContent = TagBuilder("el-selectedcontent") with get
    static member val elDropdown = TagBuilder("el-dropdown") with get
    static member val elMenu = TagBuilder("el-menu") with get
    static member val elDialog = TagBuilder("el-dialog") with get
    static member val elDialogBackdrop = TagBuilder("el-dialog-backdrop") with get
    static member val elDialogPanel = TagBuilder("el-dialog-panel") with get
    static member val elCommandPalette = TagBuilder("el-command-palette") with get
    static member val elCommandList = TagBuilder("el-command-list") with get
    static member val elCommandGroup = TagBuilder("el-command-group") with get
    static member val elCommandPreview = TagBuilder("el-command-preview") with get
    static member val elDefaults = TagBuilder("el-defaults") with get
    static member val elNoResults = TagBuilder("el-no-results") with get
    static member val elTabGroup = TagBuilder("el-tab-group") with get
    static member val elTabList = TagBuilder("el-tab-list") with get
    static member val elTabPanels = TagBuilder("el-tab-panels") with get
    static member inline _popover = { Name = "popover"; Value = ValueNone }
    static member inline _anchor (position: string) = { Name = "anchor"; Value = ValueSome position }

