// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A spine becomes a line under a theme: its colour, width and dash, and its half width.</summary>
public class SpineConfigTests
{
    /// <summary>A spine's stroke under a theme is its colour (its own, else the theme's text), its width (its own, else the theme's)
    /// and its dash pattern: the one place a spine becomes a line.</summary>
    [Fact]
    public void StrokeIn_IsTheColourTheWidthAndTheDashUnderTheTheme()
    {
        var spine = new SpineConfig { Color = Colors.Red, LineWidth = 2, LineStyle = LineStyle.Dashed };

        Assert.Equal(new StrokeStyle(Colors.Red, 2, LineStyle.Dashed), spine.StrokeIn(Theme.Default));
    }

    /// <summary>A spine that names no colour and no width takes both from its theme.</summary>
    [Fact]
    public void StrokeIn_OfASpineThatNamesNothing_FollowsTheTheme()
    {
        var spine = new SpineConfig();

        var stroke = spine.StrokeIn(Theme.Dark);

        Assert.Equal(Theme.Dark.ForegroundText, stroke.Color);
        Assert.Equal(spine.LineWidthIn(Theme.Dark), stroke.Thickness);
        Assert.Equal(LineStyle.Solid, stroke.Style);
    }

    /// <summary>Half the spine's width is the margin a tick keeps clear of the spine: one rule for the bottom, the top and the left.</summary>
    [Fact]
    public void HalfWidthIn_IsHalfTheWidthUnderTheTheme()
    {
        var spine = new SpineConfig { LineWidth = 3 };

        Assert.Equal(1.5, spine.HalfWidthIn(Theme.Default));
        Assert.Equal(new SpineConfig().LineWidthIn(Theme.Default) / 2.0, new SpineConfig().HalfWidthIn(Theme.Default));
    }
}
