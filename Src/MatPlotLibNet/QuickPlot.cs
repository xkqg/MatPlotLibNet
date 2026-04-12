// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>
/// One-liner façade for the most common chart types — modelled after <c>matplotlib.pyplot</c>.
/// Every method returns a <see cref="FigureBuilder"/> so callers can chain further configuration
/// (e.g. <c>.WithSize(…)</c>, <c>.WithTheme(…)</c>) before terminating with
/// <c>.Build()</c>, <c>.ToSvg()</c>, or <c>.Save(path)</c>.
/// </summary>
/// <example>
/// <code>
/// QuickPlot.Line(x, y, title: "Revenue").Save("revenue.svg");
/// QuickPlot.Signal(samples, sampleRate: 44100).ToSvg();
/// QuickPlot.Svg(fb =&gt; fb.Scatter(x, y).WithTitle("Dots"));
/// </code>
/// </example>
public static class QuickPlot
{
    /// <summary>Plots <paramref name="x"/> and <paramref name="y"/> as a line series.</summary>
    public static FigureBuilder Line(double[] x, double[] y, string? title = null) =>
        ApplyTitle(Plt.Create().Plot(x, y), title);

    /// <summary>Plots <paramref name="x"/> and <paramref name="y"/> as a scatter series.</summary>
    public static FigureBuilder Scatter(double[] x, double[] y, string? title = null) =>
        ApplyTitle(Plt.Create().Scatter(x, y), title);

    /// <summary>Plots the distribution of <paramref name="data"/> as a histogram.</summary>
    public static FigureBuilder Hist(double[] data, int bins = 10, string? title = null) =>
        ApplyTitle(Plt.Create().Hist(data, bins), title);

    /// <summary>
    /// Plots <paramref name="y"/> as a uniform-rate signal series.
    /// Uses O(1) index arithmetic for viewport slicing — efficient at 1 M + points.
    /// </summary>
    public static FigureBuilder Signal(double[] y, double sampleRate = 1.0,
                                       double xStart = 0.0, string? title = null) =>
        ApplyTitle(Plt.Create().Signal(y, sampleRate, xStart), title);

    /// <summary>
    /// Plots monotonically ascending <paramref name="x"/> + <paramref name="y"/> as a signal series.
    /// Uses binary search for viewport slicing — efficient at 1 M + points with non-uniform spacing.
    /// </summary>
    public static FigureBuilder SignalXY(double[] x, double[] y, string? title = null) =>
        ApplyTitle(Plt.Create().SignalXY(x, y), title);

    /// <summary>
    /// Generic escape hatch: applies <paramref name="configure"/> to a fresh <see cref="FigureBuilder"/>
    /// and returns the resulting SVG string.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static string Svg(Action<FigureBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var fb = Plt.Create();
        configure(fb);
        return fb.ToSvg();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static FigureBuilder ApplyTitle(FigureBuilder fb, string? title) =>
        title is null ? fb : fb.WithTitle(title);
}
