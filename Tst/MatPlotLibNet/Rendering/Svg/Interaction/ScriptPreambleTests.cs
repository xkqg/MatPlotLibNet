// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>What every script shares is one piece of code (SvgScriptPreamble): the chart it belongs to, a pointer held for a
/// drag, a pointer turned into the chart's own units. Each script used to write its own, and they drifted: the server script held
/// a pointer without the guard the others had, the zoom and selection scripts turned a pointer through a screen transform a chart
/// that is not laid out does not have, and four scripts fell back on the whole document where the others took its first chart.</summary>
public class ScriptPreambleTests
{
    [Fact]
    public void AServerChart_StillPans_WhenTheBrowserRefusesToHoldThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnablePan()).Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));
        var mock = h.WireSignalRMock();
        h.Document.RefusesPointerCapture = true;

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; e.button = 0; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 150; e.clientY = 130; e.button = 0; });

        Assert.Contains(mock.Calls, c => c.Method == "OnPan");
    }

    [Fact]
    public void TheZoomScript_ZoomsAChartThatIsNotLaidOut_AboutThePointerOnItsBox()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithSize(600, 500).WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]));
        h.Document.LaysOut = false;

        h.Simulate("svg", "wheel", e => { e.deltaY = -100; e.clientX = 50; e.clientY = 30; });

        var box = h.GetAttribute("svg", "viewBox")!.Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(600 * 0.85, box[2], 9);
        // The harness lays every box at (50, 30): the pointer stands on the box's corner, so the corner stays where it was.
        Assert.Equal(0, box[0], 9);
        Assert.Equal(0, box[1], 9);
    }

    [Fact]
    public void TheSelectionScript_DrawsItsBand_OnAChartThatIsNotLaidOut()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithSize(600, 500).WithSelection().Plot([1.0, 2.0], [3.0, 4.0]));
        h.Document.LaysOut = false;

        h.Simulate("svg", "pointerdown", e => { e.shiftKey = true; e.clientX = 80; e.clientY = 60; });

        Assert.NotNull(h.Document.querySelector("rect[aria-label='Data selection area']"));
    }

    [Fact]
    public void WithoutACurrentScript_AHighlightWiresTheFirstChart_AndLeavesTheSecondAlone()
    {
        using var h = InteractionScriptHarness.FromBuildersWithoutCurrentScript(
            b => b.WithHighlight().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "a").Plot([1.0, 2.0], [4.0, 3.0], s => s.Label = "b"),
            b => b.WithHighlight().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "c").Plot([1.0, 2.0], [4.0, 3.0], s => s.Label = "d"));

        h.Simulate("#mpl-chart-0 [data-series-index='0']", "mouseenter");

        Assert.Equal("0.3", h.Document.querySelector("#mpl-chart-0 [data-series-index='1']")!.style.opacity);
        Assert.Equal("", h.Document.querySelector("#mpl-chart-1 [data-series-index='0']")!.style.opacity);
        Assert.Equal("", h.Document.querySelector("#mpl-chart-1 [data-series-index='1']")!.style.opacity);
    }
}
