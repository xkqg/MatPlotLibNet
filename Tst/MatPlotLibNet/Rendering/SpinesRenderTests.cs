// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="SpinesConfig"/> controls which axis borders are rendered.</summary>
public class SpinesRenderTests
{
    /// <summary>Verifies that default spines produce 4 border lines (replacing the old rectangle frame).</summary>
    /// <summary>The spine lines drawn on an axes with a line, after <paramref name="configure"/> has said what the spines are (it is given the
    /// spines the axes starts with).</summary>
    private static int SpineLinesDrawn(Func<SpinesConfig, SpinesConfig> configure)
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = configure(ax.Spines);
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        return CountSpineLines(figure.ToSvg());
    }

    [Fact]
    public void DefaultSpines_DrawsFourSpineLines()
    {
        Assert.Equal(4, SpineLinesDrawn(spines => spines));
    }

    /// <summary>Verifies that hiding the top spine produces 3 border lines.</summary>
    [Fact]
    public void HideTopSpine_DrawsThreeLines()
    {
        Assert.Equal(3, SpineLinesDrawn(spines => spines with { Top = new SpineConfig() with { Visible = false } }));
    }

    /// <summary>Verifies that hiding top and right spines produces 2 lines (L-shape).</summary>
    [Fact]
    public void HideTopAndRightSpines_DrawsTwoLines()
    {
        Assert.Equal(2, SpineLinesDrawn(spines => spines with
        {
            Top = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        }));
    }

    /// <summary>Verifies that hiding all spines draws no frame lines.</summary>
    [Fact]
    public void AllSpinesHidden_DrawsNoFrameLines()
    {
        Assert.Equal(0, SpineLinesDrawn(spines => new SpinesConfig
        {
            Top = new SpineConfig() with { Visible = false },
            Bottom = new SpineConfig() with { Visible = false },
            Left = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        }));
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
        int strokeRectCount = svg.Elements("rect").Count(rect => rect["stroke"] is { } stroke && stroke.StartsWith('#'));
        // Background rect has no stroke, so strokeRectCount should be 0 for the frame.
        // The legend rect also has a stroke, but we have no labeled series here.
        Assert.Equal(0, strokeRectCount);
    }

    /// <summary>Counts the number of spine lines in the SVG (lines with class="spine").</summary>
    private static int CountSpineLines(string svg) =>
        svg.CountOf("class=\"spine\"");
}
