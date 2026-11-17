namespace Docs.ComponentPages.FileSelection

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let fileSelectionRegistration =
        registration "components-file-selection" "/components/file-selection" "File selection" "File selection"

    let defaultFileSelection =
        fveField {
            fveLabel {
                _for "selected-file"
                "File"
            }

            fveFileSelection {
                _id "selected-file"
                _name "selectedFile"
            }
        }

    let fileSelectionExample =
        fveField {
            fveLabel {
                _for "selected-files"
                "Files"
            }

            fveFileSelection {
                _id "selected-files"
                _name "selectedFiles"
                _accept ".csv,text/csv"
                _multiple true
                _ariaDescribedby "selected-files-help"
            }

            fveFieldDescription {
                _id "selected-files-help"
                "Choose one or more CSV files."
            }
        }

    let pdfSelectionValidation =
        "const file = el.files[0]; const message = !file ? 'Select a PDF document.' : !file.name.toLowerCase().endsWith('.pdf') ? 'The selected file must be a PDF.' : ''; el.setCustomValidity(message); $_pdf_selection_error = message;"

    let invalidFileSelectionExample =
        fveField {
            fveLabel { _for "required-file"; "Document (required)" }
            fveFileSelection {
                _id "required-file"
                _name "requiredFile"
                _accept ".pdf,application/pdf"
                _required true
                _ariaDescribedby "required-file-help"
                _dataSignals "{_pdf_selection_error: ''}"
                _dataOn ("change", pdfSelectionValidation)
                _dataOn ("invalid", pdfSelectionValidation)
                _dataOn ("reset", ["window"], "if (evt.target === el.form) queueMicrotask(() => { if (!evt.defaultPrevented) { el.setCustomValidity(''); $_pdf_selection_error = ''; } })")
                _dataAttr ("aria-invalid", "$_pdf_selection_error ? 'true' : null")
                _dataAttr ("aria-describedby", "$_pdf_selection_error ? 'required-file-help required-file-error' : 'required-file-help'")
            }
            fveFieldDescription { _id "required-file-help"; "PDF only." }
            fveFieldError {
                _id "required-file-error"
                _style "display:none"
                _dataShow "$_pdf_selection_error"
                _dataText "$_pdf_selection_error"
            }
        }

    let disabledFileSelectionExample =
        fveField {
            fveLabel {
                _for "disabled-files"
                "Files unavailable"
            }

            fveFileSelection {
                _id "disabled-files"
                _name "disabledFiles"
                _disabled true
            }
        }

    let examples () =
            [ sample "file-selection-default" "Default" [ "defaultFileSelection" ] (fieldSurface defaultFileSelection)
              sample "file-selection" "Multiple files" [ "fileSelectionExample" ] (fieldSurface fileSelectionExample)
              sample
                  "file-selection-validation"
                  "Validation"
                  [ "pdfSelectionValidation"; "invalidFileSelectionExample" ]
                  (fieldSurface invalidFileSelectionExample)
              sample
                  "file-selection-disabled"
                  "Disabled"
                  [ "disabledFileSelectionExample" ]
                  (fieldSurface disabledFileSelectionExample) ]


    let private galleryContent : Content =
        { description = "Collect one or more files with accessible native selection and validation."
          usage = [
              prose "FileSelection keeps a native chooser and always provides a quiet Clear action when files are selected. Clear is hidden while empty or unavailable for editing; required fields can still be cleared. Clearing returns focus to the chooser and emits native input/change events. Its accessible name identifies the field and, for a single selection, the filename. The native control owns the filename or multiple-file count—there is no automatic duplicate list. Field owns the label, help and error."
              prose "Validate a selection on change, or a missing required file when submitting—not when the file picker takes focus or is cancelled. accept only guides the picker. The validation example checks the filename extension for local feedback and uses setCustomValidity; your server validates the actual upload, including content, type and size. Selection alone does not upload anything."
              prose "A richer multi-file list can offer per-file Remove actions and, when useful, Clear all. Keep that upload/queue composition in your application rather than adding it to every file input."
          ]
          sections = []
          apiCategory = Some "file-selection" }

    let fileSelectionPage =
        gallery fileSelectionRegistration galleryContent (examples ())
