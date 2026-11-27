// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where the primary ticks of one axis come from and how they read.</summary>
/// <param name="Locator">The locator that places them; <see langword="null"/> for the default placement by plot
/// length.</param>
/// <param name="Formatter">The formatter that writes their labels; <see langword="null"/> for the uniform
/// format.</param>
/// <param name="NamesCategories">Whether the tick labels are category names: those of the series that stand on the axis,
/// or those of a <see cref="CategoryFormatter"/> the axis was given (a verb that names its categories along it). Their
/// width is the names', which no estimate knows, so the layout measures them. When it is <see langword="true"/>, the
/// locator and the formatter are both set.</param>
/// <param name="RangeRoundsOut">Whether the axis still rounds its range out under a theme that rounds to ticks, though it names
/// categories: the series names its rows after the range is set (<see cref="ICategoryPlacement.RangeRoundsOut"/>, a Gantt chart).</param>
internal readonly record struct TickSource(ITickLocator? Locator, ITickFormatter? Formatter, bool NamesCategories, bool RangeRoundsOut = false)
{
    /// <summary>Whether the fixed set of named ticks holds the axis' range, so a theme's rounding out to ticks does not move it: the
    /// ticks name categories, and their series does not round its rows out first.</summary>
    internal bool HoldsTheRange => NamesCategories && !RangeRoundsOut;
}

/// <summary>The one rule for the primary ticks of an axes' X or Y axis: where they stand and how they read. The axes
/// renderer draws and measures the ticks by it, and the data table reads a position by it (<see cref="NamesAt"/>): a
/// position a named tick stands on reads as that name, so the table and the picture cannot name one place
/// differently.</summary>
internal static class AxesTickSourceExtensions
{
    extension(TickSource source)
    {
        /// <summary>Which of <paramref name="ticks"/> are their scale's minor ticks with a label, as matplotlib draws them: under a theme
        /// whose minor ticks are matplotlib's minor locator's (<see cref="AxisTickLook.MinorsByLocator"/>), the ticks a log or logit locator
        /// names off its majors (<see cref="ITickSpaceLocator.NamesAsMinor"/>: a multiple of a power of ten, a logit minor). The one rule
        /// the marks, the labels' pad, the band the layout reserves and the major grid tell them apart by. The library's own themes draw
        /// every named tick as a major one.</summary>
        /// <param name="ticks">The ticks the source's locator named.</param>
        /// <param name="view">The ends of the view they were named over; <see langword="null"/> when the axis shows none.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>One flag per tick, or <see langword="null"/> when every tick is a major one.</returns>
        internal bool[]? MinorNamedAmong(double[] ticks, Range1D? view, Theme theme)
        {
            if (theme.Looks.Ticks.MinorNamer(source.Locator) is not { } named || view is not { } shown) return null;
            bool[]? minor = null;
            for (int i = 0; i < ticks.Length; i++)
                if (named.NamesAsMinor(ticks[i], shown)) (minor ??= new bool[ticks.Length])[i] = true;
            return minor;
        }

        /// <summary>The major ticks among <paramref name="ticks"/>: those <see cref="MinorNamedAmong"/> does not call minor ones. The
        /// major grid stands on these, and the minor ticks a scale places are worked out over them, so a labelled minor tick takes a
        /// minor grid line, as in matplotlib (printed for ylim (2, 80) with <c>ax.grid(True)</c>: a grid line at 10 alone).</summary>
        /// <param name="ticks">The ticks the source's locator named.</param>
        /// <param name="view">The ends of the view they were named over.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns><paramref name="ticks"/> itself when every tick is a major one; otherwise the majors, ascending.</returns>
        internal double[] MajorsAmong(double[] ticks, Range1D? view, Theme theme)
        {
            if (source.MinorNamedAmong(ticks, view, theme) is not { } minor) return ticks;
            var majors = new List<double>(ticks.Length);
            for (int i = 0; i < ticks.Length; i++)
                if (!minor[i]) majors.Add(ticks[i]);
            return [.. majors];
        }
    }

    extension(Axes axes)
    {
        /// <summary>The source of the primary ticks of the X axis (<paramref name="isX"/>) or the Y axis, one rule for both:
        /// the axis' own locator; otherwise the one its scale brings, with that scale's formatter; otherwise, when the series
        /// stand categories along that axis (upright bars, a count plot and a candlestick's dates along X, bars that lie down
        /// along Y), one tick on each category's slot centre reading its name
        /// (<see cref="CategoryTickExtensions.AsCategoryTicks(string[], bool)"/>, the pair the builder names categories with;
        /// along Y the first category at the bottom, where the bars draw it, or at the top where the series reads its rows from the top
        /// down, as a Gantt chart does: <see cref="ICategoryPlacement"/>); otherwise the default placement. The axis' own
        /// formatter is kept in every case. The labels are names (<see cref="TickSource.NamesCategories"/>) when the series
        /// stand categories along the axis and when the axis' formatter is a <see cref="CategoryFormatter"/>, whoever set it
        /// and whatever locator places its ticks. The layout and the drawing read the same source: nothing is written onto the
        /// axis between them.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
        /// <param name="theme">The theme the axes is drawn under, which puts the ticks of a box plot at its boxes where its box look stands
        /// them so (<see cref="BoxStanding.TicksAtBoxes"/>, the matplotlib themes); a reader that asks only whether the ticks name
        /// categories, which no theme changes, passes <see cref="Theme.Default"/>.</param>
        /// <returns>Where the ticks come from.</returns>
        internal TickSource PrimaryTickSource(bool isX, Theme theme)
        {
            var axis = axes.AxisAlong(isX);
            if (axis.LocatedTickSource(isX, theme) is { } located) return located;

            if (axes.CategoryNamer(alongY: !isX) is { CategoryLabels: { } names } namer)
            {
                var placement = namer as ICategoryPlacement;
                var ticks = names.AsCategoryTicks(topDown: placement is { CategoriesTopDown: true });
                return new TickSource(ticks.Locator, axis.TickFormatter ?? ticks.Formatter, true, placement is { RangeRoundsOut: true });
            }

            if (theme.Looks.Box.Standing.TicksAtBoxes && axes.BoxPositionsAlong(isX, theme) is { } boxes)
                return new TickSource(new FixedLocator(boxes), axis.TickFormatter, false);

            return new TickSource(null, axis.TickFormatter, false);
        }

        /// <summary>Where the box plots of the axes stand along the axis (<paramref name="isX"/>: X for upright boxes, Y for boxes that
        /// lie down) under a theme whose box look puts the ticks at the boxes as matplotlib's <c>bxp</c> does
        /// (<c>FixedLocator</c> at the positions): the positions of every dataset of every such series, each once, low to high.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The positions, or <see langword="null"/> when no box plot stands along that axis.</returns>
        private double[]? BoxPositionsAlong(bool isX, Theme theme)
        {
            SortedSet<double>? positions = null;
            var series = axes.Series;
            for (int s = 0; s < series.Count; s++)
            {
                if (series[s] is not BoxSeries box || box.Vert != isX) continue;
                positions ??= [];
                for (int i = 0; i < box.Datasets.Length; i++) positions.Add(box.PositionIn(i, theme));
            }

            return positions is { Count: > 0 } ? [.. positions] : null;
        }

        /// <summary>The series whose names the categories along one axis read: the first category-labelled series whose categories lie
        /// along it (<see cref="CategoryPlacementExtensions.NamesCategoriesAlongY"/>) and that names them; it also says whether they read from
        /// the top down (<see cref="ICategoryPlacement.CategoriesTopDown"/>). A bar series on numeric x positions names none: it opts into a
        /// continuous axis and shares it with companion line series (e.g. a MACD histogram alongside its MACD and signal lines).</summary>
        /// <param name="alongY">The Y axis when <see langword="true"/>, the X axis otherwise.</param>
        /// <returns>The series, or <see langword="null"/> when no series names categories along that axis.</returns>
        internal ICategoryLabeled? CategoryNamer(bool alongY) => axes.Series
            .OfType<ICategoryLabeled>()
            .Where(s => s is not BarSeries b || b.XCoordinate is null)
            .FirstOrDefault(s => s.CategoryLabels is not null && s.NamesCategoriesAlongY() == alongY);

        /// <summary>The names the X axis (<paramref name="isX"/>) or the Y axis shows at <paramref name="positions"/>: a
        /// position a tick stands on reads as that tick's label, written as the picture writes it (<see cref="PrimaryTickSource"/>).
        /// The positions are named only all together: when the axis names categories and a tick stands on every position that
        /// can be placed, so a column of positions becomes a column of names or stays as it is, never half and half. A
        /// position that cannot be placed (NaN, an infinity) is the gap between two runs of a line and has no name.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
        /// <param name="positions">Positions along that axis, in its data units.</param>
        /// <returns>One name for each position, <see langword="null"/> where the position cannot be placed; or
        /// <see langword="null"/> when the axis names no categories, no position can be placed, or a position that can be
        /// placed has no tick on it.</returns>
        internal string?[]? NamesAt(bool isX, IReadOnlyList<double> positions)
        {
            var source = axes.PrimaryTickSource(isX, Theme.Default);
            double[] placed = [.. positions.Where(double.IsFinite)];
            if (!source.NamesCategories || placed.Length == 0)
            {
                return null;
            }

            // The ticks over the positions' own extent, not over the drawn range: a table holds the data, so a category a
            // limit leaves out of the picture keeps its name.
            var ticks = new HashSet<double>(source.Locator!.LocateInterval(placed.Min(), placed.Max()));
            var names = new string?[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                double position = positions[i];
                if (!double.IsFinite(position)) continue;
                if (!ticks.Contains(position)) return null;
                names[i] = source.Formatter!.Format(position);
            }

            return names;
        }
    }

    extension(Axis axis)
    {
        /// <summary>The ticks of an axis that a locator places, the first rule of every axis' tick source
        /// (<see cref="PrimaryTickSource"/>, <see cref="OwnTickSource"/>): its own locator; otherwise the one its scale
        /// brings (<see cref="ScaleRule.Locator"/>), with that scale's formatter. The axis' own formatter is kept, and its labels
        /// are names when that formatter is a <see cref="CategoryFormatter"/>.</summary>
        /// <param name="isX">Whether the axis is the X axis: only an X axis brings date ticks with its scale.</param>
        /// <param name="theme">The theme the axis is drawn under, whose symlog threshold its scale's locator lays an axis given none by.</param>
        /// <returns>The source, or <see langword="null"/> when no locator places the axis' ticks.</returns>
        internal TickSource? LocatedTickSource(bool isX, Theme theme)
        {
            var rule = axis.ScaleRuleUnder(theme);
            return axis.LocatedBy(axis.TickLocator is null ? rule.Locator(isX) : null, rule, isX);
        }

        /// <summary>The ticks a locator places: the axis' own, otherwise <paramref name="scaleLocator"/>, the one the scale it is
        /// drawn on brings; with the formatter of the scale they are drawn on whatever locator places them
        /// (<see cref="ScaleRule.Formatter"/>), as matplotlib's scale sets the major formatter and <c>set_major_locator</c> leaves it. The axis'
        /// own formatter is kept, and its labels are names when that formatter is a <see cref="CategoryFormatter"/>.</summary>
        /// <param name="scaleLocator">The locator the scale the axis is drawn on brings, or <see langword="null"/>.</param>
        /// <param name="drawnOn">The scale the axis' ticks are drawn on (a linked axis', its primary's: log over a log primary).</param>
        /// <param name="isX">Whether the axis is the X axis: only an X axis brings date ticks with its scale.</param>
        /// <returns>The source, or <see langword="null"/> when no locator places the axis' ticks.</returns>
        private TickSource? LocatedBy(ITickLocator? scaleLocator, ScaleRule drawnOn, bool isX)
        {
            if ((axis.TickLocator ?? scaleLocator) is not { } locator) return null;
            var formatter = axis.TickFormatter ?? drawnOn.Formatter(locator, isX);
            return new TickSource(locator, formatter, formatter is CategoryFormatter);
        }

        /// <summary>The source of the ticks of a linked axis (<see cref="Axis.Link"/>), which is drawn on the scale of the primary
        /// axis it shows, not on a scale of its own: its own locator; otherwise, when the primary is logarithmic, the log
        /// locator with the log formatter, as matplotlib draws a linked axis over a log primary on its <c>functionlog</c> scale
        /// (decades); otherwise its spacing or the default placement, in its own formatter, as matplotlib's <c>function</c> scale
        /// over every other primary (symlog, logit and dates included) places them.</summary>
        /// <param name="primary">The scale of the primary axis the linked axis shows.</param>
        /// <returns>Where the linked axis' ticks come from.</returns>
        internal TickSource LinkedTickSource(ScaleRule primary)
        {
            // isX false: a linked axis is drawn on its primary's scale (ScaleRule.UnderALink), never on a date scale of its own.
            var drawnOn = primary.UnderALink;
            return axis.LocatedBy(drawnOn.Locator(isX: false), drawnOn, isX: false)
                ?? new TickSource(null, axis.TickFormatter, false);
        }

        /// <summary>The source of the ticks of an axis drawn over a range and a scale of its own, a Y axis slot stacked beside the
        /// plot or the top X axis, by the rule the primary's ticks follow (<see cref="LocatedTickSource"/>): its own locator,
        /// otherwise the one its scale brings (a log axis' decades, a symlog axis', a date X axis' dates), each with its
        /// formatter; otherwise its spacing or the default placement, in its own formatter (<see cref="TickValuesBetween"/>).</summary>
        /// <param name="isX">Whether the axis is an X axis: only an X axis brings date ticks with its scale.</param>
        /// <param name="theme">The theme the axis is drawn under, whose symlog threshold its scale's locator lays an axis given none by.</param>
        /// <returns>Where the axis' ticks come from.</returns>
        internal TickSource OwnTickSource(bool isX, Theme theme) => axis.LocatedTickSource(isX, theme) ?? new TickSource(null, axis.TickFormatter, false);

        /// <summary>The source of the ticks of a Y axis slot stacked beside the plot: <see cref="OwnTickSource"/> of a Y axis
        /// (<see cref="SlotTickValues"/>).</summary>
        /// <param name="theme">The theme the slot is drawn under.</param>
        /// <returns>Where the slot's ticks come from.</returns>
        internal TickSource SlotTickSource(Theme theme) => axis.OwnTickSource(isX: false, theme);

        /// <summary>The major tick values a Y axis slot stacked beside the plot draws over <paramref name="range"/>'s Y
        /// extent, the one source its drawing and the layout's reserve share: the locator of its tick source
        /// (<see cref="SlotTickSource"/>: its own, or its scale's), otherwise its tick spacing
        /// (<see cref="TickConfig.Spacing"/>), otherwise the default placement for the length of the plot, as the primary axis
        /// places its own (<see cref="TickValuesBetween"/>).</summary>
        /// <param name="range">The slot's data range; only its Y extent is read.</param>
        /// <param name="plotPixels">The height of the plot the slot stands beside.</param>
        /// <param name="theme">The theme, whose locator places the ticks of an axis that names none (<see cref="AxisTickLook.Ladder"/>).</param>
        /// <returns>The tick values.</returns>
        internal double[] SlotTickValues(DataRange range, double plotPixels, Theme theme) =>
            axis.TickValuesBetween(axis.SlotTickSource(theme), new Range1D(range.YMin, range.YMax), plotPixels, theme, isX: false);

        /// <summary>The major tick values of a 2-D axis between the two ends of <paramref name="span"/>: those the axis' request
        /// names (<see cref="TickRequestOver"/>, the one rule every kind of axis asks), the locator of <paramref name="source"/> in it.
        /// The primary X and Y axes over the axes' range, a Y slot with a range of its own (<see cref="SlotTickValues"/>), the top X axis
        /// with a range of its own and a linked axis on either edge, over the image of the primary's range in its own unit, are all laid
        /// out here.</summary>
        /// <remarks>The labelled minor ticks a locator names (<see cref="MinorNamedAmong"/>) are left out of an axis whose minor ticks
        /// were switched off, as matplotlib's <c>minorticks_off()</c> sets a <c>NullLocator</c>: no minor tick, so no minor label.</remarks>
        /// <param name="source">Where the axis' ticks come from.</param>
        /// <param name="span">The ends of the range the ticks are laid out over.</param>
        /// <param name="plotPixels">The length of the plot along the axis: its width along an X axis, its height along a Y
        /// axis.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <param name="isX">Whether the axis runs along X, where a label takes more room than along Y
        /// (<see cref="MaxNLocator.LabelSpanAlong"/>).</param>
        /// <returns>The tick values.</returns>
        internal double[] TickValuesBetween(TickSource source, Range1D span, double plotPixels, Theme theme, bool isX)
        {
            double[] ticks = axis.TickRequestOver(source.Locator, span, plotPixels, theme, isX).Majors();
            return source.Locator is not null && axis.MinorTicks.GivenVisible == false ? source.MajorsAmong(ticks, span, theme) : ticks;
        }

        /// <summary>What a 2-D axis asks for its major ticks over <paramref name="span"/> (<see cref="TickRequest"/>,
        /// <see cref="TickAxisKind.Plot"/>): <paramref name="given"/> told how many labels fit along the plot, else the axis' spacing
        /// (<see cref="TickConfig.Spacing"/>), else the theme's ladder laid along the plot (<see cref="AxisTickLook.Ladder"/>), else the
        /// library's placement for the plot's length (<see cref="AutoLocator.ForLength"/>); its labels at the axis' own size.</summary>
        /// <param name="given">The locator that places the axis' ticks, or <see langword="null"/>.</param>
        /// <param name="span">The ends of the range the ticks are laid out over.</param>
        /// <param name="plotPixels">The length of the plot along the axis.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <param name="isX">Whether the axis runs along X.</param>
        /// <returns>The request.</returns>
        internal TickRequest TickRequestOver(ITickLocator? given, Range1D span, double plotPixels, Theme theme, bool isX) =>
            new(span, plotPixels, axis.TickLabelSizeUnder(theme), isX, theme,
                TickAxisKind.Plot) { Given = given, Spacing = axis.MajorTicks.Spacing };

        /// <summary>How many tick labels fit along a plot of <paramref name="plotPixels"/>, as matplotlib's <c>get_tick_space</c>
        /// counts them: two label sizes a tick along a Y axis, three along an X axis, where a label takes about its width
        /// (<see cref="MaxNLocator.LabelsAlong"/>), the room the axis' tick request counts too (<see cref="TickRequest.TickSpace"/>). The
        /// minor locator of a log or logit scale is told it.</summary>
        /// <param name="plotPixels">The length of the plot along the axis.</param>
        /// <param name="theme">The theme the axis is drawn under: the size of its labels (<see cref="TickLabelSizeUnder"/>).</param>
        /// <param name="isX">Whether the axis runs along X.</param>
        /// <returns>The count, rounded down.</returns>
        internal double TickSpaceAlong(double plotPixels, Theme theme, bool isX) =>
            axis.TickRequestOver(null, default, plotPixels, theme, isX).TickSpace;

        /// <summary>The size of the axis' tick labels: its own (<see cref="TickConfig.LabelSize"/>), else the theme's tick font, as
        /// matplotlib's <c>Axis._get_tick_label_size</c> reads the axis' <c>labelsize</c>, else <c>xtick.labelsize</c>.</summary>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>The size in pixels.</returns>
        internal double TickLabelSizeUnder(Theme theme) => axis.MajorTicks.LabelSizeOver(theme.TickFont().Size);

        /// <summary>The length of a plot along which the axis lays its densest ticks under <paramref name="theme"/>, for a layout
        /// that measures the labels before the plot has a length: <see cref="AutoLocator.FullDensityLength"/>, from which the
        /// library's placement lays its most intervals, or, under a theme whose bins follow the length
        /// (<see cref="TickLadder.BinsByLength"/>), a length at which they are at their most, when that is longer: half a label span
        /// past the length where they reach it, so the count does not rest on the last bit of a division.</summary>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <param name="isX">Whether the axis runs along X.</param>
        /// <returns>The length in pixels.</returns>
        internal double DensestTickLength(Theme theme, bool isX) =>
            theme.Looks.Ticks.Ladder is { BinsByLength: true } ladder
                ? Math.Max(AutoLocator.FullDensityLength,
                    (ladder.Bins + 0.5) * MaxNLocator.LabelSpanAlong(isX) * axis.TickLabelSizeUnder(theme))
                : AutoLocator.FullDensityLength;
    }
}
