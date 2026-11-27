// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>matplotlib's <c>LogNorm</c> (3.11.2, <c>colors.py</c>, <c>_make_norm_from_scale</c> over <c>LogScale</c>): a value maps to
/// <c>log(value / vmin) / log(vmax / vmin)</c>, a value at or below 0 is masked (<c>nonpositive='mask'</c>), and <c>vmin == vmax</c>
/// gives 0. The library's public <see cref="LogNormalizer"/> is <c>log(1 + value - min) / log(1 + max - min)</c>, which its
/// documentation states, and keeps that formula; the two agree only for <c>min == 1</c>. Under the matplotlib themes a series
/// whose normaliser is the <see cref="LogNormalizer"/> reads it as <c>LogNorm</c> (<c>NormalizerUnder</c>, the one owner). Every
/// expected number is what <c>LogNorm(0.1, 100)</c> prints for the same value.</summary>
public sealed class LogNormTests
{
    [Theory]
    [InlineData(1.0, 1.0 / 3)]
    [InlineData(10.0, 2.0 / 3)]
    [InlineData(0.5, 0.23299000144533957)]
    [InlineData(200.0, 1.0)]
    [InlineData(0.05, 0.0)]
    public void Normalize_IsMatplotlibsLogNorm_OverAPositiveRange(double value, double expected) =>
        Assert.Equal(expected, LogNorm.Instance.Normalize(value, 0.1, 100), 12);

    [Fact]
    public void TheLibrarysFormula_DisagreesWithIt_AwayFromOne() =>
        Assert.NotEqual(1.0 / 3, new LogNormalizer().Normalize(1.0, 0.1, 100), 3);

    [Fact]
    public void AnEmptyRange_GivesZero_AsMatplotlibsNormDoes() =>
        Assert.Equal(0.0, LogNorm.Instance.Normalize(5, 5, 5));

    /// <summary>matplotlib takes the smallest positive value as <c>vmin</c>; a range that reaches 0 or below is not one it would
    /// make, and the library keeps drawing it as its <see cref="LogNormalizer"/> does.</summary>
    [Theory]
    [InlineData(5.0, 0.0, 100.0)]
    [InlineData(5.0, -3.0, 100.0)]
    public void ARangeThatReachesZero_FallsBackToTheLibrarysFormula(double value, double min, double max) =>
        Assert.Equal(new LogNormalizer().Normalize(value, min, max), LogNorm.Instance.Normalize(value, min, max));

    [Fact]
    public void AValueAtOrBelowZero_IsMasked_OverAPositiveRange()
    {
        Assert.True(LogNorm.Instance.Masks(0, 0.1));
        Assert.True(LogNorm.Instance.Masks(-2, 0.1));
        Assert.False(LogNorm.Instance.Masks(0.001, 0.1));
        Assert.False(LogNorm.Instance.Masks(0, 0), "a range that reaches 0 masks nothing");
        Assert.False(LinearNormalizer.Instance.Masks(-5, 0.1));
        Assert.False(new LogNormalizer().Masks(0, 0.1));
    }

    [Fact]
    public void AMaskedValue_IsDrawnInTheMapsMissingColour()
    {
        var map = ColorMaps.Viridis;

        Assert.Equal(map.MissingValueColor(), map.ColorOf(0, LogNorm.Instance, 0.1, 100));
        Assert.NotEqual(map.MissingValueColor(), map.ColorOf(1, LogNorm.Instance, 0.1, 100));
        Assert.NotEqual(map.MissingValueColor(), map.ColorOf(0, new LogNormalizer(), 0, 100));
    }

    /// <summary>matplotlib's layers of a log-scaled filled contour are <c>sqrt(lower) * sqrt(upper)</c> (contour.py,
    /// <c>_process_layers</c>), the middle in screen space; every other normaliser keeps the arithmetic middle.</summary>
    [Fact]
    public void TheMiddleOfABand_IsTheGeometricMean_UnderLogNorm()
    {
        Assert.Equal(Math.Sqrt(10), LogNorm.Instance.MiddleOf(1, 10), 12);
        Assert.Equal(5.5, LinearNormalizer.Instance.MiddleOf(1, 10), 12);
        Assert.Equal(5.5, new LogNormalizer().MiddleOf(1, 10), 12);
        Assert.Equal(1.0 / 6, LogNorm.Instance.Normalize(LogNorm.Instance.MiddleOf(1, 10), 1, 1000), 12);
    }

    /// <summary>A band with a level at 0 or below has no geometric mean: the log normaliser falls back to the arithmetic middle, the one
    /// every other normaliser keeps (<c>INormalizerRule.LinearMiddleOf</c>).</summary>
    [Fact]
    public void ABandWithoutALogarithm_TakesTheArithmeticMiddle()
    {
        Assert.Equal(INormalizerRule.LinearMiddleOf(-2, 10), LogNorm.Instance.MiddleOf(-2, 10));
        Assert.Equal(4.0, LogNorm.Instance.MiddleOf(-2, 10), 12);
        Assert.Equal(INormalizerRule.LinearMiddleOf(1, 10), LinearNormalizer.Instance.MiddleOf(1, 10));
    }

    // ---- the one owner ---------------------------------------------------------------------------------------------------

    private static HeatmapSeries Series(INormalizer? normalizer) => new(new double[,] { { 1 } }) { Normalizer = normalizer };

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void UnderTheMatplotlibThemes_ALogNormalizer_IsLogNorm(string name) =>
        Assert.Same(LogNorm.Instance, Series(new LogNormalizer()).NormalizerUnder(name.AsTheme()));

    [Fact]
    public void UnderTheLibrarysOwnTheme_ALogNormalizer_StaysAsItIs()
    {
        var own = new LogNormalizer();

        Assert.Same(own, Series(own).NormalizerUnder(Theme.Default));
    }

    [Fact]
    public void EveryOtherNormaliser_IsReadAsItIs_UnderEveryTheme()
    {
        var power = new PowerNormNormalizer(2);

        Assert.Same(power, Series(power).NormalizerUnder(Theme.MatplotlibV2));
        Assert.Same(LinearNormalizer.Instance, Series(null).NormalizerUnder(Theme.MatplotlibV2));
    }

    // ---- the grid a log norm masks ---------------------------------------------------------------------------------------

    [Fact]
    public void MaskedBy_MakesTheValuesAtOrBelowZeroMissing_OnACopy()
    {
        var grid = new double[,] { { 1, 0 }, { -2, 4 } };

        var masked = grid.MaskedBy(LogNorm.Instance, 0.5);

        Assert.True(double.IsNaN(masked[0, 1]) && double.IsNaN(masked[1, 0]));
        Assert.Equal(1, masked[0, 0]);
        Assert.Equal(0, grid[0, 1]);
    }

    [Fact]
    public void MaskedBy_CopiesNothing_WhenNothingIsMasked()
    {
        var positive = new double[,] { { 1, 2 } };
        var withZero = new double[,] { { 0, 2 } };

        Assert.Same(positive, positive.MaskedBy(LogNorm.Instance, 0.5));
        Assert.Same(withZero, withZero.MaskedBy(LinearNormalizer.Instance, 0.5));
        Assert.Same(withZero, withZero.MaskedBy(LogNorm.Instance, 0)); // a range that reaches 0 masks nothing
    }

    // ---- the theme and its readers -----------------------------------------------------------------------------------------

    [Fact]
    public void TheColourBar_ReadsALogNormalisedSeries_AsTheSeriesDoes()
    {
        var series = new HeatmapSeries(new double[,] { { 1, 10 } }) { Normalizer = new LogNormalizer() };

        Assert.Same(LogNorm.Instance, series.ColorBarNormalizerUnder(Theme.MatplotlibV2));
        Assert.IsType<LogNormalizer>(series.ColorBarNormalizerUnder(Theme.Default));
    }

    /// <summary>The bar of a filled log contour colours a band at the geometric mean of its levels over the levels, as the bands are drawn.</summary>
    [Fact]
    public void TheBarOfALogFilledContour_ColoursABandAtTheGeometricMeanOverTheLevels()
    {
        double[] axis = [0, 1];
        var series = new ContourfSeries(axis, axis, new double[,] { { 1, 100 }, { 10, 1000 } }) { Normalizer = new LogNormalizer() };
        var bar = (IDiscreteNormalizer)series.ColorBarNormalizerUnder(Theme.MatplotlibV2);
        double[] levels = bar.Boundaries;

        for (int band = 0; band < levels.Length - 1; band++)
        {
            double expected = Math.Log(Math.Sqrt(levels[band]) * Math.Sqrt(levels[band + 1]) / levels[0]) / Math.Log(levels[^1] / levels[0]);
            Assert.Equal(expected, bar.MapFractionOf(band), 12);
        }
    }
}
