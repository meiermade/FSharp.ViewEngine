module HtmlCoverageTests

open System
open System.Reflection
open Expecto
open FSharp.ViewEngine
open type FSharp.ViewEngine.Html

// Pinned from the WHATWG HTML Living Standard developer index on 2026-08-05:
// https://html.spec.whatwg.org/dev/indices.html
let private expectedElements =
    """
a
abbr
address
area
article
aside
audio
b
base
bdi
bdo
blockquote
body
br
button
canvas
caption
cite
code
col
colgroup
data
datalist
dd
del
details
dfn
dialog
div
dl
dt
em
embed
fieldset
figcaption
figure
footer
form
h1
h2
h3
h4
h5
h6
head
header
hgroup
hr
html
i
iframe
img
input
ins
kbd
label
legend
li
link
main
map
mark
menu
meta
meter
nav
noscript
object
ol
optgroup
option
output
p
picture
pre
progress
q
rp
rt
ruby
s
samp
script
search
section
select
selectedcontent
slot
small
source
span
strong
style
sub
summary
sup
table
tbody
td
template
textarea
tfoot
th
thead
time
title
tr
track
u
ul
var
video
wbr
"""
        .Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Set.ofArray

let private expectedAttributes =
    """
abbr
accept
accept-charset
accesskey
action
allow
allowfullscreen
alpha
alt
as
async
autocapitalize
autocomplete
autocorrect
autofocus
autoplay
blocking
charset
checked
cite
class
closedby
color
colorspace
cols
colspan
command
commandfor
content
contenteditable
controls
coords
crossorigin
data
datetime
decoding
default
defer
dir
dirname
disabled
download
draggable
enctype
enterkeyhint
fetchpriority
for
form
formaction
formenctype
formmethod
formnovalidate
formtarget
headers
headingoffset
headingreset
height
hidden
high
href
hreflang
http-equiv
id
imagesizes
imagesrcset
inert
inputmode
integrity
is
ismap
itemid
itemprop
itemref
itemscope
itemtype
kind
label
lang
list
loading
loop
low
max
maxlength
media
method
min
minlength
multiple
muted
name
nomodule
nonce
novalidate
open
optimum
pattern
ping
placeholder
playsinline
popover
popovertarget
popovertargetaction
poster
preload
readonly
referrerpolicy
rel
required
reversed
rows
rowspan
sandbox
scope
selected
shadowrootclonable
shadowrootcustomelementregistry
shadowrootdelegatesfocus
shadowrootmode
shadowrootserializable
shadowrootslotassignment
shape
size
sizes
slot
span
spellcheck
src
srcdoc
srclang
srcset
start
step
style
tabindex
target
title
translate
type
usemap
value
width
wrap
writingsuggestions
"""
        .Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Set.ofArray

let private argumentFor (parameter: ParameterInfo) =
    let parameterType = parameter.ParameterType

    if parameterType = typeof<string> then
        box "value"
    elif parameterType = typeof<bool> then
        box true
    elif parameterType = typeof<int> then
        box 1
    elif parameterType = typeof<float> then
        box 1.0
    elif parameterType = typeof<string seq> then
        box (Seq.singleton "value")
    elif parameterType = typeof<unit> then
        box ()
    elif parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() = typedefof<option<_>> then
        null
    else
        failwith $"Unsupported Html helper parameter type: {parameterType.FullName}"

let private renderedElementNames () =
    let fromProperties =
        typeof<Html>.GetProperties(BindingFlags.Public ||| BindingFlags.Static)
        |> Seq.choose (fun property ->
            match property.GetValue(null) with
            | :? TagBuilder as builder ->
                let rendered = builder.Run(builder.Zero()) |> Render.toString
                Some rendered
            | :? HtmlElement as element -> Some(Render.toString element)
            | _ -> None)
        |> Seq.choose (fun rendered ->
            if rendered.Length > 1 && rendered[0] = '<' then
                let nameEnd = rendered.IndexOfAny([| ' '; '>' |], 1)
                if nameEnd > 1 then Some(rendered.Substring(1, nameEnd - 1)) else None
            else
                None)
        |> Set.ofSeq

    fromProperties

let private renderedAttributeNames () =
    typeof<Html>.GetMethods(BindingFlags.Public ||| BindingFlags.Static)
    |> Seq.filter (fun methodInfo -> methodInfo.ReturnType = typeof<HtmlAttribute>)
    |> Seq.choose (fun methodInfo ->
        let args = methodInfo.GetParameters() |> Array.map argumentFor
        let attribute = methodInfo.Invoke(null, args) :?> HtmlAttribute
        if isNull attribute.Name then None else Some attribute.Name)
    |> Set.ofSeq

[<Tests>]
let tests =
    testList "HTML Living Standard Coverage" [
        test "All standard elements have dedicated helpers" {
            let missing = Set.difference expectedElements (renderedElementNames ())
            let missingNames = String.concat ", " missing
            Expect.isEmpty missing $"Missing HTML element helpers: {missingNames}"
        }

        test "All standard attributes have dedicated helpers" {
            let missing = Set.difference expectedAttributes (renderedAttributeNames ())
            let missingNames = String.concat ", " missing
            Expect.isEmpty missing $"Missing HTML attribute helpers: {missingNames}"
        }
    ]
