// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>The one question every kind of axis asks for its major ticks, and the one rule that answers it (<see cref="Majors"/>): a
/// locator the axis was given, told how many labels fit along it as matplotlib's locators read their axis' <c>get_tick_space</c>; else its
/// major tick spacing; else the theme's ladder (<see cref="AxisTickRule"/>); else the library's own placement for its kind
/// (<see cref="TickAxisKind"/>). A 2-D axis, an axis of a 3-D cube, the rings of a polar axes and a colour bar each build one and ask
/// it.</summary>
/// <param name="Span">The ends of the range the ticks are laid over, whichever way the axis runs.</param>
/// <param name="LengthPx">The length of the axis along which its labels are counted: a 2-D axis' plot length, the width of a 3-D axes
/// (a 3-D axis is matplotlib's <c>XAxis</c>), the height of a polar axes' circle (its radial axis is a <c>YAxis</c>), a colour bar's
/// length.</param>
/// <param name="LabelPx">The size of a tick label, in pixels.</param>
/// <param name="AlongX">Whether the labels are counted along X, where a label takes about its width, else along Y
/// (<see cref="MaxNLocator.LabelSpanAlong"/>): a 2-D axis by its direction, a 3-D axis along X, the rings of a polar axes along Y, a colour
/// bar the way it runs.</param>
/// <param name="Theme">The theme the axis is drawn under: its tick rule, and what a locator whose ticks follow the theme reads.</param>
/// <param name="Kind">The kind of axis, whose own placement the library's themes lay.</param>
internal readonly record struct TickRequest(Range1D Span, double LengthPx, double LabelPx, bool AlongX, Theme Theme, TickAxisKind Kind)
{
    private readonly double? _libraryLengthPx;

    /// <summary>The locator the axis was given, which places the ticks whatever else is set; <see langword="null"/> for none.</summary>
    internal ITickLocator? Given { get; init; }

    /// <summary>The axis' major tick spacing (<see cref="Models.TickConfig.Spacing"/>), whose multiples are the ticks where it gave no
    /// locator; <see langword="null"/> for none.</summary>
    internal double? Spacing { get; init; }

    /// <summary>The length the library's own placement is sized to: <see cref="LengthPx"/>, but the radius for the rings of a polar axes,
    /// which the library's themes lay as a ruler along the radius while matplotlib counts the room for its ticks along the circle's
    /// height.</summary>
    internal double LibraryLengthPx { get => _libraryLengthPx ?? LengthPx; init => _libraryLengthPx = value; }

    /// <summary>How many labels fit along the axis, matplotlib's <c>get_tick_space</c> (<see cref="MaxNLocator.LabelsAlong"/>): what a
    /// locator whose ticks follow the room is told, and the bins the theme's ladder lays when they follow the length.</summary>
    internal double TickSpace => MaxNLocator.LabelsAlong(LengthPx, LabelPx, AlongX);

    /// <summary>The major ticks of the axis.</summary>
    /// <returns>The ticks, ascending: a given locator's as it names them, a ladder's in view (a cube keeps its whole run), a ruler's inside
    /// its range.</returns>
    internal double[] Majors()
    {
        if (Given is { } given) return given.LocateAlong(Span.Lo, Span.Hi, TickSpace, Theme);
        if (Spacing is { } spacing) return new MultipleLocator(spacing).LocateInterval(Span.Lo, Span.Hi);

        var rule = Theme.Looks.TickRule;
        if (rule.Ladder is { } ladder) return Kind.Laid(ladder.ForTickSpace(TickSpace), Span, rule.RoundNumbers);
        return Kind.LibraryMajors(this);
    }

    /// <summary>Every list of major ticks the axis can name at any length, for a layout that keeps room before it knows the length (a
    /// colour bar's labels): a given locator whose ticks follow the room told every count of labels of the budget
    /// (<see cref="TickBudget.Standard"/>); a ladder whose bins follow the length laid with every count of its bins
    /// (<see cref="TickLadder.Budget"/>); the library's own placement at any length (<see cref="TickAxisKind.LibraryMajorsAtAnyLength"/>);
    /// otherwise the one list <see cref="Majors"/> names, which no length changes.</summary>
    /// <returns>The lists, one at a time.</returns>
    internal IEnumerable<double[]> MajorsAtAnyLength()
    {
        if (Given is ITickSpaceLocator spaced)
        {
            foreach (int space in TickBudget.Standard)
                yield return spaced.LocateAlong(Span.Lo, Span.Hi, space, Theme);
            yield break;
        }

        if (Given is null && Spacing is null)
        {
            var rule = Theme.Looks.TickRule;
            if (rule.Ladder is { BinsByLength: true } ladder)
            {
                foreach (int bins in ladder.Budget) yield return Kind.Laid(ladder with { Bins = bins }, Span, rule.RoundNumbers);
                yield break;
            }

            if (rule.Ladder is null)
            {
                foreach (double[] ticks in Kind.LibraryMajorsAtAnyLength(this)) yield return ticks;
                yield break;
            }
        }

        yield return Majors();
    }
}
