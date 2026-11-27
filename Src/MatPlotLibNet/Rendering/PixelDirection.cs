// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>One direction of a <see cref="DataTransform"/>: how a value along one edge of the plot becomes a pixel. The value goes
/// through its axis' scale and past its breaks (<see cref="Placed"/>), then is spread over the edge (<see cref="Spread"/>): its
/// distance from where the range starts, times one signed rate per unit, from where the plot starts. The X direction's rate is
/// positive on an axis drawn left to right; the Y direction's is negative on an axis drawn upward, since a pixel row counts down
/// the page. The two directions are the one rule; a transform holds one of each.</summary>
internal readonly struct PixelDirection
{
    /// <summary>A direction.</summary>
    /// <param name="perUnit">Pixels per unit of the drawn space, signed by the way the edge runs; 0 when the range has no
    /// width.</param>
    /// <param name="anchor">The value, in the drawn space, the range starts at.</param>
    /// <param name="start">The pixel of <paramref name="anchor"/>; when the range has no width, where every value lands: the
    /// middle of the edge.</param>
    /// <param name="breaks">The breaks cut out of the drawn space; <see langword="null"/> when the axis has none.</param>
    /// <param name="rule">The rule of the axis' scale, bound to its threshold (<see cref="ScaleRule.SymLogAt"/>), picked once for every
    /// value the direction places.</param>
    /// <param name="shown">The range the axis shows, through its scale and before its breaks.</param>
    internal PixelDirection(double perUnit, double anchor, double start, AxisBreakMap? breaks, ScaleRule rule, Range1D shown)
    {
        PerUnit = perUnit;
        Anchor = anchor;
        Start = start;
        Breaks = breaks;
        Rule = rule;
        _scales = rule.ShapesItsSpace;
        _clips = !double.IsNaN(rule.ClippedPlace);
        Shown = shown;
    }

    /// <summary>Pixels per unit of the drawn space, signed by the way the edge runs; 0 when the range has no width.</summary>
    internal double PerUnit { get; }

    /// <summary>The value, in the drawn space, the range starts at. Every value is measured from it, so a narrow range far from 0
    /// (a millisecond of dates) keeps all its digits instead of losing them to two large numbers that cancel.</summary>
    internal double Anchor { get; }

    /// <summary>The pixel of <see cref="Anchor"/>; when the range has no width, where every value lands: the middle of the edge.
    /// So one rule spreads a whole unshaped array whether the range has a width or not.</summary>
    internal double Start { get; }

    /// <summary>The breaks cut out of the drawn space; <see langword="null"/> when the axis has none.</summary>
    internal AxisBreakMap? Breaks { get; }

    /// <summary>The rule of the axis' scale, bound to its threshold: held, so a point asks it once and never picks it from the scale, in
    /// the room the scale and the threshold took (64 bytes either way); the two facts a point asks of the rule are read once, beside it,
    /// in its padding.</summary>
    internal ScaleRule Rule { get; }

    /// <summary>Whether the rule maps a value at all (<see cref="ScaleRule.ShapesItsSpace"/>), read once when the transform is made: a
    /// value on a linear or a date axis is placed as it is, with no call to the rule for each point.</summary>
    private readonly bool _scales;

    /// <summary>Whether the rule clips a value it has no place for, its <see cref="ScaleRule.ClippedPlace"/> being a number, read once:
    /// the clipped batches ask it of every value.</summary>
    private readonly bool _clips;

    /// <summary>The range the axis shows, through its scale and before its breaks.</summary>
    internal Range1D Shown { get; }

    /// <summary>Whether the direction is drawn in a space other than the data's own, or with a hole: a break, or a scale that shapes
    /// its space (<see cref="ScaleRule.ShapesItsSpace"/>, the rule <see cref="AxisMapping.IsShaped"/> reads). An unshaped direction is
    /// spread over a whole array a vector at a time (a date axis is one); a shaped one without breaks maps the array through its scale
    /// first (<see cref="ScaleRule.ForwardAll"/>) and is then spread the same way.</summary>
    internal bool IsShaped => Breaks is not null || _scales;

    /// <summary>A value in the space the axis is drawn in: through its scale, then past its breaks (NaN inside one).</summary>
    /// <param name="value">The value, in data units.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double Placed(double value) => Breaks is null ? Scaled(value) : Breaks.Remap(value);

    /// <summary>A value through the axis' scale alone: the value itself on an axis that does not shape its space.</summary>
    /// <param name="value">The value, in data units.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double Scaled(double value) => _scales ? Rule.Forward(value) : value;

    /// <summary>The one rule a value in the drawn space becomes a pixel by: the value less <see cref="Anchor"/>, times
    /// <see cref="PerUnit"/>, plus <see cref="Start"/>. Each step is rounded once, never fused, so the bits are the same on every
    /// processor and in the browser's script; the kernels of <see cref="PixelSpreadExtensions"/> compute it a vector at a time.
    /// A flat direction needs no rule of its own: its rate is 0, so a value the axis can place lands on <see cref="Start"/>, the
    /// centre, and one it cannot (NaN, an infinity, a value in a break, which <see cref="Placed"/> makes NaN) stays NaN, as on
    /// every other axis.</summary>
    /// <param name="placed">The value, in the drawn space.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double Spread(double placed) => (placed - Anchor) * PerUnit + Start;

    /// <summary>The pixel of <paramref name="value"/>: <see cref="Placed"/>, then <see cref="Spread"/>.</summary>
    /// <param name="value">The value, in data units.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double Place(double value) => Spread(Placed(value));

    /// <summary>The pixel of <paramref name="value"/> when the axis shows it: through the scale it lies on the shown range, give
    /// or take <see cref="Range1D.CloseTolerance"/> (a value inside a break lies on it but has no pixel), and its pixel is a
    /// number; otherwise <see langword="null"/>. The value goes through the scale once.</summary>
    /// <param name="value">The value, in data units.</param>
    internal double? OnPlot(double value)
    {
        double scaled = Scaled(value);
        if (!Shown.HoldsClose(scaled)) return null;
        double pos = Spread(Breaks is null ? scaled : Breaks.Remap(value));
        return pos.IsDrawable() ? pos : null;
    }

    /// <summary>The pixel a value the scale cannot place stands at: on a scale that clips it, its <see cref="ScaleRule.ClippedPlace"/>
    /// spread over the edge (the log scale's, a thousand decades below where the axis begins, as matplotlib's log scale clips such a
    /// value), so a pixel there is known to be one. NaN on every other scale (a logit axis masks what it cannot place) and on a
    /// direction with no width, which has no "below".</summary>
    internal double Beyond => _clips && PerUnit != 0 ? Spread(Rule.ClippedPlace) : double.NaN;

    /// <summary>The pixel of <paramref name="value"/> as matplotlib's transform places it before any clip: a finite value the log
    /// scale cannot place at <see cref="Beyond"/> (no break stands that far down, so the breaks move it nowhere), any other value
    /// as <see cref="Place"/> gives it (NaN where the scale masks it, as a logit axis does).</summary>
    /// <param name="value">The value, in data units.</param>
    /// <returns>The pixel along this direction.</returns>
    internal double PlaceUnclipped(double value) => ClipsBase(value) && Beyond is var beyond && !double.IsNaN(beyond)
        ? beyond
        : Place(value);

    /// <summary>The pixel of every value of <paramref name="data"/>, as <see cref="PlaceUnclipped"/> gives it: <see cref="Transform"/>,
    /// with every value the log scale cannot place at <see cref="Beyond"/>. On any other scale the result is
    /// <see cref="Transform"/>'s, bit for bit.</summary>
    /// <param name="data">The values, in data units.</param>
    /// <returns>One pixel per value.</returns>
    internal double[] TransformUnclipped(ReadOnlySpan<double> data)
    {
        var dst = Transform(data);
        double beyond = Beyond;
        if (double.IsNaN(beyond)) return dst;
        for (int i = 0; i < dst.Length; i++)
        {
            if (ClipsBase(data[i])) dst[i] = beyond;
        }

        return dst;
    }

    /// <summary>Whether the scale clips <paramref name="value"/> rather than masking it (<see cref="ScaleRule.ClippedPlace"/>): a
    /// finite value a log axis has no place for (0 or below), which matplotlib's log scale clips (<c>nonpositive='clip'</c>), so a base
    /// a mark stands on there is pinned to where the axis begins. Every other scale masks what it cannot place, a logit axis its 0 and
    /// its 1 as matplotlib's logit scale does (<c>nonpositive='mask'</c>). A missing value and an infinity are not clipped: no axis
    /// places them.</summary>
    /// <param name="value">The value, in data units.</param>
    /// <returns><see langword="true"/> when the scale clips the value.</returns>
    internal bool ClipsBase(double value) => _clips && value.IsDrawable() && !Rule.HasPlaceFor(value);

    /// <summary>The pixel of a base <paramref name="value"/> a mark stands on: <see cref="Place"/>, or <paramref name="start"/> when the
    /// scale clips the value (<see cref="ClipsBase"/>), the base pinned where the axis begins.</summary>
    /// <param name="value">The base, in data units.</param>
    /// <param name="start">The pixel of the edge where the axis begins, at its lowest value.</param>
    /// <returns>The pixel along this direction.</returns>
    internal double PlaceClipped(double value, double start) => ClipsBase(value) ? start : Place(value);

    /// <summary>Whether <paramref name="pixel"/> stands where a value the scale cannot place stands (<see cref="Beyond"/>).</summary>
    /// <param name="pixel">A pixel along this direction.</param>
    /// <returns><see langword="true"/> when the pixel is where such a value stands.</returns>
    internal bool LiesBeyond(double pixel) => pixel == Beyond;

    /// <summary><paramref name="pixel"/> moved onto <paramref name="start"/> when it lies beyond the axis (<see cref="LiesBeyond"/>), as
    /// it is otherwise.</summary>
    /// <param name="pixel">A pixel along this direction.</param>
    /// <param name="start">The pixel of the edge where the axis begins, at its lowest value.</param>
    /// <returns>The pixel, on the edge when it lay beyond it.</returns>
    internal double AtTheStart(double pixel, double start) => LiesBeyond(pixel) ? start : pixel;

    /// <summary>The rooms the axis leaves between its parts (<see cref="AxisBreakMap.IsDrawnInParts"/>) on the screen, each as
    /// <see cref="RoomAt"/> places it, in screen order (what <see cref="Rendering.DataTransform.RoomCut"/> and the spines read); none in one piece.</summary>
    internal PixelRoom[] PixelRooms()
    {
        if (Breaks is not { IsDrawnInParts: true } map) return [];
        var rooms = new PixelRoom[map.Rooms.Length];
        for (int i = 0; i < rooms.Length; i++) rooms[i] = RoomAt(i);
        Array.Sort(rooms, static (p, q) => p.Lo.CompareTo(q.Lo));
        return rooms;
    }

    /// <summary>The room at <paramref name="index"/> of the axis' rooms (<see cref="AxisBreakMap.Rooms"/>, in the order the map holds
    /// them) on the screen: both its ends placed, from its first pixel to its last along the edge, and its jump in pixels. The one rule a
    /// room stands on the screen by; the axis must be drawn in parts.</summary>
    internal PixelRoom RoomAt(int index)
    {
        double a = Place(Breaks!.Rooms[index].Lo), b = Place(Breaks.Rooms[index].Hi);
        return new PixelRoom(Math.Min(a, b), Math.Max(a, b), Breaks.Jumps[index] * Math.Abs(PerUnit));
    }

    /// <summary>The pixel of every value of <paramref name="data"/>, as <see cref="Place"/> gives it, to the bit: an unshaped direction
    /// spreads the whole array a vector at a time; a shaped one without breaks maps it through its scale in one call to the rule
    /// (<see cref="ScaleRule.ForwardAll"/>) and spreads the mapped values the same way; one with breaks places each value past them.</summary>
    /// <param name="data">The values, in data units.</param>
    /// <returns>One pixel per value.</returns>
    internal double[] Transform(ReadOnlySpan<double> data)
    {
        var dst = new double[data.Length];
        if (Breaks is not null)
        {
            for (int i = 0; i < data.Length; i++)
                dst[i] = Spread(Breaks.Remap(data[i]));
            return dst;
        }

        var spread = dst.AsSpan();
        if (_scales)
        {
            Rule.ForwardAll(data, spread);
            spread.Spread(spread, this);
        }
        else
        {
            spread.Spread(data, this);
        }

        return dst;
    }

    /// <summary>The values of <paramref name="data"/> in the space the axis is drawn in, for a direction without breaks: the values
    /// themselves on an axis that does not shape its space, otherwise mapped through its scale into <paramref name="scratch"/>
    /// (<see cref="ScaleRule.ForwardAll"/>).</summary>
    /// <param name="data">The values, in data units.</param>
    /// <param name="scratch">Room for as many values, used only on a shaped axis.</param>
    /// <returns>The values in the drawn space.</returns>
    internal ReadOnlySpan<double> PlacedAll(ReadOnlySpan<double> data, Span<double> scratch)
    {
        if (!_scales) return data;
        Rule.ForwardAll(data, scratch);
        return scratch[..data.Length];
    }

    /// <summary>The pixel of every base of <paramref name="data"/>: <see cref="Transform"/>, with every base the scale cannot place
    /// clipped to <paramref name="start"/> (<see cref="ClipsBase"/>). On an axis that places every finite value the result is
    /// <see cref="Transform"/>'s, bit for bit.</summary>
    /// <param name="data">The bases, in data units.</param>
    /// <param name="start">The pixel of the edge where the axis begins, at its lowest value.</param>
    /// <returns>One pixel per base.</returns>
    internal double[] TransformClipped(ReadOnlySpan<double> data, double start)
    {
        var dst = Transform(data);
        for (int i = 0; i < dst.Length; i++)
        {
            if (ClipsBase(data[i])) dst[i] = start;
        }

        return dst;
    }
}
