// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The layout of <see cref="FigureTemplates.SparklineDashboard(IReadOnlyList{NamedSample}, string)"/>: one open-framed row
/// per named sample, its values as a sparkline and its label on the row's Y axis, 600 pixels wide and 120 tall for each row.</summary>
internal sealed class SparklineDashboardFigure : FacetedFigure
{
    private readonly IReadOnlyList<NamedSample> _series;

    /// <summary>Makes the dashboard of the given rows.</summary>
    /// <param name="series">One named sample per row.</param>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="series"/> holds no row, or a row carries no values.</exception>
    internal SparklineDashboardFigure(IReadOnlyList<NamedSample> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        series.ThrowIfEmpty("No rows were given; a sparkline dashboard draws one row per series.");

        for (int i = 0; i < series.Count; i++)
        {
            if (series[i].Values is null)
                throw new ArgumentException($"Series '{series[i].Label}' carries no values.", nameof(series));
        }

        _series = series;
    }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        int n = _series.Count;
        fb.WithSize(600, 120 * n);   // 600 pixels across, 120 for each row

        for (int i = 0; i < n; i++)
        {
            var (label, values) = _series[i];
            fb.AddSubPlot(n, 1, i + 1, ax =>
            {
                ax.Sparkline(values);
                ax.SetYLabel(label);
                ax.OpenFrame();
            });
        }
    }
}
