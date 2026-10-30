// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an event plot series that draws rows of vertical tick lines at event positions, one row per event set.</summary>
public sealed class EventplotSeries : ChartSeries
{
    /// <summary>The events, one array per row: the values of array <c>i</c> are where the ticks of row <c>i</c> stand
    /// along X, and the row is drawn at height <c>i</c>.</summary>
    public double[][] Positions { get; }

    /// <summary>Width of each event tick, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>The colour of the ticks of each row, by row; a row past the end of the array, or every row when this is
    /// <see langword="null"/>, takes the series colour.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>Height of each tick in data units of the row axis, centred on its row; the default 1 makes the ticks of
    /// neighbouring rows touch.</summary>
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
        // Over the drawable events only: a missing event (NaN) and an infinity are not drawn, so they are not part of
        // the extent; rows without any event at all keep the 0..1 default.
        var x = Positions.Any(p => p.Length > 0) ? Positions.DrawableRange() : new Range1D(0, 1);
        double? xMin = x?.Lo, xMax = x?.Hi;
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
        LineLength = LineLength,
        Colors = Colors?.ToList(),
    };

    /// <summary>Reconstructs an <see cref="EventplotSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static EventplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Eventplot(dto.EventPositions ?? []);
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        if (dto.LineLength.HasValue)
        {
            s.LineLength = dto.LineLength.Value;
        }
        s.Colors = dto.Colors?.ToArray();
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per event, named by the 1-based row it sits on.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        for (int r = 0; r < Positions.Length; r++)
        {
            var row = DataCell.FromText((r + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var position in Positions[r])
            {
                rows.Add([row, DataCell.FromNumber(position)]);
            }
        }

        return new ChartDataTable(null, [new("row", DataColumnKind.Text), new(Label ?? "position", Axis: DataAxis.X)], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
