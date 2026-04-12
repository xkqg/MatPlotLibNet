# MatPlotLibNet.DataFrame

`Microsoft.Data.Analysis.DataFrame` extension methods for [MatPlotLibNet](../../README.md) — plot
directly from typed DataFrames with optional hue grouping and full fluent API support.

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

```csharp
// All three methods return FigureBuilder — chainable before .Build() / .ToSvg() / .Save()

FigureBuilder df.Line(string x, string y, string? hue = null, Color[]? palette = null)
FigureBuilder df.Scatter(string x, string y, string? hue = null, Color[]? palette = null)
FigureBuilder df.Hist(string column, int bins = 30, string? hue = null, Color[]? palette = null)
```

### Financial Indicators — `DataFrameIndicatorExtensions`

All indicator methods resolve the named column(s) to `double[]` and delegate to the core indicator types.
Output arrays are **trimmed** (not NaN-padded) — length equals `n - warmUp` where warmUp depends on the indicator period.

```csharp
// Price indicators (single close/price column)
double[]      df.Sma(string priceCol, int period)
double[]      df.Ema(string priceCol, int period)
double[]      df.Rsi(string priceCol, int period = 14)
BandsResult   df.BollingerBands(string priceCol, int period = 20, double stdDev = 2.0)
double[]      df.Obv(string closeCol, string volumeCol)
MacdResult    df.Macd(string priceCol, int fast = 12, int slow = 26, int signal = 9)
double[]      df.DrawDown(string priceCol)

// Candle indicators (high / low / close columns)
double[]      df.Adx(string highCol, string lowCol, string closeCol, int period = 14)
AdxResult     df.AdxFull(string highCol, string lowCol, string closeCol, int period = 14)
double[]      df.Atr(string highCol, string lowCol, string closeCol, int period = 14)
double[]      df.Cci(string highCol, string lowCol, string closeCol, int period = 20)
double[]      df.WilliamsR(string highCol, string lowCol, string closeCol, int period = 14)
StochasticResult df.Stochastic(string highCol, string lowCol, string closeCol, int period = 14)
double[]      df.ParabolicSar(string highCol, string lowCol, double step = 0.02, double max = 0.2)
BandsResult   df.KeltnerChannels(string highCol, string lowCol, string closeCol, int period = 20, double atrMultiplier = 1.5)
double[]      df.Vwap(string highCol, string lowCol, string closeCol, string volumeCol)
```

Result types:

| Type | Properties |
|------|-----------|
| `BandsResult` | `Middle[]`, `Upper[]`, `Lower[]` |
| `MacdResult` | `MacdLine[]`, `SignalLine[]`, `Histogram[]` |
| `AdxResult` | `Adx[]`, `PlusDi[]`, `MinusDi[]` |
| `StochasticResult` | `K[]`, `D[]` |

```csharp
// Example: candlestick + SMA + Bollinger Bands overlay
double[]    sma20 = df.Sma("close", 20);
BandsResult bb    = df.BollingerBands("close", period: 20, stdDev: 2.0);

string svg = Plt.Create()
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.UseBarSlotX()
          .Candlestick(open, high, low, close)
          .Signal(sma20, label: "SMA 20")
          .FillBetween(xVals, bb.Upper, bb.Lower, s => s.Alpha = 0.2);
    })
    .WithTitle("Price + Bollinger Bands")
    .ToSvg();
```

### Polynomial Regression — `DataFrameNumericsExtensions`

```csharp
// Fit a polynomial of the given degree to two numeric columns
double[] coeffs = df.PolyFit(string xCol, string yCol, int degree)

// Evaluate the fitted polynomial at every value in the X column
double[] fitY   = df.PolyEval(string xCol, double[] coefficients)

// Compute a confidence band for the fitted polynomial
ConfidenceBand band = df.ConfidenceBand(string xCol, string yCol,
                          double[] coefficients, double[] evalX, double level = 0.95)
// ConfidenceBand has Upper[] and Lower[]
```

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

Column name resolution throws `ArgumentException` with the unknown column name when a column is not found.

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

The extensions materialise named DataFrame columns to `double[]` or `string[]` via
`DataFrameColumnReader`, then delegate all grouping, palette cycling, and series-creation logic to the
existing `EnumerableFigureExtensions.Line / Scatter / Hist` methods in the core package. No grouping
code is duplicated — the DataFrame package is ~100 lines of pure plumbing.

## Related packages

| Package | Purpose |
|---------|---------|
| `MatPlotLibNet` | Core charting library |
| `MatPlotLibNet.Blazor` | Blazor component + interactive features |
| `MatPlotLibNet.Notebooks` | Polyglot Notebooks / Jupyter inline rendering |
| `MatPlotLibNet.AspNetCore` | ASP.NET Core middleware (`/chart` endpoints) |
