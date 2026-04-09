// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="SpinesConfig"/> controls which axis borders are rendered.</summary>
public class SpinesRenderTests
{
    /// <summary>Verifies that default spines produce 4 border lines (replacing the old rectangle frame).</summary>
    [Fact]
    public void DefaultSpines_DrawsFourSpineLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = figure.ToSvg();
        int spineLineCount = CountSpineLines(svg);
        Assert.Equal(4, spineLineCount);
    }

    /// <summary>Verifies that hiding the top spine produces 3 border lines.</summary>
    [Fact]
    public void HideTopSpine_DrawsThreeLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with { Top = new SpineConfig() with { Visible = false } };
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = figure.ToSvg();
        int spineLineCount = CountSpineLines(svg);
        Assert.Equal(3, spineLineCount);
    }

    /// <summary>Verifies that hiding top and right spines produces 2 lines (L-shape).</summary>
    [Fact]
    public void HideTopAndRightSpines_DrawsTwoLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with
        {
            Top = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        };
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = figure.ToSvg();
        int spineLineCount = CountSpineLines(svg);
        Assert.Equal(2, spineLineCount);
    }

    /// <summary>Verifies that hiding all spines draws no frame lines.</summary>
    [Fact]
    public void AllSpinesHidden_DrawsNoFrameLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = new SpinesConfig
        {
            Top = new SpineConfig() with { Visible = false },
            Bottom = new SpineConfig() with { Visible = false },
            Left = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        };
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = figure.ToSvg();
        int spineLineCount = CountSpineLines(svg);
        Assert.Equal(0, spineLineCount);
    }

    /// <summary>Verifies that default spines no longer produce a frame rectangle.</summary>
    [Fact]
    public void DefaultSpines_NoFrameRectangle()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = figure.ToSvg();
        // The old code drew a rect with stroke for the frame.
        // With spines, the frame is drawn as individual lines instead.
        // The background rect (fill, no stroke) should still exist.
        // We count <rect elements with stroke attributes — should be 0 for the frame.
        // (Legend and colorbar may add rects with stroke, but not in this minimal case.)
        int strokeRectCount = Regex.Matches(svg, @"<rect[^>]+stroke=""#").Count;
        // Background rect has no stroke, so strokeRectCount should be 0 for the frame.
        // The legend rect also has a stroke, but we have no labeled series here.
        Assert.Equal(0, strokeRectCount);
    }

    /// <summary>Counts the number of spine lines in the SVG (lines with class="spine").</summary>
    private static int CountSpineLines(string svg) =>
        Regex.Matches(svg, @"class=""spine""").Count;
}
