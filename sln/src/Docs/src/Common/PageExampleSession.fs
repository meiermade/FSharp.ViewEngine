namespace Docs.Common

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Docs.Pages.PageExamples

/// Bounded, expiring provider-free fixture storage. Never a production persistence API.
module PageExampleSession =
    type Image =
        { bytes: byte array
          contentType: string }

    type Session =
        { expires: DateTimeOffset
          messages: Message list
          photos: WorkspacePhoto list
          images: Map<string, Image>
          accounts: Docs.Pages.Components.AccountWorkspace }

    let private sessions = Dictionary<string, Session>()
    let private gate = obj ()
    let private cookieName = "fve-page-examples"

    let private fresh () =
        { expires = DateTimeOffset.UtcNow.AddMinutes 30.
          messages = initialMessages
          photos = initialPhotos
          images = Map.empty
          accounts = Docs.Pages.Components.defaultAccountWorkspace }

    let private sessionKey (context: HttpContext) =
        for key in
            sessions
            |> Seq.filter (fun item -> item.Value.expires < DateTimeOffset.UtcNow)
            |> Seq.map _.Key
            |> Seq.toArray do
            sessions.Remove key |> ignore

        let supplied = context.Request.Cookies[cookieName]

        if not (isNull supplied) && sessions.ContainsKey supplied then
            supplied
        else
            if sessions.Count >= 32 then
                sessions |> Seq.minBy _.Value.expires |> _.Key |> sessions.Remove |> ignore

            let key = Guid.NewGuid().ToString("N")
            sessions[key] <- fresh ()

            context.Response.Cookies.Append(
                cookieName,
                key,
                CookieOptions(
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Path = "/components/page-examples",
                    MaxAge = Nullable(TimeSpan.FromMinutes 30.)
                )
            )

            key

    let get context =
        lock gate (fun () -> sessions[sessionKey context])

    let change context apply =
        lock gate (fun () ->
            let key = sessionKey context
            let updated, result = apply sessions[key]
            sessions[key] <- updated
            result)

    let send conversation (body: string) session =
        let body = body.Trim()

        if not (conversations |> List.exists (fun item -> item.id = conversation)) then
            session, Error "Choose a conversation."
        elif String.IsNullOrWhiteSpace body then
            session, Error "Enter a message before sending."
        elif body.Length > 2000 then
            session, Error "Keep your message under 2,000 characters."
        elif session.messages.Length >= 100 then
            session, Error "This demo session is full. Start a fresh session to continue."
        else
            let message =
                { id = Guid.NewGuid().ToString("N")
                  conversation = conversation
                  author = "Andy Meier"
                  body = body
                  time = DateTime.UtcNow.ToString("HH:mm")
                  outgoing = true }

            let updated =
                { session with
                    messages = session.messages @ [ message ] }

            updated, Ok updated.messages

    let createAccount (name: string) kind session =
        let workspace = session.accounts
        let name = name.Trim()

        let valid =
            name.Length > 0
            && name.Length <= 80
            && List.contains kind [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense" ]
            && workspace.createdAccounts.Length < 20

        let duplicate = Docs.Pages.Components.accountNameExists workspace name

        if not valid || duplicate then
            { session with
                accounts =
                    { workspace with
                        draftName = name
                        draftType = kind
                        feedback =
                            "Enter a unique account name and a valid account type. This demo supports up to 20 accounts." } },
            None
        else
            let id = 9001 + workspace.createdAccounts.Length

            let row: Docs.Pages.Components.AccountRow =
                { id = id
                  name = name
                  accountType = kind
                  commodity = "USD"
                  balance = 0M
                  totalBalance = 0M }

            { session with
                accounts =
                    { workspace with
                        createdAccounts = workspace.createdAccounts @ [ row ]
                        draftName = ""
                        draftType = "Asset"
                        feedback = "Account created in this demo session." } },
            Some id

    let saveSettings (name: string) email session =
        let name = name.Trim()

        if name.Length = 0 || name.Length > 80 then
            { session with
                accounts =
                    { session.accounts with
                        feedback = "Enter a workspace name of 1–80 characters." } },
            false
        else
            { session with
                accounts =
                    { session.accounts with
                        workspaceName = name
                        emailUpdates = email
                        feedback = "Settings saved in this demo session." } },
            true

    let imageFromBytes (bytes: byte array) =
        let starts prefix =
            bytes.Length >= Array.length prefix
            && Array.forall2 (=) bytes[0 .. Array.length prefix - 1] prefix

        let mime =
            if starts [| 137uy; 80uy; 78uy; 71uy; 13uy; 10uy; 26uy; 10uy |] then
                Some "image/png"
            elif starts [| 255uy; 216uy; 255uy |] then
                Some "image/jpeg"
            elif
                bytes.Length > 12
                && Text.Encoding.ASCII.GetString(bytes, 0, 4) = "RIFF"
                && Text.Encoding.ASCII.GetString(bytes, 8, 4) = "WEBP"
            then
                Some "image/webp"
            else
                None

        if bytes.Length = 0 || bytes.Length > 2_000_000 then
            None
        else
            mime |> Option.map (fun value -> { bytes = bytes; contentType = value })

    let savePhoto id (name: string) (alt: string) (image: Image option) (session: Session) =
        let existing = session.photos |> List.tryFind (fun photo -> photo.id = id)
        let uploading = id = "new"

        let valid =
            not (String.IsNullOrWhiteSpace name)
            && name.Length <= 120
            && not (String.IsNullOrWhiteSpace alt)
            && alt.Length <= 500

        let withoutOld =
            session.images
            |> Map.filter (fun oldKey _ ->
                existing
                |> Option.forall (fun old -> not (old.source.EndsWith("/" + oldKey, StringComparison.Ordinal))))

        let bytes =
            withoutOld |> Map.toSeq |> Seq.sumBy (fun (_, image) -> image.bytes.Length)

        if
            not valid
            || (existing.IsNone && not uploading)
            || (uploading && (image.IsNone || session.photos.Length >= 10))
            || bytes
               + (image |> Option.map (fun image -> image.bytes.Length) |> Option.defaultValue 0) > 8_000_000
        then
            session, None
        else
            let key = if uploading then Guid.NewGuid().ToString("N") else id

            let photo =
                existing
                |> Option.defaultValue
                    { id = key
                      name = name
                      alt = alt
                      source = ""
                      eventId = "" }

            let imageKey = Guid.NewGuid().ToString("N")

            let photo =
                { photo with
                    name = name.Trim()
                    alt = alt.Trim()
                    source =
                        if image.IsSome then
                            "/components/page-examples/images/" + imageKey
                        else
                            photo.source }

            let images =
                match image with
                | Some image -> withoutOld |> Map.add imageKey image
                | None -> session.images

            let photos =
                if uploading then
                    session.photos @ [ photo ]
                else
                    session.photos |> List.map (fun item -> if item.id = key then photo else item)

            { session with
                photos = photos
                images = images },
            Some key
