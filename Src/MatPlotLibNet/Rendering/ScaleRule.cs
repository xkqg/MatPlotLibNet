// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What an axis scale does, said once per scale: how a value maps into the space the axis is drawn in and back, which
/// values it has a place for, the locator, minor locator and formatter it brings, the range it can draw and how it rounds a range out
/// to its ticks. One sealed instance stands for each <see cref="AxisScale"/> (<see cref="Linear"/>, <see cref="Log"/>,
/// <see cref="SymLog"/>, <see cref="Logit"/>, <see cref="Date"/>); <c>scale.Rule</c> (<see cref="AxisScaleExtensions"/>) picks it, and
/// a reader that maps many values holds the rule it picked (a transform resolves it once, a range scan once per scan). A difference
/// between two scales is a member of their rules, never a test of which scale an axis is on.</summary>
internal abstract class ScaleRule
{
    /// <summary>The linear scale: every value maps to itself.</summary>
    internal static readonly ScaleRule Linear = new LinearRule();

    /// <summary>The log scale (matplotlib's <c>LogScale</c>, base 10).</summary>
    internal static readonly ScaleRule Log = new LogRule();

    /// <summary>The symmetric-log scale (matplotlib's <c>SymmetricalLogScale</c>, base 10, <see cref="SymlogTransform"/>) at the default
    /// threshold (<see cref="SymlogTransform.DefaultLinThresh"/>); another threshold is <see cref="SymLogAt"/>.</summary>
    internal static readonly ScaleRule SymLog = new SymLogRule(SymlogTransform.DefaultLinThresh);

    /// <summary>The symmetric-log rule that is linear within <paramref name="linthresh"/> of zero: <see cref="SymLog"/> at the default
    /// threshold, a rule of its own at any other, so a reader holds the rule and never a rule and a threshold beside it. Measured (process
    /// CPU, 1e6 points): a direction that holds the bound rule places a log value 21 % and a symlog value 8 % cheaper than one that held the
    /// scale and the threshold and picked the rule for every point, in the same 64 bytes.</summary>
    /// <param name="linthresh">The threshold (<see cref="Axis.SymLogLinThresh"/>).</param>
    /// <returns>The rule.</returns>
    internal static ScaleRule SymLogAt(double linthresh) =>
        linthresh == SymlogTransform.DefaultLinThresh ? SymLog : new SymLogRule(linthresh);

    /// <summary>The logit scale (matplotlib's <c>LogitScale</c>, <c>log10(p / (1 - p))</c>).</summary>
    internal static readonly ScaleRule Logit = new LogitRule();

    /// <summary>The date scale: OLE Automation dates, drawn as the numbers they are.</summary>
    internal static readonly ScaleRule Date = new DateRule();

    private ScaleRule(AxisScale scale) => Scale = scale;

    /// <summary>The scale this is the rule of.</summary>
    internal AxisScale Scale { get; }

    /// <summary>Maps <paramref name="value"/> from data units into the space the scale draws in.</summary>
    /// <param name="value">A data value.</param>
    /// <returns>The scaled value; NaN for a value the scale has no place for (<see cref="HasPlaceFor"/>).</returns>
    internal abstract double Forward(double value);

    /// <summary>Maps every value of <paramref name="values"/> as <see cref="Forward"/> maps it, to the bit, into <paramref name="into"/>:
    /// the rule is asked once for the whole array, and the loop runs in the scale's own code.</summary>
    /// <param name="values">The data values.</param>
    /// <param name="into">Where the scaled values go; as long as <paramref name="values"/>, and it may be the same memory.</param>
    internal abstract void ForwardAll(ReadOnlySpan<double> values, Span<double> into);

    /// <summary>Maps <paramref name="value"/> from the space the scale draws in back to data units: the inverse of
    /// <see cref="Forward"/>.</summary>
    /// <param name="value">A value in the drawn space.</param>
    /// <returns>The data value.</returns>
    internal abstract double Inverse(double value);

    /// <summary>Maps the range <paramref name="min"/>..<paramref name="max"/> into the space the scale draws in, end by end
    /// (<see cref="Forward"/>): the range a Cartesian transform spreads over the plot, so ticks at 100, 1000 and 10000 stand evenly
    /// on a log axis.</summary>
    /// <param name="min">The low end, in data units.</param>
    /// <param name="max">The high end, in data units.</param>
    /// <returns>Both ends mapped; an end the scale has no place for maps to NaN.</returns>
    internal Numerics.MinMaxRange ForwardRange(double min, double max) => new(Forward(min), Forward(max));

    /// <summary>Whether the scale has a place for the finite <paramref name="value"/>: every value but 0 and the negative values on a
    /// log axis, and only the values strictly between 0 and 1 on a logit axis (matplotlib's <c>LogitScale.val_in_range</c>). The one
    /// rule the mapping into the drawn space (<see cref="Forward"/>) and the clipping of a mark's base share; a range is folded from
    /// the values at or above <see cref="LowestPlaced"/> (<c>Range1D.Including</c>, <see cref="MissingValueExtensions"/>), as matplotlib's data limits
    /// are. A missing value or an infinity has no place on any axis (<see cref="MissingValueExtensions.IsDrawable(double)"/>); this
    /// says nothing about them.</summary>
    /// <param name="value">A finite data value.</param>
    /// <returns><see langword="false"/> for 0 and below on a log axis and for 0 and below or 1 and above on a logit axis;
    /// <see langword="true"/> otherwise.</returns>
    internal virtual bool HasPlaceFor(double value) => true;

    /// <summary>The lowest value the scale has a place for (<see cref="HasPlaceFor"/>): the smallest positive number on a log and a
    /// logit axis, minus infinity on every other. A value below it is left out of a range, an axis' or a scale of colours', as
    /// matplotlib's data limits keep only the values from the smallest positive one up on such a scale.</summary>
    internal virtual double LowestPlaced => NoFloor;

    /// <summary>The lowest placed value of a scale that places every finite value (<see cref="LowestPlaced"/> of a linear, a
    /// symmetric-log and a date axis): a range folded with no rule named folds from it without asking one.</summary>
    internal const double NoFloor = double.NegativeInfinity;

    /// <summary>Whether the scale draws in a space other than the data's own (log, symmetric log, logit), so a value goes through
    /// <see cref="Forward"/> before it is spread over the plot; a linear and a date axis spread the value itself.</summary>
    internal virtual bool ShapesItsSpace => false;

    /// <summary>The name the server-interaction view writes for a scale whose drawn space is not the data's own (<c>log</c>,
    /// <c>symlog</c>, <c>logit</c>); <see langword="null"/> for a scale drawn linearly, which writes nothing.</summary>
    internal virtual string? ViewName => null;

    /// <summary>The threshold of zero within which the scale is linear, which the readers of its ticks and its view are handed: the one a
    /// symmetric-log rule holds, none on every other scale.</summary>
    internal virtual double? LinearThreshold => null;

    /// <summary>Where in the drawn space the scale puts a finite value it has no place for: a log axis clips it
    /// (<c>nonpositive='clip'</c>) a thousand decades below where it begins, so a mark standing on it is cut at the edge; every other
    /// scale masks it (NaN), a logit axis its 0 and its 1 (<c>nonpositive='mask'</c>).</summary>
    internal virtual double ClippedPlace => double.NaN;

    /// <summary>Whether the scale's minor ticks stand with no call under a theme whose minor ticks are matplotlib's minor locator's (a
    /// log and a logit axis, whose minor locator places them); a linear, symmetric-log or date axis places none until
    /// <c>minorticks_on()</c>.</summary>
    internal virtual bool PlacesMinorsByItself => false;

    /// <summary>Whether the default labels of the scale take an offset and a power of ten off the tick values (matplotlib's
    /// <c>ScalarFormatter</c>): a linear axis only.</summary>
    internal virtual bool OffsetsItsLabels => false;

    /// <summary>Whether the scale's values are dates (OLE Automation dates), which a reader writes as dates.</summary>
    internal virtual bool HoldsDates => false;

    /// <summary>Whether the scale brings a locator (<see cref="Locator"/> makes one), asked without making it: a log, symmetric-log or
    /// logit axis, and a date axis along X.</summary>
    /// <param name="isX">Whether the axis is an X axis: only an X axis brings date ticks with its scale.</param>
    /// <returns><see langword="true"/> when <see cref="Locator"/> returns a locator.</returns>
    internal virtual bool BringsLocator(bool isX) => false;

    /// <summary>The tick locator the scale brings when the axis has none: <see cref="AutoDateLocator"/> along a date X axis,
    /// <see cref="SymlogLocator"/> for symlog, <see cref="LogLocator"/> for log, <see cref="LogitLocator"/> for logit;
    /// <see langword="null"/> otherwise. It is read where it is needed (the ticks, the nice bounds of the range, the verbs that put an
    /// axis on a scale) and never written onto an axis by a draw.</summary>
    /// <param name="isX">Whether the axis is an X axis.</param>
    /// <returns>The scale's locator, or <see langword="null"/>; the symlog locator lays its ticks by the threshold the rule holds.</returns>
    internal virtual ITickLocator? Locator(bool isX) => null;

    /// <summary>The locator that places the minor ticks of an axis drawn on this scale, as matplotlib's scale sets its minor locator
    /// whatever locator places the major ticks: the log scale's (2 to 9 times each power of ten), the logit scale's and the
    /// symmetric-log scale's (none); none for a scale whose minor ticks split the step between two major ticks (linear, date). Each
    /// holds nothing, so every axis shares it.</summary>
    internal virtual IMinorTickLocator? MinorLocator => null;

    /// <summary>The formatter an axis drawn on this scale writes its labels with when it has none of its own, whatever locator places
    /// its ticks, as matplotlib's scale sets the major formatter (<c>set_default_locators_and_formatters</c>) and
    /// <c>set_major_locator</c> leaves it: <see cref="LogTickFormatter"/> on a log and a symmetric-log scale (matplotlib's
    /// <c>LogFormatterSciNotation</c>), <see cref="LogitFormatter"/> on a logit scale; on a date X scale the
    /// <see cref="AutoDateFormatter"/> over whatever locator places the ticks (<see cref="AutoDateFormatter.Over"/>);
    /// <see langword="null"/> otherwise.</summary>
    /// <param name="locator">The locator that places the axis' ticks.</param>
    /// <param name="isX">Whether the axis is an X axis: only an X axis is put on dates.</param>
    /// <returns>The formatter, or <see langword="null"/>.</returns>
    internal virtual ITickFormatter? Formatter(ITickLocator locator, bool isX) => null;

    /// <summary>The scale the ticks of a linked axis (<see cref="Axis.Link"/>) are drawn on over a primary on this scale: matplotlib
    /// draws a linked axis over a log primary on its <c>functionlog</c> scale (decades), and over every other primary (symlog, logit and
    /// dates included) on its <c>function</c> scale, linearly.</summary>
    internal virtual ScaleRule UnderALink => Linear;

    /// <summary>The range the scale can draw (matplotlib's <c>limit_range_for_scale</c>): a log axis lifts a floor at or below 0
    /// (<see cref="Lifted"/>), a logit axis takes its range inside 0 and 1, every other scale draws any range.</summary>
    /// <param name="range">The range, in data units.</param>
    /// <param name="minPositive">The smallest value above 0 the axis' data holds (matplotlib's <c>minpos</c>), which a logit axis puts an
    /// end at or past 0 or 1 at; <see langword="null"/> when it holds none (<see cref="LogitLocator.StandardMinPos"/> is taken).</param>
    /// <returns>The range the scale can draw; a reversed range keeps its orientation.</returns>
    internal virtual Range1D Limited(Range1D range, double? minPositive) => range;

    /// <summary>What the locator the scale brings widens a range of no width, or no data at all, to before an axis on the scale is
    /// padded (matplotlib 3.11.2's <c>Locator.nonsingular</c>, which <c>autoscale_view</c>, <c>set_xlim</c> and <c>set_ylim</c> ask):
    /// on a linear and a symmetric-log axis (<c>ticker.py</c> 1796, <c>_nonsingular(v0, v1, expander=.05)</c>) a range of one value
    /// widened by 5 % of it each way, at 0 to (-0.05, 0.05), and no data at all to (-0.05, 0.05); a log, a logit and a date X axis say
    /// their own. A range with a width is kept as it is. Read under the matplotlib themes only; the library's own themes open a range by
    /// <see cref="Range1D.Normalized"/>.</summary>
    /// <param name="limits">The data's limits, low to high: <c>(-inf, +inf)</c> when there are none (<see cref="Range1D.DataLimits"/>).</param>
    /// <param name="isX">Whether the axis is an X axis: only an X axis brings date ticks with its scale.</param>
    /// <param name="minPositive">The smallest value above 0 the axis' data reach (matplotlib's <c>minpos</c>), which a logit axis puts an
    /// end at or past 0 or 1 at; <see langword="null"/> when none does.</param>
    /// <returns>The range opened, low to high.</returns>
    internal virtual Range1D Nonsingular(Range1D limits, bool isX, double? minPositive) => limits.NonsingularBy(LinearExpander, NonsingularTiny);

    /// <summary>The share of a value a linear axis widens a range of that one value by (<c>Locator.nonsingular</c>'s <c>expander</c>).</summary>
    private const double LinearExpander = 0.05;

    /// <summary>The width, as a share of the larger end, up to which a range counts as one value (<c>_nonsingular</c>'s <c>tiny</c>).</summary>
    private const double NonsingularTiny = 1e-15;

    /// <summary>The view an axis on this scale holds while nothing asks for it to be scaled (no series on it, no line across it): the
    /// first view of a matplotlib axes, (0, 1) (<c>Bbox.unit()</c>), on a linear and a symmetric-log axis; on a date X axis the first day
    /// of matplotlib's dates, 1970-01-01, to the next (<c>dates.py</c> 1331); <see langword="null"/> on a log and a logit axis, whose
    /// default limits differ from the linear one's, so <c>_set_axes_scale</c> scales the axis from them at once (<c>axis.py</c> 818-831)
    /// and it is scaled as an empty axis that is asked. Read under the matplotlib themes only.</summary>
    /// <param name="isX">Whether the axis is an X axis.</param>
    /// <returns>The first view, or <see langword="null"/> when the axis is scaled.</returns>
    internal virtual Range1D? FirstView(bool isX) => UnitView;

    /// <summary>The first view of a matplotlib axes: 0 to 1.</summary>
    private static readonly Range1D UnitView = new(0, 1);

    /// <summary>The range with a floor the log scale has no place for lifted to three decades under its ceiling (a ceiling at or below
    /// 0 taken to 1): a visible, finite axis rather than a NaN transform that blanks every point. Only a log axis lifts; every other
    /// scale keeps the range. A reversed range is lifted as its interval and keeps its orientation.</summary>
    /// <param name="range">The range, in data units.</param>
    /// <returns>The lifted range.</returns>
    internal virtual Range1D Lifted(Range1D range) => range;

    /// <summary>The range rounded out to the ticks the scale lays (matplotlib's <c>round_numbers</c>, the scale's major locator's
    /// <c>view_limits</c>): a linear axis to the multiples of its spacing when it has one, else to the ends of the ticks
    /// <paramref name="ladder"/> lays over the range; a log axis to whole decades, a symmetric-log axis to the decades on either side of
    /// zero; a logit and a date axis keep the range, as their locators round nothing.</summary>
    /// <param name="range">The range, low to high.</param>
    /// <param name="spacing">The axis' tick spacing, or <see langword="null"/>.</param>
    /// <param name="ladder">The locator the theme lays a linear axis' ticks by.</param>
    /// <returns>The rounded range.</returns>
    internal virtual Range1D RoundedOut(Range1D range, double? spacing, TickLadder ladder) => range;

    /// <summary>The run of levels a contour coloured over this scale lays over <paramref name="lo"/> .. <paramref name="hi"/> when it names
    /// none, before the ones far outside the values are dropped (<see cref="Numerics.ContourLevels"/>): matplotlib 3.11.2's
    /// <c>ContourSet._ensure_locator_exists</c> takes <c>MaxNLocator(N + 1, min_n_ticks=1)</c> (with <c>_nonsingular</c>; the arm for
    /// ranges below 1e-288 is unreachable, as a contour asks only over a range at least 1e-12 wide, and left out), and a log scale
    /// its decades (<see cref="LogRule"/>).</summary>
    /// <param name="lo">The lowest value (above 0 on a log scale).</param>
    /// <param name="hi">The highest value; above <paramref name="lo"/>.</param>
    /// <param name="count">How many levels are asked for (matplotlib's <c>N</c>), 1 or more.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks, which the locator reads too.</param>
    /// <returns>The levels, ascending.</returns>
    internal virtual double[] LevelsOver(double lo, double hi, int count, bool roundNumbers)
    {
        var range = new Range1D(lo, hi).NonsingularBy(expander: 1e-13, tiny: 1e-14);
        var run = SteppedTicks.Over(range.Lo, range.Hi, TickLadder.ForLevels(count), roundNumbers);
        var levels = new double[run.Count];
        for (int i = 0; i < levels.Length; i++) levels[i] = run[i];
        return levels;
    }


    /// <summary>A scale that draws a value as the number it is (linear, date): every value maps to itself, a whole array is copied.</summary>
    private abstract class IdentityRule(AxisScale scale) : ScaleRule(scale)
    {
        internal sealed override double Forward(double value) => value;

        internal sealed override void ForwardAll(ReadOnlySpan<double> values, Span<double> into) => values.CopyTo(into);

        internal sealed override double Inverse(double value) => value;
    }

    /// <summary>The linear scale.</summary>
    private sealed class LinearRule() : IdentityRule(AxisScale.Linear)
    {
        internal override bool OffsetsItsLabels => true;

        internal override Range1D RoundedOut(Range1D range, double? spacing, TickLadder ladder) =>
            spacing is { } step && step > 0 ? MultipleLocator.MultiplesHolding(range, step, 0) : ladder.RoundedOut(range);
    }

    /// <summary>The log scale.</summary>
    private sealed class LogRule() : ScaleRule(AxisScale.Log)
    {
        /// <inheritdoc />
        /// <remarks>matplotlib's <c>LogLocator(numticks=N)</c> for a log-scaled contour: its decades (<see cref="LogLocator.DecadesLaid"/>,
        /// the rule a log axis lays its powers by; under the classic style its older stride).</remarks>
        internal override double[] LevelsOver(double lo, double hi, int count, bool roundNumbers) =>
            LogLocator.DecadesLaid(lo, hi, count, roundNumbers).Powers();

        /// <summary>The minor locator of a log scale (matplotlib's <c>LogLocator(subs='auto')</c>).</summary>
        private static readonly IMinorTickLocator Minors = new LogLocator();

        internal override double Forward(double value) => value > 0 ? Math.Log10(value) : double.NaN;

        internal override void ForwardAll(ReadOnlySpan<double> values, Span<double> into)
        {
            for (int i = 0; i < values.Length; i++)
                into[i] = Forward(values[i]);
        }

        internal override double Inverse(double value) => Math.Pow(10, value);

        internal override bool HasPlaceFor(double value) => value > 0;

        internal override double LowestPlaced => double.Epsilon;

        internal override bool ShapesItsSpace => true;

        internal override string ViewName => "log";

        /// <summary>matplotlib's <c>LogTransform</c> with <c>nonpositive='clip'</c> writes <c>out[values &lt;= 0] = -1000</c>, in
        /// decades, far enough below the axis to be cut off by any clip; no placed value comes near it (the smallest positive double
        /// is 324 decades down).</summary>
        internal override double ClippedPlace => -1000;

        internal override bool PlacesMinorsByItself => true;

        internal override bool BringsLocator(bool isX) => true;

        internal override ITickLocator Locator(bool isX) => new LogLocator();

        internal override IMinorTickLocator MinorLocator => Minors;

        internal override ITickFormatter Formatter(ITickLocator locator, bool isX) => new LogTickFormatter();

        internal override ScaleRule UnderALink => this;

        internal override Range1D Limited(Range1D range, double? minPositive) => Lifted(range);

        /// <inheritdoc />
        /// <remarks><c>LogLocator.nonsingular</c> (<c>ticker.py</c> 2613-2633): no data, or data wholly at or below 0, opens to (1, 10);
        /// one value to the decades strictly around it (<c>_decade_less</c>, <c>_decade_greater</c>). An end at or below 0 is reached
        /// only by a limit set by hand (a value the axis cannot place is left out of its data), which the scale lifts (<see cref="Lifted"/>).</remarks>
        internal override Range1D Nonsingular(Range1D limits, bool isX, double? minPositive)
        {
            var (lo, hi) = limits;
            if (!double.IsFinite(lo) || !double.IsFinite(hi) || hi <= 0) return DefaultDecades;
            return lo == hi ? new(lo.DecadeBelow(), hi.DecadeAbove()) : limits;
        }

        /// <summary>The range a log axis with no data opens to: one decade, from 1 to 10.</summary>
        private static readonly Range1D DefaultDecades = new(1, 10);

        /// <inheritdoc />
        internal override Range1D? FirstView(bool isX) => null;

        internal override Range1D Lifted(Range1D range)
        {
            var interval = range.Interval;
            double hi = interval.Hi > 0 ? interval.Hi : 1;
            double lo = interval.Lo > 0 ? interval.Lo : hi / 1000;
            return new Range1D(Math.Min(lo, hi), hi).OrientedLike(range);
        }

        internal override Range1D RoundedOut(Range1D range, double? spacing, TickLadder ladder) => LogLocator.DecadesHolding(range);
    }

    /// <summary>The symmetric-log scale, linear within the threshold it holds.</summary>
    /// <param name="threshold">The threshold of zero within which the scale is linear.</param>
    private sealed class SymLogRule(double threshold) : ScaleRule(AxisScale.SymLog)
    {
        /// <summary>The minor locator of a symmetric-log scale (<see cref="SymlogLocator"/>, which places none).</summary>
        private static readonly IMinorTickLocator Minors = new SymlogLocator();

        internal override double Forward(double value) => SymlogTransform.Forward(value, threshold);

        internal override void ForwardAll(ReadOnlySpan<double> values, Span<double> into)
        {
            for (int i = 0; i < values.Length; i++)
                into[i] = SymlogTransform.Forward(values[i], threshold);
        }

        internal override double Inverse(double value) => SymlogTransform.Inverse(value, threshold);

        internal override bool ShapesItsSpace => true;

        internal override string ViewName => "symlog";

        internal override double? LinearThreshold => threshold;

        internal override bool BringsLocator(bool isX) => true;

        internal override ITickLocator Locator(bool isX) => new SymlogLocator(threshold);

        internal override IMinorTickLocator MinorLocator => Minors;

        internal override ITickFormatter Formatter(ITickLocator locator, bool isX) => new LogTickFormatter();

        internal override Range1D RoundedOut(Range1D range, double? spacing, TickLadder ladder) => SymlogLocator.DecadesHolding(range);

    }

    /// <summary>The logit scale.</summary>
    private sealed class LogitRule() : ScaleRule(AxisScale.Logit)
    {
        /// <summary>The minor locator of a logit scale (matplotlib's <c>LogitLocator(minor=True)</c>).</summary>
        private static readonly IMinorTickLocator Minors = new LogitLocator();

        internal override double Forward(double value) =>
            value > 0 && value < 1 ? Math.Log10(value / (1 - value)) : double.NaN;

        internal override void ForwardAll(ReadOnlySpan<double> values, Span<double> into)
        {
            for (int i = 0; i < values.Length; i++)
                into[i] = Forward(values[i]);
        }

        internal override double Inverse(double value) => 1.0 / (1 + Math.Pow(10, -value));

        internal override bool HasPlaceFor(double value) => value > 0 && value < 1;

        internal override double LowestPlaced => double.Epsilon;

        internal override bool ShapesItsSpace => true;

        internal override string ViewName => "logit";

        internal override bool PlacesMinorsByItself => true;

        internal override bool BringsLocator(bool isX) => true;

        internal override ITickLocator Locator(bool isX) => new LogitLocator();

        internal override IMinorTickLocator MinorLocator => Minors;

        internal override ITickFormatter Formatter(ITickLocator locator, bool isX) => new LogitFormatter();

        /// <summary>The range a logit axis can draw, as matplotlib's <c>LogitScale.limit_range_for_scale</c> and
        /// <c>LogitLocator.nonsingular</c> make it: an end at or below 0 taken to the smallest positive value, an end at or above 1 to
        /// one less it, and a range left with no width opened to <c>0.1 * lo</c> .. <c>1 - 0.1 * lo</c>.</summary>
        internal override Range1D Limited(Range1D range, double? minPositive)
        {
            double floor = minPositive ?? LogitLocator.StandardMinPos;
            var (lo, hi) = range.Interval;
            if (lo <= 0) lo = floor;
            if (hi >= 1) hi = 1 - floor;
            return (lo < hi ? new Range1D(lo, hi) : LogitLocator.OpenedAround(lo)).OrientedLike(range);
        }

        /// <inheritdoc />
        /// <remarks><c>LogitLocator.nonsingular</c> (<see cref="LogitLocator.NonsingularOver"/>) with the axis' smallest positive value.</remarks>
        internal override Range1D Nonsingular(Range1D limits, bool isX, double? minPositive) =>
            LogitLocator.NonsingularOver(limits, minPositive ?? LogitLocator.StandardMinPos);

        /// <inheritdoc />
        internal override Range1D? FirstView(bool isX) => null;
    }

    /// <summary>The date scale.</summary>
    private sealed class DateRule() : IdentityRule(AxisScale.Date)
    {
        internal override bool HoldsDates => true;

        /// <summary>Only an X axis is put on dates (the one place that says so; <see cref="Locator"/> and <see cref="Formatter"/> ask it).</summary>
        internal override bool BringsLocator(bool isX) => isX;

        internal override ITickLocator? Locator(bool isX) => BringsLocator(isX) ? new AutoDateLocator() : null;

        internal override ITickFormatter? Formatter(ITickLocator locator, bool isX) => BringsLocator(isX) ? AutoDateFormatter.Over(locator) : null;

        /// <inheritdoc />
        /// <remarks>Along a date X axis <c>AutoDateLocator.nonsingular</c> (<c>dates.py</c> 1328-1340): no data opens to matplotlib's first
        /// day and the next (<see cref="FirstDay"/>), one value to two years of 365 days each way (<c>DAYS_PER_YEAR * 2</c>); along Y,
        /// which brings no date ticks, the linear rule.</remarks>
        internal override Range1D Nonsingular(Range1D limits, bool isX, double? minPositive)
        {
            if (!BringsLocator(isX)) return base.Nonsingular(limits, isX, minPositive);
            var (lo, hi) = limits;
            if (!double.IsFinite(lo) || !double.IsFinite(hi)) return FirstDay;
            return lo == hi ? new(lo - TwoYears, hi + TwoYears) : limits;
        }

        /// <inheritdoc />
        internal override Range1D? FirstView(bool isX) => BringsLocator(isX) ? FirstDay : base.FirstView(isX);

        /// <summary>matplotlib's first day, 1970-01-01, to the next, as OLE Automation dates (<c>new DateTime(1970, 1, 1).ToOADate()</c> is
        /// 25569): the range a date axis opens to before it holds a date.</summary>
        private static readonly Range1D FirstDay = new(25569, 25570);

        /// <summary>Two of matplotlib's years of 365 days (<c>dates.DAYS_PER_YEAR</c>), the room a date axis opens a single date by.</summary>
        private const double TwoYears = 2 * 365.0;
    }
}
