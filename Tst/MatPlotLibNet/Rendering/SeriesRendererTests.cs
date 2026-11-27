// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="SeriesRenderContext"/> behavior.</summary>
public class SeriesRendererTests
{
    /// <summary>Verifies that SeriesRenderContext is a record supporting immutable with-expressions.</summary>
    [Fact]
    public void SeriesRenderContext_IsRecord_SupportsWithExpression()
    {
        var ctx = CreateContext();
        var modified = ctx with { TooltipsEnabled = true };
        Assert.True(modified.TooltipsEnabled);
        Assert.False(ctx.TooltipsEnabled); // original unchanged (immutable)
    }

    /// <summary>Verifies that SeriesRenderContext requires all four constructor parameters to be non-default.</summary>
    [Fact]
    public void SeriesRenderContext_RequiredFourParams()
    {
        var ctx = CreateContext();
        Assert.NotNull(ctx.Transform);
        Assert.NotNull(ctx.Ctx);
        Assert.NotEqual(default, ctx.SeriesColor);
        Assert.NotEqual(default, ctx.Area);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that rendering a figure with multiple series types still produces valid SVG after refactoring.
        // Integration: render a figure with multiple series types, verify SVG output
        // line
        // scatter
        // bar
        new DrawnToken("ExistingTests_StillPass_AfterRefactor", () => Plt.Create()
            .WithTitle("Refactor Test")
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Scatter([1.0, 2.0], [12.0, 18.0])
            .Bar(["A", "B"], [5.0, 10.0])
            .ToSvg(), "<svg", "<polyline", "<circle", "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    private static SeriesRenderContext CreateContext()
    {
        var plotArea = new Rect(60, 40, 700, 500);
        var transform = new DataTransform(0, 10, 0, 100, plotArea);
        var svgCtx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var area = new RenderArea(plotArea, svgCtx);
        return new SeriesRenderContext(transform, svgCtx, Colors.Blue, area);
    }
}
