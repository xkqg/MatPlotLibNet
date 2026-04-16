# Tick Formatting

## Engineering notation

`EngFormatter` displays values as `100k`, `50M`, `2.5G`:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Engineering notation + MultipleLocator")
        .SetXLabel("Sample index")
        .SetYLabel("Amplitude")
        .Plot(x, y, line => { line.Label = "Signal"; })
        .SetYTickFormatter(new EngFormatter())
        .SetXTickLocator(new MultipleLocator(25))
        .WithMinorTicks())
    .Save("tick_locators.svg");
```

![Tick locators](../images/tick_locators.png)

## Date axes

`DateTime[]` arrays are handled automatically with `AutoDateLocator`:

```csharp
DateTime[] dates = Enumerable.Range(0, 90)
    .Select(i => new DateTime(2025, 1, 1).AddDays(i))
    .ToArray();

Plt.Create()
    .WithTitle("Stock Price — Jan to Mar 2025")
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel("Date")
        .SetYLabel("Price ($)")
        .Plot(dates, prices, line => { line.Color = Colors.Tab10Blue; })
        .WithLegend(LegendPosition.UpperRight))
    .Save("date_axis.svg");
```

![Date axis](../images/date_axis.png)
