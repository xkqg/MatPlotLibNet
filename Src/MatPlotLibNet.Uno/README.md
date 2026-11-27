# MatPlotLibNet.Uno

Uno Platform (WinUI 3) chart element for [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet).  
It renders charts natively with SkiaSharp on Windows, Android, iOS, and macCatalyst.
It does not need JavaScript, a browser, or SignalR.

## Installation

```bash
dotnet add package MatPlotLibNet.Uno
```

## Basic usage

```xml
<!-- XAML -->
<local:MplChartElement Figure="{x:Bind MyFigure, Mode=OneWay}" />
```

```csharp
// Code-behind / ViewModel
var figure = Plt.Create()
    .Plot([1.0, 2.0, 3.0], [4.0, 2.0, 5.0])
    .WithTitle("Hello Uno")
    .Build();

MyFigure = figure;
```

## Interactive mode

Set `IsInteractive="True"` and the element handles pan, zoom, reset, and brush-select locally:

```xml
<local:MplChartElement
    Figure="{x:Bind MyFigure, Mode=OneWay}"
    IsInteractive="True" />
```

| Gesture | Action |
|---|---|
| Left-drag | Pan |
| Scroll wheel | Zoom (cursor-centered) |
| Double-click | Reset to original limits |
| Home / Escape | Reset to original limits |
| Shift + left-drag | Brush select (fires `BrushSelectEvent` to your custom sink) |

## Server mode

`WithServerInteraction` makes a chart element or a streaming chart element interactive and sends each gesture through the
delegate you give it (typically `hubConnection.SendAsync`) instead of applying it to the figure; whoever receives it applies it:

```csharp
element.WithServerInteraction((method, payload) => hubConnection.SendAsync(method, payload));
```

## Animation

Set `AnimationSource` on a `MplChartElement` to an `IAnimationSource` (an `AnimationBuilder`'s animation, say) and the element
shows each frame it raises, on the UI thread.

## Requirements

- .NET 10
- Uno.WinUI 5.2+
- Supported platforms: Windows 10 19041+, Android 21+, iOS 15+, macCatalyst 15+
