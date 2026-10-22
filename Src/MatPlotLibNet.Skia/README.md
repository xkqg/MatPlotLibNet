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

## Resolution, title and author

A figure is laid out in units of one pixel at 96 dpi, and a PNG is drawn with `dpi / 96` pixels for each unit. A figure
given `WithDpi(192)` saves as a PNG twice as wide and twice as tall as its size, with the same layout, text and line
widths, drawn finer: 500 by 350 becomes 1000 by 700 pixels. The pixel count is rounded to a whole number (800 by 600 at
150 dpi is 1250 by 938), and a dpi that is not positive is refused. The frames of an animated GIF follow their figure's
dpi the same way.

`SaveOptions` set the resolution, the title and the author for one save, whatever the figure holds:

```csharp
figure.Save("chart.png", new SaveOptions { Dpi = 300, Title = "Revenue", Author = "Finance" });
figure.Save("chart.pdf", new SaveOptions { Title = "Revenue", Author = "Finance" });
```

A PNG keeps the title and the author as text chunks with the keywords Title and Author. A PDF keeps them in its
document information. A `Dpi` other than 96 replaces the figure's own resolution for that save. A PDF page is a vector
drawing, one point to a unit, so the dpi changes nothing in a PDF (nor in an SVG).

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
