---
title: "Live streaming charts in C#"
description: "Stream live data into a C# chart. Append points from any thread and the chart updates at the frame rate you set."
---

# Streaming & Realtime

> **Install:** `dotnet add package MatPlotLibNet`

## Live line chart

Append data points from any thread. The chart auto-updates at the configured frame rate:

```csharp
StreamingLineSeries? series = null;
var sf = Plt.Create()
    .WithTitle("Live Telemetry")
    .AddSubPlot(1, 1, 1, ax =>
    {
        series = ax.StreamingPlot(capacity: 5000, configure: s =>
        {
            s.Color = Colors.Blue;
            s.Label = "Sensor A";
        });
    })
    .BuildStreaming(TimeSpan.FromMilliseconds(33)); // 30fps

// Append from data source (any thread)
for (int i = 0; i < 1000; i++)
{
    series!.AppendPoint(i, Math.Sin(i * 0.1) + Random.Shared.NextDouble() * 0.5);
    await Task.Delay(10);
}

sf.Dispose();
```

## Two units on one live chart (secondary Y-axis)

A live panel usually shows two values that do not share a scale, such as a rate and a volume, or a
count and a size. Stream one value on each axis and name both: in a legend, as here, or at the end of each line
(see [Direct labels and axis colours](direct-labels-and-axis-colours.md#a-live-chart)).

```csharp
StreamingLineSeries? delivered = null;
StreamingLineSeries? traffic   = null;

var sf = Plt.Create()
    .WithTitle("Ops performance")
    .AddSubPlot(1, 1, 1, ax =>
    {
        delivered = ax.StreamingPlot(600, s => s.Label = "Delivered (msg/s)");
        ax.SetYLabel("messages / sec")
          .WithAddRightYAxis(right =>
          {
              traffic = right.StreamingPlot(600, s => s.Label = "Traffic (kB/s)");
              right.SetYLabel("kB / sec");
          })
          .WithLegend();
    })
    .BuildStreaming(TimeSpan.FromSeconds(1));   // one frame per second

delivered!.AppendPoint(t, msgPerSecond);
traffic!.AppendPoint(t, kbPerSecond);
```

> `YAxisSlotBuilder.StreamingPlot` returns the series, not the builder. You need that series
> object to append the live data. The figure makes room on the right for the right-hand axis's tick labels and
> title by itself, with or without `TightLayout()`.

## Streaming candlestick with Bollinger Bands

This example builds a live financial chart. The indicators attach to the candlestick series
automatically:

```csharp
var figure = new Figure();
var axes = figure.AddSubPlot();
var candles = axes.AddSeries(new StreamingCandlestickSeries(5000));

// Indicators auto-subscribe to BarAppended — O(1) per bar
candles.WithStreamingSma(axes, 20);
candles.WithStreamingBollinger(axes, 20, 2.0);

var sf = new StreamingFigure(figure);
sf.DefaultConfig = new StreamingAxesConfig(
    new AxisScaleMode.SlidingWindow(100),
    new AxisScaleMode.AutoScale());

// Feed price data
var rng = new Random(42);
double price = 100;
for (int i = 0; i < 200; i++)
{
    double change = (rng.NextDouble() - 0.48) * 3;
    candles.AppendBar(new OhlcBar(price, price + 5, price - 3, price + change));
    price += change;
}
```

## Streaming signal (oscilloscope)

This series stores only the Y values and computes X from the sample rate. It is the best fit for
audio and sensor data:

```csharp
var signal = ax.StreamingSignal(
    capacity: 100_000,
    sampleRate: 44100.0,
    configure: s => { s.Color = Colors.Green; });

// Append individual samples or batches
signal.AppendSample(amplitude);
signal.AppendSamples(audioBuffer);
```

## Any indicator on a live chart

57 of the 59 indicators have a streaming form. The eleven named verbs (`WithStreamingSma`, `WithStreamingEma`, `WithStreamingRsi` and so
on, up to `WithStreamingObv` and `WithStreamingVwap`) make the indicator for you; `WithStreaming(axes, indicator)` attaches any other, and
a bar can carry its volume with `OhlcvBar`:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

var figure = new Figure { Title = "Candles with a streaming average and on-balance volume", Width = 760, Height = 520 };
var price = figure.AddSubPlot(2, 1, 1);
var volume = figure.AddSubPlot(2, 1, 2);
var candles = price.AddSeries(new StreamingCandlestickSeries(200));
candles.WithStreamingSma(price, 5);
candles.WithStreamingObv(volume);

double close = 100;
ulong state = 7;
for (int i = 0; i < 60; i++)
{
    state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
    double step = ((state >> 11) / (double)(1UL << 53) - 0.5) * 6;
    state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
    double traded = 800 + (state >> 11) / (double)(1UL << 53) * 600;
    double open = close;
    close = Math.Max(1, close + step);
    candles.AppendBar(new OhlcvBar(open, Math.Max(open, close) + 1, Math.Min(open, close) - 1, close, traded));
}

figure.Save("streaming_obv.svg");
```

![Sixty candles with an orange five-bar average over them and a line of on-balance volume below](../images/streaming_obv.png)

After the bar at index *i*, the value of every form is the last value of the batch indicator over the bars up to *i*, to 1e-9, so a live
chart and a chart drawn from the same history agree. `FibonacciRetracement` and `VolumeProfile` have no streaming form: neither has a
value per bar for a stream to say.

Transfer entropy follows a window too. `TransferEntropyOptions(Neighbours: 4, Window: 100, Stride: 5)` makes the batch indicator one
value for each bar, from the last 100 bars, with the Kraskov nearest-neighbour estimate (no bins), and `StreamingTransferEntropy` says the
same as bars arrive:

![A line near zero until bar 200 that climbs to about 0.35 nats, where the target starts to follow the source](../images/rolling_transfer_entropy.png)

## Axis scale modes

```csharp
// Sliding window: always show last 100 X-units
sf.DefaultConfig = new StreamingAxesConfig(
    new AxisScaleMode.SlidingWindow(100.0),
    new AxisScaleMode.AutoScale());

// Sticky right: scroll only when data is at the edge
sf.DefaultConfig = new StreamingAxesConfig(
    new AxisScaleMode.StickyRight(100.0),
    new AxisScaleMode.AutoScale());

// Fixed: manual control, no auto-scaling
sf.DefaultConfig = new StreamingAxesConfig(
    new AxisScaleMode.Fixed(),
    new AxisScaleMode.Fixed());
```

## Putting it on screen in a desktop application

Each native host has a control that takes the `StreamingFigure` itself. Bind it once and leave it there: the
control subscribes to the figure, marshals the redraw to the UI thread, and scales the axes before each draw.

```xml
<!-- WPF -->
<wpf:MplStreamingChartControl StreamingFigure="{Binding LiveChart}" />

<!-- Avalonia -->
<avalonia:MplStreamingChartControl StreamingFigure="{Binding LiveChart}" />

<!-- Uno -->
<uno:MplStreamingChartElement StreamingFigure="{Binding LiveChart}" />
```

Reach for these rather than the ordinary chart control. `MplChartControl` takes a `Figure` and redraws when
that property changes — which, for a dependency property, means when the *reference* changes. A streaming
figure keeps its identity while its data moves, so the ordinary control shows the first frame and nothing
after it unless you hand it a different figure.

How fast it redraws is the figure's decision, not the control's:

```csharp
sf.MinRenderInterval = TimeSpan.FromMilliseconds(100);   // ten frames a second instead of thirty
```

## Rx integration

You can connect `IObservable<T>` sources without taking a dependency on System.Reactive:

```csharp
IObservable<StreamingPoint> sensorStream = ...;
using var sub = series.SubscribeTo(sensorStream);
```
