// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a vector field (quiver) series with arrows at each grid point.</summary>
/// <remarks>Given no <c>Color</c>, it is drawn black under the matplotlib themes (a figure's theme unless it is given another), as
/// matplotlib draws its arrows in every style; the library's other themes draw it in the colour of its place in the cycle.
/// <para>Given no scale (<see cref="Scale"/> left at 1 and <see cref="AutoScale"/> left at <see langword="null"/>) it is drawn under
/// the matplotlib themes as matplotlib's <c>Axes.quiver</c> draws a quiver given no <c>scale</c>: autoscaled to the width of the axes,
/// pointing along each vector as it stands on the screen, and ranged over its tails.</para></remarks>
public sealed class QuiverSeries : ChartSeries, IHasColor, IThemedRange, ISeriesKind<QuiverSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>quiver</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.VectorField;

    /// <inheritdoc />
    /// <remarks>Autoscaled, an arrow's length is no part of the range (the range is over the tails).</remarks>
    internal override bool MarksReachPastItsRange(Theme theme) => AutoScaledUnder(theme);

    /// <summary>The scale a new quiver holds, which stands for "none named" where the theme autoscales (<see cref="AutoScale"/>).</summary>
    internal const double RestingScale = 1.0;

    /// <summary>The X position of the tail of each arrow.</summary>
    public double[] XData { get; }

    /// <summary>The Y position of the tail of each arrow.</summary>
    public double[] YData { get; }

    /// <summary>The X component of each arrow: its head stands at the tail plus this value times
    /// <see cref="Scale"/>.</summary>
    public double[] UData { get; }

    /// <summary>The Y component of each arrow: its head stands at the tail plus this value times
    /// <see cref="Scale"/>.</summary>
    public double[] VData { get; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The factor that turns the U and V components into data units along the axes, so an arrow reaches from
    /// its tail by (U, V) times this value. Default <see cref="RestingScale"/>. A scale left at the default names none, which a theme that autoscales a quiver
    /// (<see cref="AutoScale"/>) takes for a quiver of no scale.</summary>
    public double Scale { get; set => Change(ref field, value); } = RestingScale;

    /// <summary>Whether the arrows are autoscaled, as matplotlib's <c>Axes.quiver</c> does when it is given no <c>scale</c>:
    /// the typical arrow, one as long as the mean of the vectors (a vector that is not a number takes no part), spans
    /// <c>1 / (1.8 * max(10, sqrt(N)))</c> of the width of the axes for <c>N</c> arrows, whatever the data units are, each points
    /// along its vector as it stands on the screen (not as it stands in data units), and the axes are ranged over the tails of
    /// the arrows. <see cref="Scale"/> is not read then. <see langword="false"/> draws every arrow <see cref="Scale"/> data units
    /// long for each unit of its vector, under every theme. <see langword="null"/> (the default) lets the theme decide: the
    /// matplotlib themes autoscale a quiver whose <see cref="Scale"/> is left at 1, the library's own themes never do.</summary>
    public bool? AutoScale { get; set => Change(ref field, value); }

    /// <summary>Whether the arrows are autoscaled when the series is drawn under <paramref name="theme"/>: what
    /// <see cref="AutoScale"/> says, else the theme's rule for a quiver of no named scale. The one rule the drawing, the key and the
    /// axes' range read.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns><see langword="true"/> when the arrows are a share of the axes' width.</returns>
    internal bool AutoScaledUnder(Theme theme) => AutoScale ?? (Scale == RestingScale && theme.Looks.VectorField.QuiverAutoScale);

    /// <summary>The share of the width of the axes that a vector of length 1 spans when the arrows are autoscaled: matplotlib's
    /// <c>scale = 1.8 * amean * sn / span</c> with <c>span = 1</c> in the width of the axes, <c>sn = max(10, sqrt(N))</c>, and
    /// <c>amean</c> the mean length of the vectors that are numbers (<c>Quiver._make_verts</c>). 0 when there is none to take the
    /// mean of, or they are all zero long, so no arrow has a length.</summary>
    /// <returns>1 over that scale.</returns>
    internal double AutoScaleShare()
    {
        double total = 0;
        int counted = 0;
        for (int i = 0; i < XData.Length; i++)
        {
            double u = UData[i], v = VData[i];
            if (!double.IsFinite(u) || !double.IsFinite(v)) continue;
            total += new Point(u, v).Length;
            counted++;
        }

        if (counted == 0 || total <= 0) return 0;
        return 1.0 / (1.8 * (total / counted) * Math.Max(10, Math.Sqrt(XData.Length)));
    }

    /// <summary>How wide the shaft of an autoscaled arrow is, as a share of the width of the axes: matplotlib's
    /// <c>width = 0.06 * span / clip(sqrt(N), 8, 25)</c> for <c>N</c> arrows (<c>Quiver._init</c>), which is also the unit an arrow's length is
    /// measured in when its polygon is made (<see cref="Rendering.SeriesRenderers.QuiverArrow"/>).</summary>
    /// <returns>The share.</returns>
    internal double AutoScaleWidth() => 0.06 / Math.Clamp(Math.Sqrt(XData.Length), 8, 25);

    /// <summary>The range of the tails of the arrows, which is what matplotlib's quiver ranges its axes over
    /// (<c>Quiver.get_datalim</c> reads <c>self.XY</c>): an arrow's length is a share of the axes and not a length in data units,
    /// so its head is no part of it. A tail that cannot be placed adds nothing.</summary>
    /// <returns>The contribution of the tails.</returns>
    private DataRangeContribution TailsRange()
    {
        var x = Range1D.Empty;
        var y = Range1D.Empty;
        for (int i = 0; i < XData.Length; i++)
        {
            if (!new Point(XData[i], YData[i]).IsDrawable()) continue;
            x = x.Including(XData[i]);
            y = y.Including(YData[i]);
        }
        return new(x.OrNone()?.Lo, x.OrNone()?.Hi, y.OrNone()?.Lo, y.OrNone()?.Hi);
    }

    /// <inheritdoc />
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) =>
        AutoScaledUnder(theme) ? TailsRange() : ComputeDataRange(context);

    /// <summary>Length of the two strokes of each arrowhead, as a fraction of the drawn length of the arrow itself.
    /// Default 0.3.</summary>
    public double ArrowHeadSize { get; set => Change(ref field, value); } = 0.3;

    /// <summary>Creates a new quiver series from the given position and vector data.</summary>
    public QuiverSeries(double[] xData, double[] yData, double[] uData, double[] vData)
    {
        XData = xData;
        YData = yData;
        UData = uData;
        VData = vData;
    }

    /// <inheritdoc />
    /// <remarks>Tails and heads, as an arrow <see cref="Scale"/> data units long reaches. An axes that draws the arrows autoscaled
    /// ranges over the tails alone.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the arrows that can be drawn, tail and head: an arrow with a coordinate that is missing (NaN) or infinite
        // is not drawn, so it is left out of the extent on both axes.
        var x = Range1D.Empty;
        var y = Range1D.Empty;
        for (int i = 0; i < XData.Length; i++)
        {
            var tail = new Point(XData[i], YData[i]);
            var head = new Point(tail.X + UData[i] * Scale, tail.Y + VData[i] * Scale);
            if (!tail.CanDrawLineTo(head)) continue;
            x = x.Including(tail.X).Including(head.X);
            y = y.Including(tail.Y).Including(head.Y);
        }
        return new(x.OrNone()?.Lo, x.OrNone()?.Hi, y.OrNone()?.Lo, y.OrNone()?.Hi);
    }

    static string ISeriesKind<QuiverSeries>.Kind => "quiver";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData,
        UData = UData, VData = VData,
        Color = Color, Scale = Scale, ArrowHeadSize = ArrowHeadSize, QuiverAutoScale = AutoScale
    };

    /// <summary>The reader of a <see cref="QuiverSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries scale and arrowhead size.</summary>
    internal static QuiverSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Quiver(dto.XData ?? [], dto.YData ?? [], dto.UData ?? [], dto.VData ?? []);
        s.Color = dto.Color;
        if (dto.Scale is { } scale) s.Scale = scale;
        if (dto.ArrowHeadSize is { } arrowHeadSize) s.ArrowHeadSize = arrowHeadSize;
        s.AutoScale = dto.QuiverAutoScale;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Where each arrow sits and what it points at.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(null, XData, YData, new NumberColumn("u", UData), new NumberColumn("v", VData));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
