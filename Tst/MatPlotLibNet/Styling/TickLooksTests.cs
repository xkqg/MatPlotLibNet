// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>What a theme's tick and range looks answer, asked by the renderers instead of reading the flags (<see cref="TickLabelLook"/>:
/// the minus sign of a label; <see cref="AxisTickLook"/>: which minor-tick facts a matplotlib theme asks for; <see cref="AxisRangeLook"/>:
/// whether a patch takes part in the range). Each question has one answer in one place.</summary>
public class TickLooksTests
{
    private static readonly TickLabelLook LibraryLabels = Theme.Default.Looks.TickLabels;
    private static readonly TickLabelLook MatplotlibLabels = Theme.MatplotlibV2.Looks.TickLabels;
    private static readonly AxisTickLook LibraryTicks = Theme.Default.Looks.Ticks;
    private static readonly AxisTickLook MatplotlibTicks = Theme.MatplotlibV2.Looks.Ticks;
    private static readonly AxisRangeLook LibraryRange = Theme.Default.Looks.Range;
    private static readonly AxisRangeLook MatplotlibRange = Theme.MatplotlibClassic.Looks.Range;

    // ── TickLabelLook: the minus sign ──

    [Fact]
    public void TheTrueMinusSign_IsU2212()
    {
        Assert.Equal('−', TickLabelLook.TrueMinusSign);
    }

    [Fact]
    public void ALabelUnderAMatplotlibTheme_IsSignedWithTheTrueMinus_AndUnderTheLibrarysWithAHyphen()
    {
        Assert.Equal("−1.5e−5", MatplotlibLabels.Signed("-1.5e-5"));
        Assert.Equal("-1.5e-5", LibraryLabels.Signed("-1.5e-5"));
    }

    [Fact]
    public void ALabelWithoutAHyphen_IsTheSameText()
    {
        const string text = "1.5";

        Assert.Same(text, MatplotlibLabels.Signed(text));
    }

    [Fact]
    public void Ticks_AreLabelledInOneFormat_AndSignedByTheLook()
    {
        Assert.Equal(["−1.0", "−0.5", "0.0", "0.5"], MatplotlibLabels.Labelled([-1.0, -0.5, 0.0, 0.5]));
        Assert.Equal(["-1.0", "-0.5", "0.0", "0.5"], LibraryLabels.Labelled([-1.0, -0.5, 0.0, 0.5]));
    }

    /// <summary>A tick drawing signs its labels by the look of the theme it is drawn under; it holds no look of its own beside it.</summary>
    [Fact]
    public void ADrawContext_SignsItsLabelsByTheLookOfItsTheme()
    {
        var draw = TickDrawContext.For(EdgeTerms.Bottom, new TickSource(null, null, false), [-1.0, 0.0], Theme.MatplotlibV2);

        Assert.Equal("−1", draw.LabelText(-1));
        Assert.Equal("-1", (draw with { Theme = Theme.Default }).LabelText(-1));
    }

    // ── AxisTickLook: the minor-tick facts ──

    private static readonly TickConfig MinorLeftAlone = new Axis().MinorTicks;
    private static readonly TickConfig MinorSwitchedOn = new Axis().MinorTicks with { Visible = true };
    private static readonly TickConfig MinorSwitchedOff = new Axis().MinorTicks with { Visible = false };

    [Fact]
    public void ALogLocatorNamesItsMinorTicks_OnlyUnderAMatplotlibTheme()
    {
        var log = new LogLocator();

        Assert.Same(log, MatplotlibTicks.MinorNamer(log));
        Assert.Null(LibraryTicks.MinorNamer(log));
        Assert.Null(MatplotlibTicks.MinorNamer(new AutoLocator()));
        Assert.Null(MatplotlibTicks.MinorNamer(null));
    }

    [Theory]
    [InlineData("matplotlib", "left alone", "log", true)]
    [InlineData("matplotlib", "left alone", "logit", true)]
    [InlineData("matplotlib", "left alone", "linear", false)]
    [InlineData("matplotlib", "switched off", "log", false)]
    [InlineData("matplotlib", "switched on", "linear", true)]
    [InlineData("library", "left alone", "log", false)]
    [InlineData("library", "switched on", "log", true)]
    public void MinorTicksStand_WhenSwitchedOn_OrLeftAloneOnAScaleThatPlacesThemUnderAMatplotlibTheme(
        string theme, string minor, string scale, bool expected)
    {
        Assert.Equal(expected, TicksOf(theme).PlacesMinors(MinorOf(minor), ScaleOf(scale)));
    }

    [Theory]
    [InlineData("library", "left alone", "linear", true)]
    [InlineData("library", "switched off", "log", true)]
    [InlineData("matplotlib", "left alone", "linear", false)]
    [InlineData("matplotlib", "left alone", "log", true)]
    [InlineData("matplotlib", "switched on", "linear", true)]
    public void AMinorGridLine_StandsAlways_UnderTheLibrarysTheme_AndOnlyWhereTheMinorTicksStand_UnderAMatplotlibOne(
        string theme, string minor, string scale, bool expected)
    {
        Assert.Equal(expected, TicksOf(theme).CarriesMinorGrid(MinorOf(minor), ScaleOf(scale)));
    }

    [Theory]
    [InlineData("library", "switched on", false)]
    [InlineData("matplotlib", "switched on", true)]
    [InlineData("matplotlib", "left alone", false)]
    public void TheGridOfACube_StandsOnTheMinorValues_OnlyUnderAMatplotlibThemeWithItsMinorTicksOn(string theme, string minor, bool expected)
    {
        Assert.Equal(expected, TicksOf(theme).GridStandsOnMinors(MinorOf(minor), ScaleRule.Linear));
    }

    [Fact]
    public void AMinorGridLine_IsTheMajorLineUnderAMatplotlibTheme_AndHalfOfItUnderTheLibrarys()
    {
        var major = StrokeStyle.Solid(Color.FromHex("#204080"), 1.6);

        Assert.Equal(major, MatplotlibTicks.MinorGridStroke(major));
        Assert.Equal(major.Faded(0.5), LibraryTicks.MinorGridStroke(major));
    }

    private static AxisTickLook TicksOf(string theme) => theme == "matplotlib" ? MatplotlibTicks : LibraryTicks;

    private static TickConfig MinorOf(string minor) =>
        minor == "switched on" ? MinorSwitchedOn : minor == "switched off" ? MinorSwitchedOff : MinorLeftAlone;

    private static ScaleRule ScaleOf(string scale) => scale switch { "log" => ScaleRule.Log, "logit" => ScaleRule.Logit, _ => ScaleRule.Linear };

    // ── AxisRangeLook: a patch in the range ──

    /// <summary>The patches range an axis whatever else asks under the library's own themes, and only an axis something asks for under
    /// the matplotlib themes: a 2-D axis (OverlayAutoscaleTests) and a polar radius alike.</summary>
    [Fact]
    public void UnderAMatplotlibTheme_ThePatchesRangeAnAxis_OnlyWhenSomethingAsksForIt()
    {
        Assert.True(LibraryRange.PatchesRange(asked: false));
        Assert.False(MatplotlibRange.PatchesRange(asked: false));
        Assert.True(MatplotlibRange.PatchesRange(asked: true));
    }

    [Fact]
    public void ARectangleOfNoSize_TakesNoPartInTheRange_UnderAMatplotlibTheme()
    {
        var empty = new Rectangle(new DataPoint(1, 1), 0, 0);

        Assert.False(MatplotlibRange.Counts(empty));
        Assert.True(LibraryRange.Counts(empty));
        Assert.True(MatplotlibRange.Counts(new Rectangle(new DataPoint(1, 1), 0, 2)));
    }
}
