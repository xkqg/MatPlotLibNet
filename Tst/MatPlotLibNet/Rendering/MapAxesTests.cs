// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series that draws a map (<see cref="IMapSeries"/>) makes its axes a map axes, as cartopy's <c>GeoAxes</c> is: one unit
/// of Y as long as one of X unless the axes was given an aspect of its own, and the map's own limits, which no theme rounds out.
/// The Geo package's map series is the one the library ships; this map of the box 0..400 x 0..100 is the rule's own case.</summary>
public class MapAxesTests
{
    /// <summary>The box 0..<paramref name="x"/> by 0..<paramref name="y"/>, sticky on its edges as the Geo package's map is.</summary>
    private static DataRangeContribution Sticky(double x, double y) => new(0, x, 0, y, StickyXMin: 0, StickyXMax: x, StickyYMin: 0, StickyYMax: y);

    /// <summary>A line from the plot's top-left corner to its bottom-right one and back.</summary>
    private static IReadOnlyList<Point> Corners(DataTransform transform) =>
        [new(transform.PlotBounds.X, transform.PlotBounds.Y), new(transform.PlotBounds.Right, transform.PlotBounds.Bottom), new(transform.PlotBounds.X, transform.PlotBounds.Y)];

    /// <summary>A map of the box 0..400 by 0..100 whose outline is a diamond through the middles of the plot's sides.</summary>
    private static TestMapSeries BoxMap => new(Sticky(400, 100), transform =>
    {
        var plot = transform.PlotBounds;
        return
        [
            new(plot.Center.X, plot.Y), new(plot.Right, plot.Center.Y), new(plot.Center.X, plot.Bottom), new(plot.X, plot.Center.Y),
            new(plot.Center.X, plot.Y),
        ];
    });

    /// <summary>The outline of a map is placed by the range the axes show, as the map is: its own limits at rest, the limits an author
    /// set (a zoom or a pan) otherwise.</summary>
    [Theory]
    [InlineData(false, 0, 400, 0, 100)]
    [InlineData(true, 100, 200, 20, 60)]
    public void TheOutline_IsPlacedByTheRangeTheAxesShow(bool limited, double xMin, double xMax, double yMin, double yMax)
    {
        var map = new TestMapSeries(Sticky(400, 100), Corners);
        var figure = Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax =>
        {
            ax.HideAllAxes();
            if (limited)
            {
                ax.SetXLim(xMin, xMax);
                ax.SetYLim(yMin, yMax);
            }
        }).Build();
        figure.SubPlots[0].AddSeries(map);

        figure.ToSvg();

        Assert.Equal(new DataRange(xMin, xMax, yMin, yMax), map.Asked);
    }

    private static string Svg(Theme theme)
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.HideAllAxes()).Build();
        figure.SubPlots[0].AddSeries(BoxMap);
        return figure.ToSvg();
    }

    /// <summary>Under the matplotlib themes a map axes is framed as cartopy frames a <c>GeoAxes</c>: its background is the map's
    /// outline (<c>ax.patch</c>), and the outline is drawn over the map in the theme's spine (<c>spines['geo']</c>, black, 0.8 pt
    /// under the default style).</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheOutlineIsTheBackgroundAndTheSpine()
    {
        string svg = Svg(Theme.MatplotlibV2);

        var diamonds = svg.Elements("polygon").Where(polygon => polygon.Points().Length == 5).ToArray();

        Assert.Equal(2, diamonds.Length);
        Assert.Equal("#FFFFFF", diamonds[0]["fill"]);
        Assert.Equal("#000000", diamonds[1]["stroke"]);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("UnderTheLibrarysThemes_AMapAxesKeepsItsRectangle", () => Svg(Theme.Default)) { Absent = ["<polygon"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    private static Axes MapAxes(double? aspect = null)
    {
        var axes = new Axes { Aspect = aspect };
        axes.AddSeries(BoxMap);
        return axes;
    }

    [Fact]
    public void AMapAxes_KeepsEqualUnits_CentredInItsCell()
    {
        var fitted = MapAxes().FittedTo(new Rect(0, 0, 800, 600), Theme.Default);

        Assert.Equal(new Rect(0, 200, 800, 200), fitted);
    }

    [Fact]
    public void AnAspectOfItsOwn_Wins()
    {
        var fitted = MapAxes(aspect: 2).FittedTo(new Rect(0, 0, 800, 600), Theme.Default);

        Assert.Equal(new Rect(0, 100, 800, 400), fitted);
    }

    [Fact]
    public void AnAxesWithoutAMap_KeepsItsCell()
    {
        var axes = new Axes();
        axes.Plot([0.0, 400.0], [0.0, 100.0]);

        Assert.Null(axes.Map);
        Assert.Equal(new Rect(0, 0, 800, 600), axes.FittedTo(new Rect(0, 0, 800, 600), Theme.Default));
    }

    /// <summary>Measured before the change: the classic theme rounded the axes out to its ticks, 0..400 by 0..100 becoming
    /// 0..400 by 0..100 only by luck of the numbers; a map of 0..390 by 0..95 became 0..400 by 0..100.</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [InlineData("default")]
    public void TheLimits_AreTheMapsOwn_UnderEveryTheme(string themeId)
    {
        var theme = themeId.AsTheme();
        var axes = new Axes();
        axes.AddSeries(new TestMapSeries(Sticky(390, 95), Corners));
        axes.Scatter([10.0], [20.0]);

        var range = axes.ComputeDataRanges(theme);

        Assert.Equal(new DataRange(0, 390, 0, 95), range);
        Assert.True(axes.AskUnder(theme).Tight);
    }
}
