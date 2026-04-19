// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase Y.7 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="MplChart"/> Expandable display mode's ToggleExpand path
/// (line 55 was 0%-covered, lines 19/22 were 50/75%-covered for the
/// `_expanded == true` arm). Pre-Y.7: 86.7%L / 88.9%B.</summary>
public class MplChartCoverageTests : BunitContext
{
    /// <summary>Click the Expand button → `_expanded` flips to true → div gets the
    /// `mpl-expanded` class and button text becomes "Close" (covers line 19, 22, 55).</summary>
    [Fact]
    public void Expandable_ClickButton_TogglesExpanded()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, fig);
            p.Add(x => x.DisplayMode, DisplayMode.Expandable);
        });

        var btn = cut.Find("button.mpl-expand-btn");
        Assert.Equal("Expand", btn.TextContent);
        Assert.DoesNotContain("mpl-expanded", cut.Find("div.mpl-expandable").GetAttribute("class")!);

        btn.Click();

        var btnAfter = cut.Find("button.mpl-expand-btn");
        Assert.Equal("Close", btnAfter.TextContent);
        Assert.Contains("mpl-expanded", cut.Find("div.mpl-expandable").GetAttribute("class")!);
    }

    /// <summary>Click twice — toggles back to collapsed (covers the false arm of
    /// `_expanded` after a flip).</summary>
    [Fact]
    public void Expandable_ClickTwice_TogglesBackToCollapsed()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, fig);
            p.Add(x => x.DisplayMode, DisplayMode.Expandable);
        });

        cut.Find("button.mpl-expand-btn").Click();
        cut.Find("button.mpl-expand-btn").Click();

        Assert.Equal("Expand", cut.Find("button.mpl-expand-btn").TextContent);
    }
}
