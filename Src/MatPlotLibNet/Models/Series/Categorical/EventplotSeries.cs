// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an event plot series that draws rows of vertical tick lines at event positions, one row per event set.</summary>
public sealed class EventplotSeries : ChartSeries
{
    public double[][] Positions { get; }

    public double LineWidth { get; set; } = 1.0;

    public Color[]? Colors { get; set; }

    public double LineLength { get; set; } = 1.0;

    /// <summary>Initializes a new instance of <see cref="EventplotSeries"/> with the specified position sets.</summary>
    /// <param name="positions">An array of event position sets, one per row.</param>
    public EventplotSeries(double[][] positions)
    {
        Positions = positions;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Positions.Length == 0) return new(0, 1, 0, 1);
        var allPos = Positions.SelectMany(p => p).ToArray();
        double xMin = allPos.Length > 0 ? allPos.Min() : 0;
        double xMax = allPos.Length > 0 ? allPos.Max() : 1;
        // Each row i draws tick lines from y = i - LineLength/2 to y = i + LineLength/2.
        // Report the enlarged Y-bbox to the auto-limiter so it rounds out to nice tick
        // boundaries (e.g. [-0.5, 3.5] rounds to [-1, 4] for 4 rows at linelength=1).
        // Deliberately NO sticky-Y: event rows don't semantically "touch" a spine the way
        // bar baselines do, and sticky would block the auto-limiter from expanding to the
        // nice range. Sticky-X is kept because event positions are the data X range.
        double halfLen = LineLength / 2.0;
        double yMin = -halfLen;
        double yMax = (Positions.Length - 1) + halfLen;
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "eventplot",
        EventPositions = Positions,
        LineWidth = LineWidth,
        LineLength = LineLength
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
