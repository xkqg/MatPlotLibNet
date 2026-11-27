// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>What the three rubber bands share (<see cref="RubberBandModifier"/>), each with its own key: a left press with the key held
/// inside the plot opens the band, and a drag shorter than the band's floor sends nothing. The zoom rectangle and the span need two
/// pixels along x, the brush needs none, so a drag of one pixel is a zoom or a span to no one and a brush all the same.</summary>
public class RubberBandModifierTests
{
    public static TheoryData<string, ModifierKeys, Func<IChartLayout, Action<FigureInteractionEvent>, RubberBandModifier>, bool> Bands => new()
    {
        { "zoom rectangle", ModifierKeys.Ctrl, (layout, sink) => new RectangleZoomModifier("c", layout, sink), false },
        { "span", ModifierKeys.Alt, (layout, sink) => new SpanSelectModifier("c", layout, sink), false },
        { "brush", ModifierKeys.Shift, (layout, sink) => new BrushSelectModifier("c", layout, sink), true },
    };

    /// <summary>A press without the key, with the right button or outside the plot opens nothing.</summary>
    [Theory]
    [MemberData(nameof(Bands))]
    public void APress_OpensTheBand_OnlyAsALeftPressWithItsKeyInsideThePlot(string band,
        ModifierKeys key, Func<IChartLayout, Action<FigureInteractionEvent>, RubberBandModifier> make, bool sends)
    {
        var modifier = make(FigureLayout.Plain().Layout, _ => { });

        Assert.True(modifier.HandlesPointerPressed(new PointerInputArgs(60, 30, PointerButton.Left, key)), $"the {band} did not open");
        Assert.False(modifier.HandlesPointerPressed(new PointerInputArgs(60, 30, PointerButton.Left, ModifierKeys.None)));
        Assert.False(modifier.HandlesPointerPressed(new PointerInputArgs(60, 30, PointerButton.Right, key)));
        Assert.False(modifier.HandlesPointerPressed(new PointerInputArgs(5, 5, PointerButton.Left, key)));
        _ = sends;
    }

    [Theory]
    [MemberData(nameof(Bands))]
    public void ADragOfOnePixel_SendsAnEventOnlyWhenTheBandHasNoFloor(string band,
        ModifierKeys key, Func<IChartLayout, Action<FigureInteractionEvent>, RubberBandModifier> make, bool sends)
    {
        var events = new List<FigureInteractionEvent>();
        var modifier = make(FigureLayout.Plain().Layout, events.Add);

        modifier.OnPointerPressed(new PointerInputArgs(60, 30, PointerButton.Left, key));
        modifier.OnPointerMoved(new PointerInputArgs(61, 30, PointerButton.Left, key));
        modifier.OnPointerReleased(new PointerInputArgs(61, 30, PointerButton.Left, key));

        Assert.True(sends == (events.Count == 1), $"the {band} sent {events.Count} events for a drag of one pixel");
    }

    /// <summary>The state a band shows while it is dragged, read the same way off each modifier: the pixel it started at and the pixel the
    /// pointer is at now (the span has no height, so it has no Y).</summary>
    /// <param name="StartX">The X of the start pixel.</param>
    /// <param name="CurrentX">The X of the pixel the pointer is at.</param>
    /// <param name="StartY">The Y of the start pixel, or <see langword="null"/> for a band with no height.</param>
    /// <param name="CurrentY">The Y of the pixel the pointer is at, or <see langword="null"/> for a band with no height.</param>
    public readonly record struct BandPixels(double StartX, double CurrentX, double? StartY, double? CurrentY);

    public static TheoryData<string, ModifierKeys, Func<IChartLayout, Action<FigureInteractionEvent>, RubberBandModifier>, Func<RubberBandModifier, BandPixels?>, bool> Shown => new()
    {
        { "zoom rectangle", ModifierKeys.Ctrl, (layout, sink) => new RectangleZoomModifier("c", layout, sink),
            band => ((RectangleZoomModifier)band).ActiveZoomRect is { } s ? new BandPixels(s.StartPixelX, s.CurrentPixelX, s.StartPixelY, s.CurrentPixelY) : null, true },
        { "span", ModifierKeys.Alt, (layout, sink) => new SpanSelectModifier("c", layout, sink),
            band => ((SpanSelectModifier)band).ActiveSpan is { } s ? new BandPixels(s.StartPixelX, s.CurrentPixelX, null, null) : null, false },
        { "brush", ModifierKeys.Shift, (layout, sink) => new BrushSelectModifier("c", layout, sink),
            band => ((BrushSelectModifier)band).ActiveBrush is { } s ? new BandPixels(s.StartPixelX, s.CurrentPixelX, s.StartPixelY, s.CurrentPixelY) : null, true },
    };

    /// <summary>A band shows nothing before the press, the pixel it started at and the one the pointer is at while it is dragged, and
    /// nothing again after the release.</summary>
    [Theory]
    [MemberData(nameof(Shown))]
    public void TheBand_ShowsNothingBeforeThePress_FollowsThePointerWhileDragged_AndShowsNothingAfterTheRelease(string band,
        ModifierKeys key, Func<IChartLayout, Action<FigureInteractionEvent>, RubberBandModifier> make,
        Func<RubberBandModifier, BandPixels?> shown, bool hasHeight)
    {
        PointerInputArgs At(double x, double y) => new(x, y, PointerButton.Left, key);
        double? Y(double y) => hasHeight ? y : null;
        var modifier = make(FigureLayout.Wide().Layout, _ => { });
        Assert.True(shown(modifier) is null, $"the {band} showed a band before the press");

        modifier.OnPointerPressed(At(50, 50));
        Assert.Equal(new BandPixels(50, 50, Y(50), Y(50)), shown(modifier));

        modifier.OnPointerMoved(At(120, 80));
        Assert.Equal(new BandPixels(50, 120, Y(50), Y(80)), shown(modifier));

        modifier.OnPointerReleased(At(120, 80));
        Assert.True(shown(modifier) is null, $"the {band} showed a band after the release");
    }
}
