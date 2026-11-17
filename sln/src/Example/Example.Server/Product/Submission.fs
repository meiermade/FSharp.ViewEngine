namespace Example.Server.Product

open Example.Components.Views.Model

/// Response-only form outcome; private drafts never become URL or session values.
type Submission =
    { state: string
      selection: string list
      draft: EditorDraft option
      destination: ApplicationPage option }

module Submission =
    let result state =
        { state = state; selection = []; draft = None; destination = None }
