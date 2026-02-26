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
<PackageReference Include="FSharp.ViewEngine" Version="2026.2.2" />
```

## Requirements

- .NET 8.0, 9.0, or 10.0

## Next Steps

Once you have FSharp.ViewEngine installed, head over to the [Usage](usage) guide to start building your first HTML views.