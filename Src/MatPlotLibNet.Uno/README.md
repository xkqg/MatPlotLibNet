# MatPlotLibNet.Uno

Uno Platform (WinUI 3) chart element for [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet).  
Renders charts natively via SkiaSharp across Windows, Android, iOS, and macCatalyst —
no JavaScript, no browser, no SignalR required.

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

Set `IsInteractive="True"` to enable local pan / zoom / reset / brush-select:

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

## Requirements

- .NET 10
- Uno.WinUI 5.2+
- Supported platforms: Windows 10 19041+, Android 21+, iOS 15+, macCatalyst 15+
