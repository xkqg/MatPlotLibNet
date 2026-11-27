// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Data;

/// <summary>The observations of a figure and the hue label of each: drawn as one series when there are no labels, and otherwise as
/// one series per hue group (<see cref="HueGrouper.GroupBy"/>: the groups in the order their labels are first seen, the palette's
/// colours in that order), each wearing the colour and the label of its group. The one place a figure of observations split by a
/// hue is drawn: the figures of a sequence (<see cref="EnumerableFigureExtensions"/>), of a data frame, and the panels of a faceted
/// figure.</summary>
/// <param name="X">The first value of each observation.</param>
/// <param name="Y">The second value of each observation (the first again for a histogram, which reads one).</param>
/// <param name="Hue">The hue label of each observation; <see langword="null"/> draws them as one series.</param>
/// <param name="Palette">The colours of the groups; <see langword="null"/> or empty for the ten-colour cycle.</param>
internal readonly record struct HueSplit(double[] X, double[] Y, IReadOnlyList<string>? Hue, Color[]? Palette)
{
    /// <summary>The opacity the histogram of each hue group is drawn with, so the bars of the groups show through each other.</summary>
    internal const double OverlapAlpha = 0.7;

    /// <summary>A figure of one line per group.</summary>
    /// <returns>The figure.</returns>
    internal FigureBuilder Lines()
    {
        var figure = Plt.Create();
        Draw<LineSeries>((x, y, dress) => figure.Plot(x, y, dress));
        return figure;
    }

    /// <summary>A figure of one scatter per group.</summary>
    /// <returns>The figure.</returns>
    internal FigureBuilder Scatters()
    {
        var figure = Plt.Create();
        Draw<ScatterSeries>((x, y, dress) => figure.Scatter(x, y, dress));
        return figure;
    }

    /// <summary>A figure of one histogram of <see cref="X"/> per group, drawn see-through over each other.</summary>
    /// <param name="bins">How many bins each histogram has.</param>
    /// <returns>The figure.</returns>
    internal FigureBuilder Histograms(int bins)
    {
        var figure = Plt.Create();
        DrawOverlapping((values, dress) => figure.Hist(values, bins, dress));
        return figure;
    }

    /// <summary>Draws the observations: one series with no configuration when there are no labels, otherwise one series per
    /// group, configured to wear the group's colour and label.</summary>
    /// <typeparam name="TSeries">The kind of series the verb draws.</typeparam>
    /// <param name="add">The verb: draws one series of the values it is given and runs the configuration on it.</param>
    internal void Draw<TSeries>(Action<double[], double[], Action<TSeries>?> add) where TSeries : ChartSeries, IHasColor
    {
        if (Hue is not { } hue)
        {
            add(X, Y, null);
            return;
        }

        double[] x = X, y = Y;
        foreach (var group in HueGrouper.GroupBy(Enumerable.Range(0, x.Length), i => hue[i], i => x[i], i => y[i], Palette))
            add(group.X, group.Y, s => { s.Color = group.Color; s.Label = group.Label; });
    }

    /// <summary>Draws the values of <see cref="X"/> as histograms: one when there are no labels, otherwise one per group at
    /// <see cref="OverlapAlpha"/>, in the group's colour and with its label.</summary>
    /// <param name="add">The verb: draws one histogram of the values it is given and runs the configuration on it.</param>
    internal void DrawOverlapping(Action<double[], Action<HistogramSeries>?> add) =>
        Draw<HistogramSeries>((values, _, dress) => add(values, dress is null ? null : s => { dress(s); s.Alpha = OverlapAlpha; }));
}
