// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Regression battery for the v1.2.2 non-mutating tier: every subclass of
/// <see cref="FigureNotificationEvent"/> MUST leave the figure unchanged. The sealed
/// <c>ApplyTo</c> on the tier-2 record enforces this structurally; these tests verify the
/// structural guarantee holds and that the two concrete notification events
/// (<see cref="BrushSelectEvent"/>, <see cref="HoverEvent"/>) follow the stacked-record
/// pattern the house style mandates.</summary>
public class FigureNotificationEventTests
{
    [Fact]
    public void FigureNotificationEvent_IsAbstract()
    {
        Assert.True(typeof(FigureNotificationEvent).IsAbstract);
    }

    [Fact]
    public void FigureNotificationEvent_Inherits_FigureInteractionEvent()
    {
        Assert.True(typeof(FigureInteractionEvent)
            .IsAssignableFrom(typeof(FigureNotificationEvent)));
    }

    [Fact]
    public void BrushSelectEvent_Inherits_FigureNotificationEvent()
    {
        Assert.True(typeof(FigureNotificationEvent)
            .IsAssignableFrom(typeof(BrushSelectEvent)));
    }

    [Fact]
    public void HoverEvent_Inherits_FigureNotificationEvent()
    {
        Assert.True(typeof(FigureNotificationEvent)
            .IsAssignableFrom(typeof(HoverEvent)));
    }

    [Fact]
    public void BrushSelectEvent_ApplyTo_IsNoOp_FigureUnchanged()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 10;
        var visibleBefore = ((MatPlotLibNet.Models.Series.ChartSeries)axes.Series[0]).Visible;

        var evt = new BrushSelectEvent("c1", 0, 1.0, 2.0, 3.0, 4.0);
        evt.ApplyTo(figure);

        Assert.Equal(0.0, axes.XAxis.Min);
        Assert.Equal(10.0, axes.XAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(10.0, axes.YAxis.Max);
        Assert.Equal(visibleBefore, ((MatPlotLibNet.Models.Series.ChartSeries)axes.Series[0]).Visible);
    }

    [Fact]
    public void HoverEvent_ApplyTo_IsNoOp_FigureUnchanged()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = -5; axes.XAxis.Max = 5;
        axes.YAxis.Min = -5; axes.YAxis.Max = 5;

        var evt = new HoverEvent("c1", 0, 1.5, 2.5, CallerConnectionId: "conn-A");
        evt.ApplyTo(figure);

        Assert.Equal(-5.0, axes.XAxis.Min);
        Assert.Equal(5.0, axes.XAxis.Max);
        Assert.Equal(-5.0, axes.YAxis.Min);
        Assert.Equal(5.0, axes.YAxis.Max);
    }

    [Fact]
    public void BrushSelectEvent_CarriesRect()
    {
        var evt = new BrushSelectEvent("live-1", 2, 0.5, 1.5, 9.5, 8.5);
        Assert.Equal("live-1", evt.ChartId);
        Assert.Equal(2, evt.AxesIndex);
        Assert.Equal(0.5, evt.X1);
        Assert.Equal(1.5, evt.Y1);
        Assert.Equal(9.5, evt.X2);
        Assert.Equal(8.5, evt.Y2);
    }

    [Fact]
    public void HoverEvent_CarriesPointAndCallerConnectionId()
    {
        var evt = new HoverEvent("c", 0, 3.14, 2.71, CallerConnectionId: "abc123");
        Assert.Equal(3.14, evt.X);
        Assert.Equal(2.71, evt.Y);
        Assert.Equal("abc123", evt.CallerConnectionId);
    }

    [Fact]
    public void HoverEvent_CallerConnectionId_DefaultsToNull_WhenOmitted()
    {
        var evt = new HoverEvent("c", 0, 1, 2);
        Assert.Null(evt.CallerConnectionId);
    }

    [Fact]
    public void BrushSelectEvent_RecordEquality()
    {
        var a = new BrushSelectEvent("c", 0, 1, 2, 3, 4);
        var b = new BrushSelectEvent("c", 0, 1, 2, 3, 4);
        Assert.Equal(a, b);
    }

    [Fact]
    public void HoverEvent_RecordEquality_IncludesCallerConnectionId()
    {
        var a = new HoverEvent("c", 0, 1, 2, CallerConnectionId: "conn-A");
        var b = new HoverEvent("c", 0, 1, 2, CallerConnectionId: "conn-A");
        var c = new HoverEvent("c", 0, 1, 2, CallerConnectionId: "conn-B");
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void BrushSelectEvent_IsFigureInteractionEvent()
    {
        FigureInteractionEvent evt = new BrushSelectEvent("c", 0, 0, 0, 1, 1);
        Assert.NotNull(evt);
    }

    [Fact]
    public void HoverEvent_IsFigureInteractionEvent()
    {
        FigureInteractionEvent evt = new HoverEvent("c", 0, 1, 2);
        Assert.NotNull(evt);
    }
}
