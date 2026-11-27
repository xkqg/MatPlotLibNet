// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An image keeps square cells under the matplotlib themes, as <c>imshow</c> does. matplotlib 3.11.2, read with python:
/// <c>rcParams['image.aspect']</c> is <c>'equal'</c> under the default and the classic style; <c>ax.imshow(np.zeros((170, 220)))</c> on an
/// 8 x 6 inch figure gives <c>get_aspect()</c> 1.0 and narrows the axes box to the image's shape (v2 position 0.1388, 0.11, 0.7474, 0.77).
/// <c>imshow(aspect='auto')</c> (<c>set_aspect('auto')</c>) lets the image fill the axes. The library's own themes let it fill the plot, as
/// they always have; an aspect the caller gives wins under every theme.</summary>
public class ImageAspectTests
{
    private static Rect PlotOf(Theme theme, Action<AxesBuilder>? more = null) =>
        new ChartRenderer().ComputeLayout(Figure(theme, more), new SvgRenderContext()).PlotAreas[0];

    private static Figure Figure(Theme theme, Action<AxesBuilder>? more = null) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => { ax.Image(new double[170, 220]); more?.Invoke(ax); })
        .Build();

    [Fact]
    public void TheImshowLook_AsksAnAspectOfOne_TheLibrarysNone()
    {
        Assert.Equal(1.0, ImageLook.Imshow.Aspect);
        Assert.Null(ImageLook.Library.Aspect);
        Assert.Equal(1.0, Theme.MatplotlibV2.Looks.Image.Aspect);
        Assert.Equal(1.0, Theme.MatplotlibClassic.Looks.Image.Aspect);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheMatplotlibThemes_AnImagesPlot_TakesItsShape(string themeId)
    {
        var theme = themeId.AsTheme();

        var plot = PlotOf(theme);

        // A cell is as tall as it is wide: the plot is 220 cells across and 170 down.
        Assert.Equal(170.0 / 220.0, plot.Height / plot.Width, 6);
    }

    [Fact]
    public void UnderTheLibrarysThemes_AnImage_FillsThePlot()
    {
        var withImage = PlotOf(Theme.Default);
        var plain = new ChartRenderer().ComputeLayout(Plt.Create().WithSize(800, 600).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build(), new SvgRenderContext()).PlotAreas[0];

        Assert.Equal(plain, withImage);
    }

    [Fact]
    public void AnAspectTheCallerGives_Wins()
    {
        var plot = PlotOf(Theme.MatplotlibV2, ax => ax.SetAspect(2));

        Assert.Equal(2 * 170.0 / 220.0, plot.Height / plot.Width, 6);
    }

    [Fact]
    public void AnAutoAspect_LetsTheImageFillThePlot_UnderEveryTheme()
    {
        var auto = PlotOf(Theme.MatplotlibV2, ax => ax.SetAspectAuto());
        var plain = new ChartRenderer().ComputeLayout(Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build(), new SvgRenderContext()).PlotAreas[0];

        Assert.Equal(plain, auto);
    }

    [Fact]
    public void AnAutoAspect_IsGiven_AndRoundTripsThroughTheJson()
    {
        var figure = Figure(Theme.MatplotlibV2, ax => ax.SetAspectAuto());

        var back = figure.RoundTripped();

        Assert.Equal(0.0, back.SubPlots[0].Aspect);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ThePicture_LocksAZoom_OfAPlotThatHasAnAspect_GivenOrAsked (#1)", () => Figure(Theme.MatplotlibV2).ToSvg(), "data-aspect-lock=\"true\""),
        new DrawnToken("ThePicture_LocksAZoom_OfAPlotThatHasAnAspect_GivenOrAsked (#2)", () => Figure(Theme.Default).ToSvg()) { Absent = ["data-aspect-lock"] },
        new DrawnToken("ThePicture_LocksAZoom_OfAPlotThatHasAnAspect_GivenOrAsked (#3)", () => Figure(Theme.MatplotlibV2, ax => ax.SetAspectAuto()).ToSvg()) { Absent = ["data-aspect-lock"] },
        new DrawnToken("ThePicture_LocksAZoom_OfAPlotThatHasAnAspect_GivenOrAsked (#4)", () => Figure(Theme.Default, ax => ax.SetAspect(1)).ToSvg(), "data-aspect-lock=\"true\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
