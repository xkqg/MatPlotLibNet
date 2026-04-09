// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>Pre-built figure layouts for common use cases.</summary>
/// <remarks>Each method returns a <see cref="FigureBuilder"/> that can be further customized before calling
/// <c>Build()</c>, <c>ToSvg()</c>, or <c>Save()</c>.</remarks>
public static class FigureTemplates
{
    /// <summary>Creates a 3-panel financial dashboard: price (60%), volume (15%), oscillator (25%) with shared X axis.</summary>
    /// <param name="open">Open prices.</param>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume data.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="configurePricePanel">Optional customization for the price axes.</param>
    /// <param name="configureVolumePanel">Optional customization for the volume axes.</param>
    /// <param name="configureOscillatorPanel">Optional customization for the oscillator axes.</param>
    public static FigureBuilder FinancialDashboard(
        double[] open, double[] high, double[] low, double[] close, double[] volume,
        string? title = null,
        Action<AxesBuilder>? configurePricePanel = null,
        Action<AxesBuilder>? configureVolumePanel = null,
        Action<AxesBuilder>? configureOscillatorPanel = null)
    {
        int n = close.Length;
        var labels = new string[n];
        for (int i = 0; i < n; i++) labels[i] = i.ToString();

        var builder = Plt.Create()
            .WithGridSpec(3, 1, heightRatios: [0.60, 0.15, 0.25]);

        if (title is not null) builder.WithTitle(title);

        builder.AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
        {
            ax.Candlestick(open, high, low, close, labels);
            ax.SetYLabel("Price");
            configurePricePanel?.Invoke(ax);
        });

        builder.AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
        {
            var volLabels = new string[n];
            for (int i = 0; i < n; i++) volLabels[i] = i.ToString();
            ax.Bar(volLabels, volume);
            ax.SetYLabel("Volume");
            configureVolumePanel?.Invoke(ax);
        });

        builder.AddSubPlot(new GridPosition(2, 3, 0, 1), ax =>
        {
            ax.SetYLabel("Oscillator");
            configureOscillatorPanel?.Invoke(ax);
        });

        return builder;
    }

    /// <summary>Creates a clean scientific-paper figure: 150 DPI, tight layout, hidden top/right spines.</summary>
    /// <param name="rows">Number of subplot rows (default 1).</param>
    /// <param name="cols">Number of subplot columns (default 1).</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="width">Figure width in pixels (default 800).</param>
    /// <param name="height">Figure height in pixels (default 600).</param>
    public static FigureBuilder ScientificPaper(
        int rows = 1, int cols = 1,
        string? title = null,
        double width = 800, double height = 600)
    {
        var builder = Plt.Create()
            .WithSize(width, height)
            .WithDpi(150)
            .TightLayout();

        if (title is not null) builder.WithTitle(title);

        for (int i = 1; i <= rows * cols; i++)
        {
            int idx = i;
            builder.AddSubPlot(rows, cols, idx, ax =>
            {
                ax.HideTopSpine();
                ax.HideRightSpine();
            });
        }

        return builder;
    }

    /// <summary>Creates a vertically stacked sparkline dashboard with one row per series.</summary>
    /// <param name="series">Array of (label, values) tuples. Each tuple becomes one subplot row.</param>
    /// <param name="title">Optional figure title.</param>
    public static FigureBuilder SparklineDashboard(
        (string Label, double[] Values)[] series,
        string? title = null)
    {
        int n = series.Length;
        var builder = Plt.Create()
            .WithSize(600, 120 * n);

        if (title is not null) builder.WithTitle(title);

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var (label, values) = series[idx];
            builder.AddSubPlot(n, 1, idx + 1, ax =>
            {
                ax.Sparkline(values);
                ax.SetYLabel(label);
                ax.HideTopSpine();
                ax.HideRightSpine();
            });
        }

        return builder;
    }
}
