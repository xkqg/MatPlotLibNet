// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="CountSeries"/> rendering.</summary>
public class CountSeriesRenderTests
{
    private static readonly string[] Values = ["a", "b", "a", "c", "a", "b"];

    [Fact]
    public void Countplot_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Countplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Countplot(Values)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Branch coverage ──────────────────────────────────────────────────────

    [Fact]
    public void Countplot_EmptyValues_ReturnsDefaultDataRange()
    {
        // CountSeries.ComputeDataRange: Values.Length == 0 branch
        // CountSeriesRenderer.Render: Values.Length == 0 early-return branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot([]))
            .ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    /// <summary>A count plot writes each category's name under the centre of its bar: the bars stand in their categories'
    /// slots (CategorySlot), where the axis writes the names. Measured before the fix in a 400 px figure: bars centred at
    /// 101.7, 205 and 308.3 px, names at 153.3, 256.7 and 360 px, the last on the right spine.</summary>
    [Fact]
    public void Countplot_NamesEachCategoryUnderTheCentreOfItsBar()
    {
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["cat", "dog", "dog", "eel", "eel", "eel"]))
            .ToSvg();

        var barCentres = System.Text.RegularExpressions.Regex
            .Matches(svg, @"<rect x=""([0-9.eE+-]+)"" y=""[0-9.eE+-]+"" width=""([0-9.eE+-]+)"" height=""[0-9.eE+-]+"" fill=""#1F77B4""")
            .Select(m => Number(m.Groups[1].Value) + Number(m.Groups[2].Value) / 2)
            .ToArray();
        var nameXs = new[] { "cat", "dog", "eel" }
            .Select(name => Number(System.Text.RegularExpressions.Regex.Match(svg, $@"<text[^>]*?x=""([-0-9.]+)""[^>]*>{name}</text>").Groups[1].Value))
            .ToArray();

        Assert.Equal(3, barCentres.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(nameXs[i], barCentres[i], 6);
        }
    }

    /// <summary>A count plot spans the slots of its categories, 0 to the number of categories, sticky at both ends: each bar
    /// keeps the margin of a tenth of a slot to its neighbour and to the spine.</summary>
    [Fact]
    public void Countplot_RangesOverTheSlotsOfItsCategories()
    {
        var range = new CountSeries(["cat", "dog", "dog", "eel", "eel", "eel"]).ComputeDataRange(new NoLimits());

        Assert.Equal(CategorySlot.Of(0).Left, range.XMin);
        Assert.Equal(CategorySlot.Of(2).Right, range.XMax);
        Assert.Equal(range.XMin, range.StickyXMin);
        Assert.Equal(range.XMax, range.StickyXMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax);
    }

    private static double Number(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    private sealed class NoLimits : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    /// <summary>A horizontal count plot ranges its categories' slots along Y and its counts along X, the range a vertical
    /// one reports on the other axes.</summary>
    [Fact]
    public void AHorizontalCountplot_RangesItsCategoriesAlongY_AndItsCountsAlongX()
    {
        var range = new CountSeries(["cat", "dog", "dog", "eel", "eel", "eel"]) { Orientation = BarOrientation.Horizontal }
            .ComputeDataRange(new NoLimits());

        Assert.Equal(CategorySlot.Of(0).Left, range.YMin);
        Assert.Equal(CategorySlot.Of(2).Right, range.YMax);
        Assert.Equal(range.YMin, range.StickyYMin);
        Assert.Equal(range.YMax, range.StickyYMax);
        Assert.Equal(0, range.XMin);
        Assert.Equal(3, range.XMax);
        Assert.Null(range.StickyXMin);
    }

    /// <summary>A horizontal count plot draws each count as a bar along X and names its category on the Y axis, level with
    /// the bar.</summary>
    [Fact]
    public void AHorizontalCountplot_NamesEachCategoryOnTheYAxis_LevelWithItsBar()
    {
        string[] names = ["cat", "dog", "eel"];
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["cat", "dog", "dog", "eel", "eel", "eel"], s => s.Orientation = BarOrientation.Horizontal))
            .ToSvg();

        var bars = SeriesRenderers.BarSeriesRendererTests.Bars(svg);
        var plot = SeriesRenderers.BarSeriesRendererTests.PlotArea(svg);

        Assert.Equal(3, bars.Length);
        Assert.Equal(2 * bars[0].W, bars[1].W, 6);
        Assert.Equal(3 * bars[0].W, bars[2].W, 6);
        Assert.All(bars, b => Assert.InRange(b.X + b.W, plot.X, plot.X + plot.W + 1e-6));
        for (int i = 0; i < 3; i++)
        {
            var name = Assert.Single(SeriesRenderers.BarSeriesRendererTests.Texts(svg, names[i]));
            Assert.Equal(bars[i].Y + bars[i].H / 2 + 4, name.Y, 6);
            Assert.True(name.X < plot.X, $"{names[i]} stands left of the Y spine");
        }
    }

    [Fact]
    public void Countplot_HorizontalOrientation_RendersRects()
    {
        // CountSeriesRenderer.Render: else-branch (Horizontal orientation)
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values, s =>
            {
                s.Orientation = BarOrientation.Horizontal;
            }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }
}
