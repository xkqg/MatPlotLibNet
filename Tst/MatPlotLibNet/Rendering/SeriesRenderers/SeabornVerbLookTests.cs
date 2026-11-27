// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What seaborn's verbs do beyond their marks, where their author names nothing, are values of the theme's verb look
/// (<see cref="SeabornVerbLook"/>). Under the matplotlib themes they are seaborn 0.13.2's over matplotlib 3.11.2, read with python:
/// <c>sns.countplot</c> and <c>sns.kdeplot</c> name their value axis (<c>_add_axis_labels</c>: printed <c>ax.get_ylabel()</c> 'count' and
/// 'Density', <c>ax.get_xlabel()</c> '') only where the axis has no label yet. The library's own themes keep theirs: no name.</summary>
public class SeabornVerbLookTests
{
    private static Axes AxesHolding(string kind)
    {
        var axes = new Axes();
        switch (kind)
        {
            case "count": axes.Countplot(["a", "b", "a"]); break;
            case "count lying": axes.Countplot(["a", "b", "a"]).Orientation = BarOrientation.Horizontal; break;
            case "kde": axes.Kde([1.0, 2, 2, 3]); break;
            default: axes.Plot([1.0, 2], [1.0, 2]); break;
        }
        return axes;
    }

    [Theory]
    [InlineData("MatplotlibV2", "count", null, "count")]
    [InlineData("MatplotlibClassic", "count", null, "count")]
    [InlineData("MatplotlibV2", "count lying", "count", null)]
    [InlineData("MatplotlibV2", "kde", null, "Density")]
    [InlineData("MatplotlibClassic", "kde", null, "Density")]
    [InlineData("MatplotlibV2", "line", null, null)]
    [InlineData("Default", "count", null, null)]
    [InlineData("Default", "kde", null, null)]
    [InlineData("Dark", "count lying", null, null)]
    public void AVerbsValueAxis_IsNamedAsSeabornNamesIt_UnderItsTheme(string themeName, string kind, string? x, string? y)
    {
        var theme = themeName.AsTheme();
        var axes = AxesHolding(kind);

        Assert.Equal(x, axes.TitleOf(axes.XAxis, theme));
        Assert.Equal(y, axes.TitleOf(axes.YAxis, theme));
    }

    [Fact]
    public void AGivenLabel_Wins_AndAnAxisPastThePrimaryIsNotNamed()
    {
        var axes = AxesHolding("count");
        axes.YAxis.Label = "how many";
        var right = axes.YAxes.Right.Add().Axis;

        Assert.Equal("how many", axes.TitleOf(axes.YAxis, Theme.MatplotlibV2));
        Assert.Null(axes.TitleOf(right, Theme.MatplotlibV2));
    }

    [Fact]
    public void TheJson_WritesNoNameTheVerbGave()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Countplot(["a", "b", "a"])).Build();

        var back = figure.RoundTripped().SubPlots[0];
        Assert.Null(back.YAxis.Label);
        Assert.Equal("count", back.TitleOf(back.YAxis, Theme.MatplotlibV2));
    }

    /// <summary>The name a verb gives its axis is drawn and kept room for as a label given by hand is: the picture of a count plot that
    /// names nothing is the picture of the same plot labelled "count", and under a library theme it is the picture with no label.</summary>
    [Theory]
    [InlineData("MatplotlibV2", "count", true)]
    [InlineData("MatplotlibClassic", "Density", true)]
    [InlineData("Default", "count", false)]
    public void TheName_IsDrawnAndKeptRoomFor_AsAGivenLabelIs(string themeName, string name, bool named)
    {
        var theme = themeName.AsTheme();
        string Svg(string? label) => Plt.Create().WithTheme(theme).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                if (name == "count") ax.Countplot(["a", "b", "a"]); else ax.Kde([1.0, 2, 2, 3]);
                if (label is not null) ax.SetYLabel(label);
            }).Build().ToSvg();

        Assert.Equal(named, Svg(null).Contains($">{name}</text>", StringComparison.Ordinal));
        Assert.Equal(Svg(named ? name : null), Svg(null));
    }

    /// <summary>seaborn's <c>countplot</c> fills its bars with the palette's colour desaturated to 0.75 (<c>saturation=.75</c>; printed face
    /// (31.88, 31.88, 223.12) under classic, (49.62, 115.62, 161.38) under the default style), and the legend handle copies the bar. A
    /// colour the author gave is drawn as given; the library's own themes draw the cycle's colour as it is.</summary>
    [Theory]
    [InlineData("MatplotlibClassic", null, "#2020DF")]
    [InlineData("MatplotlibV2", null, "#3274A1")]
    [InlineData("MatplotlibV2", "#FF0000", "#FF0000")]
    [InlineData("Default", null, null)]
    public void ACountplotsBars_AndItsHandle_WearTheColourSeabornFillsThemWith(string themeName, string? given, string? expected)
    {
        var theme = themeName.AsTheme();
        string svg = Plt.Create().WithTheme(theme).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["a", "b", "a"], s =>
            {
                s.Label = "counts";
                if (given is not null) s.Color = Color.FromHex(given);
            }).WithLegend()).Build().ToSvg();

        string fill = expected ?? theme.FirstCycleColor.ToHex();
        // Two bars and the legend's handle.
        Assert.True(svg.CountOf($"fill=\"{fill}\"") >= 3, $"expected the bars and the handle in {fill}");
    }

    /// <summary>A count plot's bars stand on 0 as every bar does (<c>Axes.bar</c> makes each bar's foot sticky: printed sticky y [0]), so
    /// no margin opens below them under any theme: under the default style the counts 3 and 1 range 0 .. 3.15 (seaborn: 0 .. 82.95 for
    /// counts up to 79).</summary>
    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void ACountplotsBars_StandOnZero_WithNoMarginBelow(string themeName)
    {
        var theme = themeName.AsTheme();
        var upright = AxesHolding("count");
        var lying = AxesHolding("count lying");

        Assert.Equal(0, upright.ComputeDataRanges(theme).YMin);
        Assert.Equal(0, lying.ComputeDataRanges(theme).XMin);
        if (themeName == "MatplotlibV2")
        {
            var counts = new Axes();
            counts.Countplot(["a", "a", "a", "b"]);
            Assert.Equal(3.15, counts.ComputeDataRanges(theme).YMax, 12);
        }
    }

    /// <summary>seaborn's <c>kdeplot</c> draws the curve alone (<c>fill=False</c>: printed no collection, one <c>Line2D</c>); the library's
    /// own themes fill under it. A choice the author gave is drawn as given under every theme, and the JSON writes only a given choice.</summary>
    [Theory]
    [InlineData("MatplotlibV2", null, false)]
    [InlineData("MatplotlibClassic", null, false)]
    [InlineData("Default", null, true)]
    [InlineData("MatplotlibV2", true, true)]
    [InlineData("Default", false, false)]
    public void ADensityCurve_IsFilledAsItsThemeFillsIt_UnlessAFillWasGiven(string themeName, bool? given, bool fills)
    {
        var kde = new KdeSeries([1.0, 2, 2, 3]);
        if (given is { } fill) kde.Fill = fill;

        Assert.Equal(fills, kde.FillsUnder(themeName.AsTheme()));
        Assert.Equal(given, kde.ToSeriesDto().Fill);
        Assert.Equal(given ?? true, kde.Fill);   // the public value reads the library's rest
    }

    /// <summary>What a density curve is to matplotlib is what seaborn draws: a <c>Line2D</c> (zorder 2) for the curve alone, a
    /// <c>fill_between</c> collection (zorder 1) where a fill was given; its legend handle is a line where the curve is drawn alone and a
    /// filled box where it is filled, under its theme.</summary>
    [Theory]
    [InlineData("MatplotlibV2", null, "Line")]
    [InlineData("Default", null, "Patch")]
    [InlineData("MatplotlibV2", true, "Patch")]
    [InlineData("Default", false, "Line")]
    public void ADensityCurvesArtistAndHandle_AreWhatItsThemeDraws(string themeName, bool? given, string handle)
    {
        var kde = new KdeSeries([1.0, 2, 2, 3]) { Label = "density" };
        if (given is { } fill) kde.Fill = fill;

        Assert.Equal(given == true ? 1 : 2, kde.ArtistZOrder);
        Assert.Equal(handle, Assert.Single(kde.LegendPartsUnder(themeName.AsTheme())).Swatch.Kind.ToString());
    }

    /// <summary>seaborn's density curve stands on 0 (<c>sticky_edges.y = (0, inf)</c>) and is ranged as its line is, from its lowest
    /// density to its peak, the axes' margin above it: printed for 1, 2, 2, 3, 5 under the default style, lowest 0.0008070566802014859,
    /// peak 0.24295843407052284, ylim (0, 0.2550660029400389) = the peak and 5 % of the line's extent. The library's own themes keep 5 %
    /// above the peak in the curve's own range, from 0, and no sticky base.</summary>
    [Fact]
    public void ADensityCurvesRange_StandsOnZeroUnderSeaborn_AndKeepsTheLibrarysHeadroomElsewhere()
    {
        var axes = new Axes();
        var kde = axes.Kde([1.0, 2, 2, 3, 5]);
        double seabornPeak = kde.CurveUnder(Theme.MatplotlibV2.Looks)!.Value.Y.Max();
        double libraryPeak = kde.CurveUnder(ThemeLooks.Library)!.Value.Y.Max();
        var context = new AxesContextAdapter(axes);

        var seaborn = kde.DataRangeUnder(context, Theme.MatplotlibV2);
        Assert.Equal(seabornPeak, seaborn.YMax);
        Assert.Equal(0.0008070566802014859, seaborn.YMin!.Value, 12);
        Assert.Equal(0, seaborn.StickyYMin);
        var drawn = axes.ComputeDataRanges(Theme.MatplotlibV2);
        Assert.Equal(0, drawn.YMin);
        Assert.Equal(0.2550660029400389, drawn.YMax, 12);

        var library = kde.DataRangeUnder(context, Theme.Default);
        Assert.Equal(kde.ComputeDataRange(context), library);
        Assert.Equal(libraryPeak * 1.05, library.YMax!.Value, 12);
        Assert.Null(library.StickyYMin);
    }

    /// <summary>seaborn's <c>rugplot</c> asks <c>ax.margins(x=xmarg, y=ymarg + 2 * height)</c>, whose <c>tight=True</c> ends both axes where
    /// the data and the margin end, and draws its ticks in the axes' own height (a <c>LineCollection</c> on <c>blended(transData,
    /// transAxes)</c>), which adds nothing to the Y range. Printed for the sample 1.3, 2, 2, 3, 4.6 with <c>height=0.3</c> and
    /// <c>ax.set_ylim(0, 1)</c>: xlim (1.3, 4.6) classic (no tick rounds it out), (1.135, 4.765) default; for 1, 2, 2, 3, 5 over a line from (0, 0) to (10, 10) with <c>height=0.1</c>:
    /// ylim (-2, 12) classic, (-2.5, 12.5) default (the margin raised by 0.2), xlim (0, 10) and (-0.5, 10.5).</summary>
    [Theory]
    [InlineData("MatplotlibClassic", 1.3, 4.6)]
    [InlineData("MatplotlibV2", 1.135, 4.765)]
    public void ARugsAxes_EndWhereTheDataAndTheMarginEnd(string themeName, double xMin, double xMax)
    {
        var axes = new Axes();
        axes.Rugplot(new double[] { 1.3, 2, 2, 3, 4.6 }).Height = 0.3;
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;

        var range = axes.ComputeDataRanges(themeName.AsTheme());
        Assert.Equal(xMin, range.XMin, 12);
        Assert.Equal(xMax, range.XMax, 12);
    }

    [Theory]
    [InlineData("MatplotlibClassic", 0.0, 10.0, -2.0, 12.0)]
    [InlineData("MatplotlibV2", -0.5, 10.5, -2.5, 12.5)]
    public void ARug_RaisesTheMarginAcrossIt_ByTwiceItsHeight(string themeName, double xMin, double xMax, double yMin, double yMax)
    {
        var axes = new Axes();
        axes.Plot([0.0, 10], [0.0, 10]);
        axes.Rugplot(new double[] { 1.0, 2, 2, 3, 5 }).Height = 0.1;

        var range = axes.ComputeDataRanges(themeName.AsTheme());
        Assert.Equal(new DataRange(xMin, xMax, yMin, yMax), range with { XMin = Math.Round(range.XMin, 12), XMax = Math.Round(range.XMax, 12), YMin = Math.Round(range.YMin, 12), YMax = Math.Round(range.YMax, 12) });
    }

    /// <summary>A density curve drawn alone is ranged as its line is (matplotlib's data limits: from its lowest density to its peak),
    /// held at 0 below by its sticky base; with a rug of height 0.3 the margin above is the axes' and 0.6. Printed: ylim (0,
    /// 0.38824926050471564) classic, (0, 0.4003568293742317) default, the curve's grid from -2.2975483484297237 (tight under classic),
    /// xlim from -2.8273031832726963 under the default style.</summary>
    [Theory]
    [InlineData("MatplotlibClassic", -2.2975483484297237, 0.38824926050471564)]
    [InlineData("MatplotlibV2", -2.8273031832726963, 0.4003568293742317)]
    public void ADensityWithARug_IsRangedAsSeaborns(string themeName, double xMin, double yMax)
    {
        var axes = new Axes();
        axes.Kde([1.0, 2, 2, 3, 5]);
        axes.Rugplot(new double[] { 1.0, 2, 2, 3, 5 }).Height = 0.3;

        var range = axes.ComputeDataRanges(themeName.AsTheme());
        Assert.Equal(xMin, range.XMin, 9);
        Assert.Equal(0, range.YMin);
        Assert.Equal(yMax, range.YMax, 9);
    }

    /// <summary>Under the library's own themes a rug asks nothing of its axes and ranges its ticks from 0 to 1 (or its height), as it
    /// always has.</summary>
    [Fact]
    public void ARug_KeepsTheLibrarysRangeUnderItsThemes()
    {
        var axes = new Axes();
        var rug = axes.Rugplot(new double[] { 1.0, 2, 2, 3, 5 });
        var context = new AxesContextAdapter(axes);

        Assert.Equal(rug.ComputeDataRange(context), rug.DataRangeUnder(context, Theme.Default));
        Assert.Equal(default, axes.AskUnder(Theme.Default));
        Assert.Equal(1, rug.DataRangeUnder(context, Theme.Default).YMax);
        Assert.Null(rug.DataRangeUnder(context, Theme.MatplotlibV2).YMax);
    }

    private static readonly double[] ResidualX = [0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly double[] ResidualY = [0.3, -0.2, 0.5, 1.1, -0.7, 0.2, -0.4, 0.9, -1.2, 0.1, 0.6];

    private static RecordingRenderContext DrawnResiduals(Theme theme)
    {
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Residplot(ResidualX, ResidualY)).Build(), drawn);
        return drawn;
    }

    /// <summary>seaborn's <c>residplot</c> draws its dots with <c>ax.scatter(color=c, alpha=.8, linewidths=lines.markeredgewidth)</c>
    /// (printed face = edge = the cycle's colour at alpha 0.8, linewidths [1.0] default, [0.5] classic) and its zero line with
    /// <c>ax.axhline(0, ls=':', c='.2')</c> at <c>lines.linewidth</c> (printed 1.5 default, 1.0 classic). The library's own themes draw
    /// the dots opaque and unedged and the zero line dashed, in the dots' colour at half opacity, one pixel wide.</summary>
    [Theory]
    [InlineData("MatplotlibV2", 1.0, 1.5)]
    [InlineData("MatplotlibClassic", 0.5, 1.0)]
    public void AResidualPlot_IsDrawnAsSeaborns(string themeName, double edgePoints, double zeroPoints)
    {
        var drawn = DrawnResiduals(themeName.AsTheme());

        var dots = drawn.Of<DrawCircleCall>().ToList();
        Assert.Equal(ResidualX.Length, dots.Count);
        Assert.All(dots, dot =>
        {
            Assert.Equal(204, (dot.Fill!.Value).A);
            Assert.Equal(dot.Fill!.Value, dot.Stroke);
            Assert.Equal(edgePoints.PointsInPixels, dot.StrokeThickness, 9);
        });
        var zero = Assert.Single(drawn.Of<DrawLineCall>(), line => line.Color == Color.FromHex("#333333"));
        Assert.Equal(LineStyle.Dotted, zero.Style);
        Assert.Equal(zeroPoints.PointsInPixels, zero.Thickness, 9);
    }

    [Fact]
    public void AResidualPlot_KeepsTheLibrarysLookUnderItsThemes()
    {
        var drawn = DrawnResiduals(Theme.Default);
        var dot = drawn.Of<DrawCircleCall>()[0];
        var colour = dot.Fill!.Value;

        Assert.Equal(255, colour.A);
        Assert.Null(dot.Stroke);
        var zero = Assert.Single(drawn.Of<DrawLineCall>(), line => line.Color == colour.WithOpacityGiven(0.5));
        Assert.Equal(LineStyle.Dashed, zero.Style);
        Assert.Equal(1.0, zero.Thickness, 9);
    }

    /// <summary>seaborn's residual plot is ranged as its scatter and its zero line are (matplotlib's data limits, the scatter's 5 % margin
    /// under classic, no padding of its own). Printed for x 0 .. 10 and the residuals of the sample: xlim (-2, 12) and ylim (-1.5, 1.5)
    /// classic, (-0.5, 10.5) and (-1.3502272727272726, 1.0547727272727274) default.</summary>
    [Theory]
    [InlineData("MatplotlibClassic", -2.0, 12.0, -1.5, 1.5)]
    [InlineData("MatplotlibV2", -0.5, 10.5, -1.3502272727272726, 1.0547727272727274)]
    public void AResidualPlot_IsRangedAsSeaborns(string themeName, double xMin, double xMax, double yMin, double yMax)
    {
        var axes = new Axes();
        axes.Residplot(ResidualX, ResidualY);

        var range = axes.ComputeDataRanges(themeName.AsTheme());
        Assert.Equal(xMin, range.XMin, 9);
        Assert.Equal(xMax, range.XMax, 9);
        Assert.Equal(yMin, range.YMin, 9);
        Assert.Equal(yMax, range.YMax, 9);
    }

    /// <summary>A density curve given a fill under seaborn's look is ranged from the 0 its fill stands on (<c>fill_between</c>'s data limits
    /// reach the base), not from the line's lowest density.</summary>
    [Fact]
    public void AFilledDensityCurve_IsRangedFromZero_UnderSeaborn()
    {
        var axes = new Axes();
        var kde = axes.Kde([1.0, 2, 2, 3, 5]);
        kde.Fill = true;

        Assert.Equal(0, kde.DataRangeUnder(new AxesContextAdapter(axes), Theme.MatplotlibV2).YMin);
    }

    /// <summary>A residual plot's dots are edged at the theme's marker edge width; a theme whose dot look edges them but names no marker
    /// edge draws them unedged.</summary>
    [Fact]
    public void AResidualPlotsDots_AreUnedged_WhereTheThemeNamesNoMarkerEdge()
    {
        var noEdge = new Theme("no marker edge", Theme.MatplotlibV2)
        {
            Looks = Theme.MatplotlibV2.Looks with { Markers = Theme.MatplotlibV2.Looks.Markers with { LineEdge = null } },
        };

        var dot = DrawnResiduals(noEdge).Of<DrawCircleCall>()[0];
        Assert.Null(dot.Stroke);
        Assert.Equal(204, (dot.Fill!.Value).A);
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheVerbLook_TravelsThroughTheThemeJson(string themeName)
    {
        var theme = themeName.AsTheme();

        Assert.Same(theme.Looks.Verbs, theme.ThroughDto().Looks.Verbs);
    }
}
