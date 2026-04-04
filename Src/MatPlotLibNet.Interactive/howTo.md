# How to use MatPlotLibNet.Interactive

## Install

```
dotnet add package MatPlotLibNet.Interactive
```

## 1. Show a chart in the browser

Call `Show()` on any `Figure` to open it in your default browser. A lightweight Kestrel server starts automatically on the first call.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Interactive;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

var figure = Plt.Create()
    .WithTitle("My Chart")
    .WithTheme(Theme.Seaborn)
    .Plot(x, y, line => line.Color = Color.Blue)
    .Build();

var handle = figure.Show();

Console.ReadLine(); // keep the process alive
```

`Show()` returns an `InteractiveFigure` handle that you can use to push updates later.

## 2. Update a chart in real-time

After `Show()`, mutate the figure and call `UpdateAsync()` to push the changes to the browser via SignalR.

```csharp
var figure = Plt.Create()
    .WithTitle("Live Sensor Data")
    .Plot(time, values)
    .Build();

var handle = figure.Show();

while (true)
{
    await Task.Delay(2000);

    var newData = ReadSensor();

    // Rebuild or modify the figure
    var updated = Plt.Create()
        .WithTitle($"Sensor -- {DateTime.Now:HH:mm:ss}")
        .Plot(newData.Time, newData.Values, l => l.Color = Color.Red)
        .Build();

    // Replace the figure in the handle and push
    handle = new InteractiveFigure(handle.ChartId, updated);
    await handle.UpdateAsync();
}
```

## 3. Show multiple charts

Each `Show()` call opens a new browser tab. The single Kestrel server handles all of them.

```csharp
var fig1 = Plt.Create()
    .WithTitle("Temperature")
    .Plot(time, temp)
    .Build();

var fig2 = Plt.Create()
    .WithTitle("Humidity")
    .Plot(time, humidity)
    .Build();

var handle1 = fig1.Show();
var handle2 = fig2.Show();

// Update them independently
await handle1.UpdateAsync();
await handle2.UpdateAsync();
```

## 4. Use from a background service

```csharp
public class DashboardService : BackgroundService
{
    private InteractiveFigure? _handle;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var figure = Plt.Create().WithTitle("Dashboard").Build();
        _handle = figure.Show();

        while (!ct.IsCancellationRequested)
        {
            var data = await FetchDataAsync(ct);

            var updated = Plt.Create()
                .WithTitle($"Updated {DateTime.Now:T}")
                .Plot(data.X, data.Y)
                .Build();

            _handle = new InteractiveFigure(_handle.ChartId, updated);
            await _handle.UpdateAsync();

            await Task.Delay(5000, ct);
        }
    }
}
```

## 5. Combine with Blazor DisplayMode

If your app is a Blazor application, use the `DisplayMode` parameter on `MplChart` to switch between inline and popup views.

```razor
@using MatPlotLibNet.Blazor

<!-- Inline (default) -->
<MplChart Figure="@_figure" />

<!-- With expand/collapse overlay -->
<MplChart Figure="@_figure" DisplayMode="DisplayMode.Expandable" />

<!-- With link to open in new tab -->
<MplChart Figure="@_figure"
          DisplayMode="DisplayMode.Popup"
          PopupUrl="http://127.0.0.1:5123/chart/my-chart-id" />
```

## 6. How it works

1. `figure.Show()` calls `ChartServer.Instance.EnsureStarted()` which spins up a Kestrel server on an auto-selected port
2. The figure is registered with a unique GUID chart ID
3. The default browser opens `http://127.0.0.1:{port}/chart/{chartId}`
4. The HTML page renders the initial SVG and connects to a SignalR hub at `/charts-hub`
5. When you call `handle.UpdateAsync()`, the server pushes the re-rendered SVG to the browser via SignalR
6. The browser replaces the chart content without a page reload

## 7. Server lifecycle

- The server starts on the first `Show()` call and stays alive until the process exits
- To shut down explicitly: `await ChartServer.Instance.DisposeAsync()`
- Port is auto-selected (available via `ChartServer.Instance.Port`)
- All charts share a single server instance
