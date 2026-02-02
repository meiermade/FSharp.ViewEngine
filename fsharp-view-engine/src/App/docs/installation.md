# Installation

FSharp.ViewEngine is distributed as a NuGet package. You can install it using your preferred package manager.

## Using .NET CLI

```bash
dotnet add package FSharp.ViewEngine
```

## Using Paket

Add to your `paket.dependencies`:

```text
nuget FSharp.ViewEngine
```

Then add to your `paket.references`:

```text
FSharp.ViewEngine
```

## Using PackageReference

Add to your `.fsproj` file:

```xml
<PackageReference Include="FSharp.ViewEngine" Version="2025.9.1" />
```

## Requirements

- .NET 10.0 or later
- F# 10 or later

## Next Steps

Once you have FSharp.ViewEngine installed, head over to the [Quickstart](quickstart) guide to start building your first HTML views.