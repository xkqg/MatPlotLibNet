// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one place a direction picks the X or the Y member of a pair: the axis of an axes, its breaks and its scale, the
/// length, start, end, centre and a fraction of a rectangle (and a rectangle built along and across), the extent of a label across an
/// edge and its length along one, the ends of a range, of a range's contribution and of an axes' limits, a point's coordinate and a
/// point at a distance along and one across, the member of an <see cref="AxisPair{T}"/>, and the orientation of a line or of bars. A
/// caller says which way it runs (<c>isX</c>, <c>alongX</c>, or the way its own flag reads: a vertical colour bar runs along Y) and asks
/// here, so no site picks a member with a ternary of its own. A type that keeps the same thing for X and for Y (a transform, the placement
/// of a linked axis, a device grid) holds it as one <see cref="AxisPair{T}"/> and picks through it.</summary>
internal static class AlongExtensions
{
    extension(Axes axes)
    {
        /// <summary>The X axis along X, the primary Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The axis.</returns>
        internal Axis AxisAlong(bool isX) => isX ? axes.XAxis : axes.YAxis;

        /// <summary>The breaks of the X axis along X, those of the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The breaks.</returns>
        internal IReadOnlyList<AxisBreak> BreaksAlong(bool isX) => isX ? axes.XBreaks : axes.YBreaks;

        /// <summary>The axes this one shares its X scale with along X (<see cref="Axes.ShareXWith"/>), its Y scale otherwise.</summary>
        /// <param name="isX">The X scale when <see langword="true"/>.</param>
        /// <returns>The axes, or <see langword="null"/> when it shares none.</returns>
        internal Axes? SharesScaleAlong(bool isX) => isX ? axes.ShareXWith : axes.ShareYWith;

        /// <summary>The X axis and the primary Y axis as one pair, each in its direction's place.</summary>
        internal AxisPair<Axis> PrimaryAxes => new(axes.XAxis, axes.YAxis);
    }

    extension(Theme theme)
    {
        /// <summary>The margin the theme pads the X axis' range by along X (<see cref="Theme.AxisXMargin"/>), the Y axis' otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The margin, as a fraction of the range.</returns>
        internal double AxisMarginAlong(bool isX) => isX ? theme.AxisXMargin : theme.AxisYMargin;

        /// <summary>The share of the mean part the parts of a broken axis stand apart by: the theme's share between neighbouring columns of
        /// subplots along X (<see cref="SubPlotSpacing.HorizontalGapFraction"/>, matplotlib's <c>figure.subplot.wspace</c>), between rows
        /// otherwise (<see cref="SubPlotSpacing.VerticalGapFraction"/>, <c>figure.subplot.hspace</c>), as the parts of matplotlib's
        /// broken-axis recipe are subplots of their own; 0 under a theme that stands its subplots a number of pixels apart (the library's
        /// own), whose break is cut to nothing.</summary>
        /// <param name="isX">A break of the X axis, whose parts stand side by side, when <see langword="true"/>.</param>
        /// <returns>The share.</returns>
        internal double BreakSpacingAlong(bool isX) =>
            (isX ? theme.DefaultSpacing?.HorizontalGapFraction : theme.DefaultSpacing?.VerticalGapFraction) ?? 0;
    }

    extension(Rect rect)
    {
        /// <summary>The width along X, the height otherwise.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The length, in pixels.</returns>
        internal double LengthAlong(bool alongX) => alongX ? rect.Width : rect.Height;

        /// <summary>The left edge along X, the top edge otherwise: where the rectangle starts.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double StartAlong(bool alongX) => alongX ? rect.X : rect.Y;

        /// <summary>The right edge along X, the bottom edge otherwise: where the rectangle ends.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double EndAlong(bool alongX) => alongX ? rect.Right : rect.Bottom;

        /// <summary>The middle of the rectangle along X, its middle along Y otherwise.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double CentreAlong(bool alongX) => alongX ? rect.X + rect.Width / 2 : rect.Y + rect.Height / 2;

        /// <summary>The rectangle moved along X so it starts at <paramref name="start"/>, or along Y otherwise; its size and its place
        /// across stay.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="start">Where it starts along the direction.</param>
        /// <returns>The rectangle, moved.</returns>
        internal Rect WithStartAlong(bool alongX, double start) => alongX ? rect with { X = start } : rect with { Y = start };

        /// <summary>The rectangle with <paramref name="length"/> as its width along X, its height otherwise; its start and its
        /// size across stay.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="length">The new length along the direction.</param>
        /// <returns>The rectangle.</returns>
        internal Rect WithLengthAlong(bool alongX, double length) => alongX ? rect with { Width = length } : rect with { Height = length };

        /// <summary>The coordinate <paramref name="fraction"/> of the way along the rectangle, counted the way a value grows on the
        /// screen: rightward from its left edge along X, upward from its bottom edge along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="fraction">The share of the length, 0 at the low end, 1 at the high end.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double FractionAlong(bool alongX, double fraction) =>
            alongX ? rect.X + rect.Width * fraction : rect.Y + rect.Height * (1 - fraction);

        /// <summary>The share of the rectangle's length <paramref name="coordinate"/> stands at, counted the way a value grows on the
        /// screen (<see cref="FractionAlong"/> read back): from its left edge rightward along X, from its bottom edge upward along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="coordinate">The coordinate, in pixels.</param>
        /// <returns>The share, 0 at the low end, 1 at the high end.</returns>
        internal double FractionAt(bool alongX, double coordinate) =>
            alongX ? (coordinate - rect.X) / rect.Width : 1 - (coordinate - rect.Y) / rect.Height;

        /// <summary>Where values laid along the rectangle start, the way a value grows on the screen: its left edge along X, its
        /// bottom edge along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double ValueStartAlong(bool alongX) => alongX ? rect.X : rect.Bottom;

        /// <summary>Where values laid along the rectangle end: its right edge along X, its top edge along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate, in pixels.</returns>
        internal double ValueEndAlong(bool alongX) => alongX ? rect.Right : rect.Y;

        /// <summary>Which way a value grows in pixels along a direction: +1 along X (rightward), -1 along Y (upward, where the pixels
        /// grow downward). A direction whose growth is -1 shows its high values first.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>+1 or -1.</returns>
        internal static double ValueGrowthAlong(bool alongX) => alongX ? 1 : -1;

        /// <summary>The rectangle that starts at <paramref name="start"/> and runs <paramref name="length"/> along X, and stands from
        /// <paramref name="acrossStart"/> <paramref name="acrossLength"/> across it; the other way round along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="start">Where it starts along the direction.</param>
        /// <param name="length">How long it is along the direction.</param>
        /// <param name="acrossStart">Where it starts across the direction.</param>
        /// <param name="acrossLength">How long it is across the direction.</param>
        /// <returns>The rectangle.</returns>
        internal static Rect Along(bool alongX, double start, double length, double acrossStart, double acrossLength) =>
            alongX ? new Rect(start, acrossStart, length, acrossLength) : new Rect(acrossStart, start, acrossLength, length);
    }

    extension(TextExtent size)
    {
        /// <summary>How far an upright label reaches across an edge that runs along X (a top or a bottom edge): its height; across
        /// one that runs along Y (a left or a right edge): its width.</summary>
        /// <param name="alongX">Whether the edge runs along X.</param>
        /// <returns>The extent, in pixels.</returns>
        internal double ExtentAcross(bool alongX) => alongX ? size.Height : size.Width;
    }

    extension(DataRange range)
    {
        /// <summary>The X ends along X, the Y ends otherwise, in the order they are held.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The range.</returns>
        internal Range1D Along(bool isX) => isX ? new Range1D(range.XMin, range.XMax) : new Range1D(range.YMin, range.YMax);
    }

    extension(DataRangeContribution contribution)
    {
        /// <summary>The low end the contribution reaches on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The end, or <see langword="null"/> when the contribution reaches none there.</returns>
        internal double? MinAlong(bool isX) => isX ? contribution.XMin : contribution.YMin;

        /// <summary>The high end the contribution reaches on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The end, or <see langword="null"/> when the contribution reaches none there.</returns>
        internal double? MaxAlong(bool isX) => isX ? contribution.XMax : contribution.YMax;

        /// <summary>The low sticky edge of the contribution on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The edge, or <see langword="null"/> when the contribution holds none there.</returns>
        internal double? StickyMinAlong(bool isX) => isX ? contribution.StickyXMin : contribution.StickyYMin;

        /// <summary>The high sticky edge of the contribution on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The edge, or <see langword="null"/> when the contribution holds none there.</returns>
        internal double? StickyMaxAlong(bool isX) => isX ? contribution.StickyXMax : contribution.StickyYMax;

        /// <summary>A range measured as upright bars measure it (the categories' extent as X, the values as Y), put on the axes the
        /// bars stand on: as it is for upright bars, with X and Y swapped (their sticky edges with them, <c>Transposed</c>) for bars
        /// that lie down, their values along X.</summary>
        /// <param name="orientation">The bars' orientation.</param>
        /// <returns>The range on the axes.</returns>
        internal DataRangeContribution OnAxes(BarOrientation orientation) =>
            orientation.ValuesAlongX ? contribution.Transposed() : contribution;
    }

    extension(bool alongX)
    {
        /// <summary>Of a low and a high value laid along a direction, the one that stands where the screen starts along it: the low one
        /// along X (values grow rightward from the left edge), the high one along Y (values grow upward, and the screen starts at the
        /// top). The way back too: of the two ends that stand at the screen's start and end, the one that holds the low value.</summary>
        /// <typeparam name="T">What is laid along the direction (a bound, a value, a pixel).</typeparam>
        /// <param name="low">What stands at the low value.</param>
        /// <param name="high">What stands at the high value.</param>
        /// <returns>The one at the screen's start.</returns>
        internal T AtScreenStart<T>(T low, T high) => alongX ? low : high;

        /// <summary>Of a low and a high value laid along a direction, the one that stands where the screen ends along it: the high one
        /// along X, the low one along Y (<see cref="AtScreenStart"/>'s other).</summary>
        /// <typeparam name="T">What is laid along the direction.</typeparam>
        /// <param name="low">What stands at the low value.</param>
        /// <param name="high">What stands at the high value.</param>
        /// <returns>The one at the screen's end.</returns>
        internal T AtScreenEnd<T>(T low, T high) => alongX ? high : low;
    }

    extension(IAxesContext context)
    {
        /// <summary>The lower limit set on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The limit, or <see langword="null"/> when none is set.</returns>
        internal double? LimitMinAlong(bool isX) => isX ? context.XAxisMin : context.YAxisMin;

        /// <summary>The upper limit set on the X axis along X, on the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The limit, or <see langword="null"/> when none is set.</returns>
        internal double? LimitMaxAlong(bool isX) => isX ? context.XAxisMax : context.YAxisMax;

        /// <summary>The scale of the X axis along X, of the Y axis otherwise.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <returns>The scale.</returns>
        internal AxisScale ScaleAlong(bool isX) => isX ? context.XScale : context.YScale;
    }

    extension(Point point)
    {
        /// <summary>The point <paramref name="along"/> pixels along X and <paramref name="across"/> along Y when
        /// <paramref name="alongX"/>, the other way round otherwise: a mark or a line placed along an edge and across it.</summary>
        /// <param name="alongX">Whether the distance along runs along X.</param>
        /// <param name="along">The coordinate along the direction.</param>
        /// <param name="across">The coordinate across it.</param>
        /// <returns>The point.</returns>
        internal static Point Along(bool alongX, double along, double across) => alongX ? new Point(along, across) : new Point(across, along);

        /// <summary>The point's X along X, its Y otherwise.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The coordinate.</returns>
        internal double CoordinateAlong(bool alongX) => alongX ? point.X : point.Y;

        /// <summary>The point's Y across a direction along X, its X across one along Y.</summary>
        /// <param name="alongX">Whether the direction runs along X.</param>
        /// <returns>The coordinate.</returns>
        internal double CoordinateAcross(bool alongX) => alongX ? point.Y : point.X;

        /// <summary>The point with its X set to <paramref name="coordinate"/> along X, its Y otherwise; the other coordinate stays.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="coordinate">The new coordinate along the direction.</param>
        /// <returns>The point.</returns>
        internal Point WithCoordinateAlong(bool alongX, double coordinate) =>
            alongX ? point with { X = coordinate } : point with { Y = coordinate };
    }

    extension(SeriesRenderers.PixelSegment segment)
    {
        /// <summary>Which way a straight segment runs: along X (<see langword="true"/>) when its ends share their Y, along Y
        /// (<see langword="false"/>) when they share their X; <see langword="null"/> for one that runs neither way (slanted) or both (a
        /// point).</summary>
        internal bool? StraightAlongX =>
            (segment.From.Y == segment.To.Y) == (segment.From.X == segment.To.X) ? null : segment.From.Y == segment.To.Y;

        /// <summary>Whether a segment runs neither along X nor along Y: its ends share neither their X nor their Y (a point, whose ends
        /// share both, is not slanted).</summary>
        internal bool Slanted => segment.From.X != segment.To.X && segment.From.Y != segment.To.Y;
    }

    extension<T>(in AxisPair<T> pair)
    {
        /// <summary>The member of the pair along X, the one along Y otherwise, by reference: the value held, never a copy of it.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The member.</returns>
        internal ref readonly T Along(bool alongX) => ref alongX ? ref pair.X : ref pair.Y;

        /// <summary>The pair with <paramref name="value"/> as its member along X, along Y otherwise; the other member stays.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="value">The new member along the direction.</param>
        /// <returns>The pair.</returns>
        internal AxisPair<T> With(bool alongX, T value) => alongX ? new(value, pair.Y) : new(pair.X, value);

        /// <summary>The pair each of whose members is <paramref name="map"/> of this pair's member along the same direction: the one place
        /// a pair is worked out per direction, so a caller never names a direction by a literal of its own.</summary>
        /// <typeparam name="TState">What the map reads besides the member and its direction.</typeparam>
        /// <typeparam name="TOut">What the new pair holds.</typeparam>
        /// <param name="state">Handed to <paramref name="map"/> as it is, so the map needs no closure.</param>
        /// <param name="map">The member, whether it is the one along X, and <paramref name="state"/>, to the new member.</param>
        /// <returns>The new pair.</returns>
        internal AxisPair<TOut> Select<TState, TOut>(TState state, Func<T, bool, TState, TOut> map) =>
            new(map(pair.X, true, state), map(pair.Y, false, state));
    }


    extension(AxisPair<bool> flags)
    {
        /// <summary>Whether the flag stands along either direction (a mark that reaches beyond either log axis).</summary>
        internal bool Any => flags.X || flags.Y;

        /// <summary>The directions either of two pairs of flags names (the axes either of two marks reaches beyond).</summary>
        /// <param name="a">One pair.</param>
        /// <param name="b">The other pair.</param>
        /// <returns>Each direction either pair names.</returns>
        public static AxisPair<bool> operator |(AxisPair<bool> a, AxisPair<bool> b) => new(a.X || b.X, a.Y || b.Y);

        /// <summary>The directions both of two pairs of flags name (the axes both ends of one mark reach beyond).</summary>
        /// <param name="a">One pair.</param>
        /// <param name="b">The other pair.</param>
        /// <returns>Each direction both pairs name.</returns>
        public static AxisPair<bool> operator &(AxisPair<bool> a, AxisPair<bool> b) => new(a.X && b.X, a.Y && b.Y);
    }

    extension(Orientation orientation)
    {
        /// <summary>The orientation of a line, a span or a mark that runs along X (horizontal), or along Y (vertical).</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The orientation.</returns>
        internal static Orientation Along(bool alongX) => alongX ? Orientation.Horizontal : Orientation.Vertical;

        /// <summary>Whether a line, a span or a mark of this orientation runs along X: a horizontal one does, its value on the Y
        /// axis; a vertical one runs along Y, its value on the X axis.</summary>
        internal bool RunsAlongX => orientation == Orientation.Horizontal;
    }

    extension(BarOrientation orientation)
    {
        /// <summary>The orientation of bars (or boxes, or a fill) whose values run along X (they lie down, horizontal), or along Y
        /// (they stand up, vertical).</summary>
        /// <param name="alongX">The values along X when <see langword="true"/>.</param>
        /// <returns>The orientation.</returns>
        internal static BarOrientation ValuesAlong(bool alongX) => alongX ? BarOrientation.Horizontal : BarOrientation.Vertical;

        /// <summary>Whether bars (or boxes, or a fill) of this orientation have their values along X: they lie down
        /// (horizontal), their categories along Y; upright ones have their values along Y.</summary>
        internal bool ValuesAlongX => orientation == BarOrientation.Horizontal;

        /// <summary>The rule of the scale of the axis bars of this orientation measure their values along: the Y axis' for
        /// upright bars, the X axis' for bars that lie down. Their range takes only the values that axis can place.</summary>
        /// <param name="context">The axes the bars are drawn on.</param>
        /// <returns>The rule of the value axis' scale.</returns>
        internal ScaleRule ValueScale(IAxesContext context) => context.ScaleAlong(orientation.ValuesAlongX).Rule;
    }

    extension(ColorBarOrientation orientation)
    {
        /// <summary>Whether a colour bar of this orientation runs along X: a horizontal one does and stands below its axes; a
        /// vertical one runs along Y beside them.</summary>
        internal bool RunsAlongX => orientation == ColorBarOrientation.Horizontal;
    }

    extension(SankeyOrientation orientation)
    {
        /// <summary>Whether a Sankey diagram of this orientation flows along X (left to right, horizontal), its nodes stacked
        /// across it along Y; a vertical one flows along Y, top to bottom.</summary>
        internal bool FlowsAlongX => orientation == SankeyOrientation.Horizontal;
    }

    extension(Size size)
    {
        /// <summary>The width along X, the height otherwise.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The length, in pixels.</returns>
        internal double LengthAlong(bool alongX) => alongX ? size.Width : size.Height;
    }

    extension<T>(Sides<T> sides)
    {
        /// <summary>The side a direction starts at: the left side along X, the top side along Y (where a rectangle starts,
        /// <c>StartAlong</c> of a <see cref="Rect"/>).</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>What that side holds.</returns>
        internal T StartAlong(bool alongX) => alongX ? sides.Left : sides.Top;

        /// <summary>The side a direction ends at: the right side along X, the bottom side along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>What that side holds.</returns>
        internal T EndAlong(bool alongX) => alongX ? sides.Right : sides.Bottom;

        /// <summary>The sides with <paramref name="value"/> on the side a direction ends at (<see cref="EndAlong"/>): the right side
        /// along X, the bottom side along Y; the others as they were.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <param name="value">What that side holds.</param>
        /// <returns>The sides.</returns>
        internal Sides<T> WithEndAlong(bool alongX, T value) => alongX ? sides with { Right = value } : sides with { Bottom = value };
    }

    extension(LayoutBounds bounds)
    {
        /// <summary>The bounds of the gap between columns along X, of the gap between rows along Y.</summary>
        /// <param name="alongX">Along X when <see langword="true"/>.</param>
        /// <returns>The bounds of the gap.</returns>
        internal MarginBounds GapAlong(bool alongX) => alongX ? bounds.ColumnGap : bounds.RowGap;
    }

    extension(DataRangeContribution)
    {
        /// <summary>A contribution that reaches from <paramref name="min"/> to <paramref name="max"/> on the X axis along X, on
        /// the Y axis otherwise, and reaches nothing on the other axis.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>.</param>
        /// <param name="min">The low end.</param>
        /// <param name="max">The high end.</param>
        /// <returns>The contribution.</returns>
        internal static DataRangeContribution Along(bool isX, double? min, double? max) =>
            isX ? new DataRangeContribution(min, max, null, null) : new DataRangeContribution(null, null, min, max);
    }
}
