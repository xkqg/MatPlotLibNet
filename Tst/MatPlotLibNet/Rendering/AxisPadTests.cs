// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The pad of an axes title and of an axis title where its author names none. matplotlib 3.11.2, printed under each style:
/// <c>rcParams['axes.titlepad']</c> is 6.0 points in the default style and 5.0 in the classic one, and <c>rcParams['axes.labelpad']</c>
/// is 4.0 and 5.0, the same for the X axis and the Y axis (<c>Axes.set_title</c> puts the title <c>pad</c> points above the axes;
/// <c>Axis.set_label_text</c> and <c>_update_label_position</c> put a label <c>labelpad</c> points from the tick labels). The library's
/// own themes keep an axes title 8 pixels above the plot, an X axis title 6 pixels from its labels and a Y axis title
/// <c>YAxisContainer.TitleGap</c>, 4, from its labels. A gap that is named is drawn as named, 4 included.</summary>
public class AxisPadTests
{
    private static readonly double TitlePadV2 = 6.0.PointsInPixels;
    private static readonly double TitlePadClassic = 5.0.PointsInPixels;
    private static readonly double LabelPadV2 = 4.0.PointsInPixels;
    private static readonly double LabelPadClassic = 5.0.PointsInPixels;

    [Fact]
    public void TheGapsUnderATheme_AreTheThemesOrTheLibrarysOwn()
    {
        Assert.Equal(TitlePadV2, Theme.MatplotlibV2.TitleGapUnder, 9);
        Assert.Equal(TitlePadClassic, Theme.MatplotlibClassic.TitleGapUnder, 9);
        Assert.Equal(8.0, Theme.Default.TitleGapUnder);
        Assert.Equal(LabelPadV2, Theme.MatplotlibV2.XLabelGapUnder, 9);
        Assert.Equal(LabelPadClassic, Theme.MatplotlibClassic.XLabelGapUnder, 9);
        Assert.Equal(6.0, Theme.Default.XLabelGapUnder);
    }

    [Fact]
    public void AYAxisContainersGap_IsTheThemesWhereNoneWasGiven_AndTheGivenOneOtherwise_FourIncluded()
    {
        var container = new Axes().YAxes;

        Assert.Equal(4.0, container.TitleGap);                                   // what a new container reads: unchanged
        Assert.Null(container.GivenTitleGap);
        Assert.Equal(LabelPadV2, container.TitleGapUnder(Theme.MatplotlibV2), 9);
        Assert.Equal(LabelPadClassic, container.TitleGapUnder(Theme.MatplotlibClassic), 9);
        Assert.Equal(4.0, container.TitleGapUnder(Theme.Default));

        container.TitleGap = 4.0;

        Assert.Equal(4.0, container.TitleGapUnder(Theme.MatplotlibV2));
        Assert.Equal(4.0, container.TitleGapUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void TheTitleBaseline_StandsTheGapAboveTheTopBandAndTheStylesPad()
    {
        var axes = new Axes { Title = "Revenue" };
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Assert.Equal(100 - 8 - 12, axes.TitleBaseline(100, 12, axes.TitleLift(ctx, Theme.Default)));                  // the library's own gap
        Assert.Equal(100 - TitlePadV2 - 12, axes.TitleBaseline(100, 12, axes.TitleLift(ctx, Theme.MatplotlibV2)), 9);
    }

    // ── drawn ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static Theme With(Theme baseTheme, double? titlePad = null, double? labelPad = null) =>
        new("padded", baseTheme)
        {
            Looks = baseTheme.Looks with
            {
                Text = baseTheme.Looks.Text with { TitlePad = titlePad ?? baseTheme.Looks.Text.TitlePad, LabelPad = labelPad ?? baseTheme.Looks.Text.LabelPad },
            },
        };

    private static XDocument Doc(Theme theme, Action<YAxisContainer>? gap = null) =>
        BuildFigure(theme, gap).ToSvg().Xml();

    private static Figure BuildFigure(Theme theme, Action<YAxisContainer>? gap = null)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1.0, 2.0], [0.0, 1.0, 0.5]).WithTitle("The title").SetXLabel("The X axis").SetYLabel("The Y axis")).Build();
        gap?.Invoke(figure.SubPlots[0].YAxes);
        return figure;
    }

    private static XElement TextOf(XDocument doc, string text) =>
        doc.Each("text").First(e => e.Value == text);

    /// <summary>The anchor of a rotated text: the point its transform turns about.</summary>
    private static (double X, double Y) Anchor(XElement text)
    {
        if (SvgElement.Of(text).RotationCentre() is { } centre)
            return (centre.X, centre.Y);
        return (text.Number("x"), text.Number("y"));
    }

    [Fact]
    public void TheAxesTitle_MovesWithTheThemesTitlePad_AndNothingElseDoes()
    {
        var near = Doc(With(Theme.MatplotlibV2, titlePad: 10));
        var far = Doc(With(Theme.MatplotlibV2, titlePad: 30));

        Assert.Equal(20.0, TextOf(near, "The title").Number("y") - TextOf(far, "The title").Number("y"), 1);
        Assert.Equal(TextOf(near, "The X axis").Number("y"), TextOf(far, "The X axis").Number("y"), 6);
    }

    [Fact]
    public void TheAxesTitleBand_GrowsWithTheThemesTitlePad()
    {
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var axes = new Axes { Title = "The title" };

        double near = axes.TitleBand(ctx, With(Theme.MatplotlibV2, titlePad: 10), 0, 0);
        double far = axes.TitleBand(ctx, With(Theme.MatplotlibV2, titlePad: 30), 0, 0);

        Assert.Equal(20.0, far - near, 6);
    }

    [Fact]
    public void TheXAxisTitleBand_GrowsWithTheThemesLabelPad_AndSoDoesTheYAxisTitleBand()
    {
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var axis = new Axis { Label = "The X axis" };

        double nearX = axis.BottomTitleExtent(axis.Label, ctx, With(Theme.MatplotlibV2, labelPad: 10), 12);
        double farX = axis.BottomTitleExtent(axis.Label, ctx, With(Theme.MatplotlibV2, labelPad: 30), 12);

        var container = new Axes().YAxes;
        double nearY = new Axis().YTitleExtent("The Y axis", new YAxisTitleExtensions.YAxisTitleInput(20, AxisSide.Left, container.TitleGapUnder(With(Theme.MatplotlibV2, labelPad: 10)), Theme.MatplotlibV2.LabelFont())).Band;
        double farY = new Axis().YTitleExtent("The Y axis", new YAxisTitleExtensions.YAxisTitleInput(20, AxisSide.Left, container.TitleGapUnder(With(Theme.MatplotlibV2, labelPad: 30)), Theme.MatplotlibV2.LabelFont())).Band;

        Assert.Equal(20.0, farX - nearX, 6);
        Assert.Equal(20.0, farY - nearY, 6);
    }

    [Fact]
    public void AYAxisGapThatIsGiven_IsDrawnAsGiven_FourIncluded()
    {
        var unset = Doc(Theme.MatplotlibV2);
        var four = Doc(Theme.MatplotlibV2, y => y.TitleGap = 4.0);

        // the unset gap is axes.labelpad, 4 points = 5.56 px, so the title stands 1.56 px further out than the 4 given
        Assert.Equal(LabelPadV2 - 4.0, Anchor(TextOf(four, "The Y axis")).X - Anchor(TextOf(unset, "The Y axis")).X, 0.2);
    }

    [Fact]
    public void AYAxisGap_IsWrittenToTheJsonWhenGiven_AndNotOtherwise()
    {
        string unset = BuildFigure(Theme.MatplotlibV2).ToJson();
        string given = BuildFigure(Theme.MatplotlibV2, y => y.TitleGap = 4.0).ToJson();

        Assert.DoesNotContain("yAxisTitleGap", unset);
        Assert.Contains("\"yAxisTitleGap\":4", given);
        Assert.Equal(4.0, ChartSerializerDefault.Current.FromJson(given).SubPlots[0].YAxes.GivenTitleGap);
        Assert.Null(ChartSerializerDefault.Current.FromJson(unset).SubPlots[0].YAxes.GivenTitleGap);
    }
}
