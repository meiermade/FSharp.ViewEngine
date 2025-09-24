# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview
FSharp.ViewEngine is a view engine for F# web applications, inspired by Giraffe.ViewEngine and Feliz.ViewEngine. It provides a functional approach to generating HTML with type-safe F# code, including integrated support for HTMX, Alpine.js, Tailwind CSS, and SVG elements.

## Core Architecture

### Element System
The view engine is built around a discriminated union system:
- **Element**: The core type representing DOM elements (Text, Tag, Void, Fragment, Raw, Noop)
- **Attribute**: Represents HTML attributes (KeyValue, Boolean, Children, Noop)
- All HTML is generated through composition of these types

### Module Structure
- `Core.fs`: Defines the core Element and Attribute types, plus the rendering engine
- `Html.fs`: Standard HTML elements and attributes (div, p, h1, _class, _id, etc.)
- `Htmx.fs`: HTMX-specific attributes (_hxGet, _hxPost, _hxTarget, etc.)
- `Alpine.fs`: Alpine.js directives (_xData, _xShow, _xModel, etc.)
- `Tailwind.fs`: Tailwind UI custom elements (el-select, el-dialog, etc.)
- `Svg.fs`: SVG elements and attributes

## Common Development Commands

### Build and Test
```bash
# Restore tools and packages
dotnet tool restore
dotnet paket install

# Run tests (cross-platform)
./fake.sh Test      # Linux/macOS
./fake.cmd Test     # Windows

# Alternative direct test command
dotnet test src/Tests/Tests.fsproj
```

### Package Management
```bash
# Add NuGet package dependencies
# Edit paket.dependencies file, then:
dotnet paket install

# Update packages
dotnet paket update
```

### Building and Packaging
```bash
# Clean build artifacts
./fake.sh Clean     # Linux/macOS
./fake.cmd Clean    # Windows

# Create NuGet package (requires GITHUB_REF_NAME environment variable)
./fake.sh Pack      # Linux/macOS
./fake.cmd Pack     # Windows
```

### Single Test Execution
To run a specific test, use xunit's filtering:
```bash
# Run specific test by name pattern
dotnet test src/Tests/Tests.fsproj --filter "Should render html document"
```

## Development Patterns

### Creating New HTML Elements
When adding HTML elements to `Html.fs`, follow the established patterns:
- Standard elements: `static member elementName (attrs:Attribute seq) = Tag ("elementname", attrs)`
- Void elements (self-closing): Use `Void` instead of `Tag`
- Convenience overloads for common cases (e.g., `p (text:string)` for simple text paragraphs)

### Adding Framework-Specific Attributes
- HTMX attributes go in `Htmx.fs` with `_hx` prefix
- Alpine.js directives go in `Alpine.fs` with `_x` prefix
- Use the `_frameworkPrefix` function pattern for consistency

### Testing HTML Output
Tests use string comparison with whitespace normalization. The `String.clean` helper removes excess whitespace for reliable comparisons. Use the `// language=HTML` comment for syntax highlighting in expected output strings.

## Key Dependencies
- **Paket**: Package management
- **FAKE**: Build automation via F# DSL
- **xUnit**: Testing framework
- **System.Web**: HTML encoding utilities

## Usage Pattern
The library uses F# static type extensions with `open type` declarations:
```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx
open type Alpine

// Creates type-safe, composable HTML
div [
    _class "container"
    _hxGet "/api/data"
    _xData "{loading: false}"
    _children [
        h1 [ _children "Hello World" ]
    ]
]
|> Element.render
```

## Build System Notes
- Uses FAKE build system with F# build scripts in `src/Build/Program.fs`
- Cross-platform build scripts: `fake.sh` (Unix) and `fake.cmd` (Windows)
- Targets: Test, Clean, Pack, Publish
- Version extraction from Git tags for packaging
- CI/CD via GitHub Actions for PR validation and releases