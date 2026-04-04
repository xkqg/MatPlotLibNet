# MatPlotLibNet.Interactive

Interactive display for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Show charts in a browser popup with live SignalR updates.

## Installation

```
dotnet add package MatPlotLibNet.Interactive
```

## Quick Start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Interactive;

var figure = Plt.Create()
    .WithTitle("My Chart")
    .Plot(x, y)
    .Build();

// Opens default browser with the chart
var handle = figure.Show();

// Later, update the chart in the browser
figure.Title = "Updated Chart";
await handle.UpdateAsync();
```

## License

[GPL-3.0](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
