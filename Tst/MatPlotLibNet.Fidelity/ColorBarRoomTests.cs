// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;
using SkiaSharp;

namespace MatPlotLibNet.Fidelity;

/// <summary>Under the matplotlib themes the library's own colour bar takes the room <c>fig.colorbar(mappable, ax=ax)</c> takes, so a
/// check that draws such a reference puts the plot and the bar on the reference's columns with nothing set by hand, and a score says
/// how the bar is drawn, not how the layout shares the figure. The columns are read off the reference's own picture, row 120 of
/// <c>heatmap.png</c>, never computed from the rule the library follows.</summary>
public class ColorBarRoomTests : FidelityTest
{
    /// <summary>The first and last column of a stretch of pixels that are not the background.</summary>
    private readonly record struct Run(int First, int Last);

    /// <summary>The stretches of non-background pixels along a row, left to right: the plot with its frame, the colour bar with its
    /// frame, then whatever glyphs of a tick label the row crosses.</summary>
    private static List<Run> NonBackgroundRuns(SKBitmap picture, int row)
    {
        var background = picture.GetPixel(picture.Width - 4, row);
        var runs = new List<Run>();
        int first = -1;
        for (int x = 90; x < picture.Width - 8; x++)
        {
            bool drawn = picture.GetPixel(x, row) != background;
            if (drawn && first < 0) first = x;
            if (!drawn && first >= 0)
            {
                runs.Add(new Run(first, x - 1));
                first = -1;
            }
        }

        return runs;
    }

    private static SKBitmap Reference(string style) =>
        SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib", style, "heatmap.png"));

    private SKBitmap Ours(string themeId)
    {
        var theme = themeId.AsTheme();
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(ReferenceData.Load("heatmap").Matrix("data"), s => s.ColorMap = ColorMaps.Viridis)
                .WithColorBar())
            .Build();
        return SKBitmap.Decode(RenderToPng(figure));
    }

    [Theory]
    [InlineData("classic", "classic")]
    [InlineData("v2", "v2")]
    public void ThePlotAndTheBar_StandOnTheColumnsOfTheReference(string themeId, string style)
    {
        using var reference = Reference(style);
        using var ours = Ours(themeId);

        // Row 120 crosses the plot and the bar, between two ticks and two tick labels of either picture.
        var expected = NonBackgroundRuns(reference, 120);
        var drawn = NonBackgroundRuns(ours, 120);

        foreach (int part in new[] { 0, 1 })   // the plot, then the bar
        {
            Assert.InRange(drawn[part].First, expected[part].First - 1, expected[part].First + 1);
            Assert.InRange(drawn[part].Last, expected[part].Last - 1, expected[part].Last + 1);
        }
    }
}
