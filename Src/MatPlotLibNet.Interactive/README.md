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

// Opens default browser with the chart; disposing the handle lets the server go of the figure
using var handle = await figure.ShowAsync();

// Later, update the chart in the browser
figure.Title = "Updated Chart";
await handle.UpdateAsync();
```

The server keeps a shown figure so its page can be loaded again for as long as the handle lives. A program that shows many
figures disposes each handle when it is done with the chart; after that its page is no longer served and the handle pushes
nothing more.

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort

The package embeds the SignalR browser client; its licence is in `THIRD-PARTY-NOTICES.md` in the package.
