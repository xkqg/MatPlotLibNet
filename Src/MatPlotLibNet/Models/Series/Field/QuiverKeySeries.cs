// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a quiver key (reference arrow) series showing the scale of a quiver field.</summary>
/// <remarks>Given no <c>Color</c>, it is drawn black under the matplotlib themes (a figure's theme unless it is given another), as
/// matplotlib draws a quiver key's arrow in every style; the library's other themes draw it in the colour of its place in the cycle.</remarks>
public sealed class QuiverKeySeries : ChartSeries, IHasColor, ISeriesKind<QuiverKeySeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>quiverkey</c> takes its quiver's colour and asks neither cycle. 1.1: a <c>QuiverKey</c> stands at its quiver's zorder (1) plus 0.1.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.VectorFieldKey;

    /// <summary>Horizontal position of the key's point, as a fraction of the width of the axes: 0 is the left edge and 1
    /// the right. The point is the middle of the arrow, or its head or its tail by the <see cref="LabelPosition"/>.</summary>
    public double X { get; set => Change(ref field, value); }

    /// <summary>Vertical position of the key's point, as a fraction of the height of the axes: 0 is the bottom edge and 1
    /// the top.</summary>
    public double Y { get; set => Change(ref field, value); }

    /// <summary>Length of the key arrow in data units along X, so 1 is as long as one unit of the X axis.</summary>
    public double U { get; set => Change(ref field, value); }

    /// <summary>The text printed beside the arrow, naming what its length stands for, such as <c>"1 m/s"</c>.</summary>
    public new string Label { get; set => Change(ref field, value); }

    /// <summary>Where the label stands against the arrow, as matplotlib's <c>labelpos</c> puts it: <see cref="QuiverKeyLabelPosition.Above"/>
    /// (the default, <see cref="RestingLabelPosition"/>) or <see cref="QuiverKeyLabelPosition.Below"/> with the key's point in the middle of the arrow,
    /// <see cref="QuiverKeyLabelPosition.Right"/> with the point on its head, <see cref="QuiverKeyLabelPosition.Left"/> with the
    /// point on its tail. The label stands a tenth of an inch from the arrow.</summary>
    public QuiverKeyLabelPosition LabelPosition { get; set => Change(ref field, value); } = RestingLabelPosition;

    private const QuiverKeyLabelPosition RestingLabelPosition = QuiverKeyLabelPosition.Above;

    /// <summary>The size of the label, in points. Given no size, the label is the size of the theme's text under every theme, as
    /// matplotlib's is the style's <c>font.size</c> (10 points in the default style, 12 in the classic one); a size named here is that
    /// many points, the default included. A new key reads <see cref="RestingFontSize"/>.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size a new key reads from <see cref="FontSize"/>.</summary>
    private const double RestingFontSize = 12;

    /// <summary>The label size that was given, in points, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    /// <summary>The size the label is drawn at under <paramref name="theme"/>, in pixels: the points given, else the size of the theme's
    /// text under every theme (matplotlib's key label takes <c>font.size</c>).</summary>
    /// <param name="theme">The theme the key is drawn under.</param>
    /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
    internal double LabelSizeUnder(Theme theme) => GivenFontSize?.PointsInPixels ?? theme.DataTextFont().Size;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    private const double UnnamedX = 0.5, UnnamedY = 0.9, UnnamedU = 1.0;

    /// <summary>Initializes a new instance of <see cref="QuiverKeySeries"/>.</summary>
    public QuiverKeySeries(double x, double y, double u, string label)
    {
        X = x; Y = y; U = u; Label = label;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => default;

    static string ISeriesKind<QuiverKeySeries>.Kind => "quiverkey";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        QuiverKeyX = X,
        QuiverKeyY = Y,
        QuiverKeyU = U,
        QuiverKeyLabel = Label,
        QuiverKeyLabelPosition = LabelPosition.OrNullAtRest(RestingLabelPosition),
        // The label's point size has always travelled in the marker size field: every document written so far carries it
        // there, and the golden corpus pins the bytes.
        MarkerSize = GivenFontSize,
        Color = Color,
    };

    /// <summary>The reader of a <see cref="QuiverKeySeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static QuiverKeySeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.QuiverKey(dto.QuiverKeyX ?? UnnamedX, dto.QuiverKeyY ?? UnnamedY, dto.QuiverKeyU ?? UnnamedU, dto.QuiverKeyLabel ?? "");
        if (dto.MarkerSize is { } markerSize) s.FontSize = markerSize;
        if (dto.QuiverKeyLabelPosition is { } quiverKeyLabelPosition) s.LabelPosition = quiverKeyLabelPosition;
        s.Color = dto.Color;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
