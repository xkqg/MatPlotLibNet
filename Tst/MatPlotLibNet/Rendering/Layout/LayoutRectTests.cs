// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The rectangle a measured layout fits the subplots into (matplotlib's <c>tight_layout(rect=...)</c>): fractions of the
/// figure counted from its bottom-left corner. The layout works out what the subplots, their labels and the figure's title need,
/// then keeps the figure outside the rectangle free on top of that.</summary>
[Collection("ChartSerializerGlobalState")]
public sealed class LayoutRectTests
{
    private static Figure Titled(Func<FigureBuilder, FigureBuilder> layout) =>
        layout(Plt.Create().WithSize(640, 480).WithTitle("A title of the figure"))
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("panel").SetXLabel("x").SetYLabel("y").Plot([1.0, 2.0, 3.0], [2.0, 4.0, 3.0]))
            .Build();

    private static SubPlotSpacing SpacingOf(Figure figure) => new ChartRenderer().FrameOf(figure, new SvgRenderContext()).Spacing;

    [Fact]
    public void ALayoutRect_HoldsItsFourFractions()
    {
        var rect = new LayoutRect(0.05, 0.1, 0.9, 0.93);

        Assert.Equal((0.05, 0.1, 0.9, 0.93), (rect.Left, rect.Bottom, rect.Right, rect.Top));
        Assert.Equal(new LayoutRect(0.05, 0.1, 0.9, 0.93), rect);
    }

    [Theory]
    [InlineData(-0.1, 0, 1, 1)]
    [InlineData(0, -0.1, 1, 1)]
    [InlineData(0, 0, 1.1, 1)]
    [InlineData(0, 0, 1, 1.1)]
    [InlineData(0.5, 0, 0.5, 1)]
    [InlineData(0.6, 0, 0.5, 1)]
    [InlineData(0, 0.5, 1, 0.5)]
    [InlineData(double.NaN, 0, 1, 1)]
    public void ALayoutRect_RefusesFractionsThatAreNoRectangleInTheFigure(double left, double bottom, double right, double top)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutRect(left, bottom, right, top));
    }

    /// <summary>matplotlib 3.11.2, 640 by 480 under the default style, an axes under a suptitle: <c>tight_layout()</c> ends the
    /// axes at 0.853 of the height and <c>tight_layout(rect=[0, 0, 1, 0.93])</c> at 0.783, so the top 7 % is kept free on top of
    /// the band the title takes; without the title 0.925 and 0.855.</summary>
    [Fact]
    public void ARectThatEndsAt93Percent_KeepsTheTop7PercentFree_OnTopOfTheTitle()
    {
        var whole = SpacingOf(Titled(b => b.TightLayout()));
        var rect = SpacingOf(Titled(b => b.TightLayout(new LayoutRect(0, 0, 1, 0.93))));

        Assert.Equal(0.07 * 480, rect.MarginTop - whole.MarginTop, 9);
        Assert.Equal(whole.MarginBottom, rect.MarginBottom, 9);
        Assert.Equal(whole.MarginLeft, rect.MarginLeft, 9);
        Assert.Equal(whole.MarginRight, rect.MarginRight, 9);
    }

    [Fact]
    public void EachSideOfTheRect_KeepsItsShareOfTheFigureFree()
    {
        var whole = SpacingOf(Titled(b => b.TightLayout()));
        var rect = SpacingOf(Titled(b => b.TightLayout(new LayoutRect(0.05, 0.1, 0.9, 1))));

        Assert.Equal(0.05 * 640, rect.MarginLeft - whole.MarginLeft, 9);
        Assert.Equal(0.1 * 480, rect.MarginBottom - whole.MarginBottom, 9);
        Assert.Equal(0.1 * 640, rect.MarginRight - whole.MarginRight, 9);
        Assert.Equal(whole.MarginTop, rect.MarginTop, 9);
    }

    [Fact]
    public void TheConstrainedLayout_KeepsTheRectToo()
    {
        var whole = SpacingOf(Titled(b => b.ConstrainedLayout()));
        var rect = SpacingOf(Titled(b => b.ConstrainedLayout().WithSubPlotSpacing(s => s with { LayoutRect = new LayoutRect(0, 0, 1, 0.9) })));

        Assert.Equal(0.1 * 480, rect.MarginTop - whole.MarginTop, 9);
    }

    /// <summary>The rect is a setting of the measured layouts, as matplotlib's is of <c>tight_layout</c>: the default layout's
    /// margins are the author's, so it reads none.</summary>
    [Fact]
    public void TheDefaultLayout_ReadsNoRect()
    {
        var plain = SpacingOf(Titled(b => b.WithSubPlotSpacing(s => s with { MarginLeft = 61 })));
        var rect = SpacingOf(Titled(b => b.WithSubPlotSpacing(s => s with { MarginLeft = 61, LayoutRect = new LayoutRect(0, 0, 1, 0.5) })));

        Assert.Equal(plain with { LayoutRect = rect.LayoutRect }, rect);
    }

    [Fact]
    public void TightLayoutWithARect_SwitchesTheTightLayoutOn_AndKeepsTheOtherSpacing()
    {
        var figure = Plt.Create().WithSubPlotSpacing(s => s with { HorizontalGap = 7 }).TightLayout(new LayoutRect(0, 0, 1, 0.93)).Build();

        Assert.True(figure.Spacing.TightLayout);
        Assert.Equal(new LayoutRect(0, 0, 1, 0.93), figure.Spacing.LayoutRect);
        Assert.Equal(7, figure.Spacing.HorizontalGap);
    }

    [Fact]
    public void ALayoutRect_TravelsThroughJson_AsMatplotlibsFourNumbers()
    {
        var serializer = new ChartSerializer();
        var figure = Plt.Create().TightLayout(new LayoutRect(0.05, 0.1, 0.9, 0.93)).Build();

        string json = serializer.ToJson(figure);
        using var document = JsonDocument.Parse(json);

        Assert.Equal([0.05, 0.1, 0.9, 0.93], document.RootElement.GetProperty("spacing").GetProperty("layoutRect").EnumerateArray().Select(n => n.GetDouble()));
        Assert.Equal(new LayoutRect(0.05, 0.1, 0.9, 0.93), serializer.FromJson(json).Spacing.LayoutRect);
        Assert.DoesNotContain("layoutRect", serializer.ToJson(Plt.Create().TightLayout().Build()));
    }

    [Theory]
    [InlineData("[0, 0, 1]")]
    [InlineData("[0, 0, 1, 0.9, 1]")]
    [InlineData("[0, 0, 0, 0.9]")]
    public void ALayoutRectThatIsNoRectangle_IsRefusedByItsField(string rect)
    {
        var serializer = new ChartSerializer();

        var refusal = Assert.ThrowsAny<ArgumentException>(() =>
            serializer.FromJson($$"""{"width":400,"height":300,"spacing":{"tightLayout":true,"layoutRect":{{rect}}},"subPlots":[]}"""));

        Assert.Contains("layoutRect", refusal.Message + refusal.ParamName);
    }
}
