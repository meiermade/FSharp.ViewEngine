namespace Docs.Pages

open System
open System.Text.Json
open System.Text.RegularExpressions
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar

/// Documentation search metadata and composition, not an installable component API.
module IconBrowser =
    type Entry = { name:IconName; label:string; tags:string; search:string }

    let private brands = set [ IconName.GitHub; IconName.X; IconName.LinkedIn ]
    let isBrand name = Set.contains name brands

    let private families =
        [ "Arrow", "navigation direction move transfer pointer"
          "Chevron", "navigation expand collapse dropdown previous next"
          "Archive", "storage archive backup file history"
          "Bars", "menu navigation hamburger list alignment"
          "Battery", "power charge energy device"
          "Bell", "notifications alerts reminders alarm"
          "Bookmark", "save favorites reading collection"
          "Building", "organization company business office location"
          "Calendar", "date schedule appointment event time"
          "Chart", "analytics dashboard statistics graph report data"
          "ChatBubble", "chat message conversation comment discussion feedback"
          "Check", "success confirm complete done approved verified"
          "Clipboard", "copy paste checklist task clipboard"
          "Cloud", "cloud storage network hosting service"
          "CodeBracket", "code developer programming source html"
          "Cog", "settings preferences configuration gear tooth setup customize"
          "Currency", "money currency finance payment exchange"
          "Device", "device screen mobile tablet responsive hardware"
          "Document", "documents files paper content records"
          "DocumentCurrency", "money currency invoice bill finance payment"
          "DocumentChart", "analytics report statistics financial data"
          "Ellipsis", "more options actions overflow menu dots"
          "Envelope", "email mail message letter notification"
          "Exclamation", "warning alert attention error caution"
          "Eye", "view visibility preview show inspect"
          "Face", "reaction emotion mood sentiment emoji"
          "Folder", "folder directory files collection storage"
          "Gift", "present reward celebration promotion"
          "Globe", "world international internet website language region"
          "Hand", "gesture touch interaction feedback approval"
          "Home", "home start dashboard house property"
          "Inbox", "inbox receive incoming mail queue"
          "Lock", "security authentication privacy password permissions"
          "MagnifyingGlass", "search find lookup discover inspect"
          "Minus", "minus subtract remove decrease reduce"
          "Phone", "phone call telephone contact communication"
          "Play", "play playback media video music start resume"
          "Presentation", "presentation slides analytics graph report meeting"
          "Receipt", "receipt invoice purchase payment expense bill"
          "Rectangle", "layout panels cards collection arrange"
          "Server", "server hosting infrastructure backend database storage"
          "Shield", "security protection permission trust verification"
          "Shopping", "shopping commerce ecommerce checkout purchase basket"
          "Signal", "connection network broadcast wireless reception"
          "Speaker", "audio sound volume speaker music"
          "Square", "layout grid cards tiles layers collection"
          "Stop", "stop end terminate playback media"
          "User", "user profile accounts person people identity members avatar"
          "VideoCamera", "video recording camera meeting call" ]

    let private groups =
        [ "github git code source repository developer hosting social brand logo", [IconName.GitHub]
          "twitter x social network post tweet microblogging brand logo", [IconName.X]
          "linkedin professional network business career social brand logo", [IconName.LinkedIn]
          "team group organization collaboration users", [IconName.UserGroup; IconName.Users]
          "font text typography formatting", [IconName.Bold; IconName.Italic; IconName.Underline; IconName.Strikethrough; IconName.H1; IconName.H2; IconName.H3]
          "favorites", [IconName.Heart; IconName.Star]
          "settings preferences configuration controls customize", [IconName.AdjustmentsHorizontal; IconName.AdjustmentsVertical; IconName.Wrench; IconName.WrenchScrewdriver]
          "warning attention alert caution", [IconName.ShieldExclamation; IconName.BellAlert; IconName.ExclamationCircle; IconName.ExclamationTriangle]
          "refresh reload sync synchronize retry update", [IconName.ArrowPath; IconName.ArrowPathRoundedSquare; IconName.ArrowsRightLeft; IconName.ArrowsUpDown]
          "delete remove discard erase", [IconName.Trash; IconName.Backspace; IconName.DocumentMinus; IconName.FolderMinus; IconName.UserMinus; IconName.Minus; IconName.MinusCircle]
          "close dismiss cancel exit", [IconName.XMark; IconName.XCircle; IconName.ArchiveBoxXMark; IconName.PhoneXMark]
          "add create new insert increase", [IconName.Plus; IconName.PlusCircle; IconName.DocumentPlus; IconName.FolderPlus; IconName.UserPlus; IconName.SquaresPlus]
          "download import save receive", [IconName.ArrowDownTray; IconName.DocumentArrowDown; IconName.CloudArrowDown; IconName.FolderArrowDown; IconName.ArrowDownOnSquare; IconName.ArrowDownOnSquareStack; IconName.InboxArrowDown]
          "upload export send publish", [IconName.ArrowUpTray; IconName.DocumentArrowUp; IconName.CloudArrowUp; IconName.ArrowUpOnSquare; IconName.ArrowUpOnSquareStack]
          "login sign in enter", [IconName.ArrowLeftEndOnRectangle; IconName.ArrowRightEndOnRectangle]
          "logout sign out leave exit", [IconName.ArrowLeftStartOnRectangle; IconName.ArrowRightStartOnRectangle]
          "external open new tab launch", [IconName.ArrowTopRightOnSquare]
          "undo revert back reset", [IconName.ArrowUturnLeft; IconName.ArrowUturnDown]
          "redo repeat forward", [IconName.ArrowUturnRight; IconName.ArrowUturnUp]
          "resize expand maximize fullscreen zoom", [IconName.ArrowsPointingOut; IconName.MagnifyingGlassPlus]
          "shrink minimize collapse zoom", [IconName.ArrowsPointingIn; IconName.MagnifyingGlassMinus]
          "sort reorder ascending descending", [IconName.BarsArrowDown; IconName.BarsArrowUp; IconName.ChevronUpDown]
          "education school university graduation learning student", [IconName.AcademicCap]
          "contact email address mention", [IconName.AtSymbol]
          "rewind previous playback media", [IconName.Backward]
          "fast forward next playback media", [IconName.Forward]
          "money cash finance bank payment", [IconName.Banknotes; IconName.CreditCard; IconName.Wallet]
          "science experiment lab chemistry test", [IconName.Beaker]
          "text typography formatting strong emphasis", [IconName.Bold]
          "text typography formatting italic emphasis", [IconName.Italic]
          "text typography formatting underline", [IconName.Underline]
          "text typography formatting crossed deleted", [IconName.Strikethrough]
          "electricity power energy lightning fast", [IconName.Bolt; IconName.BoltSlash]
          "read reading documentation manual library knowledge", [IconName.BookOpen; IconName.Newspaper]
          "work business job career portfolio", [IconName.Briefcase]
          "bug debug issue insect error defect", [IconName.BugAnt]
          "birthday celebration party food", [IconName.Cake]
          "calculate math numbers accounting total", [IconName.Calculator]
          "camera photo photograph capture image", [IconName.Camera; IconName.Photo]
          "database storage records stack", [IconName.CircleStack]
          "time history recent duration timer", [IconName.Clock]
          "terminal console shell command developer", [IconName.CommandLine]
          "computer desktop monitor screen display", [IconName.ComputerDesktop; IconName.Tv; IconName.Window]
          "processor hardware computer chip ai technology", [IconName.CpuChip]
          "box package product inventory three dimensional", [IconName.Cube; IconName.CubeTransparent]
          "cursor click pointer mouse select", [IconName.CursorArrowRays; IconName.CursorArrowRipple]
          "math arithmetic division calculator", [IconName.Divide]
          "math equal equality compare", [IconName.Equals]
          "copy duplicate clone", [IconName.DocumentDuplicate; IconName.Square2Stack]
          "hidden hide invisible password private", [IconName.EyeSlash]
          "color picker sample eyedropper design", [IconName.EyeDropper]
          "sad negative unhappy feedback", [IconName.FaceFrown; IconName.HandThumbDown]
          "happy positive like feedback smile", [IconName.FaceSmile; IconName.HandThumbUp]
          "movie video cinema media animation", [IconName.Film; IconName.Gif]
          "biometric fingerprint authentication identity security", [IconName.FingerPrint]
          "fire hot trending popular flame", [IconName.Fire]
          "flag mark report milestone bookmark", [IconName.Flag]
          "filter refine narrow selection", [IconName.Funnel]
          "heading title text typography", [IconName.H1; IconName.H2; IconName.H3]
          "stop wait permission hand", [IconName.HandRaised]
          "hash number tag topic social", [IconName.Hashtag]
          "favorite love like health heart", [IconName.Heart]
          "identity profile account passport badge id", [IconName.Identification]
          "info information help about details", [IconName.InformationCircle]
          "key password credential access authentication security", [IconName.Key]
          "translate translation locale language international", [IconName.Language]
          "support help rescue assistance", [IconName.Lifebuoy]
          "idea tip inspiration insight light", [IconName.LightBulb]
          "link url hyperlink connect connection", [IconName.Link; IconName.LinkSlash]
          "unlink disconnect broken", [IconName.LinkSlash]
          "list checklist items tasks bullets", [IconName.ListBullet; IconName.NumberedList; IconName.QueueList]
          "location place address pin map geography directions", [IconName.Map; IconName.MapPin]
          "announcement marketing broadcast promotion", [IconName.Megaphone]
          "microphone voice record speech audio", [IconName.Microphone]
          "dark mode night sleep theme appearance", [IconName.Moon]
          "music note song audio sound", [IconName.MusicalNote; IconName.Radio]
          "forbidden blocked prohibited disabled unavailable", [IconName.NoSymbol; IconName.Slash]
          "paint art drawing design brush color", [IconName.PaintBrush; IconName.Swatch]
          "send message email airplane", [IconName.PaperAirplane]
          "attachment attach file paperclip", [IconName.PaperClip]
          "pause playback media wait suspend", [IconName.Pause; IconName.PauseCircle]
          "edit write compose rename pencil", [IconName.Pencil; IconName.PencilSquare]
          "discount percent promotion sale offer", [IconName.PercentBadge; IconName.ReceiptPercent]
          "print printer paper document", [IconName.Printer]
          "plugin integration extension puzzle module", [IconName.PuzzlePiece]
          "qr code scan barcode payment", [IconName.QrCode]
          "question help support unknown faq", [IconName.QuestionMarkCircle]
          "refund return reimbursement money", [IconName.ReceiptRefund]
          "rocket launch deploy release start", [IconName.RocketLaunch]
          "rss feed subscribe updates news", [IconName.Rss]
          "balance justice legal weight compare", [IconName.Scale]
          "cut scissors trim split", [IconName.Scissors]
          "share social distribute send", [IconName.Share]
          "ai magic sparkle automation enhancement", [IconName.Sparkles]
          "mute silence volume off", [IconName.SpeakerXMark]
          "rating favorite star featured", [IconName.Star]
          "light mode day brightness sun theme appearance", [IconName.Sun]
          "table spreadsheet rows columns data grid", [IconName.TableCells; IconName.ViewColumns]
          "tag label category price metadata", [IconName.Tag]
          "ticket admission support event voucher", [IconName.Ticket]
          "award achievement winner trophy success", [IconName.Trophy]
          "delivery shipping transport vehicle truck", [IconName.Truck]
          "variable placeholder parameter code math", [IconName.Variable]
          "viewfinder focus target scan capture", [IconName.ViewfinderCircle]
          "wifi internet wireless connection network", [IconName.Wifi]
          "power on off shutdown switch", [IconName.Power] ]

    let entries =
        Enum.GetValues<IconName>()
        |> Array.map (fun name ->
            let label = string name
            let tags =
                [ for prefix, tags in families do
                      if label.StartsWith(prefix, StringComparison.Ordinal) then yield tags
                  for tags, names in groups do
                      if List.contains name names then yield tags ]
                |> String.concat " "
            { name=name; label=label; tags=tags; search=(label + " " + tags).ToLowerInvariant() })
        |> Array.sortBy _.label

    let terms (query:string) = Regex.Matches(query.ToLowerInvariant(), "[a-z0-9]+") |> Seq.map _.Value |> Seq.toArray
    let matches query entry = terms query |> Array.forall entry.search.Contains
    let private json (value:string) = JsonSerializer.Serialize value
    let batchSize = 24

    let private variants name =
        if isBrand name then [ IconVariant.Solid, "Solid" ]
        else [ IconVariant.Outline, "Outline"; IconVariant.Solid, "Solid"; IconVariant.Mini, "Mini"; IconVariant.Micro, "Micro" ]

    let private copyVariant (entry:Entry) (family:IconVariant, label:string) =
        fveButton {
            _id ($"icon-copy-{entry.label}-{family}")
            _variant ButtonVariant.Ghost
            _ariaLabel ($"Copy {entry.label} {label} icon")
            _title ($"Copy {entry.label} ({label}) F# expression")
            _class "h-full w-full min-w-0"
            let expression =
                if isBrand entry.name then json $"fveIcon {{ _name IconName.{entry.label} }}"
                else json $"fveIcon {{ _name IconName.{entry.label}; _variant IconVariant.{family} }}"
            let copied = json $"{entry.label} ({label})"
            _dataOn("click", $"$iconCopyError = false; if (navigator.clipboard?.writeText) {{ navigator.clipboard.writeText({expression}).then(() => {{ $iconCopied = {copied} }}).catch(() => {{ $iconCopied = ''; $iconCopyError = true }}) }} else {{ $iconCopied = ''; $iconCopyError = true }}")
            div {
                _class "grid min-w-0 justify-items-center gap-2 py-2"
                span { _class "flex h-6 items-center justify-center"; fveIcon { _name entry.name; _variant family } }
                span { _class "text-xs"; label }
            }
        }

    let private gallery count query =
        let results = entries |> Array.filter (matches query)
        let limit = max batchSize (min entries.Length count)
        let visible = results |> Array.truncate limit
        div {
            _id "icon-gallery"; _class "grid min-w-0 gap-4"
            p {
                _id "icon-result-count"; _data("icon-result-count", "")
                _role "status"; _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"
                if visible.Length < results.Length then $"Showing {visible.Length} of {results.Length} icons"
                elif results.Length = 1 then "1 icon"
                else $"{results.Length} icons"
            }
            ul {
                _id "icon-grid"; _ariaLabel "Available icons"; _tabindex 0
                _class "grid max-h-[32rem] min-w-0 grid-cols-1 items-start gap-4 overflow-y-auto p-1 outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] @2xl/icon-browser:grid-cols-2"
                for entry in visible do
                    li {
                        _id ("icon-tile-" + entry.label)
                        _class "grid min-w-0 content-start gap-3 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] p-3 sm:p-4"
                        _data("icon-entry", entry.label)
                        h3 { _class "font-mono text-sm font-medium [overflow-wrap:anywhere]"; entry.label }
                        div {
                            _role "group"; _ariaLabel (entry.label + " variants")
                            _class "grid min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,3.5rem),1fr))] gap-2"
                            for variant in variants entry.name do copyVariant entry variant
                        }
                    }
                if visible.Length < results.Length then
                    li {
                        _class "col-span-full flex justify-center py-2"
                        fveLink {
                            _id "icon-load-more"
                            _href ($"/components/icon?search={Uri.EscapeDataString query}&count={min results.Length (visible.Length + batchSize)}")
                            _variant LinkVariant.Outline
                            _data("fve-local-navigation", "true")
                            _dataOn("click", "if ($iconSearchPending) { evt.preventDefault(); document.getElementById('icon-filter-form').requestSubmit() } else { $iconMoreScroll = document.getElementById('icon-grid').scrollTop }")
                            "Show more icons"
                        }
                    }
            }
            if results.Length = 0 then
                p {
                    _id "icon-empty"; _class "py-8 text-base text-[var(--fve-muted-text)]"
                    "No icons match your search. Try another name or common use."
                }
            p {
                _id "icon-copy-status"; _role "status"; _ariaLive "polite"
                _class "min-h-6 text-sm text-[var(--fve-muted-text)]"
                _dataText "$iconCopyError ? 'Could not copy. Check clipboard access and try again.' : $iconCopied ? 'Copied ' + $iconCopied + ' F# expression.' : ''"
            }
        }

    let render count (search:string) =
        let query = if search.Length > 256 then search.Substring(0, 256) else search
        section {
            _id "icon-browser"; _ariaLabel "Icon browser"
            _class "@container/icon-browser grid min-w-0 grid-cols-1 gap-4"
            _dataSignals "{iconSearchPending: false, iconCopied: '', iconCopyError: false, iconMoreScroll: -1}"
            // WebKit can clamp the scrolling list while mounted controls are morphed.
            _dataOn("fve-navigation-complete__document", "if ($iconMoreScroll >= 0) { document.getElementById('icon-grid').scrollTop = $iconMoreScroll; $iconMoreScroll = -1 }")
            _dataOn("submit__capture", "$iconSearchPending = false; $iconCopied = ''; $iconCopyError = false; $iconMoreScroll = -1")
            h2 { _class "text-xl font-semibold"; "Browse icons" }
            p { _class "text-base leading-7 text-[var(--fve-muted-text)]"; "Search all icons by name or use. Select a variant to copy its F#." }
            form {
                _id "icon-filter-form"; _method "get"; _action "/components/icon"
                _data("fve-local-navigation", "true")
                _dataOn("input", "$iconSearchPending = true; $iconMoreScroll = -1; window.fveNavigationController?.abort(); document.getElementById('icon-grid').scrollTop = 0")
                _dataOn("input__debounce.150ms", "if ($iconSearchPending) { el.requestSubmit() }")
                fveField {
                    fveLabel { _for "icon-search"; "Search icons" }
                    fveInput { _id "icon-search"; _name "search"; _type "search"; _value query; _maxlength "256"; _placeholder "Search" }
                }
            }
            gallery count query
        }
