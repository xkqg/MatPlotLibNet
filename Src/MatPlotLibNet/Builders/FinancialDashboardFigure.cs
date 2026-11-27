// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet;

/// <summary>The layout of <see cref="FigureTemplates.FinancialDashboard"/>: three panels on one bar axis, the price (60 %), the
/// volume (15 %) and an oscillator (25 %), each tick at the centre of a bar.</summary>
internal sealed class FinancialDashboardFigure : FacetedFigure
{
    private readonly double[] _open, _high, _low, _close, _volume;

    /// <summary>Makes the dashboard of the given bars.</summary>
    /// <param name="open">Open prices.</param>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume data.</param>
    internal FinancialDashboardFigure(double[] open, double[] high, double[] low, double[] close, double[] volume)
    {
        _open = open;
        _high = high;
        _low = low;
        _close = close;
        _volume = volume;
    }

    /// <summary>Runs on the price axes after its candles are placed.</summary>
    internal Action<AxesBuilder>? ConfigurePrice { get; init; }

    /// <summary>Runs on the volume axes after its bars are placed.</summary>
    internal Action<AxesBuilder>? ConfigureVolume { get; init; }

    /// <summary>Runs on the oscillator axes, which holds no series of its own.</summary>
    internal Action<AxesBuilder>? ConfigureOscillator { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        int n = _close.Length;
        var labels = new string[n];
        for (int i = 0; i < n; i++) labels[i] = i.InvariantText();

        fb.WithGridSpec(3, 1, heightRatios: [0.60, 0.15, 0.25])
          .TightLayout();   // gives each panel its own tick + label gutters

        // Tick labels centered inside each bar slot: ticks at k*step+0.5, formatted as integer bar index.
        // Step is chosen to yield ~10 visible ticks regardless of n.
        double tickStep = Math.Max(1, Math.Round((double)n / 10));
        var barLocator  = new MultipleLocator(tickStep, CategorySlot.CentreOffset);
        var barFormatter = new BarCenterFormatter();

        fb.AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
        {
            ax.Candlestick(_open, _high, _low, _close, labels);
            ax.SetYLabel("Price");
            ax.SetXMargin(0);
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            ConfigurePrice?.Invoke(ax);
        });

        fb.AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
        {
            ax.Bar(labels, _volume);
            ax.SetYLabel("Volume");
            ax.SetXMargin(0);
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            ConfigureVolume?.Invoke(ax);
        });

        fb.AddSubPlot(new GridPosition(2, 3, 0, 1), ax =>
        {
            ax.SetYLabel("Oscillator");
            ax.SetXMargin(0);
            ax.SetXLim(0, n);  // align x range with price/volume panels
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            ax.UseBarSlotX();  // panel indicators (RSI, etc.) will auto-align to bar centres
            ConfigureOscillator?.Invoke(ax);
        });
    }
}
