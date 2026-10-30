// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>What the page of one viewer does for the others. It sends where its pointer is, no more often than one
/// position in fifty milliseconds, and draws the pointers of the other viewers with the name the chart gave them. It keeps
/// the gesture it is in the middle of safe from a redraw: while the viewer drags or draws a band the chart says it is busy,
/// and says so again when it is idle, so the code that mounts a redrawn chart can wait for the end of the gesture instead
/// of cutting it off.</summary>
public class ViewerPresenceScriptTests
{
    private static Action<FigureBuilder> Chart(bool cursors) => b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => { s.EnableZoom().EnablePan().EnableBrushSelect(); if (cursors) s.EnableViewerCursors(); })
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).SetXLim(0, 10).SetYLim(0, 100));

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    private static DomEvent Move(DomElement svg, double x = 300, double y = 250) =>
        new("pointermove") { target = svg, clientX = x, clientY = y };

    // ── sending my pointer ──────────────────────────────────────────────────────

    [Fact]
    public void WithCursorsOn_AMove_SendsThePointerInDataUnits()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));
        var svg = Svg(h);

        svg.Fire(Move(svg));

        Assert.Contains("OnCursor", hub.Invoked);
        var payload = hub.LastPayload("OnCursor")!;
        Assert.Contains("\"chartId\":\"c\"", payload);
        Assert.Contains("\"axesIndex\":0", payload);
    }

    [Fact]
    public void WithCursorsOn_MovesCloserThanFiftyMilliseconds_SendOnlyTheFirst()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));
        var svg = Svg(h);

        svg.Fire(Move(svg, 300, 250));
        svg.Fire(Move(svg, 310, 250));
        svg.Fire(Move(svg, 320, 250));

        Assert.Single(hub.Invoked, m => m == "OnCursor");
    }

    [Fact]
    public void WithCursorsOn_AMoveFiftyMillisecondsLater_IsSentToo()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));
        var svg = Svg(h);

        svg.Fire(Move(svg, 300, 250));
        Thread.Sleep(70);
        svg.Fire(Move(svg, 320, 250));

        Assert.Equal(2, hub.Invoked.Count(m => m == "OnCursor"));
    }

    [Fact]
    public void WithCursorsOff_NothingIsSent()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: false));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: false));
        var svg = Svg(h);

        svg.Fire(Move(svg));

        Assert.DoesNotContain("OnCursor", hub.Invoked);
        Assert.Equal(0, hub.HandlerCount("ReceiveCursor"));
    }

    // ── drawing theirs ──────────────────────────────────────────────────────────

    private static DomElement[] Markers(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("g[data-mpl-peer]");

    [Fact]
    public void AnotherViewersPointer_IsDrawnWithTheirName()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 5.0, 50.0);

        var marker = Assert.Single(Markers(h));
        Assert.Equal("viewer-2", marker.getAttribute("data-mpl-peer"));
        Assert.Contains("Viewer 2", marker.innerHTML);
        Assert.Contains("translate(", marker.getAttribute("transform"));
    }

    [Fact]
    public void ThePointerOfTheSameViewer_Moves_InsteadOfAddingAnother()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 5.0, 50.0);
        var first = Assert.Single(Markers(h)).getAttribute("transform");
        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 8.0, 20.0);

        var marker = Assert.Single(Markers(h));
        Assert.NotEqual(first, marker.getAttribute("transform"));
    }

    [Fact]
    public void TwoViewers_AreTwoMarkers()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 5.0, 50.0);
        hub.DeliverMessage("ReceiveCursor", "c", "viewer-3", "Viewer 3", 0, 6.0, 60.0);

        Assert.Equal(2, Markers(h).Length);
    }

    [Fact]
    public void AViewerWhoLeaves_TakesTheirPointerWithThem()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));
        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 5.0, 50.0);

        hub.DeliverMessage("ViewerLeft", "c", "viewer-2");

        Assert.Empty(Markers(h));
    }

    [Fact]
    public void APointerForAnotherChart_IsNotDrawnHere()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));

        hub.DeliverMessage("ReceiveCursor", "other", "viewer-2", "Viewer 2", 0, 5.0, 50.0);

        Assert.Empty(Markers(h));
    }

    [Fact]
    public void ARedraw_LeavesOneHandlerOnTheConnection_AndTheNewChartDrawsThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: true));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: true));
        h.Remount(Chart(cursors: true));

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 5.0, 50.0);

        Assert.Equal(1, hub.HandlerCount("ReceiveCursor"));
        Assert.Single(Markers(h));
    }

    // ── who moved the view ──────────────────────────────────────────────────────

    [Fact]
    public void AViewChange_IsRaisedOnTheChart_NamingItsAuthor()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: false));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: false));
        var svg = Svg(h);
        DomEvent? raised = null;
        svg.addEventListener("mpl:viewchanged", e => raised = e);

        hub.DeliverMessage("ViewChanged", "c", "viewer-2", "Viewer 2");

        Assert.NotNull(raised);
        var detail = Assert.IsAssignableFrom<IDictionary<string, object?>>(raised!.detail);
        Assert.Equal("Viewer 2", detail["viewerName"]);
        Assert.Equal("viewer-2", detail["viewerId"]);
    }

    // ── a gesture in progress ───────────────────────────────────────────────────

    [Fact]
    public void WhileTheViewerDrags_TheChartIsBusy_AndSaysWhenItIsIdle()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: false));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: false));
        var svg = Svg(h);
        int idle = 0;
        svg.addEventListener("mpl:idle", _ => idle++);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = 300, clientY = 250, button = 0 });
        Assert.Equal("true", svg.getAttribute("data-mpl-busy"));
        Assert.Equal(0, idle);

        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = 320, clientY = 250 });

        Assert.Null(svg.getAttribute("data-mpl-busy"));
        Assert.Equal(1, idle);
    }

    [Fact]
    public void WhileTheViewerDrawsABand_TheChartIsBusyToo()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: false));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: false));
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = 300, clientY = 250, button = 0, shiftKey = true });
        Assert.Equal("true", svg.getAttribute("data-mpl-busy"));

        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = 340, clientY = 280 });
        Assert.Null(svg.getAttribute("data-mpl-busy"));
    }

    [Fact]
    public void ACancelledGesture_ReleasesTheChart()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart(cursors: false));
        var hub = h.WireJsSignalRHub();
        h.Remount(Chart(cursors: false));
        var svg = Svg(h);
        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = 300, clientY = 250, button = 0 });

        svg.Fire(new DomEvent("pointercancel") { target = svg });

        Assert.Null(svg.getAttribute("data-mpl-busy"));
    }
}

/// <summary>Which connection a chart's script sends through. A live chart component gives the host element of its chart a
/// proxy (<c>host.__mplHub</c>) to the connection its client holds, so the script needs no global; a page that wires its
/// own connection into <c>window.__mpl_signalr_connection</c> keeps working, and a send the connection rejects does not
/// surface in the page.</summary>
public class LiveHostConnectionScriptTests
{
    private static Action<FigureBuilder> Chart() => b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.EnableZoom())
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).SetXLim(0, 10).SetYLim(0, 100));

    private sealed class HostHub
    {
        public List<string> Invoked { get; } = [];
        public Func<object?>? Result { get; set; }
        public object? invoke(string method, object? payload) { Invoked.Add(method); return Result?.Invoke(); }
        public void on(string name, object? fn) { }
    }

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    private static void Wheel(DomElement svg) =>
        svg.Fire(new DomEvent("wheel") { target = svg, clientX = 300, clientY = 250, deltaY = -100 });

    [Fact]
    public void TheHostsConnection_IsUsedBeforeTheGlobalOne()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart());
        var global = h.WireJsSignalRHub();
        var host = new HostHub();
        var root = h.Document.Wrap(h.Document.Xml.Root!);
        root.setAttribute("data-mpl-live", "c");
        root.__mplHub = host;
        h.Remount(Chart());

        Wheel(Svg(h));

        Assert.Equal(["OnZoom"], host.Invoked);
        Assert.Empty(global.Invoked);
    }

    [Fact]
    public void WithoutAHost_TheGlobalConnectionStillCarriesTheGestures()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart());
        var global = h.WireJsSignalRHub();
        h.Remount(Chart());

        Wheel(Svg(h));

        Assert.Equal(["OnZoom"], global.Invoked);
    }

    [Fact]
    public void ASendTheConnectionRejects_DoesNotThrowOutOfTheScript()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart());
        h.WireJsSignalRHub();
        var host = new HostHub { Result = () => new RejectedSend() };
        var root = h.Document.Wrap(h.Document.Xml.Root!);
        root.setAttribute("data-mpl-live", "c");
        root.__mplHub = host;
        h.Remount(Chart());

        var thrown = Record.Exception(() => Wheel(Svg(h)));

        Assert.Null(thrown);
        Assert.True(RejectedSend.Caught);
    }

    /// <summary>A promise-like whose <c>catch</c> is called, as the script must.</summary>
    private sealed class RejectedSend
    {
        public static bool Caught { get; private set; }
        public object @catch(object? handler) { Caught = true; return this; }
    }
}
