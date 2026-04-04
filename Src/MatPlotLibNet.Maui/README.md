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
        .Plot(time, values, l => l.Color = Color.Blue)
        .Build()
};
```

`MplChartView` extends `GraphicsView` and re-renders automatically when the `Figure` property changes.

## Supported platforms

- Android 21+
- iOS 15+
- Mac Catalyst 15+
- Windows 10.0.19041+

## Dependencies

- `MatPlotLibNet` (core)
- `Microsoft.Maui.Controls`

## License

[GPL-3.0](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
