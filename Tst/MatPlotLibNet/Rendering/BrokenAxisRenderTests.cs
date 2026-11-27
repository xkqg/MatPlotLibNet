// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that axis breaks render correctly in SVG output.</summary>
public class BrokenAxisRenderTests
{
    private static double[] MakeX() => Enumerable.Range(0, 20).Select(i => (double)i * 10).ToArray();
    private static double[] MakeY() => Enumerable.Range(0, 20).Select(i => (double)i).ToArray();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Zigzag markers produce line elements
        new DrawnToken("XBreak_Zigzag_ContainsMarkerLines", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithXBreak(80, 120, BreakStyle.Zigzag);
            })
            .ToSvg(), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The mark of every break stands where the drawn transform puts the break's start, the place the data jumps:
    /// with breaks (30, 50) and (80, 120) the second mark used to stand at the pixel of 60 (the first gap taken off twice).</summary>
    [Fact]
    public void TwoXBreaks_TheSecondMark_StandsWhereTheDataJumps()
    {
        var axes = new Axes();
        axes.Plot(Enumerable.Range(0, 151).Select(i => (double)i).ToArray(), Enumerable.Range(0, 151).Select(i => i * 0.5).ToArray());
        axes.AddXBreak(30, 50);
        axes.AddXBreak(80, 120);
        var plot = new Rect(80, 60, 640, 480);
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(axes, plot, ctx, Theme.Default);

        renderer.Render();

        var gaps = ctx.Rects()
            .Where(r => r.Width == 8 && r.Height == plot.Height).ToArray();
        Assert.Equal(2, gaps.Length);
        Assert.Equal(renderer.DrawnTransform!.DataToPixel(80, 0).X, gaps[1].X + 4, 9);
    }
}
