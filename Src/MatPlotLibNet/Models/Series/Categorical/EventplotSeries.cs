// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an event plot series that draws rows of vertical tick lines at event positions, one row per event set.</summary>
public sealed class EventplotSeries : ChartSeries, ISeriesKind<EventplotSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>eventplot</c> draws in <c>lines.color</c> and asks neither cycle. 2: <c>eventplot</c> draws an <c>EventCollection</c>, a <c>LineCollection</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.LineCollection;

    /// <summary>The events, one array per row: the values of array <c>i</c> are where the ticks of row <c>i</c> stand
    /// along X, and the row is drawn at height <c>i</c>.</summary>
    public double[][] Positions { get; }

    /// <summary>The width of each event line, in pixels, read and drawn as a line's (<see cref="LineSeries.LineWidth"/>) but resting at
    /// the library's one-pixel width of a collection.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the event lines are drawn: as a collection of lines (matplotlib's <c>EventCollection</c>, which takes
    /// <c>lines.linewidth</c> and never the cycler's).</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.OnePixelCollection;

    /// <summary>The width the series was given; <see langword="null"/> when it was given none, and its rule decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>The colour of the ticks of each row, by row; a row past the end of the array, or every row when this is
    /// <see langword="null"/>, takes the series colour.</summary>
    public Color[]? Colors { get; set => Change(ref field, value); }

    /// <summary>Height of each tick in data units of the row axis, centred on its row; the default 1 makes the ticks of
    /// neighbouring rows touch.</summary>
    public double LineLength { get; set => Change(ref field, value); } = 1.0;

    /// <summary>Initializes a new instance of <see cref="EventplotSeries"/> with the specified position sets.</summary>
    /// <param name="positions">An array of event position sets, one per row.</param>
    public EventplotSeries(double[][] positions)
    {
        Positions = positions;
    }

    /// <inheritdoc />
    /// <remarks>No row holds an event.</remarks>
    internal override bool HoldsNoData => !Positions.Any(p => p.Length > 0);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>eventplot</c> adds its rows to the data limits and asks for the autoscale only when a row holds an event
    /// (<c>if len(min_max) > 0</c>): with none the axes keep (0, 1).</remarks>
    internal override bool AsksWhenEmpty => false;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Positions.Length == 0) return NoDataUnderTheLibrary;
        // Over the drawable events only: a missing event (NaN) and an infinity are not drawn, so they are not part of
        // the extent; rows without any event at all keep the 0..1 default.
        var x = HoldsNoData ? new Range1D(0, 1) : Positions.DrawableRange();
        double? xMin = x?.Lo, xMax = x?.Hi;
        // Row i draws its lines from y = i - LineLength / 2 to y = i + LineLength / 2, but matplotlib's eventplot puts a whole
        // line length either side of the rows into the data limits (minline = (lineoffsets - linelengths).min(), maxline =
        // (lineoffsets + linelengths).max()) and registers no sticky edge, so the margin pads the events and the rows alike.
        return new(xMin, xMax, -LineLength, (Positions.Length - 1) + LineLength);
    }

    static string ISeriesKind<EventplotSeries>.Kind => "eventplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        EventPositions = Positions,
        LineWidth = GivenLineWidth,
        LineLength = LineLength,
        Colors = Colors?.ToList(),
    };

    /// <summary>The reader of a <see cref="EventplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static EventplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Eventplot(dto.EventPositions ?? []);
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        if (dto.LineLength is { } lineLength) s.LineLength = lineLength;
        s.Colors = dto.Colors?.ToArray();
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per event, named by the 1-based row it sits on.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.OfGroups([new("row", DataColumnKind.Text), ChartDataColumn.Position(DataAxis.X, Label ?? "position")], null, "", Positions,
            static position => [DataCell.FromNumber(position)]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
