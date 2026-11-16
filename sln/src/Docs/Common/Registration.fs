namespace Docs.Common

module Registration =
    let registration id path navLabel title : DocPage =
        { id = id
          path = path
          aliases = []
          navLabel = navLabel
          category = "Components"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Components"
          nodes = [] }

    let applicationRegistration id path navLabel title = registration id path navLabel title

    let packageRegistration id path navLabel title =
        { registration id path navLabel title with
            category = "Source distribution" }
