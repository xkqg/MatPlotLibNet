# Financial Charts

## Financial dashboard template

The built-in `FigureTemplates.FinancialDashboard` creates a 3-panel layout: price + Bollinger Bands on top, volume in the middle, and an oscillator (RSI, CCI, etc.) at the bottom.

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

Williams %R, On-Balance Volume, Parabolic SAR, and CCI:

```csharp
Plt.Create()
    .WithTitle("Phase F Indicators")
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
        ax.WilliamsR(high, low, close, 14);
        ax.WithTitle("Williams %R");
    })
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
    {
        ax.Obv(close, vol);
        ax.WithTitle("On-Balance Volume");
    })
    .AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
    {
        ax.Cci(high, low, close, 20);
        ax.WithTitle("CCI(20)");
    })
    .Save("phase_f_indicators.svg");
```

![Technical indicators](../images/phase_f_indicators.png)
