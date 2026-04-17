# Streaming & Realtime

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

## Streaming candlestick with Bollinger Bands

Financial live chart with auto-attached indicators:

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

Y-only storage — X computed from sample rate. Optimal for audio/sensor data:

```csharp
var signal = ax.StreamingSignal(
    capacity: 100_000,
    sampleRate: 44100.0,
    configure: s => { s.Color = Colors.Green; });

// Append individual samples or batches
signal.AppendSample(amplitude);
signal.AppendSamples(audioBuffer);
```

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

## Rx integration

Connect `IObservable<T>` sources without System.Reactive dependency:

```csharp
IObservable<(double x, double y)> sensorStream = ...;
using var sub = series.SubscribeTo(sensorStream);
```
