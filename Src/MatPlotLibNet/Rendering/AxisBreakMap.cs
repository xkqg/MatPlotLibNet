// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering;

/// <summary>The breaks of one axis and the range they are cut from, put in order once: the ONE rule that places a value on an
/// axis with breaks, and reads it back. A value inside a break has no place; any other value is mapped through the axis' scale
/// (<see cref="ScaleRule.Forward"/>) and moved down by the width, in that scaled space, of every break wholly below
/// it. So a break is cut out of the axis in the space the axis is drawn in: on a log axis the decades on either side keep
/// their width, and the start and the end of a break stand at one place, where the data jumps. On a linear axis the scaled
/// space is the data's own. Breaks that overlap are cut as their union, so a stretch two of them hide is taken out once and
/// the axis stays in order. <see cref="Inverse"/> undoes exactly this. Where the parts of the axis stand apart (matplotlib's broken-axis
/// recipe, under a theme whose subplots stand a share apart), a break between two parts gives back a room, a share of the parts' mean
/// span, so its start and its end stand that room apart (<see cref="Rooms"/>).</summary>
/// <remarks>A transform asks for every point it draws, so nothing is worked out per call: the breaks are merged and sorted by
/// where they begin, and the gap each removes from the range is measured, when the map is made. A call is one pass over the
/// cuts and allocates nothing. The map reads the breaks when it is made;
/// breaks added to the list afterwards are not seen.</remarks>
internal sealed class AxisBreakMap
{
    /// <summary>One break: where it lies in data units, and how much of the drawn range it removes.</summary>
    /// <param name="From">Where the break begins, in data units.</param>
    /// <param name="To">Where it ends, in data units.</param>
    /// <param name="Gap">How much of the drawn range it removes: 0 when none of it lies inside the range.</param>
    internal readonly record struct Cut(double From, double To, double Gap);

    private readonly Cut[] _cuts;
    // Beside the cuts, not in them: placing a point reads only the cuts, and a wider record costs every point it places.
    private readonly double[] _edges;
    // The rule of the scale, bound to its threshold, asked only where a value is mapped (a shaped axis), and whether it maps a value at
    // all, read once beside it: a value on a linear axis is placed past the cuts with no call to the rule.
    private readonly ScaleRule _rule;
    private readonly bool _scales;
    // The breaks that stand between two parts and the parts they leave, in data units, in order; none when the parts stand no room apart.
    private readonly Range1D[] _rooms;
    private readonly Range1D[] _parts;
    private readonly double[] _jumps;
    // The room each break between two parts leaves in the drawn space, and which cuts are such breaks; 0 and none when the parts
    // stand no room apart.
    private readonly double _room;
    private readonly bool[] _isRoom;

    /// <summary>Makes the map of <paramref name="breaks"/> cut from [<paramref name="fullMin"/>, <paramref name="fullMax"/>] on an
    /// axis drawn by <paramref name="rule"/>, its parts standing <paramref name="spacing"/> of their mean span apart.</summary>
    /// <param name="breaks">The hidden regions of the axis, in data units, in any order.</param>
    /// <param name="fullMin">Where the full (uncompressed) data range begins; a break is clipped to the range.</param>
    /// <param name="fullMax">Where it ends; a range handed over high to low (a reversed axis) is cut by its low and high ends
    /// all the same, and its compressed range stays high to low.</param>
    /// <param name="rule">The rule of the axis' scale, bound to its threshold: the space the breaks are cut out of;
    /// <see langword="null"/> for a linear axis.</param>
    /// <param name="spacing">The room left in place of each break that stands between two parts of the range (one with values of the
    /// range on either side), as a share of the mean span of the parts (<see cref="Models.SubPlotSpacing.SeparationOf"/>, matplotlib's
    /// <c>GridSpec</c> gap): the parts of matplotlib's broken-axis recipe are subplots of their own. 0 (the default) cuts every break
    /// to nothing.</param>
    public AxisBreakMap(IReadOnlyList<AxisBreak> breaks, double fullMin, double fullMax, ScaleRule? rule = null, double spacing = 0)
    {
        var scaling = rule ?? ScaleRule.Linear;
        _rule = scaling;
        _scales = scaling.ShapesItsSpace;

        // The range by its low and high ends: a range handed over high to low (a reversed axis) has the same breaks cut from it.
        double low = Math.Min(fullMin, fullMax), high = Math.Max(fullMin, fullMax);

        // The breaks are merged into their union first, so a stretch two breaks both hide is taken out once; what each removes is
        // measured, then the room the parts stand apart by is given back in place of every break between two parts, and the gaps
        // are added in the order the cuts begin, for the compressed range and for a placed value alike.
        _cuts = Union(breaks);
        _edges = new double[_cuts.Length];
        double removed = 0;
        int between = 0;
        for (int i = 0; i < _cuts.Length; i++)
        {
            var cut = _cuts[i];
            _cuts[i] = cut with { Gap = GapOf(cut.From, cut.To, low, high) };
            removed += _cuts[i].Gap;
            if (StandsBetween(cut, low, high)) between++;
        }

        double room = spacing > 0 && between > 0
            ? Models.SubPlotSpacing.SeparationOf(spacing, (Math.Abs(scaling.Forward(high) - scaling.Forward(low)) - removed) / (between + 1))
            : 0;
        double before = 0;
        for (int i = 0; i < _cuts.Length; i++)
        {
            var cut = _cuts[i];
            if (room > 0 && StandsBetween(cut, low, high)) _cuts[i] = cut = cut with { Gap = cut.Gap - room };
            _edges[i] = scaling.Forward(Math.Max(cut.From, low)) - before;
            before += cut.Gap;
        }

        var parted = room > 0 ? PartsOf(_cuts, low, high) : default;
        _rooms = parted.Rooms ?? [];
        _parts = parted.Parts ?? [];
        _jumps = parted.Jumps ?? [];
        _room = room;
        _isRoom = room > 0 ? new bool[_cuts.Length] : [];
        for (int i = 0; i < _isRoom.Length; i++) _isRoom[i] = StandsBetween(_cuts[i], low, high);

        // Every gap comes off the high end, which stands first on a reversed range.
        double first = scaling.Forward(fullMin), second = scaling.Forward(fullMax);
        CompressedRange = fullMin <= fullMax ? new MinMaxRange(first, second - before) : new MinMaxRange(first - before, second);
    }

    /// <summary>The range the axis is drawn over: the full one mapped through the scale, less every break's gap taken off
    /// its high end, (<c>Forward(fullMin)</c>, <c>Forward(fullMax) − totalGap</c>), or (<c>Forward(fullMin) − totalGap</c>,
    /// <c>Forward(fullMax)</c>) on a range handed over high to low: the range a <see cref="DataTransform"/> maps onto the
    /// plot.</summary>
    public MinMaxRange CompressedRange { get; }

    /// <summary>Whether <paramref name="value"/> falls strictly inside a break (From &lt; value &lt; To), in data units: inside
    /// the union of the breaks, so the value two touching breaks share is not.</summary>
    /// <param name="value">Data-space value to test.</param>
    public bool IsInBreak(double value)
    {
        foreach (var cut in _cuts)
            if (value > cut.From && value < cut.To) return true;
        return false;
    }

    /// <summary>Places <paramref name="value"/> on the axis: through the scale, then down by the gap of every cut that lies
    /// wholly below it.</summary>
    /// <param name="value">Data-space value to place.</param>
    /// <returns>The coordinate in the drawn space, or <see cref="double.NaN"/> when the value is inside a break or the scale has
    /// no place for it.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Remap(double value)
    {
        // The cuts are apart and in order, so one pass decides both: past a cut its gap is taken off, and inside one the value
        // has no place. A NaN value is past no cut and inside none, so it stays NaN. The pass has no early exit: with the few
        // breaks an axis has, a branch out of the loop costs more than the cuts it skips (measured on the break rows).
        double shift = 0;
        foreach (var cut in _cuts)
        {
            if (value >= cut.To) shift += cut.Gap;
            else if (value > cut.From) return double.NaN;
        }
        return (_scales ? _rule.Forward(value) : value) - shift;
    }

    /// <summary>Reads a coordinate in the drawn space back as a data value: the inverse of <see cref="Remap"/>, moved up by the
    /// gap of every break cut below it, then through the scale's inverse (<see cref="ScaleRule.Inverse"/>). The
    /// place a break is cut, where both its ends stand, reads as where the break begins; a coordinate just past it reads just
    /// past where the break ends. A coordinate inside the room a break between two parts leaves (<see cref="Rooms"/>) reads as a value
    /// the room's width short of where the break ends: the browser script reads the pointer by this same rule.</summary>
    /// <param name="drawn">The coordinate in the drawn space.</param>
    /// <returns>The data value; every value <see cref="Remap"/> places reads back as itself, except where a break ends.</returns>
    public double Inverse(double drawn)
    {
        double shift = 0;
        for (int i = 0; i < _cuts.Length; i++)
            if (_edges[i] < drawn) shift += _cuts[i].Gap;
        return _scales ? _rule.Inverse(drawn + shift) : drawn + shift;
    }

    /// <summary>Moves <paramref name="value"/> by <paramref name="delta"/> in the drawn space and reads it back by
    /// <see cref="Inverse"/>: the shift a pan makes, past every break it carries the value across. A value inside a break moves
    /// from where the break is cut, the place an end of the range inside a break is drawn at.</summary>
    /// <param name="value">The value, in data units.</param>
    /// <param name="delta">The shift, in the drawn space.</param>
    /// <returns>The value moved, in data units.</returns>
    internal double Shifted(double value, double delta) => Inverse(Drawn(value) + delta);

    /// <summary>Where <paramref name="value"/> stands in the drawn space: <see cref="Remap"/>'s place, and for a value inside a
    /// break the place that break is cut.</summary>
    private double Drawn(double value)
    {
        for (int i = 0; i < _cuts.Length; i++)
            if (value > _cuts[i].From && value < _cuts[i].To) return _edges[i];
        return Remap(value);
    }

    /// <summary>The breaks merged into their union, in the order they begin, each with its gap: with <see cref="Edges"/>, what
    /// a view written for the browser carries, so the script reads a pointer back by <see cref="Inverse"/>'s rule and places a
    /// value by <see cref="Remap"/>'s.</summary>
    internal ReadOnlySpan<Cut> Cuts => _cuts;

    /// <summary>Where each of <see cref="Cuts"/> is cut, at the same index: the one place in the drawn space both ends of the break
    /// stand at, where it begins (its start, or the start of the range when it begins below it) through the scale, less the
    /// gap of every break before it.</summary>
    internal ReadOnlySpan<double> Edges => _edges;

    /// <summary>The room each break between two parts leaves in the drawn space, the same for each (<see cref="Rooms"/>); 0 when the
    /// parts stand no room apart.</summary>
    internal double Room => _room;

    /// <summary>Whether the axis is drawn in parts that stand room apart (<see cref="Room"/> above 0): only then has it rooms and parts.</summary>
    internal bool IsDrawnInParts => Room > 0;

    /// <summary>Where <paramref name="drawn"/>, a place in the drawn space, stands in the space of the parts alone, every room taken
    /// out: less the room of every break between two parts whose room ends at or below it; a place inside a room stands where that
    /// break is cut. The room is a share of the parts' mean span, so it grows and shrinks with a zoom: a zoom about a pointer is taken
    /// in this space (<see cref="DataTransform.ZoomedAbout"/>), where the parts keep their scale, so the value under the pointer stays
    /// under it while no break comes into the view or leaves it.</summary>
    /// <param name="drawn">The place in the drawn space.</param>
    /// <returns>The place in the space of the parts.</returns>
    internal double WithoutRooms(double drawn)
    {
        double rooms = 0;
        for (int i = 0; i < _isRoom.Length; i++)
        {
            if (!_isRoom[i]) continue;
            if (drawn >= _edges[i] + _room) rooms += _room;
            else if (drawn > _edges[i]) return _edges[i] - rooms;
        }

        return drawn - rooms;
    }

    /// <summary>Reads <paramref name="place"/>, a place in the space of the parts alone (<see cref="WithoutRooms"/>), back as a data
    /// value: moved up by the whole width every break below it removes, then through the scale's inverse.</summary>
    /// <param name="place">The place in the space of the parts.</param>
    /// <returns>The data value.</returns>
    internal double InverseWithoutRooms(double place)
    {
        double shift = 0, rooms = 0;
        for (int i = 0; i < _cuts.Length; i++)
        {
            bool room = _isRoom.Length > 0 && _isRoom[i];
            if (_edges[i] - rooms < place) shift += _cuts[i].Gap + (room ? _room : 0);
            if (room) rooms += _room;
        }

        return _scales ? _rule.Inverse(place + shift) : place + shift;
    }

    /// <summary>The breaks, in data units and in order, that stand between two parts of the range and leave room between them in the
    /// drawn space (each from where it begins to where it ends: <see cref="Remap"/> places its start and its end that room apart); none
    /// when the parts stand no room apart. A break whose hidden stretch reaches past an end of the range leaves no part beyond it and
    /// no room.</summary>
    internal ReadOnlySpan<Range1D> Rooms => _rooms;

    /// <summary>The parts of the range the breaks leave, in data units and in order, each from its low end to its high end, when the parts
    /// stand room apart (<see cref="Rooms"/>); none otherwise. An axis drawn in parts places the ticks of each part as an axis of its own.</summary>
    internal ReadOnlySpan<Range1D> Parts => _parts;

    /// <summary>How much further each break between two parts (<see cref="Rooms"/>, at the same index) moves what lies past it than its
    /// room shows, in the drawn space: what it removes less its room. A part sees a value past that break this much further away
    /// along its own scale, so a straight line in the data that crosses the break is cut where it enters and leaves it
    /// (<see cref="DataTransform.CutAtTheStart"/>).</summary>
    internal ReadOnlySpan<double> Jumps => _jumps;

    /// <summary>Whether <paramref name="cut"/> stands between two parts of [<paramref name="low"/>, <paramref name="high"/>]: the range
    /// holds values on either side of it.</summary>
    private static bool StandsBetween(Cut cut, double low, double high) => cut.From > low && cut.To < high;

    /// <summary>The breaks of <paramref name="cuts"/> that stand between two parts of [<paramref name="low"/>, <paramref name="high"/>],
    /// and the parts: from the range's low end, or the end of a break that hides it, to the next break, and from there on to the high end
    /// or the start of a break that hides it.</summary>
    private static Parted PartsOf(Cut[] cuts, double low, double high)
    {
        var rooms = new List<Range1D>(cuts.Length);
        var jumps = new List<double>(cuts.Length);
        var parts = new List<Range1D>(cuts.Length + 1);
        double start = low, end = high;
        foreach (var cut in cuts)
        {
            if (cut.To <= low || cut.From >= high) continue;
            if (StandsBetween(cut, low, high))
            {
                parts.Add(new Range1D(start, cut.From));
                rooms.Add(new Range1D(cut.From, cut.To));
                jumps.Add(cut.Gap);
                start = cut.To;
            }
            else if (cut.From <= low) start = Math.Max(start, cut.To);
            else end = Math.Min(end, cut.From);
        }

        parts.Add(new Range1D(start, end));
        return new Parted([.. rooms], [.. parts], [.. jumps]);
    }

    /// <summary>The breaks that stand between two parts, and the parts, as <see cref="PartsOf"/> finds them.</summary>
    /// <param name="Rooms">The breaks between two parts, in order.</param>
    /// <param name="Parts">The parts, in order.</param>
    /// <param name="Jumps">What each break between two parts removes less its room.</param>
    private readonly record struct Parted(Range1D[] Rooms, Range1D[] Parts, double[] Jumps);

    /// <summary>The width, in the drawn space, of the part of [<paramref name="from"/>, <paramref name="to"/>] inside the full
    /// range; 0 when none of it is.</summary>
    private double GapOf(double from, double to, double fullMin, double fullMax)
    {
        double lo = Math.Max(from, fullMin);
        double hi = Math.Min(to, fullMax);
        return hi > lo ? _rule.Forward(hi) - _rule.Forward(lo) : 0;
    }

    /// <summary>The stretches <paramref name="breaks"/> hide, in the order they begin: breaks that overlap merged into one, a
    /// break that hides nothing (its end not past its start) left out. Breaks that only touch stay two, so the value they share
    /// keeps its place on the axis. Each is a cut whose gap is not measured yet. Sorted and merged in the one array the map
    /// keeps, which is copied shorter only when a break was merged or left out.</summary>
    private static Cut[] Union(IReadOnlyList<AxisBreak> breaks)
    {
        var cuts = new Cut[breaks.Count];
        for (int i = 0; i < cuts.Length; i++)
            cuts[i] = new Cut(breaks[i].From, breaks[i].To, 0);
        // Breaks that begin at one place either overlap, and are merged, or hide nothing, so the sort need not be stable.
        Array.Sort(cuts, static (a, b) => a.From.CompareTo(b.From));

        int count = 0;
        foreach (var cut in cuts)
        {
            if (!(cut.To > cut.From)) continue;
            if (count > 0 && cut.From < cuts[count - 1].To)
                cuts[count - 1] = cuts[count - 1] with { To = Math.Max(cuts[count - 1].To, cut.To) };
            else
                cuts[count++] = cut;
        }
        if (count < cuts.Length) Array.Resize(ref cuts, count);
        return cuts;
    }
}
