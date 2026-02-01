namespace FSharp.ViewEngine

type Tailwind =
    static member _popover = Boolean "popover"
    static member _anchor (position:string) = KeyValue ("anchor", position)
    static member elAutocomplete (attrs:Attribute seq) = Tag ("el-autocomplete", attrs)
    static member elOptions (attrs:Attribute seq) = Tag ("el-options", attrs)
    static member elOption (attrs:Attribute seq) = Tag ("el-option", attrs)
    static member elSelect (attrs:Attribute seq) = Tag ("el-select", attrs)
    static member elSelectedContent (attrs:Attribute seq) = Tag ("el-selectedcontent", attrs)
    static member elDropdown (attrs:Attribute seq) = Tag ("el-dropdown", attrs)
    static member elMenu (attrs:Attribute seq) = Tag ("el-menu", attrs)
    static member elDialog (attrs:Attribute seq) = Tag ("el-dialog", attrs)
    static member elDialogBackdrop (attrs:Attribute seq) = Tag ("el-dialog-backdrop", attrs)
    static member elDialogPanel (attrs:Attribute seq) = Tag ("el-dialog-panel", attrs)
    static member elCommandPalette (attrs:Attribute seq) = Tag ("el-command-palette", attrs)
    static member elCommandList (attrs:Attribute seq) = Tag ("el-command-list", attrs)
    static member elCommandGroup (attrs:Attribute seq) = Tag ("el-command-group", attrs)
    static member elCommandPreview (attrs:Attribute seq) = Tag ("el-command-preview", attrs)
    static member elDefaults (attrs:Attribute seq) = Tag ("el-defaults", attrs)
    static member elNoResults (attrs:Attribute seq) = Tag ("el-no-results", attrs)
    static member elTabGroup (attrs:Attribute seq) = Tag ("el-tab-group", attrs)
    static member elTabList (attrs:Attribute seq) = Tag ("el-tab-list", attrs)
    static member elTabPanels (attrs:Attribute seq) = Tag ("el-tab-panels", attrs)
