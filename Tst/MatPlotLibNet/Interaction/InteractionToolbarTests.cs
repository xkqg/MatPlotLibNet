// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Verifies <see cref="InteractionToolbar"/>: every tool id activates its <see cref="InteractionToolbar.ToolMode"/>, and the
/// active mode reads back as its id.</summary>
public class InteractionToolbarTests
{
    /// <summary>Activate(toolId) switch — every recognised ID maps to its
    /// matching <see cref="InteractionToolbar.ToolMode"/>. Theory walks all 4 IDs.</summary>
    [Theory]
    [InlineData("pan",       InteractionToolbar.ToolMode.Pan)]
    [InlineData("zoom",      InteractionToolbar.ToolMode.Zoom)]
    [InlineData("rotate3d",  InteractionToolbar.ToolMode.Rotate3D)]
    [InlineData("cursor",    InteractionToolbar.ToolMode.DataCursor)]
    [InlineData("trendline", InteractionToolbar.ToolMode.Trendline)]
    [InlineData("level",     InteractionToolbar.ToolMode.Level)]
    [InlineData("fibonacci", InteractionToolbar.ToolMode.Fibonacci)]
    [InlineData("span",      InteractionToolbar.ToolMode.SpanSelect)]
    public void Activate_RecognisedId_MapsToMatchingMode(string toolId, InteractionToolbar.ToolMode expected)
    {
        var toolbar = new InteractionToolbar();
        toolbar.Activate(toolId);
        Assert.Equal(expected, toolbar.ActiveTool);
    }

    /// <summary>Activate("unknown") falls into the switch's `_ =&gt; ActiveTool`
    /// arm — and leaves the active tool unchanged.</summary>
    [Fact]
    public void Activate_UnrecognisedId_KeepsCurrentMode()
    {
        var toolbar = new InteractionToolbar();
        toolbar.Activate("zoom");                  // first set Zoom so we can detect a no-op
        toolbar.Activate("unknown-id-xyz");
        Assert.Equal(InteractionToolbar.ToolMode.Zoom, toolbar.ActiveTool);
    }

    /// <summary>ActiveToolId getter (switch) — each <see cref="InteractionToolbar.ToolMode"/>
    /// value is reachable through Activate, so the round-trip from ID → ToolMode →
    /// ID covers every arm. SpanSelect doesn't have a public Activate ID so it falls
    /// through to the default arm — see <see cref="ActiveToolId_SpanSelect_FallsBackToPan"/>.</summary>
    [Theory]
    [InlineData("pan",       "pan")]
    [InlineData("zoom",      "zoom")]
    [InlineData("rotate3d",  "rotate3d")]
    [InlineData("cursor",    "cursor")]
    [InlineData("trendline", "trendline")]
    [InlineData("level",     "level")]
    [InlineData("fibonacci", "fibonacci")]
    [InlineData("span",      "span")]
    public void ActiveToolId_RoundTrip_MatchesActivateId(string activateId, string expectedReadBackId)
    {
        var toolbar = new InteractionToolbar();
        toolbar.Activate(activateId);
        Assert.Equal(expectedReadBackId, toolbar.ActiveToolId);
    }

    /// <summary>CreateDefault includes the three drawing tool buttons.</summary>
    [Fact]
    public void CreateDefault_IncludesDrawingToolButtons()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Contains(toolbar.Buttons, b => b.Id == InteractionToolbar.ToolIds.Trendline);
        Assert.Contains(toolbar.Buttons, b => b.Id == InteractionToolbar.ToolIds.Level);
        Assert.Contains(toolbar.Buttons, b => b.Id == InteractionToolbar.ToolIds.Fibonacci);
    }

    /// <summary>CreateDefault with a non-3D figure — false arm of HasAnyThreeD.
    /// Toolbar should NOT include a "rotate3d" button.</summary>
    [Fact]
    public void CreateDefault_NoThreeD_DoesNotIncludeRotate3DButton()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.DoesNotContain(toolbar.Buttons, b => b.Id == "rotate3d");
    }

    /// <summary>CreateDefault with a 3D figure — true arm of HasAnyThreeD.
    /// Toolbar includes a "rotate3d" toggle button.</summary>
    [Fact]
    public void CreateDefault_With3D_IncludesRotate3DButton()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }))
            .Build();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Contains(toolbar.Buttons, b => b.Id == "rotate3d");
    }
}
