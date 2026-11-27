// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar and where each stands along it: the one place a colour bar decides which values it
/// names and where they stand. The ticks come from a tick locator, as an axis's do, so a range of 0 to 11 reads 0, 2, 4, 6, 8
/// and 10 and not fifths of 11; how many it asks for depends on how long the bar is, so a short bar is not crowded. Under the
/// matplotlib themes the locator is the theme's <c>AutoLocator</c> laid along the bar (<see cref="AxisTickLook.Ladder"/>), as
/// matplotlib's colour-bar axis lays its ticks; under the library's own themes it is one sized to the bar's length. A locator the
/// bar was given (<see cref="ColorBar.Locator"/>) places them instead, every one of its ticks inside the range.
/// <para>A value stands where the chart's normaliser sends its colour: the gradient is the colour map laid out evenly along the
/// bar, and the colour of a value is the map's at the normaliser's fraction of it, so under a log normaliser the tick of 200 in
/// 1 to 1000 stands at 0.767 of the bar and not at its 0.199 share of the range. A tick that would stand within one label of the
/// one before it is left out.</para>
/// <para>Under matplotlib's <c>LogNorm</c> the ticks are the ones a log axis names over the range (<see cref="LogLocator.TicksNamedOver"/>):
/// the powers of ten, and the multiples of them when the range holds one power or none.</para>
/// <para>A normaliser that sends the values between two boundaries to one colour (<see cref="IDiscreteNormalizer"/>) makes the
/// bar bands: a tick on each boundary, the boundaries evenly spaced, and a gradient of one colour per band.</para></summary>
/// <param name="Min">The value at the start of the gradient (the bottom of a vertical bar, the left of a horizontal one).</param>
/// <param name="Max">The value at the end of the gradient.</param>
/// <param name="Ticks">The tick values, in ascending order; none when the range cannot be drawn.</param>
/// <param name="Labels">The text of each tick, in one number format, beside <paramref name="Ticks"/>.</param>
/// <param name="Fractions">Where each tick stands along the gradient, from its start (0) to its end (1), beside
/// <paramref name="Ticks"/>.</param>
/// <param name="Bins">The number of bands of a bar of bands; 0 for a bar of continuous colour.</param>
/// <param name="BandFractions">The fraction of the colour map each band is drawn in, beside the bands; <see langword="null"/> for the
/// even share of each, band by band, the colours of bins that run over the whole map.</param>
/// <param name="Normalizer">The normaliser the bar is drawn with, which says where a value stands along it (<see cref="FractionOf"/>);
/// <see langword="null"/> for the linear one.</param>
/// <param name="LogMarks">Where the tick marks of a bar of matplotlib's log norm stand, under a theme that marks its bars
/// (<see cref="ColorBarLook.TickMarks"/>); <see langword="null"/> for every other bar, whose marks stand on its ticks.</param>
internal readonly record struct ColorBarScale(double Min, double Max, double[] Ticks, string[] Labels, double[] Fractions, int Bins,
    double[]? BandFractions = null, INormalizer? Normalizer = null, ColorBarScale.ColorBarLogMarks? LogMarks = null)
{
    /// <summary>Where the major tick marks stand along the bar: on the powers of ten of a bar of matplotlib's log norm, on every named
    /// tick of any other bar.</summary>
    internal double[] MajorMarks => LogMarks?.Majors ?? Fractions;

    /// <summary>Where the minor tick marks stand along the bar: between the powers of a bar of matplotlib's log norm, none on any
    /// other bar (matplotlib's colour-bar axes has a <c>NullLocator</c> for its minor ticks but on a log scale).</summary>
    internal double[] MinorMarks => LogMarks?.Minors ?? [];

    /// <summary>Whether the label of tick <paramref name="i"/> belongs to a minor tick, which stands its label from a minor mark: a
    /// named multiple of a power on a bar of matplotlib's log norm.</summary>
    /// <param name="i">The index of the tick.</param>
    /// <returns><see langword="true"/> when the label belongs to a minor tick.</returns>
    internal bool IsMinorLabel(int i) => LogMarks is { } marks && marks.MinorLabels[i];

    /// <summary>Where <paramref name="value"/> stands along this bar, from the start of its gradient (0) to its end (1): the one rule
    /// a tick a locator places and a level line of a contour's bar stand by (<see cref="FractionAlong"/>).</summary>
    /// <param name="value">The value.</param>
    /// <returns>The fraction; a value outside the range lies outside 0 to 1 on a bar of continuous colour, and at the nearer end of a
    /// bar of bands.</returns>
    internal double FractionOf(double value) => FractionAlong(value, Min, Max, Normalizer ?? LinearNormalizer.Instance);

    /// <summary>Where <paramref name="value"/> stands along a bar from <paramref name="min"/> to <paramref name="max"/> drawn with
    /// <paramref name="normalizer"/>: on a bar of bands as far along its band as it lies between the band's two boundaries
    /// (<see cref="UniformFractionOf"/>), else where the normaliser sends it, mirrored for a range from high to low.</summary>
    private static double FractionAlong(double value, double min, double max, INormalizer normalizer)
    {
        if (normalizer is IDiscreteNormalizer { Boundaries.Length: >= 2 } bands) return UniformFractionOf(bands.Boundaries, value);
        double lo = Math.Min(min, max), hi = Math.Max(min, max);
        double along = normalizer.Normalize(value, lo, hi);
        return min > max ? 1 - along : along;
    }

    /// <summary>The ticks of a bar <paramref name="lengthPx"/> long whose labels are set in <paramref name="tickFont"/>.</summary>
    /// <param name="min">The value at the start of the gradient; it may be above <paramref name="max"/>.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="lengthPx">The length of the bar along which the ticks stand.</param>
    /// <param name="vertical">Whether the bar is vertical, where a label takes the height of its font, and not horizontal,
    /// where it takes its width.</param>
    /// <param name="normalizer">The normaliser the chart colours its values with.</param>
    /// <param name="ctx">The context the width of a label is measured with.</param>
    /// <param name="tickFont">The tick font.</param>
    /// <param name="theme">The theme the bar is drawn under, whose log labels a bar of matplotlib's log norm names
    /// (<see cref="LogLocator.TicksNamedOver"/>) and whose tick label look signs a negative label (<see cref="TickLabelLook.Signed"/>);
    /// <see cref="Theme.Default"/> for the library's own themes'.</param>
    /// <param name="locator">The locator the bar was given (<see cref="ColorBar.Locator"/>), or <see langword="null"/>. A given
    /// locator places the ticks, as matplotlib's <c>Colorbar.locator</c> does: every tick it places inside the range is named, on a
    /// bar of any length, where the bar puts its value (between two boundaries of a bar of bands, a tick stands as far along its band
    /// as its value lies between them, matplotlib's uniform spacing).</param>
    /// <returns>The scale; with no tick when the range is not finite or has no width.</returns>
    internal static ColorBarScale For(double min, double max, double lengthPx, bool vertical,
        INormalizer normalizer, IRenderContext ctx, Styling.Font tickFont, Theme theme, ITickLocator? locator = null)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return new(min, max, [], [], [], 0, Normalizer: normalizer);
        var labelLook = theme.Looks.TickLabels;

        // The normaliser takes an ascending range, so a range from high to low is placed in the ascending one and mirrored.
        double lo = Math.Min(min, max), hi = Math.Max(min, max);
        double Along(double tick) => min > max ? 1 - normalizer.Normalize(tick, lo, hi) : normalizer.Normalize(tick, lo, hi);
        // The bar's request (a ruler): how many labels fit along it is matplotlib's get_tick_space, two label sizes a tick up a vertical
        // bar, three across a horizontal one.
        var request = RequestOver(lo, hi, lengthPx, EdgeTerms.Past(!vertical), tickFont, theme, locator);

        var bands = normalizer is IDiscreteNormalizer { Boundaries.Length: >= 2 } discrete ? discrete : null;
        int bins = bands is null ? 0 : bands.Boundaries.Length - 1;
        double[]? bandFractions = bands is null ? null : [.. Enumerable.Range(0, bins).Select(bands.MapFractionOf)];
        // matplotlib's Colorbar takes the scale of its norm: under its log norm a LogLocator and a LogFormatterSciNotation, as a log
        // axis has.
        bool logNorm = normalizer.Rule.NamesDecadesOver(lo);

        double[] ticks, fractions;
        string[]? labels = null;
        if (locator is not null)
        {
            ticks = Inside(request.Majors(), lo, hi);
            fractions = [.. ticks.Select(tick => FractionAlong(tick, min, max, normalizer))];
            var given = logNorm && bands is null && theme.Looks.ColorBar.TickMarks
                ? new ColorBarLogMarks(fractions, MinorFractions(ticks, lo, hi, request.TickSpace, theme, Along), new bool[ticks.Length])
                : (ColorBarLogMarks?)null;
            return new(min, max, ticks, GivenLabels(ticks, logNorm, labelLook), fractions, bins, bandFractions, normalizer, given);
        }

        if (bands is not null)
        {
            int[] named = NamedBoundaries(bands.Boundaries, bands.MostNamed);
            ticks = [.. named.Select(k => bands.Boundaries[k])];
            fractions = [.. named.Select(k => (double)k / bins)];
        }
        else if (logNorm)
        {
            ticks = LogLocator.TicksNamedOver(lo, hi, request.TickSpace, request.Theme);
            labels = LogLabels(ticks);
            fractions = [.. ticks.Select(Along)];
        }
        else
        {
            // The ticks are chosen in the value space, as an axis chooses them, and placed where the normaliser puts them: under the
            // matplotlib themes by the theme's AutoLocator laid along the bar, as matplotlib's colour-bar axis lays them; under the
            // library's own by a ruler sized to the bar's length.
            ticks = request.Majors();
            fractions = [.. ticks.Select(Along)];
        }

        labels ??= labelLook.Labelled(ticks);
        int[] kept = KeptTicks(fractions, labels, lengthPx, vertical, ctx, tickFont);
        ColorBarLogMarks? logMarks = null;
        if (logNorm && bands is null && theme.Looks.ColorBar.TickMarks)
        {
            // matplotlib's major LogLocator lays the powers; every other named tick is one of its minor locator's, labelled, by the
            // rule a log axis tells them apart by.
            double[] powers = [.. ticks.Where(tick => !LogLocator.NamesAsMinor(tick))];
            logMarks = new ColorBarLogMarks([.. powers.Select(Along)], MinorFractions(powers, lo, hi, request.TickSpace, theme, Along),
                [.. kept.Select(i => LogLocator.NamesAsMinor(ticks[i]))]);
        }

        return new(min, max, [.. kept.Select(i => ticks[i])], [.. kept.Select(i => labels[i])], [.. kept.Select(i => fractions[i])], bins, bandFractions,
            normalizer, logMarks);
    }

    /// <summary>What a bar over <paramref name="lo"/> .. <paramref name="hi"/> asks for its ticks (<see cref="TickRequest"/>, a
    /// <see cref="TickAxisKind.Ruler"/>): the locator it was given, as matplotlib's <c>Colorbar</c> sets its locator on the bar's long axis,
    /// told how many labels fit along it; else the theme's ladder laid along it; else the library's ruler sized to its length.</summary>
    /// <param name="lo">The low end of the range.</param>
    /// <param name="hi">The high end of the range.</param>
    /// <param name="lengthPx">The length of the bar.</param>
    /// <param name="marks">The edge past the bar its marks and labels stand on (<see cref="EdgeTerms.Past"/>): the labels are counted
    /// along it, along a horizontal bar, where a label takes about its width (<see cref="MaxNLocator.LabelSpanAlong"/>), else up a
    /// vertical one.</param>
    /// <param name="tickFont">The tick font.</param>
    /// <param name="theme">The theme the bar is drawn under.</param>
    /// <param name="locator">The locator the bar was given, or <see langword="null"/>.</param>
    private static TickRequest RequestOver(double lo, double hi, double lengthPx, EdgeTerms marks, Styling.Font tickFont, Theme theme,
        ITickLocator? locator) =>
        new(new Range1D(lo, hi), lengthPx, tickFont.Size, marks.AlongX, theme, TickAxisKind.Ruler) { Given = locator };

    /// <summary>The ticks of <paramref name="ticks"/> inside the range: a bar names the ticks of a locator it was given only there.</summary>
    private static double[] Inside(double[] ticks, double lo, double hi) => [.. ticks.Where(tick => tick >= lo && tick <= hi)];

    /// <summary>The labels of the ticks of a given locator: in scientific notation on a bar of matplotlib's log norm, the values' own
    /// otherwise.</summary>
    private static string[] GivenLabels(double[] ticks, bool logNorm, TickLabelLook labelLook) => logNorm ? LogLabels(ticks) : labelLook.Labelled(ticks);

    /// <summary>Where the minor marks of a bar of matplotlib's log norm stand: its minor <c>LogLocator</c>'s ticks in view, less those
    /// on a major tick (<see cref="LogLocator.MinorsOver"/>), for as many labels as fit along the bar (matplotlib's
    /// <c>get_tick_space</c>: its length over two label sizes along a vertical bar, three along a horizontal one).</summary>
    private static double[] MinorFractions(double[] majors, double lo, double hi, double tickSpace, Theme theme, Func<double, double> along) =>
        [.. LogLocator.MinorsOver(new Range1D(lo, hi), majors, tickSpace, theme).Select(along)];

    /// <summary>The labels of the ticks of a bar of matplotlib's log norm, in scientific notation (<c>10²</c>, <c>2×10²</c>), as
    /// <c>LogFormatterSciNotation</c> writes them.</summary>
    /// <param name="ticks">The ticks, above 0.</param>
    private static string[] LogLabels(double[] ticks)
    {
        var format = new LogTickFormatter();
        var labels = new string[ticks.Length];
        for (int i = 0; i < ticks.Length; i++) labels[i] = format.SciNotation(ticks[i]);
        return labels;
    }

    /// <summary>Where <paramref name="value"/> stands along a bar of bands whose boundaries stand evenly spaced: as far along its band
    /// as it lies between the band's two boundaries (matplotlib's <c>Colorbar</c> with <c>spacing='uniform'</c>,
    /// <c>np.interp(value, boundaries, linspace(0, 1, n))</c>); a value outside the boundaries stands at the nearer end.</summary>
    /// <param name="boundaries">The boundaries, ascending, at least two.</param>
    /// <param name="value">The value.</param>
    private static double UniformFractionOf(double[] boundaries, double value)
    {
        int bins = boundaries.Length - 1;
        if (value <= boundaries[0]) return 0;
        for (int k = 1; k <= bins; k++)
            if (value <= boundaries[k])
                return (k - 1 + (value - boundaries[k - 1]) / (boundaries[k] - boundaries[k - 1])) / bins;
        return 1;
    }

    /// <summary>The boundaries a bar of bands names, as indices: every one, or, when there are more than <paramref name="most"/>, every
    /// <c>step</c>-th (matplotlib's <c>FixedLocator.tick_values</c> with <c>nbins</c>: <c>step = ceil(n / nbins)</c>, and of the
    /// <c>step</c> runs of boundaries that start at 0, 1, ... the one whose lowest value is nearest 0, the first of them on a tie).</summary>
    private static int[] NamedBoundaries(double[] boundaries, int most)
    {
        int step = Math.Max((int)Math.Ceiling((double)boundaries.Length / most), 1);
        int[] best = [.. Enumerable.Range(0, (boundaries.Length + step - 1) / step).Select(k => k * step)];
        for (int start = 1; start < step; start++)
        {
            int[] run = [.. Enumerable.Range(0, (boundaries.Length - start + step - 1) / step).Select(k => start + k * step)];
            if (run.Min(k => Math.Abs(boundaries[k])) < best.Min(k => Math.Abs(boundaries[k]))) best = run;
        }

        return best;
    }

    /// <summary>How many steps a gradient asked to be drawn in <paramref name="steps"/> is drawn in: as asked for a bar of continuous
    /// colour, a whole number of steps for each band for a bar of bands, so that a band's edge falls on a step's edge and the block
    /// is as tall as its share.</summary>
    /// <param name="steps">The steps asked for.</param>
    internal int StepsOf(int steps) => Bins == 0 ? steps : (steps + Bins - 1) / Bins * Bins;
    /// <summary>The ticks that stay, as indices into the ticks in the order they stand in: walking along the bar from its
    /// start, a tick stays when it stands a label or more past the one before it that stayed. A label takes the height of its
    /// font on a vertical bar, and its width on a horizontal one. A bar of continuous colour under a linear normaliser spaces its
    /// ticks further apart than that to begin with, so none is left out there.</summary>
    private static int[] KeptTicks(double[] fractions, string[] labels, double lengthPx, bool vertical, IRenderContext ctx, Styling.Font tickFont)
    {
        var kept = new List<int>();
        foreach (int i in Enumerable.Range(0, fractions.Length).OrderBy(i => fractions[i]))
        {
            if (kept.Count > 0)
            {
                double gap = (fractions[i] - fractions[kept[^1]]) * lengthPx;
                double needed = LabelRoomAlong(vertical, labels[i], tickFont, ctx);
                if (gap < needed) continue;
            }

            kept.Add(i);
        }

        kept.Sort();
        return [.. kept];
    }

    /// <summary>The room a tick label takes along its bar, as <see cref="KeptTicks"/> counts it: up a vertical bar a line of its font
    /// (a label stacked up a bar is not measured), along a horizontal bar its measured width.</summary>
    /// <param name="vertical">Whether the bar runs along Y.</param>
    /// <param name="label">The label.</param>
    /// <param name="font">Its font.</param>
    /// <param name="ctx">The surface that measures the label.</param>
    /// <returns>The room, in pixels.</returns>
    internal static double LabelRoomAlong(bool vertical, string label, Styling.Font font, IRenderContext ctx)
    {
        if (vertical) return font.Size;
        return ctx.ExtentOf(TextLabel.Of(label), font).Width;
    }

    /// <summary>The widest label the ticks of this range can have over every number of ticks a bar of any length asks for,
    /// which the layout needs because it reserves the room before it knows how long the bar turns out to be. A tick is only
    /// ever left out, so what is drawn is never wider than this. A bar of bands names its boundaries, and they count too; a bar of
    /// matplotlib's log norm names powers of ten and their multiples at every length, and only those count.</summary>
    /// <param name="min">The value at the start of the gradient.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="font">The tick font.</param>
    /// <param name="normalizer">The normaliser the chart colours its values with.</param>
    /// <param name="theme">The theme the bar is drawn under, whose log labels a bar of matplotlib's log norm names and whose tick label
    /// look signs a negative label: <see cref="Theme.Default"/> for the library's own themes'.</param>
    /// <param name="locator">The locator the bar was given, or <see langword="null"/>: the widest of the labels <see cref="For"/> names
    /// its ticks with, over every count of labels a log locator can be told (2 to 9); any other locator's ticks do not depend on the
    /// length of the bar.</param>
    /// <returns>The widest label width in pixels; 0 when the range cannot be drawn.</returns>
    internal static double WidestLabel(double min, double max, IRenderContext ctx, Styling.Font font, INormalizer normalizer,
        Theme theme, ITickLocator? locator = null)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return 0;
        var labelLook = theme.Looks.TickLabels;

        double lo = Math.Min(min, max), hi = Math.Max(min, max);
        bool logNorm = normalizer.Rule.NamesDecadesOver(lo);
        double widest = 0;

        // A bar of a log norm names its ticks in scientific notation at every length, never the values' own labels.
        if (locator is null && logNorm)
        {
            foreach (var label in LogLabels(LogLocator.TicksNamedAtAnyLength(lo, hi, theme)))
                widest = Math.Max(widest, ctx.ExtentOf(TextLabel.Of(label), font).Width);
            return widest;
        }

        // Every list of ticks the bar's request names at some length: a given locator's for every count of labels it can be told, the
        // theme's ladder with every count of bins, the library's ruler for two to nine ticks. The length and the side the labels stand on
        // play no part in them: the request is the one of the bar beside the plot, whose labels stand past its right side.
        var request = RequestOver(lo, hi, 0, EdgeTerms.Right, font, theme, locator);
        foreach (double[] ticks in request.MajorsAtAnyLength())
            foreach (var label in locator is null ? labelLook.Labelled(ticks) : GivenLabels(Inside(ticks, lo, hi), logNorm, labelLook))
                widest = Math.Max(widest, ctx.ExtentOf(TextLabel.Of(label), font).Width);
        if (locator is not null) return widest;

        if (normalizer is IDiscreteNormalizer bands)
            foreach (var label in labelLook.Labelled(bands.Boundaries))
                widest = Math.Max(widest, ctx.ExtentOf(TextLabel.Of(label), font).Width);

        return widest;
    }

    /// <summary>The fraction of the colour map one step of the gradient is drawn in. A bar of continuous colour draws a step in
    /// the colour at the edge it is sampled at, as it always did; a bar of bands draws it in the colour of the band its middle
    /// falls in, the colour the normaliser gives the values of that band, so a band is one colour from edge to edge.</summary>
    /// <param name="edge">The fraction along the bar at the edge of the step the gradient samples.</param>
    /// <param name="middle">The fraction along the bar at the middle of the step.</param>
    internal double GradientFraction(double edge, double middle)
    {
        if (Bins == 0) return edge;

        int band = (int)Math.Min(Math.Floor(middle * Bins), Bins - 1);
        return BandFractions is null ? (double)band / Bins : BandFractions[band];
    }

    /// <summary>Where the tick marks of a bar of matplotlib's log norm stand, as fractions along the gradient: matplotlib's colour-bar axis
    /// takes the major and minor locator of its log scale, so its marks are not its labels.</summary>
    /// <param name="Majors">The major marks: the powers of ten among the named ticks, or the ticks of the bar's own locator.</param>
    /// <param name="Minors">The minor marks.</param>
    /// <param name="MinorLabels">For each named tick that is kept, whether it is a minor tick (a multiple of a power, labelled).</param>
    internal readonly record struct ColorBarLogMarks(double[] Majors, double[] Minors, bool[] MinorLabels);
}
