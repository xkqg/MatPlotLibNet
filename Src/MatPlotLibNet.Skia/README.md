# MatPlotLibNet.Skia

PNG and PDF export for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library using [SkiaSharp](https://github.com/mono/SkiaSharp).

## Installation

```
dotnet add package MatPlotLibNet.Skia
```

## Usage

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Transforms;

var figure = Plt.Create()
    .WithTitle("My Chart")
    .Plot([1, 2, 3, 4, 5], [2, 4, 3, 5, 1])
    .Build();

// Export as PNG
figure.Transform(new PngTransform()).ToFile("chart.png");

// Export as PDF
figure.Transform(new PdfTransform()).ToFile("chart.pdf");

// Export to stream
using var stream = new MemoryStream();
figure.Transform(new PngTransform()).ToStream(stream);

// Export to byte array
byte[] bytes = figure.Transform(new PdfTransform()).ToBytes();
```

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
