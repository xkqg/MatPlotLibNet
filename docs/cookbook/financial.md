---
title: "Candlestick and OHLC charts in C#"
description: "Draw financial charts in C#: candlesticks, OHLC bars, volume panels, Bollinger Bands, RSI and other technical indicators, an anchored VWAP and a volume profile."
---

# Financial Charts

> **Install:** `dotnet add package MatPlotLibNet`

## Financial dashboard template

The built-in `FigureTemplates.FinancialDashboard` creates a 3-panel layout. The top panel shows the price with Bollinger Bands. The middle panel shows volume. The bottom panel shows an oscillator, such as RSI or CCI.

```csharp
FigureTemplates.FinancialDashboard(
        open, high, low, close, vol,
        title: "ACME Corp — 50 Day",
        configurePricePanel: ax => ax.BollingerBands(20),
        configureVolumePanel: ax => ax
            .SetYTickLocator(new MaxNLocator(3))
            .SetYTickFormatter(new EngFormatter()),
        configureOscillatorPanel: ax => ax
            .Rsi(close, 14)
            .SetYLim(0, 100)
            .AxHLine(70, l => { l.Color = Colors.Red;   l.LineStyle = LineStyle.Dashed; })
            .AxHLine(20, l => { l.Color = Colors.Green; l.LineStyle = LineStyle.Dashed; }))
    .WithSize(1200, 900)
    .Save("financial_dashboard.svg");
```

![Financial dashboard](../images/financial_dashboard.png)

## Technical indicators

This example plots four indicators: Williams %R, On-Balance Volume, Parabolic SAR, and CCI:

```csharp
Plt.Create()
    .WithTitle("Momentum and Volume Indicators")
    .WithSize(1000, 800)
    .WithGridSpec(2, 2)
    .TightLayout()
    .AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
    {
        ax.Plot(x, close);
        ax.ParabolicSar(high, low);
        ax.WithTitle("Parabolic SAR");
    })
    .AddSubPlot(new GridPosition(0, 1, 1, 2), ax =>
    {
        ax.WilliamsR(MatPlotLibNet.Indicators.OhlcvBars.Hlc(high, low, close), 14);
        ax.WithTitle("Williams %R");
    })
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
    {
        ax.Obv(close, vol);
        ax.WithTitle("On-Balance Volume");
    })
    .AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
    {
        ax.Cci(MatPlotLibNet.Indicators.OhlcvBars.Hlc(high, low, close), 20);
        ax.WithTitle("CCI(20)");
    })
    .Save("phase_f_indicators.svg");
```

![Technical indicators](../images/phase_f_indicators.png)

### Anchored VWAP and volume profile

`Vwap(bars, anchor)` averages the typical price, (high + low + close) / 3, weighted by volume, from the anchor bar on:
the average price paid since a swing, a gap or an earnings day. Before the anchor there is no value and the line starts at
the anchor bar; with the default anchor 0 it is the cumulative VWAP. `VolumeProfile(bars, bins)` cuts the range of the
closes into equal price bins (12 by default) and adds each bar's volume to the bin its close falls in, drawn as horizontal
bars on the secondary X axis, growing from the right edge, the longest covering a quarter of the plot. A bin takes the
closes from its lower edge up to, but not including, its upper edge; the last bin includes its upper edge.

```csharp
Plt.Create()
    .WithTitle("Volume by price, and the VWAP since bar 30")
    .WithSize(900, 500)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(barIndex, closes, line => line.Label = "Close")
        .Vwap(MatPlotLibNet.Indicators.OhlcvBars.Hlcv(highs, lows, closes, volumes), anchor: 30, vwap => vwap.Label = "VWAP from bar 30")
        .VolumeProfile(MatPlotLibNet.Indicators.OhlcvBars.Cv(closes, volumes))
        .SetXLabel("Bar")
        .SetYLabel("Price")
        .WithLegend())
    .Save("volume_profile.svg");
```

![Volume by price and a VWAP anchored at bar 30](../images/volume_profile.png)

The streaming form is `StreamingVwap`, whose `AnchorHere()` forgets the sums so the average restarts at the next bar, and
`df.Vwap(highCol, lowCol, closeCol, volumeCol, anchor)` is the DataFrame bridge. `Vwap(prices, volumes)` refuses arrays of
different lengths, and the profile skips a bar whose close or volume is missing.

## Relative Rotation Graph (RRG)

The `RelativeRotation` method renders a JdK-style 2D scatter of (RS-Ratio, RS-Momentum) per asset
against a benchmark. It adds a fading tail and a 100/100 quadrant grid.

While the quadrant grid is on, the range of the plot always includes the 100/100 centre, so the four quadrants and the crosshair
are in view even when every asset leads or every asset lags:

```csharp
using MatPlotLibNet;

int rotationWeeks = 80;
var rotationNoise = new Random(3);
double[] rotationBenchmark = new double[rotationWeeks];
double rotationLevel = 100;
for (int week = 0; week < rotationWeeks; week++)
{
    rotationLevel *= 1 + (rotationNoise.NextDouble() - 0.5) * 0.004;
    rotationBenchmark[week] = rotationLevel;
}

double[] Leader(double pull) =>
    [.. rotationBenchmark.Select((b, week) => b * (1 + pull * Math.Pow(week / (double)rotationWeeks, 2)))];

Plt.Create()
    .WithTitle("Three assets that all lead their benchmark")
    .WithSize(800, 650)
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel("RS-Ratio")
        .SetYLabel("RS-Momentum")
        .RelativeRotation([Leader(0.6), Leader(0.4), Leader(0.25)], rotationBenchmark, ["Alpha", "Beta", "Gamma"],
            s => s.TailLength = 12))
    .Save("rrg_centre.svg");
```

![A relative rotation graph of three assets in the upper right quadrant, with the 100/100 centre and the three other quadrants in the lower left corner](../images/rrg_centre.png)


### Basic usage

```csharp
// 104 weekly bars per asset, vs BTC benchmark
double[][] altCloses = [ethWeekly, bnbWeekly, solWeekly];
string[]   labels    = ["ETH", "BNB", "SOL"];

Plt.Create()
    .WithTitle("Crypto Rotation — Weekly DualEma(10,26)")
    .WithSize(900, 700)
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel("RS-Ratio")
        .SetYLabel("RS-Momentum")
        .RelativeRotation(altCloses, btcWeekly, labels, s =>
        {
            s.TailLength = 12;
            s.ColorMap   = QualitativeColorMaps.Tab10;
        }))
    .Save("rrg_crypto.svg");
```

### With absorption + ENB overlays

When you have a portfolio-level absorption ratio and ENB (Effective Number of Bets),
pass them as overlays. Each dot on the trail then also shows market stress:

```csharp
// absorption[t] ∈ [0..1]: 0 = diversified (green), 1 = panic (red)
// enb[t]: larger = more diversified portfolio; radius ∝ ENB
double[] absorption = absorptionTimeSeries.AbsorptionRatio;
double[] enb        = absorptionTimeSeries.Enb;

Plt.Create()
    .WithTitle("Rotation + Risk Overlay")
    .WithSize(900, 700)
    .AddSubPlot(1, 1, 1, ax => ax
        .RelativeRotation(altCloses, btcWeekly, labels, s =>
        {
            s.AbsorptionRatioPerBar = absorption;
            s.EnbPerBar             = enb;
            s.TailLength            = 12;
        }))
    .Save("rrg_with_overlays.svg");
```

**Visual encoding:**
- The dot **fill** maps through RdYlGn reversed. Green means low absorption, and red means high absorption (panic).
- The asset colour becomes the **edge ring** of each dot.
- The dot **radius** is `max(1.5, enb × 1.5)` px. The head dot is 1.5× larger than the trail dots.
- A **gray ghost trail** is drawn behind the per-point circles.

### Formula selection

```csharp
// ZScore formula for equities-vs-index RRGs
ax.RelativeRotation(closes, benchmark, labels, s =>
{
    s.Formula          = RrgFormula.ZScore;
    s.ShortPeriod      = 14;   // rolling z-score window
    s.MomentumLookback = 14;   // ROC lookback before z-scoring momentum
});
```

See [RelativeRotationSeries](https://github.com/xkqg/MatPlotLibNet/wiki/RelativeRotationSeries) for minimum data requirements per formula.

## Prices without a time axis: Point and Figure, Renko and Kagi

Three charts cut a series of closes into boxes, bricks and reversals of one size and draw them in order, with no axis for
time. All three read the closes alone.

```csharp
double[] closes =
[
    100, 102, 105, 103, 108, 112, 109, 107, 111, 115, 118, 114, 110, 106, 103,
    107, 111, 116, 121, 119, 115, 112, 117, 123, 128, 125, 121, 118, 122, 127,
];

Plt.Create()
    .WithTitle("Point and Figure, boxes of 2")
    .WithSize(700, 450)
    .AddSubPlot(1, 1, 1, ax => ax
        .PointAndFigure(closes, 2)
        .SetAspect(1.0 / 2))
    .Save("point_and_figure_closes.svg");
```

![Point and Figure chart](../images/point_and_figure_closes.png)

`PointAndFigure(prices, boxSize, reversal = 3)` draws columns of boxes, an X where the price rose and an O where it fell. A
column grows while the price reaches the next box in its direction and gives way to the other kind once the price has come
back `reversal` boxes from the last box drawn. `SetAspect(1.0 / boxSize)` makes the boxes square, and the table beside the
chart has a row for each column (column, mark, from, to). It is a series, so it travels through JSON as itself.

```csharp
// closes: the array of the first example
Plt.Create()
    .WithTitle("Renko, bricks of 3")
    .WithSize(700, 450)
    .AddSubPlot(1, 1, 1, ax => ax
        .Renko(closes, 3)
        .WithLegend())
    .Save("renko_closes.svg");
```

![Renko chart](../images/renko_closes.png)

`Renko(prices, brickSize)` is a staircase of bricks of one size, anchored on the first close. A close on the next brick's
trigger draws it, a close that covers several bricks draws each, and a reversal needs two bricks from the far edge, so a
reversing brick never overlaps the one before it. `RenkoParts` hands you the rising and the falling bricks as two floating
bar series. A decimal brick size never loses a step to binary error.

```csharp
// closes: the array of the first example
Plt.Create()
    .WithTitle("Kagi, a reversal of 4")
    .WithSize(700, 450)
    .AddSubPlot(1, 1, 1, ax => ax.Kagi(closes, 4))
    .Save("kagi_closes.svg");
```

![Kagi chart](../images/kagi_closes.png)

`Kagi(prices, reversal)` is one path of vertical lines and short connectors that turns thick exactly where the price breaks
above the last shoulder and thin exactly where it breaks below the last waist, in the middle of a line. `KagiParts` hands you
the thick and the thin line. Renko and Kagi carry no ticks on the X axis, since it is only an order; both verbs live in
`PriceAxesExtensions`.
