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

## Text and fonts

Text is shaped with HarfBuzz. Arabic letters join, Hebrew reads right to left, Latin pairs are kerned, and a label
that mixes scripts is laid out in reading order. The font that ships in the package, DejaVu Sans, covers Latin,
Greek, Cyrillic, Arabic and Hebrew.

For other scripts, register a font file once at startup and use its family name:

```csharp
var font = SkiaFonts.Register("fonts/NotoSansDevanagari-Regular.ttf");
var theme = Theme.CreateFrom(Theme.Seaborn).WithFont(f => f with { Family = font.Family }).Build();
```

A font installed on the machine can be used by its family name without registering it. When a family cannot be
found anywhere, the chart falls back to the operating system's default face and reports the fallback once through
`ChartDiagnostics.Emitted`.

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
