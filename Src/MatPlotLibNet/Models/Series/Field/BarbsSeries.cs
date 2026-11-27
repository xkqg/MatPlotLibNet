// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a wind barb series using meteorological notation to show speed and direction.</summary>
/// <remarks>Given no <c>Color</c>, it is drawn black under the matplotlib themes (a figure's theme unless it is given another), as
/// matplotlib draws its barbs in every style; the library's other themes draw it in the colour of its place in the cycle. Under the
/// matplotlib themes each barb is matplotlib's glyph: a staff 24.5 pt long pointing where the wind comes from, a flag for each 50, a
/// full barb for each 10 and a half barb for a remaining 5 from its far end, the speed rounded to the nearest 5, and a calm (a speed
/// that rounds to 0) drawn as a small circle.</remarks>
public sealed class BarbsSeries : ChartSeries, IHasColor, ISeriesKind<BarbsSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>barbs</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.VectorField;

    /// <summary>The X position of the base of each barb.</summary>
    public Vec X { get; }

    /// <summary>The Y position of the base of each barb.</summary>
    public Vec Y { get; }

    /// <summary>The wind speed at each position, in knots: a flag stands for 50, a full barb for 10 and a half barb for
    /// 5.</summary>
    public Vec Speed { get; }

    /// <summary>The direction the staff of each barb points, in degrees clockwise from north: 0 is straight up and 90
    /// is to the right.</summary>
    public Vec Direction { get; }

    /// <summary>Length of the staff of each barb, in pixels. A series given no length reads <see cref="RestingBarbLength"/> and is drawn as long as its theme
    /// draws a staff (the matplotlib themes draw matplotlib's, 24.5 pt; the library's own themes <see cref="RestingBarbLength"/> pixels); a length that was given
    /// is drawn as it is under every theme.</summary>
    public double BarbLength { get => GivenBarbLength ?? RestingBarbLength; set => GivenBarbLength = value; }

    /// <summary>The value of <see cref="BarbLength"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenBarbLength { get; private set => Change(ref field, value); }

    /// <summary>The length a series reads, and is drawn at under a theme that draws the library's own barb, when none was given.</summary>
    private const double RestingBarbLength = 15;

    /// <summary>The length of matplotlib's staff, in points: <c>Barbs</c> draws its polygon of a staff <c>length</c> = 7 long at
    /// <c>sizes = length ** 2 / 4</c>, one unit to <c>sqrt(length ** 2 / 4)</c> = 3.5 pt, so 24.5 pt.</summary>
    private const double MatplotlibStaffPoints = 24.5;

    /// <summary>The length the staff is drawn at under <paramref name="theme"/>: the one given, else matplotlib's where the theme draws
    /// matplotlib's barbs (<see cref="VectorFieldLook.BarbsLikeMatplotlib"/>), else <see cref="RestingBarbLength"/> pixels.</summary>
    /// <param name="theme">The theme the barbs are drawn under.</param>
    /// <returns>The length of the staff, in pixels.</returns>
    internal double BarbLengthIn(Theme theme) =>
        GivenBarbLength ?? (theme.Looks.VectorField.BarbsLikeMatplotlib ? MatplotlibStaffPoints.PointsInPixels : RestingBarbLength);

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="BarbsSeries"/>.</summary>
    public BarbsSeries(Vec x, Vec y, Vec speed, Vec direction)
    {
        X = x; Y = y; Speed = speed; Direction = direction;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => X.Length == 0;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => OverItsPoints(X.Data, Y.Data);

    static string ISeriesKind<BarbsSeries>.Kind => "barbs";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        Speed = Speed,
        Direction = Direction,
        BarbLength = GivenBarbLength,
        Color = Color
    };

    /// <summary>The reader of a <see cref="BarbsSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static BarbsSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Barbs(dto.XData ?? [], dto.YData ?? [], dto.Speed ?? [], dto.Direction ?? []);
        if (dto.BarbLength is { } barbLength) s.BarbLength = barbLength;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Where each barb sits, and the wind it reports.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(null, X.Data, Y.Data, new NumberColumn("speed", Speed.Data), new NumberColumn("direction", Direction.Data));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
