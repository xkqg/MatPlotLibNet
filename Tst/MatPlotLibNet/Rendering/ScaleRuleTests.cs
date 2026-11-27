// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What an axis scale does, said once by its rule (<see cref="ScaleRule"/>, picked by <c>scale.Rule</c>): which finite values
/// it has a place for (matplotlib's <c>LogScale</c> and <c>LogitScale.val_in_range</c>), how a value maps into the space it is drawn in
/// and back, the locator, minor locator and formatter it brings, the range it can draw (matplotlib's <c>limit_range_for_scale</c>) and
/// how it rounds a range out to its ticks.</summary>
public class ScaleRuleTests
{
    public static TheoryData<AxisScale> EveryScale { get; } = new(Enum.GetValues<AxisScale>());

    [Theory]
    [MemberData(nameof(EveryScale))]
    public void EveryScale_HasOneRule_ThatNamesIt(AxisScale scale)
    {
        var rule = scale.Rule;

        Assert.Equal(scale, rule.Scale);
        Assert.Same(rule, scale.Rule);
        Assert.True(rule.GetType().IsSealed);
    }

    [Fact]
    public void TheRules_AreOnePerScale() =>
        Assert.Equal(Enum.GetValues<AxisScale>().Length, Enum.GetValues<AxisScale>().Select(s => s.Rule).Distinct().Count());

    [Fact]
    public void AScaleTheLibraryDoesNotKnow_IsReadAsLinear() => Assert.Same(ScaleRule.Linear, ((AxisScale)99).Rule);

    [Theory]
    [InlineData(AxisScale.Log, 1e-300, true)]
    [InlineData(AxisScale.Log, 10.0, true)]
    [InlineData(AxisScale.Log, 0.0, false)]
    [InlineData(AxisScale.Log, -3.0, false)]
    [InlineData(AxisScale.SymLog, -3.0, true)]
    [InlineData(AxisScale.SymLog, 0.0, true)]
    [InlineData(AxisScale.Linear, -3.0, true)]
    [InlineData(AxisScale.Logit, 0.0, false)]
    [InlineData(AxisScale.Logit, 1.0, false)]
    [InlineData(AxisScale.Logit, 1e-300, true)]
    [InlineData(AxisScale.Logit, 0.999, true)]
    [InlineData(AxisScale.Date, -3.0, true)]
    public void AScale_HasAPlaceForEveryFiniteValue_ButZeroAndBelowOnALogAxis(AxisScale scale, double value, bool expected) =>
        Assert.Equal(expected, scale.Rule.HasPlaceFor(value));

    [Fact]
    public void ALogScale_HasNoPlaceForNegativeZero() => Assert.False(ScaleRule.Log.HasPlaceFor(-0.0));

    /// <summary>The mapping says the same: a value the scale has no place for maps to NaN, and only such a value does.</summary>
    [Theory]
    [InlineData(AxisScale.Log, 0.0)]
    [InlineData(AxisScale.Log, -2.0)]
    [InlineData(AxisScale.Log, 5.0)]
    [InlineData(AxisScale.SymLog, -2.0)]
    [InlineData(AxisScale.Linear, -2.0)]
    [InlineData(AxisScale.Logit, 0.0)]
    [InlineData(AxisScale.Logit, 1.5)]
    [InlineData(AxisScale.Logit, 0.3)]
    public void TheForwardMapping_IsNaN_ExactlyWhereTheScaleHasNoPlace(AxisScale scale, double value) =>
        Assert.Equal(!scale.Rule.HasPlaceFor(value), double.IsNaN(scale.Rule.Forward(value)));

    /// <summary>A whole array maps through the one rule a single value takes, to the bit, on every scale.</summary>
    [Theory]
    [MemberData(nameof(EveryScale))]
    public void AnArray_MapsAsEachOfItsValuesDoes(AxisScale scale)
    {
        double[] values = [-5, -0.0, 0, 1e-300, 0.25, 0.5, 0.999, 1, 2, 300, double.NaN, double.PositiveInfinity, double.NegativeInfinity];
        var mapped = new double[values.Length];

        scale.RuleAt(2).ForwardAll(values, mapped);

        for (int i = 0; i < values.Length; i++)
            Assert.Equal(BitConverter.DoubleToInt64Bits(scale.RuleAt(2).Forward(values[i])), BitConverter.DoubleToInt64Bits(mapped[i]));
    }

    /// <summary>A range maps into the drawn space end by end, through the one mapping a value takes.</summary>
    [Theory]
    [InlineData(AxisScale.Linear, 2.0, 5.0, 1.0, 2.0, 5.0)]
    [InlineData(AxisScale.Log, 10.0, 1000.0, 1.0, 1.0, 3.0)]
    [InlineData(AxisScale.Date, -3.0, 4.0, 1.0, -3.0, 4.0)]
    [InlineData(AxisScale.Logit, 0.1, 0.5, 1.0, -0.9542425094393249, 0.0)]
    public void ARange_MapsIntoTheDrawnSpaceEndByEnd(AxisScale scale, double min, double max, double linthresh, double lo, double hi)
    {
        var mapped = scale.RuleAt(linthresh).ForwardRange(min, max);

        Assert.Equal(lo, mapped.Min, 12);
        Assert.Equal(hi, mapped.Max, 12);
    }

    [Fact]
    public void ASymlogRange_MapsEachEndByTheSymlogTransform()
    {
        var mapped = ScaleRule.SymLogAt(2).ForwardRange(-50, 200);

        Assert.Equal(SymlogTransform.Forward(-50, 2), mapped.Min);
        Assert.Equal(SymlogTransform.Forward(200, 2), mapped.Max);
        Assert.Equal(200, ScaleRule.SymLogAt(2).Inverse(SymlogTransform.Forward(200, 2)), 12);
    }

    [Fact]
    public void ALogRange_ReachingZero_MapsThatEndToNaN() =>
        Assert.True(double.IsNaN(ScaleRule.Log.ForwardRange(0, 100).Min));

    [Theory]
    [InlineData(AxisScale.Log, double.Epsilon)]
    [InlineData(AxisScale.Linear, double.NegativeInfinity)]
    [InlineData(AxisScale.SymLog, double.NegativeInfinity)]
    [InlineData(AxisScale.Logit, double.Epsilon)]
    [InlineData(AxisScale.Date, double.NegativeInfinity)]
    public void TheLowestPlacedValue_IsTheLowestTheScaleHasAPlaceFor(AxisScale scale, double expected)
    {
        Assert.Equal(expected, scale.Rule.LowestPlaced);
        Assert.True(scale.Rule.HasPlaceFor(scale.Rule.LowestPlaced) || double.IsNegativeInfinity(scale.Rule.LowestPlaced));
    }

    /// <summary>matplotlib's <c>LogisticTransform</c>, <c>1 / (1 + 10 ** -v)</c>, undoes the logit; <c>10 ** v</c> undoes the log; a
    /// linear and a date axis map a value to itself both ways.</summary>
    [Theory]
    [InlineData(AxisScale.Logit, 0.001)]
    [InlineData(AxisScale.Logit, 0.5)]
    [InlineData(AxisScale.Logit, 0.9)]
    [InlineData(AxisScale.Log, 0.02)]
    [InlineData(AxisScale.Log, 3000.0)]
    [InlineData(AxisScale.Linear, -7.5)]
    [InlineData(AxisScale.Date, 45000.25)]
    public void TheInverse_UndoesTheMapping(AxisScale scale, double value) =>
        Assert.Equal(value, scale.Rule.Inverse(scale.Rule.Forward(value)), Math.Abs(value) * 1e-12);

    /// <summary>The two values a scale's readers take rather than a flag to branch on: the linear threshold its ticks and its view are
    /// handed (symlog's own, none elsewhere), and where in the drawn space it puts a finite value it has no place for (matplotlib's log
    /// scale, <c>nonpositive='clip'</c>, writes <c>out[values &lt;= 0] = -1000</c>; every other scale masks it, NaN).</summary>
    [Theory]
    [InlineData(AxisScale.Linear, null, double.NaN)]
    [InlineData(AxisScale.Log, null, -1000.0)]
    [InlineData(AxisScale.SymLog, 2.5, double.NaN)]
    [InlineData(AxisScale.Logit, null, double.NaN)]
    [InlineData(AxisScale.Date, null, double.NaN)]
    public void EachScale_GivesItsLinearThresholdAndThePlaceOfWhatItClips_AsValues(AxisScale scale, double? threshold, double clipped)
    {
        Assert.Equal(threshold, scale.RuleAt(2.5).LinearThreshold);
        Assert.Equal(clipped, scale.Rule.ClippedPlace);
    }

    /// <summary>The facts of each scale: whether it shapes its space, the name the browser's view gives it, whether its minor ticks
    /// stand with no call (log, logit, as matplotlib's minor locator places them), whether its labels take an offset (linear only,
    /// matplotlib's <c>ScalarFormatter</c>), whether its values are dates.</summary>
    [Theory]
    [InlineData(AxisScale.Linear, false, null, false, true, false)]
    [InlineData(AxisScale.Log, true, "log", true, false, false)]
    [InlineData(AxisScale.SymLog, true, "symlog", false, false, false)]
    [InlineData(AxisScale.Logit, true, "logit", true, false, false)]
    [InlineData(AxisScale.Date, false, null, false, false, true)]
    public void EachScale_SaysItsFacts(AxisScale scale, bool shapes, string? viewName, bool minorsByItself, bool offsets, bool dates)
    {
        var rule = scale.Rule;

        Assert.Equal(shapes, rule.ShapesItsSpace);
        Assert.Equal(viewName, rule.ViewName);
        Assert.Equal(minorsByItself, rule.PlacesMinorsByItself);
        Assert.Equal(offsets, rule.OffsetsItsLabels);
        Assert.Equal(dates, rule.HoldsDates);
    }

    /// <summary>The locator a scale brings when the axis has none: a log, symmetric-log and logit axis on either side, a date axis only
    /// along X; <c>BringsLocator</c> says so without making one.</summary>
    [Theory]
    [InlineData(AxisScale.Linear, true, null)]
    [InlineData(AxisScale.Linear, false, null)]
    [InlineData(AxisScale.Log, false, typeof(LogLocator))]
    [InlineData(AxisScale.SymLog, true, typeof(SymlogLocator))]
    [InlineData(AxisScale.Logit, false, typeof(LogitLocator))]
    [InlineData(AxisScale.Date, true, typeof(AutoDateLocator))]
    [InlineData(AxisScale.Date, false, null)]
    public void AScale_BringsItsLocator(AxisScale scale, bool isX, Type? expected)
    {
        var locator = scale.RuleAt(2.5).Locator(isX);

        Assert.Equal(expected, locator?.GetType());
        Assert.Equal(expected is not null, scale.Rule.BringsLocator(isX));
    }

    [Fact]
    public void ASymlogLocator_TakesTheThresholdItIsGiven()
    {
        var locator = Assert.IsType<SymlogLocator>(ScaleRule.SymLogAt(2.5).Locator(isX: false));

        Assert.Equal(2.5, typeof(SymlogLocator).GetField("_linthresh",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(locator));
    }

    [Theory]
    [InlineData(AxisScale.Linear, null)]
    [InlineData(AxisScale.Date, null)]
    [InlineData(AxisScale.Log, typeof(LogLocator))]
    [InlineData(AxisScale.Logit, typeof(LogitLocator))]
    [InlineData(AxisScale.SymLog, typeof(SymlogLocator))]
    public void AScale_PlacesItsMinorTicksByItsOwnLocator_OrSplitsTheStep(AxisScale scale, Type? expected)
    {
        Assert.Equal(expected, scale.Rule.MinorLocator?.GetType());
        Assert.Same(scale.Rule.MinorLocator, scale.Rule.MinorLocator);
    }

    [Theory]
    [InlineData(AxisScale.Linear, true, null)]
    [InlineData(AxisScale.Log, true, typeof(LogTickFormatter))]
    [InlineData(AxisScale.SymLog, false, typeof(LogTickFormatter))]
    [InlineData(AxisScale.Logit, false, typeof(LogitFormatter))]
    [InlineData(AxisScale.Date, true, typeof(AutoDateFormatter))]
    [InlineData(AxisScale.Date, false, null)]
    public void AScale_WritesItsLabelsWithItsFormatter(AxisScale scale, bool isX, Type? expected) =>
        Assert.Equal(expected, scale.Rule.Formatter(new MultipleLocator(1), isX)?.GetType());

    /// <summary>A linked axis is drawn on its primary's <c>functionlog</c> scale over a log primary (decades) and on the <c>function</c>
    /// scale, linear, over every other.</summary>
    [Theory]
    [InlineData(AxisScale.Log, AxisScale.Log)]
    [InlineData(AxisScale.Linear, AxisScale.Linear)]
    [InlineData(AxisScale.SymLog, AxisScale.Linear)]
    [InlineData(AxisScale.Logit, AxisScale.Linear)]
    [InlineData(AxisScale.Date, AxisScale.Linear)]
    public void ALinkedAxis_IsDrawnOnLogOverALogPrimary_AndLinearlyOverAnyOther(AxisScale primary, AxisScale drawnOn) =>
        Assert.Same(drawnOn.Rule, primary.Rule.UnderALink);

    /// <summary>The range a scale can draw (matplotlib's <c>limit_range_for_scale</c>): a log axis lifts a floor at or below 0 to three
    /// decades under its ceiling; a logit axis takes its range inside 0 and 1 by the smallest positive value it holds; every other scale
    /// draws any range.</summary>
    [Fact]
    public void ARange_IsLimitedToWhatItsScaleCanDraw()
    {
        Assert.Equal(new Range1D(0.25, 250), ScaleRule.Log.Limited(new Range1D(-5, 250), 3));
        Assert.Equal(new Range1D(0.001, 1), ScaleRule.Log.Limited(new Range1D(-5, 0), null));
        Assert.Equal(new Range1D(250, 0.25), ScaleRule.Log.Limited(new Range1D(250, -5), null));
        Assert.Equal(new Range1D(0.09, 0.91), ScaleRule.Logit.Limited(new Range1D(0, 1), 0.09));
        Assert.Equal(new Range1D(0.91, 0.09), ScaleRule.Logit.Limited(new Range1D(1, 0), 0.09));
        Assert.Equal(new Range1D(0.03, 0.97), ScaleRule.Logit.Limited(new Range1D(0.3, 0.3), 1e-7));
        Assert.Equal(new Range1D(1e-7, 1 - 1e-7), ScaleRule.Logit.Limited(new Range1D(0, 1), null));
        Assert.Equal(new Range1D(-5, 0), ScaleRule.Linear.Limited(new Range1D(-5, 0), 3));
        Assert.Equal(new Range1D(-5, 0), ScaleRule.SymLog.Limited(new Range1D(-5, 0), 3));
        Assert.Equal(new Range1D(-5, 0), ScaleRule.Date.Limited(new Range1D(-5, 0), 3));
    }

    /// <summary>Only a log axis lifts a floor at or below 0 (<c>Range1D.PositiveForLog</c>, which forwards here); a logit axis keeps the
    /// range it is handed by this rule.</summary>
    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.SymLog)]
    [InlineData(AxisScale.Logit)]
    [InlineData(AxisScale.Date)]
    public void OnlyALogAxis_LiftsItsFloorAboveZero(AxisScale scale)
    {
        Assert.Equal(new Range1D(-5, 0), scale.Rule.Lifted(new Range1D(-5, 0)));
        Assert.Equal(new Range1D(0.001, 1), ScaleRule.Log.Lifted(new Range1D(-5, 0)));
    }

    /// <summary>How a scale rounds a range out to its ticks under <c>round_numbers</c>: a linear axis to the multiples of its spacing,
    /// else to the ends of the ladder's ticks; a log axis to whole decades, a symmetric-log axis to the decades either side of zero; a
    /// logit and a date axis keep the range.</summary>
    [Fact]
    public void AScale_RoundsARangeOutByItsOwnTicks()
    {
        var range = new Range1D(0.3, 7.2);

        Assert.Equal(new Range1D(0, 8), ScaleRule.Linear.RoundedOut(range, null, TickLadder.Classic));
        Assert.Equal(new Range1D(0, 9), ScaleRule.Linear.RoundedOut(range, 3, TickLadder.Classic));
        Assert.Equal(new Range1D(0.1, 10), ScaleRule.Log.RoundedOut(range, 3, TickLadder.Classic));
        Assert.Equal(SymlogLocator.DecadesHolding(range), ScaleRule.SymLog.RoundedOut(range, null, TickLadder.Classic));
        Assert.Equal(range, ScaleRule.Logit.RoundedOut(range, 3, TickLadder.Classic));
        Assert.Equal(range, ScaleRule.Date.RoundedOut(range, 3, TickLadder.Classic));
    }
}
