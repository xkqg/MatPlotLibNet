// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Showing the viewers of a chart each other's pointers is an opt-in of a server-interactive figure, and like every
/// other setting of the figure it survives a round trip through JSON: a figure read back draws the pointers it drew.</summary>
public class ViewerCursorsFlagTests
{
    private static Figure Build(Action<ServerInteractionBuilder> configure) =>
        Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).WithServerInteraction("c", configure).Build();

    [Fact]
    public void ItIsOffUnlessAskedFor()
    {
        Assert.False(Build(s => s.EnableZoom()).EnableViewerCursors);
    }

    [Fact]
    public void EnablingIt_SetsTheFiguresFlag()
    {
        Assert.True(Build(s => s.EnableViewerCursors()).EnableViewerCursors);
    }

    [Fact]
    public void EveryEvent_DoesNotIncludeIt_BecauseItIsNotAnEvent()
    {
        Assert.False(Build(s => s.All()).EnableViewerCursors);
    }

    [Fact]
    public void TheFlag_SurvivesJson_AndIsAbsentFromItAtRest()
    {
        var on = Build(s => s.EnableViewerCursors());
        var off = Build(s => s.EnableZoom());

        Assert.True(ChartSerializerDefault.Current.FromJson(on.ToJson()).EnableViewerCursors);
        Assert.DoesNotContain("enableViewerCursors", off.ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheFlag_CountsAsInteractivity()
    {
        Assert.True(new Figure { EnableViewerCursors = true }.HasInteractivity);
    }
}
