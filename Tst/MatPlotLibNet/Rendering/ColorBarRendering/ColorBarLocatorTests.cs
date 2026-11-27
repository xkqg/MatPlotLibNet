// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>A colour bar given a tick locator of its own (<see cref="ColorBar.Locator"/>) names the ticks that locator places, as
/// matplotlib's <c>Colorbar.locator</c> does (<c>cbar.locator = MultipleLocator(0.5)</c>): every one inside the range, whatever the
/// length of the bar, each where the chart's normaliser sends its value.</summary>
public class ColorBarLocatorTests
{
    private static readonly Font TickFont = new() { Size = 13 };

    private static ColorBarScale Scale(double min, double max, double lengthPx, INormalizer normalizer, ITickLocator? locator) =>
        ColorBarScale.For(min, max, lengthPx, vertical: true, normalizer, new SvgRenderContext(), TickFont, Theme.Default, locator);

    [Fact]
    public void AGivenLocator_PlacesTheTicks_WhereTheLengthAloneWouldPlaceOthers()
    {
        // 300 px alone asks for nine ticks over 0..2, steps of 0.25; MultipleLocator(0.5) names the halves.
        var scale = Scale(0, 2, 300, LinearNormalizer.Instance, new MultipleLocator(0.5));

        Assert.Equal([0.0, 0.5, 1, 1.5, 2], scale.Ticks);
        Assert.Equal(["0.0", "0.5", "1.0", "1.5", "2.0"], scale.Labels);
        Assert.Equal([0.0, 0.25, 0.5, 0.75, 1], scale.Fractions);
    }

    [Fact]
    public void AGivenLocator_NamesEveryTickItPlaces_OnAShortBarToo()
    {
        // 45 px alone asks for two ticks; the locator's eleven are all drawn, as matplotlib draws every tick its locator returns.
        var scale = Scale(0, 1, 45, LinearNormalizer.Instance, new MultipleLocator(0.1));

        Assert.Equal(11, scale.Ticks.Length);
    }

    [Fact]
    public void AGivenLocatorsTicksOutsideTheRange_AreLeftOut()
    {
        var scale = Scale(1, 10, 300, LinearNormalizer.Instance, new FixedLocator([0, 3, 6, 9, 12]));

        Assert.Equal([3.0, 6, 9], scale.Ticks);
    }

    [Fact]
    public void UnderALogNorm_AGivenLocatorsTicks_StandWhereTheNormSendsThem_NamedInScientificNotation()
    {
        var given = Scale(1, 1000, 300, LogNorm.Instance, new FixedLocator([1, 10, 100]));
        var named = Scale(1, 1000, 300, LogNorm.Instance, null);

        Assert.Equal([1.0, 10, 100], given.Ticks);
        Assert.Equal([0.0, 1.0 / 3, 2.0 / 3], given.Fractions, new ToleranceComparer(1e-12));
        Assert.Equal(named.Labels[..3], given.Labels);
    }

    [Fact]
    public void OnABarOfBands_AGivenLocatorsTicks_StandBetweenTheBoundaries_AsMatplotlibsUniformSpacingInterpolatesThem()
    {
        // Three even bands between 0, 1, 4 and 10: 2.5 is half way through the second (0.5 of the bar), 7 half way through the third.
        var scale = Scale(0, 10, 300, new BoundaryNormalizer([0, 1, 4, 10]), new FixedLocator([0, 2.5, 7, 10]));

        Assert.Equal([0.0, 2.5, 7, 10], scale.Ticks);
        Assert.Equal([0.0, 0.5, 5.0 / 6, 1], scale.Fractions, new ToleranceComparer(1e-12));
    }

    [Fact]
    public void OnABarOfBands_ATickOutsideTheBoundaries_StandsAtTheNearerEnd()
    {
        // np.interp holds a value below the first boundary at 0 and one above the last at 1.
        var scale = Scale(0, 10, 300, new BoundaryNormalizer([2, 5, 8]), new FixedLocator([0, 3.5, 10]));

        Assert.Equal([0.0, 0.25, 1], scale.Fractions, new ToleranceComparer(1e-12));
    }

    [Fact]
    public void TheWidestLabel_OfABarWithAGivenLocator_IsTheWidestOfTheLabelsItsTicksAreNamedBy()
    {
        var ctx = new SvgRenderContext();

        double widest = ColorBarScale.WidestLabel(0, 2000, ctx, TickFont, LinearNormalizer.Instance, Theme.Default, new FixedLocator([5, 1234.5]));

        Assert.Equal(ctx.MeasureText("1234.5", TickFont).Width, widest, 9);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Alone the bar would name 0.0, 2.5, ... 20.0; the heatmap's own axes are numbered 0.00 to 2.00.
        new DrawnToken("TheAxesColourBar_DrawsTheTicksOfItsLocator", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 10 }, { 15, 20 } })
                .WithColorBar(cb => cb with { Locator = new MultipleLocator(5) }))
            .ToSvg(), ">15<") { Absent = [">17.5<"] },
        new DrawnToken("TheFiguresColourBar_DrawsTheTicksOfItsLocator", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 10 }, { 15, 20 } }))
            .WithColorBar(cb => cb with { Locator = new MultipleLocator(5) })
            .ToSvg(), ">15<") { Absent = [">17.5<"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ALocatorGivenToABar_ComesBackFromJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 2 } }).WithColorBar(cb => cb with { Locator = new MultipleLocator(0.5) }))
            .Build();

        var read = figure.RoundTripped().SubPlots[0].ColorBar!.Locator;

        Assert.IsType<MultipleLocator>(read);
        Assert.Equal([0.0, 0.5, 1], read.Locate(0, 1));
    }

    /// <summary>Two numbers are the same to within a tolerance.</summary>
    private sealed class ToleranceComparer(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(double obj) => 0;
    }
}
