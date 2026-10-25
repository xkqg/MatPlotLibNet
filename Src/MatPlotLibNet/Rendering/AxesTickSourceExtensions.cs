// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

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
internal readonly record struct TickSource(ITickLocator? Locator, ITickFormatter? Formatter, bool NamesCategories);

/// <summary>The one rule for the primary ticks of an axes' X or Y axis: where they stand and how they read. The axes
/// renderer draws and measures the ticks by it, and the data table reads a position by it (<see cref="NamesAt"/>): a
/// position a named tick stands on reads as that name, so the table and the picture cannot name one place
/// differently.</summary>
internal static class AxesTickSourceExtensions
{
    extension(Axes axes)
    {
        /// <summary>The source of the primary ticks of the X axis (<paramref name="isX"/>) or the Y axis, one rule for both:
        /// the axis' own locator; otherwise the one its scale brings, with that scale's formatter; otherwise, when the series
        /// stand categories along that axis (upright bars, a count plot and a candlestick's dates along X, bars that lie down
        /// along Y), one tick on each category's slot centre reading its name
        /// (<see cref="CategoryTickExtensions.AsCategoryTicks(string[], bool)"/>, the pair the builder names categories with;
        /// along Y the first category at the bottom, where the bars draw it); otherwise the default placement. The axis' own
        /// formatter is kept in every case. The labels are names (<see cref="TickSource.NamesCategories"/>) when the series
        /// stand categories along the axis and when the axis' formatter is a <see cref="CategoryFormatter"/>, whoever set it
        /// and whatever locator places its ticks. Read before anything is installed (layout) and after (drawing) alike.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
        /// <returns>Where the ticks come from.</returns>
        internal TickSource PrimaryTickSource(bool isX)
        {
            var axis = isX ? axes.XAxis : axes.YAxis;
            if ((axis.TickLocator ?? axis.ScaleLocator(isX)) is { } locator)
            {
                var formatter = axis.TickFormatter ?? (axis.TickLocator is null ? locator.ScaleFormatter() : null);
                return new TickSource(locator, formatter, formatter is CategoryFormatter);
            }

            if (axes.CategoryNames(alongY: !isX) is { } names)
            {
                var ticks = names.AsCategoryTicks();
                return new TickSource(ticks.Locator, axis.TickFormatter ?? ticks.Formatter, true);
            }

            return new TickSource(null, axis.TickFormatter, false);
        }

        /// <summary>The names of the categories the series stand along one axis: those of the first category-labelled series
        /// whose categories lie along it (<see cref="BarOrientationExtensions.NamesCategoriesAlongY"/>). A bar series on
        /// numeric x positions names none: it opts into a continuous axis and shares it with companion line series (e.g. a
        /// MACD histogram alongside its MACD and signal lines).</summary>
        /// <param name="alongY">The Y axis when <see langword="true"/>, the X axis otherwise.</param>
        /// <returns>The names, or <see langword="null"/> when no series names categories along that axis.</returns>
        internal string[]? CategoryNames(bool alongY) => axes.Series
            .OfType<ICategoryLabeled>()
            .Where(s => s is not BarSeries b || b.XCoordinate is null)
            .FirstOrDefault(s => s.CategoryLabels is not null && s.NamesCategoriesAlongY() == alongY)
            ?.CategoryLabels;

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
            var source = axes.PrimaryTickSource(isX);
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
        /// <summary>The tick locator a scale brings when the axis has none: <see cref="AutoDateLocator"/> for a date X
        /// axis, <see cref="SymlogLocator"/> for symlog, <see cref="LogLocator"/> for log; <see langword="null"/>
        /// otherwise.</summary>
        /// <param name="isX">Whether the axis is the X axis: only an X axis brings date ticks with its scale.</param>
        /// <returns>The scale's locator, or <see langword="null"/>.</returns>
        internal ITickLocator? ScaleLocator(bool isX) => axis.Scale switch
        {
            AxisScale.Date when isX => new AutoDateLocator(),
            AxisScale.SymLog        => new SymlogLocator(axis.SymLogLinThresh),
            AxisScale.Log           => new LogLocator(),
            _                       => null,
        };
    }

    extension(ITickLocator? locator)
    {
        /// <summary>The formatter that comes with a scale's locator: <see cref="AutoDateFormatter"/> with the date
        /// locator, <see cref="LogTickFormatter"/> with the log locator; <see langword="null"/> otherwise.</summary>
        /// <returns>The formatter that goes with it, or <see langword="null"/>.</returns>
        internal ITickFormatter? ScaleFormatter() => locator switch
        {
            AutoDateLocator date => new AutoDateFormatter(date),
            LogLocator           => new LogTickFormatter(),
            _                    => null,
        };
    }
}
