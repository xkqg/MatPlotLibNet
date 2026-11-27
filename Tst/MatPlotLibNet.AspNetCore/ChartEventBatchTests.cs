// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies <see cref="AspNetCore.ChartSession.ChartEventBatch"/>, what one read cycle of a session's reader leaves to publish: whether the
/// figure changed, which viewer moved the shared view last, and the latest tooltip of each viewer who hovered.</summary>
public class ChartEventBatchTests
{
    private const string Chart = "c";

    private static PanEvent Pan(string? viewerId = "v1", string? viewerName = "Ann") =>
        new(Chart, 0, 1, 0) { ViewerId = viewerId, ViewerName = viewerName };

    [Fact]
    public void ANewBatch_ChangedNothing_HasNoMoverAndNoTooltip()
    {
        var batch = new ChartSession.ChartEventBatch();

        Assert.False(batch.Mutated);
        Assert.Null(batch.Mover);
        Assert.Empty(batch.Tooltips);
    }

    [Fact]
    public void Applied_AViewChangeOfAViewer_MutatesAndNamesTheMover()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(Pan());

        Assert.True(batch.Mutated);
        Assert.Equal(new ChartSession.ViewMover("v1", "Ann"), batch.Mover);
    }

    [Fact]
    public void Applied_TwoViewChanges_TheLastOneIsTheMover()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(Pan("v1", "Ann"));
        batch.Applied(Pan("v2", "Bob"));

        Assert.Equal(new ChartSession.ViewMover("v2", "Bob"), batch.Mover);
    }

    [Fact]
    public void Applied_AnEventThatDoesNotChangeTheView_MutatesButMovesNobody()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(new LegendToggleEvent(Chart, 0, 0) { ViewerId = "v1", ViewerName = "Ann" });

        Assert.True(batch.Mutated);
        Assert.Null(batch.Mover);
    }

    [Fact]
    public void Applied_AViewChangeAfterAnEventThatDoesNot_KeepsTheEarlierMover()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(Pan("v1", "Ann"));
        batch.Applied(new LegendToggleEvent(Chart, 0, 0) { ViewerId = "v2", ViewerName = "Bob" });

        Assert.Equal(new ChartSession.ViewMover("v1", "Ann"), batch.Mover);
    }

    [Theory]
    [InlineData(null, "Ann")]
    [InlineData("v1", null)]
    public void Applied_AViewChangeWithoutAWholeViewer_MovesNobody(string? viewerId, string? viewerName)
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(Pan(viewerId, viewerName));

        Assert.True(batch.Mutated);
        Assert.Null(batch.Mover);
    }

    [Fact]
    public void Applied_AViewChangeWithAnIdButNoName_OvertakesTheEarlierMover_SoNobodyIsNamed()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Applied(Pan("v1", "Ann"));
        batch.Applied(Pan("v2", null));

        Assert.Null(batch.Mover);
    }

    [Fact]
    public void Faulted_AMutationEvent_CountsAsAChangeButMovesNobody()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Faulted(Pan());

        Assert.True(batch.Mutated);
        Assert.Null(batch.Mover);
    }

    [Fact]
    public void Faulted_ANotificationEvent_ChangesNothing()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Faulted(new HoverEvent(Chart, 0, 1, 2, "conn"));

        Assert.False(batch.Mutated);
    }

    [Fact]
    public void Tooltip_TheSameViewerTwice_KeepsTheLatest_AndAnotherViewerHasItsOwn()
    {
        var batch = new ChartSession.ChartEventBatch();

        batch.Tooltip("a", "first");
        batch.Tooltip("b", "other");
        batch.Tooltip("a", "second");

        Assert.Equal(
            [new KeyValuePair<string, string>("a", "second"), new KeyValuePair<string, string>("b", "other")],
            batch.Tooltips.OrderBy(t => t.Key));
    }
}
