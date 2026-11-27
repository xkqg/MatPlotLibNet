// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A treemap under the matplotlib themes is drawn as squarify draws one in a matplotlib axes: its tiles are bars
/// (<c>ax.bar(x, dy, width=dx, bottom=y, align='edge', alpha=0.8)</c>, laid out from y = 0 in a Y that runs up, so the largest tile
/// stands at the bottom left) and take the patch edge of a bar, black at 1 pt under classic (<c>patch.force_edgecolor</c>) and none
/// under the default style; the library's own themes lay it out from the top left, each tile edged in a white pixel.</summary>
public class TreemapThemeTests
{
    // Seven tiles in an area wider than it is high: the largest two share the left column, so the largest is not the whole height of it.
    private static readonly TreeNode Tree = new()
    {
        Color = Colors.Transparent,
        Children =
        [
            new TreeNode { Value = 6, Color = Colors.Red }, new TreeNode { Value = 6, Color = Colors.Blue },
            .. new double[] { 4, 3, 2, 2, 1 }.Select(v => new TreeNode { Value = v, Color = Colors.Green }),
        ],
    };

    private static Figure Of(Theme theme) => Plt.Create().WithSize(600, 400).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Treemap(Tree, s => { s.Padding = 0; s.ShowLabels = false; })).Build();

    /// <summary>The leaf tiles as drawn: each rectangle that is not the root's (the first one).</summary>
    private static List<(Rect Rect, ShapeStyle Style)> Tiles(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Of<DrawRectangleCall>()
            .Where(args => (args.Shape).Fill == Colors.Red || (args.Shape).Fill == Colors.Blue || (args.Shape).Fill == Colors.Green)
            .Select(args => (args.Rect, args.Shape))];
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_TheLargestTileStandsAtTheBottomLeft(string style)
    {
        var figure = Of(style.AsTheme());
        var plot = figure.FirstPlotArea();

        var largest = Tiles(figure).Single(t => t.Style.Fill == Colors.Red).Rect;

        Assert.Equal(plot.X, largest.X, 9);
        Assert.Equal(plot.Y + plot.Height, largest.Y + largest.Height, 9);
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheLargestTileStandsAtTheTopLeft()
    {
        var figure = Of(Theme.Default);
        var plot = figure.FirstPlotArea();

        var largest = Tiles(figure).Single(t => t.Style.Fill == Colors.Red).Rect;

        Assert.Equal(plot.X, largest.X, 9);
        Assert.Equal(plot.Y, largest.Y, 9);
    }

    [Fact]
    public void UnderClassic_ATileIsEdgedInBlackOnePointWide()
    {
        // matplotlib 3.11.2, classic style: patch.force_edgecolor True, patch.edgecolor 'k', patch.linewidth 1.0 (printed).
        Assert.All(Tiles(Of(Theme.MatplotlibClassic)), t =>
        {
            Assert.Equal(Colors.Black, t.Style.Stroke);
            Assert.Equal(100.0 / 72, t.Style.StrokeThickness, 9);
        });
    }

    [Fact]
    public void UnderTheDefaultStyle_ATileHasNoEdge()
    {
        // matplotlib 3.11.2, default style: patch.force_edgecolor False, so a bar of no edgecolor has none (printed: bar.get_edgecolor() alpha 0).
        Assert.All(Tiles(Of(Theme.MatplotlibV2)), t => Assert.False(t.Style.HasVisibleStroke));
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_ATileIsEdgedInAWhitePixel()
    {
        Assert.All(Tiles(Of(Theme.Default)), t =>
        {
            Assert.Equal(Colors.White, t.Style.Stroke);
            Assert.Equal(1.0, t.Style.StrokeThickness);
        });
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_TheTilesAreClippedToTheAxes(string style)
    {
        // matplotlib 3.11.2: ax.bar's patches have clip_on True and the axes' bbox as their clip box, so the outer half of the edge of a
        // tile on the frame is cut off (printed: bar.get_clip_on() True, bar.get_clip_box() the axes bbox).
        var figure = Of(style.AsTheme());
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);

        var plot = figure.FirstPlotArea();
        var clips = recording.Of<PushClipCall>().Select(call => call.ClipRect).ToList();
        Assert.Contains(plot, clips);
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheTilesAreNotClipped()
    {
        var figure = Of(Theme.Default);
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);

        var plot = figure.FirstPlotArea();
        var clips = recording.Of<PushClipCall>().Select(call => call.ClipRect).ToList();
        Assert.DoesNotContain(plot, clips);
    }

}
