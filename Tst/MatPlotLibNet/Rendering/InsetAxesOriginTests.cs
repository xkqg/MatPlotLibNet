// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where an inset stands: its Y is the distance from the parent's top edge down to the inset's top edge, as a
/// fraction of the parent's height (0 is the top, as <see cref="InsetBounds"/> says), which is what the renderer does and
/// unlike matplotlib's <c>inset_axes</c>, which counts up from the bottom. The documentation of every way to add an inset
/// says the same.</summary>
public class InsetAxesOriginTests
{
    private static readonly Rect ParentArea = new(100, 100, 400, 200);

    /// <summary>The rectangles drawn when the subplot of <paramref name="figure"/> is drawn into <see cref="ParentArea"/>.</summary>
    private static List<Rect> DrawnRectangles(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().RenderAxes(figure, figure.SubPlots[0], new RenderPass(ParentArea, ctx, figure.Theme));
        return [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)((dynamic)c.Args!).rect)];
    }

    private static Figure WithInset(double x, double y, double width, double height)
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([0.0, 1.0], [0.0, 1.0]);
        axes.InsetAxes(x, y, width, height).Plot([0.0, 1.0], [0.0, 1.0]);
        return figure;
    }

    [Fact]
    public void AnInsetAtYZero_HasItsTopEdgeOnTheParentsTopEdge()
    {
        var rectangles = DrawnRectangles(WithInset(0.25, 0, 0.5, 0.25));

        Assert.Contains(new Rect(200, 100, 200, 50), rectangles);
    }

    [Fact]
    public void AnInsetAtYOneLessItsHeight_HasItsBottomEdgeOnTheParentsBottomEdge()
    {
        var rectangles = DrawnRectangles(WithInset(0.25, 0.75, 0.5, 0.25));

        Assert.Contains(new Rect(200, 250, 200, 50), rectangles);
    }

    /// <summary>The documentation of Y on each way to add an inset by four numbers says it counts down from the top.</summary>
    [Theory]
    [InlineData("M:MatPlotLibNet.Models.Axes.InsetAxes(System.Double,System.Double,System.Double,System.Double)")]
    [InlineData("M:MatPlotLibNet.Models.Axes.AddInset(System.Double,System.Double,System.Double,System.Double)")]
    public void TheDocumentedY_IsTheTopEdge_CountedDownFromTheParentsTop(string member)
    {
        var documentation = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MatPlotLibNet.xml"));
        var y = documentation.Descendants("member")
            .Single(m => (string?)m.Attribute("name") == member)
            .Elements("param").Single(p => (string?)p.Attribute("name") == "y").Value;

        Assert.Contains("top edge", y, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("down", y, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bottom edge", y, StringComparison.OrdinalIgnoreCase);
    }
}
