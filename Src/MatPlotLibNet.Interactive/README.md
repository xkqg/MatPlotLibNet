# MatPlotLibNet.Interactive

MatPlotLibNet.Interactive is the interactive display for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. It shows charts in a browser popup and updates them live over SignalR.

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
var handle = await figure.ShowAsync();

// Later, update the chart in the browser
figure.Title = "Updated Chart";
await handle.UpdateAsync();
```

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
