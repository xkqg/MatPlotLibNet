// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for series that operate on multiple datasets (one per category).
/// Provides the shared <see cref="Datasets"/> property and a default <see cref="ComputeDataRange"/>.</summary>
public abstract class DatasetSeries : ChartSeries
{
    /// <summary>The observations, one array per group: array <c>i</c> is drawn at position <c>i</c> unless the series
    /// sets positions of its own.</summary>
    public double[][] Datasets { get; }

    /// <summary>Whether any group holds an observation.</summary>
    internal bool HasObservations
    {
        get
        {
            for (int i = 0; i < Datasets.Length; i++)
            {
                if (Datasets[i].Length > 0) return true;
            }

            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>No group holds an observation, for a series whose range then is the library's default alone (a strip, a swarm, a point
    /// plot); a box and a violin range their positions even then (<see cref="RangesItsPositionsWhenEmpty"/>).</remarks>
    internal override bool HoldsNoData => !HasObservations && !RangesItsPositionsWhenEmpty;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>stripplot</c>, <c>swarmplot</c> and <c>pointplot</c> return before they touch the axes when they are handed no
    /// data, which keep (0, 1); matplotlib's <c>boxplot</c> and <c>violinplot</c> ask.</remarks>
    internal override bool AsksWhenEmpty => RangesItsPositionsWhenEmpty;

    /// <summary>Whether the series ranges its positions when no group holds an observation, as matplotlib's <c>boxplot</c> and
    /// <c>violinplot</c> do (<c>boxplot([[]])</c>: xlim (0.5, 1.5)): a box and a violin; seaborn's dot series do not.</summary>
    internal virtual bool RangesItsPositionsWhenEmpty => false;

    /// <summary>Creates a new dataset series.</summary>
    protected DatasetSeries(double[][] datasets)
    {
        Datasets = datasets;
    }

    /// <inheritdoc />
    /// <remarks>Long form: one row per sample, named by the 1-based dataset it belongs to. A wide form would
    /// need every dataset to have the same length, which is exactly what a distribution series does not
    /// promise.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.OfGroups([new("dataset", DataColumnKind.Text), new("value")], null, "", Datasets, static value => [DataCell.FromNumber(value)]);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Datasets.Length == 0) return NoDataUnderTheLibrary;
        // Over the drawable samples only (a missing sample is NaN); 0..1 when there is none, as for no sample at all.
        var y = Datasets.DrawableRange() ?? new Range1D(0, 1);
        return new(-0.5, Datasets.Length - 0.5, y.Lo, y.Hi);
    }
}
