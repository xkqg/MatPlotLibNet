// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What is drawn over the data: a signal of each direction, an arrow to its target, and every patch through the one method.</summary>
public class AxesBuilderOverlayVerbsTests
{
    [Fact]
    public void BuyAt_AndSellAt_PlaceAMarkerOfTheirDirection()
    {
        var signals = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.BuyAt(1, 20).SellAt(3, 25)).Build().SubPlots[0].Signals;

        Assert.Equal([SignalDirection.Buy, SignalDirection.Sell], signals.Select(signal => signal.Direction));
    }

    [Fact]
    public void Annotate_WithAnArrowTarget_PointsTheArrowThere()
    {
        var annotation = Assert.Single(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Annotate("point", 1.0, 1.0, 2.5, 2.5)).Build().SubPlots[0].Annotations);

        Assert.Equal(2.5, annotation.ArrowTargetX);
        Assert.Equal(2.5, annotation.ArrowTargetY);
    }

    [Fact]
    public void AddPatch_PutsThePatchOnTheSubplot_AndTheChainGoesOn()
    {
        var rectangle = new Rectangle(new(1, 1), 2, 3) { FaceColor = Colors.Red };

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => Assert.Same(ax, ax.AddPatch(rectangle))).Build();

        Assert.Same(rectangle, Assert.Single(figure.SubPlots[0].Patches));
    }

    [Fact]
    public void EveryKindOfPatch_GoesThroughTheOneMethod_InTheOrderAdded()
    {
        var arrow = new Arrow(new(0, 0), 1, 1);
        var polygon = new Polygon([new(0, 0), new(1, 0), new(0, 1)]) { Closed = false };
        var rectangle = new Rectangle(new(0, 0), 1, 1) { Angle = 30 };

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(arrow).AddPatch(polygon).AddPatch(rectangle)).Build();

        Assert.Equal(new Patch[] { arrow, polygon, rectangle }, figure.SubPlots[0].Patches);
    }

    [Fact]
    public void AddingNoPatch_IsRefused_BeforeAnythingIsAdded()
    {
        var refused = Assert.Throws<ArgumentNullException>(() => new AxesBuilder().AddPatch(null!));

        Assert.Equal("patch", refused.ParamName);
    }
}
