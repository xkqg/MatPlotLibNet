// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>An event that arrives from a browser says who sent it. The sender cannot be left to say so itself: the viewer
/// id travels in the same JSON payload as the zoom range, and a page that wrote someone else's id into it would speak for
/// them. The hub therefore stamps every event it receives, over whatever the payload carried, with the connection the
/// message came in on; this is the stamp.</summary>
public class ViewerStampTests
{
    [Fact]
    public void AnEvent_StartsWithoutAViewer()
    {
        var evt = new ZoomEvent("c", 0, 0, 1, 0, 1);

        Assert.Null(evt.ViewerId);
        Assert.Null(evt.ViewerName);
    }

    [Fact]
    public void Stamping_ReturnsTheSameKindOfEvent_WithTheViewerSet_AndLeavesTheOriginalAlone()
    {
        var original = new ZoomEvent("c", 0, 0, 1, 0, 1);

        var stamped = Assert.IsType<ZoomEvent>(original.StampedBy("connection-1", "Viewer 1"));

        Assert.Equal("connection-1", stamped.ViewerId);
        Assert.Equal("Viewer 1", stamped.ViewerName);
        Assert.Equal(1.0, stamped.XMax);
        Assert.Null(original.ViewerId);
    }

    [Fact]
    public void Stamping_OverwritesAViewerThePayloadClaimed()
    {
        var claimed = new PanEvent("c", 0, 1, 1) { ViewerId = "someone-else", ViewerName = "Viewer 9" };

        var stamped = claimed.StampedBy("connection-1", "Viewer 1");

        Assert.Equal("connection-1", stamped.ViewerId);
        Assert.Equal("Viewer 1", stamped.ViewerName);
    }

    [Fact]
    public void TheContext_IsReadFromTheStamp()
    {
        var evt = new ResetEvent("c", 0, 0, 1, 0, 1).StampedBy("connection-1", "Viewer 1");

        var context = evt.ViewerContext;

        Assert.Equal(new ChartEventContext("connection-1", "Viewer 1"), context);
    }

    [Fact]
    public void AnEventWithoutAStamp_HasNoViewerContext()
    {
        Assert.Equal(default, new ResetEvent("c", 0, 0, 1, 0, 1).ViewerContext);
    }
}
