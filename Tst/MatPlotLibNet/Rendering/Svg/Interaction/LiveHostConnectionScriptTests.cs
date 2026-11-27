// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

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

        Wheel(h.Chart());

        Assert.Equal(["OnZoom"], host.Invoked);
        Assert.Empty(global.Invoked);
    }

    [Fact]
    public void WithoutAHost_TheGlobalConnectionStillCarriesTheGestures()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart());
        var global = h.WireJsSignalRHub();
        h.Remount(Chart());

        Wheel(h.Chart());

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

        var thrown = Record.Exception(() => Wheel(h.Chart()));

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
