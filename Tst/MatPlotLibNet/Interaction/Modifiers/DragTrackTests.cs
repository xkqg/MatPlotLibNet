// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>The rubber band of a drag a held key opens (<see cref="DragTrack"/>): opened by a left press on an axes with its key held,
/// followed while the pointer moves, and handed back on release as one <see cref="Drag"/>, unless the drag is shorter than its
/// <see cref="DragFloor"/>, the one value the zoom rectangle, the span and the brush differ by.</summary>
public class DragTrackTests
{
    private static IChartLayout Layout()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 5;
        return ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
    }

    private static PointerInputArgs At(double x, double y, ModifierKeys keys = ModifierKeys.Ctrl, PointerButton button = PointerButton.Left) =>
        new(x, y, button, keys);

    [Fact]
    public void ALeftPressOnAnAxesWithItsKeyHeld_OpensTheTrack_AnythingElseDoesNot()
    {
        var layout = Layout();
        var track = new DragTrack(ModifierKeys.Ctrl, DragFloor.Box);

        Assert.True(track.Opens(layout, At(60, 35)));
        Assert.True(track.Opens(layout, At(60, 35, ModifierKeys.Ctrl | ModifierKeys.Shift)));
        Assert.False(track.Opens(layout, At(60, 35, ModifierKeys.Alt)));
        Assert.False(track.Opens(layout, At(60, 35, button: PointerButton.Right)));
        Assert.False(track.Opens(layout, At(500, 500)));
    }

    [Fact]
    public void ADrag_IsFollowedFromItsPress_AndHandedBackOnRelease()
    {
        var layout = Layout();
        var track = new DragTrack(ModifierKeys.Ctrl, DragFloor.None);

        track.Begin(layout, At(20, 20));
        track.MoveTo(At(40, 30));

        Assert.True(track.IsActive);
        Assert.Equal(0, track.AxesIndex);
        Assert.Equal(new Point(20, 20), track.Start);
        Assert.Equal(new Point(40, 30), track.Current);

        var drag = track.End(layout, At(60, 50));

        Assert.False(track.IsActive);
        Assert.NotNull(drag);
        var start = layout.PixelToData(20, 20, 0)!.Value;
        var end = layout.PixelToData(60, 50, 0)!.Value;
        Assert.Equal(new Drag(0, new Point(20, 20), new Point(60, 50), new Point(start.DataX, start.DataY), new Point(end.DataX, end.DataY)), drag);
        Assert.Equal(DataRange.Between(new Point(start.DataX, start.DataY), new Point(end.DataX, end.DataY)), drag.Value.DataBox);
    }

    [Fact]
    public void APressNoDataLiesUnder_OpensNothing_AndAMoveOrReleaseWithoutAPressIsNothing()
    {
        var layout = Layout();
        var track = new DragTrack(ModifierKeys.Ctrl, DragFloor.None);

        track.Begin(layout, At(500, 500));
        track.MoveTo(At(40, 30));

        Assert.False(track.IsActive);
        Assert.Equal(default, track.Current);
        Assert.Null(track.End(layout, At(60, 50)));
    }

    [Fact]
    public void AReleaseNoDataLiesUnder_EndsTheDragWithNothing()
    {
        var layout = Layout();
        var track = new DragTrack(ModifierKeys.Ctrl, DragFloor.None);
        track.Begin(layout, At(20, 20));

        Assert.Null(track.End(layout, At(500, 500)));
        Assert.False(track.IsActive);
    }

    public static TheoryData<string, double, double, bool> Floors => new()
    {
        { nameof(DragFloor.Box), 1, 1, false },
        { nameof(DragFloor.Box), 1, 2, true },
        { nameof(DragFloor.Box), 2, 1, true },
        { nameof(DragFloor.AlongX), 1, 30, false },
        { nameof(DragFloor.AlongX), 2, 0, true },
        { nameof(DragFloor.None), 0, 0, true },
    };

    [Theory]
    [MemberData(nameof(Floors))]
    public void ADragShorterThanItsFloor_DoesNotCount(string floor, double dx, double dy, bool counts)
    {
        var value = floor switch
        {
            nameof(DragFloor.Box) => DragFloor.Box,
            nameof(DragFloor.AlongX) => DragFloor.AlongX,
            _ => DragFloor.None,
        };
        var layout = Layout();
        var track = new DragTrack(ModifierKeys.Ctrl, value);
        track.Begin(layout, At(20, 20));

        var drag = track.End(layout, At(20 + dx, 20 + dy));

        Assert.Equal(counts, drag is not null);
    }
}
