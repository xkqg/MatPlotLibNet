// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The server-interaction script runs again each time a page mounts a redrawn chart. Its hover branch keeps one
/// tooltip element and one <c>ReceiveTooltipContent</c> handler per chart, and the handler writes into the tooltip of the
/// chart as it is now. Before, every run added a handler on the connection and a tooltip element of its own: after two
/// redraws a tooltip arrived twice, and the tooltip of an earlier run stayed on screen, because the only thing that hid it
/// was a pointerleave listener on an SVG that had been removed.</summary>
public class ServerHoverRemountTests
{
    private static Action<FigureBuilder> Chart(double last) => b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.EnableHover())
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 40.0, last]));

    private static int Tooltips(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("div[role=tooltip]").Length;

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    [Fact]
    public void TwoRedraws_LeaveOneHandler_AndOneTooltip()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(20));
        var hub = h.WireJsSignalRHub();

        h.Remount(Chart(21));
        hub.Deliver("ReceiveTooltipContent", "c", "first");
        h.Remount(Chart(22));
        hub.Deliver("ReceiveTooltipContent", "c", "second");

        Assert.Equal(1, hub.HandlerCount("ReceiveTooltipContent"));
        Assert.Equal(1, Tooltips(h));
        var tooltip = h.Document.QuerySelectorAllRaw("div[role=tooltip]").Single();
        Assert.Equal("second", tooltip.innerHTML);
        Assert.Equal("block", tooltip.style.display);
    }

    [Fact]
    public void ARedraw_HidesTheTooltipShownBeforeIt()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(20));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(21));
        hub.Deliver("ReceiveTooltipContent", "c", "shown");

        h.Remount(Chart(22));

        Assert.All(h.Document.QuerySelectorAllRaw("div[role=tooltip]"), t => Assert.Equal("none", t.style.display));
    }

    [Fact]
    public void AfterARedraw_TheHoverOfTheNewChart_StillSendsAndShowsItsTooltip()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(20));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(21));
        h.Remount(Chart(22));
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointermove") { target = svg, clientX = 300, clientY = 250 });
        hub.Deliver("ReceiveTooltipContent", "c", "now");
        // A pointerleave on the chart as it is now hides its tooltip.
        svg.Fire(new DomEvent("pointerleave") { target = svg });

        Assert.Equal(["OnHover"], hub.Invoked);
        var tooltip = h.Document.QuerySelectorAllRaw("div[role=tooltip]").Single();
        Assert.Equal("now", tooltip.innerHTML);
        Assert.Equal("none", tooltip.style.display);
    }

    [Fact]
    public void ATooltipForAnotherChart_IsNotShownOnThisOne()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(20));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(21));

        hub.Deliver("ReceiveTooltipContent", "other", "not mine");

        Assert.Equal(0, Tooltips(h));
    }
}
