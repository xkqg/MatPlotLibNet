# MatPlotLibNet.DataFrame

[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet.DataFrame)](https://www.nuget.org/packages/MatPlotLibNet.DataFrame)

Extension methods on `Microsoft.Data.Analysis.DataFrame` for [MatPlotLibNet](../../README.md). Plot
directly from a typed DataFrame, group the series by a hue column when you want to, and keep the
full fluent API.

## Install

```
dotnet add package MatPlotLibNet.DataFrame
```

## Quick start

```csharp
using MatPlotLibNet;                    // extension methods live in the top-level namespace
using Microsoft.Data.Analysis;

// Load from CSV or build in memory
var df = DataFrame.LoadCsv("prices.csv");

// Line chart — two numeric columns
df.Line("date", "close")
  .WithTitle("Closing Price")
  .Save("price.svg");

// Scatter with hue grouping — one series per ticker
df.Scatter("date", "close", hue: "ticker")
  .WithTitle("Portfolio")
  .Save("portfolio.svg");

// Histogram with hue grouping
df.Hist("returns", bins: 40, hue: "sector")
  .WithTitle("Return Distribution by Sector")
  .Save("returns.svg");
```

## API

### Charting — `DataFrameFigureExtensions`

All three return a `FigureBuilder`, so the chain goes on to `.Build()`, `.ToSvg()` or `.Save(path)`:

| Call | Returns |
|---|---|
| `df.Line(x, y, hue?, palette?)` | `FigureBuilder` |
| `df.Scatter(x, y, hue?, palette?)` | `FigureBuilder` |
| `df.Hist(column, bins?, hue?, palette?)` | `FigureBuilder` |

### Financial Indicators — `DataFrameIndicatorExtensions` and `Values`

An indicator is made from its own class in `MatPlotLibNet.Indicators`, which says its parameters and their defaults; the
package reads its input from named columns, with one call for each shape of bars an indicator takes and one for a single
column. Output arrays are **trimmed**, not NaN-padded: their length is `n - warmUp`, where warmUp depends on the period.

| Call | Returns | For |
|---|---|---|
| `df.Values(column)` | `double[]` | one column: SMA, EMA, RSI, MACD, Bollinger, ... |
| `df.Hlc(highCol, lowCol, closeCol)` | `OhlcvBars` | ADX, ATR, CCI, Williams %R, stochastic, Keltner, ... |
| `df.Hlcv(highCol, lowCol, closeCol, volumeCol)` | `OhlcvBars` | VWAP, Klinger, Twiggs money flow |
| `df.Ohlc(openCol, highCol, lowCol, closeCol)` | `OhlcvBars` | Garman-Klass, Yang-Zhang, RVI |
| `df.Cv(closeCol, volumeCol)` | `OhlcvBars` | force index, Amihud, VPIN, VWAP z-score |
| `df.Hl(highCol, lowCol)` | `OhlcvBars` | Aroon oscillator, Corwin-Schultz |
| `df.Hlv(highCol, lowCol, volumeCol)` | `OhlcvBars` | ease of movement |

```csharp
// Example: candlestick + SMA + Bollinger Bands overlay
double[]    close = df.Values("close");
double[]    sma20 = new Sma(close, 20).Compute();
BandsResult bb    = new BollingerBands(close, period: 20).Compute();
double[]    atr   = new Atr(df.Hlc("high", "low", "close")).Compute();

string svg = Plt.Create()
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.UseBarSlotX()
          .Candlestick(open, high, low, close)
          .Signal(sma20, configure: s => s.Label = "SMA 20")
          .FillBetween(xVals, bb.Upper, bb.Lower, s => s.Alpha = 0.2);
    })
    .WithTitle("Price + Bollinger Bands")
    .ToSvg();
```

### Polynomial Regression — `DataFrameNumericsExtensions`

| Call | Returns | Does |
|---|---|---|
| `df.PolyFit(xCol, yCol, degree)` | `double[]` | fits a polynomial of the given degree to two numeric columns |
| `df.PolyEval(xCol, coefficients)` | `double[]` | evaluates the fitted polynomial at every value of the X column |
| `df.ConfidenceBand(xCol, yCol, coefficients, evalX, level?)` | `ConfidenceBand` | the confidence band of the fit, `level` 0.95 by default; a `ConfidenceBand` has `Upper` and `Lower` |

```csharp
// Example: scatter + linear fit + 95 % confidence band
double[] xVals  = DataFrameColumnReader.ToDoubleArray(df["x"]);
double[] yVals  = DataFrameColumnReader.ToDoubleArray(df["y"]);
double[] coeffs = df.PolyFit("x", "y", degree: 1);
double[] fitY   = df.PolyEval("x", coeffs);
ConfidenceBand band = df.ConfidenceBand("x", "y", coeffs, evalX: xVals);

string svg = Plt.Create()
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Scatter(xVals, yVals, s => s.Label = "Data")
          .Plot(xVals, fitY, s => s.Label = "Linear fit")
          .FillBetween(xVals, band.Upper, band.Lower,
              s => { s.Alpha = 0.2; s.Label = "95 % CI"; });
    })
    .WithTitle("Regression with Confidence Band")
    .ToSvg();
```

If a column is not found, the call throws `ArgumentException` and reports the unknown column name.

## Column type support

| C# type | ToDoubleArray | ToStringArray |
|---------|--------------|---------------|
| `double` | identity | `.ToString()` |
| `float` | widening | `.ToString()` |
| `int` / `long` / `short` / `byte` | `Convert.ToDouble` | `.ToString()` |
| `decimal` | `Convert.ToDouble` | `.ToString()` |
| `DateTime` | `DateTime.ToOADate()` | `.ToString()` |
| `string` | not supported | identity |
| `null` | `double.NaN` | `""` |

## How it works

The extensions use `DataFrameColumnReader` to materialise the named DataFrame columns as `double[]`
or `string[]`. They then hand all grouping, palette cycling, and series creation to the existing
`EnumerableFigureExtensions.Line / Scatter / Hist` methods in the core package. No grouping code is
duplicated. The DataFrame package is about 100 lines that only pass the data along.

## Related packages

| Package | Purpose |
|---------|---------|
| `MatPlotLibNet` | Core charting library |
| `MatPlotLibNet.Blazor` | Blazor component with interactive features |
| `MatPlotLibNet.Notebooks` | Inline rendering in Polyglot Notebooks and Jupyter |
| `MatPlotLibNet.Mcp` | MCP server that lets an AI agent render charts over stdio |
| `MatPlotLibNet.AspNetCore` | ASP.NET Core middleware (`/chart` endpoints) |
