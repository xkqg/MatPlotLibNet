// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.TestFixtures;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="CountSeries"/> rendering.</summary>
public class CountSeriesRenderTests
{
    private static readonly string[] Values = ["a", "b", "a", "c", "a", "b"];

    /// <summary>The cycle's first blue, given, so the bars are found by it under any theme (a count plot's own colour is desaturated
    /// under the matplotlib themes, <c>SeabornVerbLookTests</c>).</summary>
    private static readonly MatPlotLibNet.Styling.Color Blue = MatPlotLibNet.Styling.Color.FromHex("#1F77B4");

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Countplot_SvgContainsRectangles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values))
            .ToSvg(), "<rect"),
        // CountSeriesRenderer.Render: else-branch (Horizontal orientation)
        new DrawnToken("Countplot_HorizontalOrientation_RendersRects", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values, s =>
            {
                s.Orientation = BarOrientation.Horizontal;
            }))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Branch coverage ──────────────────────────────────────────────────────

    /// <summary>A count plot writes each category's name under the centre of its bar: the bars stand in their categories'
    /// slots (CategorySlot), where the axis writes the names. Measured before the fix in a 400 px figure: bars centred at
    /// 101.7, 205 and 308.3 px, names at 153.3, 256.7 and 360 px, the last on the right spine.</summary>
    [Fact]
    public void Countplot_NamesEachCategoryUnderTheCentreOfItsBar()
    {
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["cat", "dog", "dog", "eel", "eel", "eel"], s => s.Color = Blue))
            .ToSvg();

        var barCentres = svg.RectsFilled("#1F77B4").Select(rect => rect.X + rect.Width / 2).ToArray();
        var nameXs = new[] { "cat", "dog", "eel" }.Select(name => svg.TextReading(name).Number("x")).ToArray();

        Assert.Equal(3, barCentres.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(nameXs[i], barCentres[i], SvgMarkupExtensions.WrittenPrecision);
        }
    }

    /// <summary>A count plot spans the slots of its categories, 0 to the number of categories, sticky at both ends: each bar
    /// keeps the margin of a tenth of a slot to its neighbour and to the spine.</summary>
    [Fact]
    public void Countplot_RangesOverTheSlotsOfItsCategories()
    {
        var range = new CountSeries(["cat", "dog", "dog", "eel", "eel", "eel"]).ComputeDataRange(new TestAxesContext());

        Assert.Equal(CategorySlot.Of(0).Left, range.XMin);
        Assert.Equal(CategorySlot.Of(2).Right, range.XMax);
        Assert.Equal(range.XMin, range.StickyXMin);
        Assert.Equal(range.XMax, range.StickyXMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax);
        Assert.Equal(0, range.StickyYMin);   // the bars stand on 0, as every bar does
    }

    /// <summary>A horizontal count plot ranges its categories' slots along Y and its counts along X, the range a vertical
    /// one reports on the other axes.</summary>
    [Fact]
    public void AHorizontalCountplot_RangesItsCategoriesAlongY_AndItsCountsAlongX()
    {
        var range = new CountSeries(["cat", "dog", "dog", "eel", "eel", "eel"]) { Orientation = BarOrientation.Horizontal }
            .ComputeDataRange(new TestAxesContext());

        Assert.Equal(CategorySlot.Of(0).Left, range.YMin);
        Assert.Equal(CategorySlot.Of(2).Right, range.YMax);
        Assert.Equal(range.YMin, range.StickyYMin);
        Assert.Equal(range.YMax, range.StickyYMax);
        Assert.Equal(0, range.XMin);
        Assert.Equal(3, range.XMax);
        Assert.Equal(0, range.StickyXMin);   // the bars' foot, lying down
    }

    /// <summary>A horizontal count plot draws each count as a bar along X and names its category on the Y axis, level with
    /// the bar.</summary>
    [Fact]
    public void AHorizontalCountplot_NamesEachCategoryOnTheYAxis_LevelWithItsBar()
    {
        string[] names = ["cat", "dog", "eel"];
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["cat", "dog", "dog", "eel", "eel", "eel"], s => { s.Orientation = BarOrientation.Horizontal; s.Color = Blue; }))
            .ToSvg();

        var bars = SeriesRenderers.BarSeriesRendererTests.Bars(svg);
        var plot = svg.PlotArea();

        Assert.Equal(3, bars.Length);
        // Each width is written to the hundredth (half a step off at most), so k times the first is k + 1 half steps off at most.
        Assert.Equal(2 * bars[0].W, bars[1].W, 1.5 * SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(3 * bars[0].W, bars[2].W, 2 * SvgMarkupExtensions.WrittenPrecision);
        Assert.All(bars, b => Assert.InRange(b.X + b.W, plot.X, plot.X + plot.Width + 1e-6));
        for (int i = 0; i < 3; i++)
        {
            var name = Assert.Single(SeriesRenderers.BarSeriesRendererTests.Texts(svg, names[i]));
            Assert.Equal(bars[i].Y + bars[i].H / 2 + 0.35 * MatPlotLibNet.Styling.Theme.MatplotlibV2.TickFont().Size, name.Y, SvgMarkupExtensions.WrittenPrecision);   // centred on the bar
            Assert.True(name.X < plot.X, $"{names[i]} stands left of the Y spine");
        }
    }
}
