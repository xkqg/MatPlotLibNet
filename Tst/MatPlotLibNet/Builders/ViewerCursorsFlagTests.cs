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

    public static TheoryData<string, Action<ServerInteractionBuilder>, bool> Asks => new()
    {
        { "a zoom only", s => s.EnableZoom(), false },
        { "the pointers", s => s.EnableViewerCursors(), true },
        { "every event, which the pointers are not", s => s.All(), false },
    };

    /// <summary>The pointers are on only when they are asked for.</summary>
    [Theory]
    [MemberData(nameof(Asks))]
    public void ThePointers_AreOnOnlyWhenAskedFor(string asked, Action<ServerInteractionBuilder> configure, bool on)
    {
        Assert.True(on == Build(configure).EnableViewerCursors, $"asked for {asked}");
    }

    [Fact]
    public void TheFlag_SurvivesJson_AndIsAbsentFromItAtRest()
    {
        var on = Build(s => s.EnableViewerCursors());
        var off = Build(s => s.EnableZoom());

        Assert.True(on.RoundTripped().EnableViewerCursors);
        Assert.DoesNotContain("enableViewerCursors", off.ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheFlag_CountsAsInteractivity()
    {
        Assert.True(new Figure { EnableViewerCursors = true }.HasInteractivity);
    }
}
