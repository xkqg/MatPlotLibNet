# MatPlotLibNet.Maui

.NET MAUI control for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts natively via `Microsoft.Maui.Graphics`.

## Installation

```
dotnet add package MatPlotLibNet.Maui
```

## Quick Start

### XAML

```xml
<ContentPage xmlns:mpl="clr-namespace:MatPlotLibNet.Maui;assembly=MatPlotLibNet.Maui">
    <mpl:MplChartView Figure="{Binding ChartFigure}" />
</ContentPage>
```

### Code-behind

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Maui;

var chart = new MplChartView
{
    Figure = Plt.Create()
        .WithTitle("Sensor Data")
        .Plot(time, values, l => l.Color = MatPlotLibNet.Styling.Colors.Blue)
        .Build()
};
```

`MplChartView` extends `GraphicsView` and re-renders automatically when the `Figure` property changes. Set `IsInteractive`
and a finger works the chart: a drag does what `DragAction` says (a pan unless told otherwise), a pinch zooms, a tap picks
the point under it and a double tap goes Home. `WithServerInteraction` sends the gestures through a delegate (typically
`hubConnection.SendAsync`) instead of applying them to the figure, and `AnimationSource` shows the frames of an
`IAnimationSource`. `MplStreamingChartView` draws a `StreamingFigure` and redraws it when data arrives.

## Supported platforms

- Android 21+
- iOS 15+
- Mac Catalyst 15+
- Windows 10.0.19041+

## Dependencies

- `MatPlotLibNet` (core)
- `Microsoft.Maui.Controls`

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
