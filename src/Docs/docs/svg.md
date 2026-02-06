# SVG

FSharp.ViewEngine provides type-safe SVG elements and attributes through the `Svg` type.

## Setup

Open the `Svg` type to access SVG elements and attributes:

```fsharp
open FSharp.ViewEngine
open type Html
open type Svg
```

## Elements

### svg

The root SVG container element:

```fsharp
svg {
    _viewBox "0 0 24 24"
    _width 24
    _height 24
    // child elements here
}
```

### path

Define a shape using a path data string:

```fsharp
svg {
    _viewBox "0 0 24 24"
    _fill "none"
    path {
        _d "M12 2L2 22h20L12 2z"
        _stroke "currentColor"
        _strokeWidth 2
    }
}
```

### circle

Draw a circle:

```fsharp
svg {
    _viewBox "0 0 100 100"
    circle {
        _cx 50
        _cy 50
        _r 40
        _fill "blue"
        _stroke "black"
        _strokeWidth 2
    }
}
```

## Attributes

### Dimensions

Set the width and height of the SVG:

```fsharp
svg { _width 48; _height 48 }
```

### viewBox

Define the coordinate system and aspect ratio:

```fsharp
svg { _viewBox "0 0 100 100" }
```

### Fill and Stroke

Control the fill color and stroke styling:

```fsharp
path {
    _d "M0 0 L10 10"
    _fill "none"
    _stroke "red"
    _strokeWidth 2
    _strokeLinecap "round"
    _strokeLinejoin "round"
}
```

### Fill Rule and Clip Rule

Control how overlapping paths are filled or clipped:

```fsharp
path {
    _d "M0 0 L10 10 L20 0 Z"
    _fillRule "evenodd"
    _clipRule "evenodd"
}
```

## Complete Example

Here's a complete icon example:

```fsharp
let checkIcon =
    svg {
        _viewBox "0 0 24 24"
        _width 24
        _height 24
        _fill "none"
        path {
            _d "M5 13l4 4L19 7"
            _stroke "currentColor"
            _strokeWidth 2
            _strokeLinecap "round"
            _strokeLinejoin "round"
        }
    }
```

And a more complex example with multiple elements:

```fsharp
let statusIcon =
    svg {
        _viewBox "0 0 100 100"
        _width 100
        _height 100
        circle {
            _cx 50
            _cy 50
            _r 45
            _fill "green"
            _stroke "darkgreen"
            _strokeWidth 2
        }
        path {
            _d "M30 50 L45 65 L70 35"
            _fill "none"
            _stroke "white"
            _strokeWidth 6
            _strokeLinecap "round"
            _strokeLinejoin "round"
        }
    }
```
