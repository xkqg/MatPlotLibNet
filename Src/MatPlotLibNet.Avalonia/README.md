# MatPlotLibNet.Avalonia

Avalonia 12 chart control for [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet).  
Renders charts natively via SkiaSharp — no JavaScript, no browser, no SignalR required.

## Installation

```bash
dotnet add package MatPlotLibNet.Avalonia
```

## Basic usage

```xml
<!-- XAML -->
<local:MplChartControl Figure="{Binding MyFigure}" />
```

```csharp
// Code-behind / ViewModel
var figure = Plt.Create()
    .Plot([1.0, 2.0, 3.0], [4.0, 2.0, 5.0])
    .WithTitle("Hello Avalonia")
    .Build();

MyFigure = figure;
```

## Interactive mode

Set `IsInteractive="True"` to enable local pan / zoom / reset / brush-select:

```xml
<local:MplChartControl
    Figure="{Binding MyFigure}"
    IsInteractive="True" />
```

| Gesture | Action |
|---|---|
| Left-drag | Pan |
| Scroll wheel | Zoom (cursor-centered) |
| Double-click | Reset to original limits |
| Home / Escape | Reset to original limits |
| Shift + left-drag | Brush select (fires `BrushSelectEvent` to your custom sink) |

## Custom event sink (server mode)

For SignalR-connected charts, construct the `InteractionController` with your own sink:

```csharp
var ctrl = InteractionController.Create(figure, layout, evt =>
{
    hubConnection.SendAsync("ApplyEvent", evt);
});
```

## Requirements

- .NET 8 or .NET 10
- Avalonia 12 with Skia backend (the default on all platforms)
