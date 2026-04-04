# How to use MatPlotLibNet.Maui

## Install

```
dotnet add package MatPlotLibNet.Maui
```

## 1. Add a chart view in XAML

`MplChartView` extends `GraphicsView` and renders a `Figure` natively using `Microsoft.Maui.Graphics`.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:mpl="clr-namespace:MatPlotLibNet.Maui;assembly=MatPlotLibNet.Maui"
             x:Class="MyApp.DashboardPage">

    <VerticalStackLayout Padding="20">
        <Label Text="Sensor Dashboard" FontSize="24" />

        <mpl:MplChartView Figure="{Binding ChartFigure}"
                          HeightRequest="400"
                          WidthRequest="600" />
    </VerticalStackLayout>
</ContentPage>
```

## 2. Create from code-behind

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Maui;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

        var chart = new MplChartView
        {
            HeightRequest = 400,
            WidthRequest = 600,
            Figure = Plt.Create()
                .WithTitle("Temperature")
                .WithTheme(Theme.Dark)
                .Plot(time, values, l =>
                {
                    l.Color = Color.Red;
                    l.Label = "Sensor A";
                })
                .Build()
        };

        Content = chart;
    }
}
```

## 3. Data binding with MVVM

The `Figure` property is a `BindableProperty`, so it works with MAUI data binding. The view invalidates and re-draws automatically when `Figure` changes.

### ViewModel

```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private Figure? _chartFigure;

    [RelayCommand]
    private void Refresh()
    {
        var readings = _sensorService.GetLatest();

        ChartFigure = Plt.Create()
            .WithTitle($"Updated {DateTime.Now:HH:mm:ss}")
            .WithTheme(Theme.Seaborn)
            .Plot(readings.Time, readings.Values)
            .Build();
    }
}
```

### XAML

```xml
<mpl:MplChartView Figure="{Binding ChartFigure}"
                  HeightRequest="400" />

<Button Text="Refresh" Command="{Binding RefreshCommand}" />
```

## 4. Multiple charts in a layout

```xml
<Grid ColumnDefinitions="*,*" RowDefinitions="*,*" Padding="10" ColumnSpacing="10" RowSpacing="10">

    <mpl:MplChartView Figure="{Binding TempChart}"
                      Grid.Row="0" Grid.Column="0" HeightRequest="300" />

    <mpl:MplChartView Figure="{Binding HumidityChart}"
                      Grid.Row="0" Grid.Column="1" HeightRequest="300" />

    <mpl:MplChartView Figure="{Binding PressureChart}"
                      Grid.Row="1" Grid.Column="0" HeightRequest="300" />

    <mpl:MplChartView Figure="{Binding WindChart}"
                      Grid.Row="1" Grid.Column="1" HeightRequest="300" />
</Grid>
```

## 5. Periodic refresh with a timer

```csharp
public partial class LivePage : ContentPage
{
    private readonly MplChartView _chart;

    public LivePage()
    {
        _chart = new MplChartView { HeightRequest = 400 };
        Content = _chart;

        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            var data = ReadSensor();
            _chart.Figure = Plt.Create()
                .WithTitle("Live")
                .Plot(data.X, data.Y)
                .Build();
            return true;  // keep ticking
        });
    }
}
```

## 6. Supported platforms

| Platform | Minimum version |
|----------|----------------|
| Android | 21 |
| iOS | 15.0 |
| Mac Catalyst | 15.0 |
| Windows | 10.0.19041.0 |

## 7. How rendering works

`MplChartView` uses an internal `IDrawable` that:

1. Creates a `MauiGraphicsRenderContext` wrapping the MAUI `ICanvas`
2. Passes it to `ChartRenderer.Render(figure, context)`
3. The renderer translates the figure model into drawing primitives (lines, rectangles, text, paths)

The view calls `Invalidate()` whenever the `Figure` property changes, triggering a re-draw.
