// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How far a tick mark reaches out of the plot and into it from its edge (<see cref="TickMarkSpan"/>), the one reader of a
/// tick's direction: the drawn mark, matplotlib's marker and the part of a mark outside the axes all take it from here.</summary>
public class TickMarkSpanTests
{
    /// <summary>A 10 px mark whose end at the edge reaches 0.5 px past it: out of the plot its length (that end in the spine), into it
    /// its length (that end out past the spine), across the edge half its length each way.</summary>
    [Theory]
    [InlineData(TickDirection.Out, 10.0, 0.5)]
    [InlineData(TickDirection.In, 0.5, 10.0)]
    [InlineData(TickDirection.InOut, 5.0, 5.0)]
    public void AMark_ReachesByItsDirection_ItsEndAtTheEdgeTheGivenWayPast(TickDirection direction, double outer, double inner)
    {
        var span = TickMarkSpan.Of(direction, 10, 0.5);

        Assert.Equal(outer, span.Outer);
        Assert.Equal(inner, span.Inner);
    }

    /// <summary>A value that names no direction is drawn across the edge its whole length each way, as the drawing always drew it.</summary>
    [Fact]
    public void AValueThatNamesNoDirection_ReachesItsLengthBothWays()
    {
        Assert.Equal(new TickMarkSpan(10, 10), TickMarkSpan.Of((TickDirection)99, 10, 0.5));
    }

    /// <summary>The mark of a look, worked out once: in its own colour, else the ink, as wide as it is, the drawn span's end half the
    /// spine past the edge and the marker's at the edge.</summary>
    [Fact]
    public void TheMarkOfALook_TakesItsColourOrTheInk_ItsWidth_AndBothSpans()
    {
        var look = new TickConfig { Length = 10, Width = 2, Direction = TickDirection.Out };

        var plain = TickMark.Of(look, Colors.Black, 0.5);
        var red = TickMark.Of(look with { Color = Colors.Red }, Colors.Black, 0.5);

        Assert.Equal(StrokeStyle.Solid(Colors.Black, 2), plain.Stroke);
        Assert.Equal(StrokeStyle.Solid(Colors.Red, 2), red.Stroke);
        Assert.Equal(new TickMarkSpan(10, 0.5), plain.Drawn);
        Assert.Equal(new TickMarkSpan(10, 0), plain.Marker);
    }

    /// <summary>The part of a mark outside the axes, where its label's pad starts, is the span with its end at the edge (matplotlib's
    /// <c>Tick.get_tick_padding</c>: none pointing in, half across, all pointing out).</summary>
    [Theory]
    [InlineData(TickDirection.Out, 8.0)]
    [InlineData(TickDirection.In, 0.0)]
    [InlineData(TickDirection.InOut, 4.0)]
    public void ThePartOfAMarkOutsideTheAxes_IsItsSpanOutOfThePlot(TickDirection direction, double outside)
    {
        Assert.Equal(outside, new TickConfig { Length = 8, Direction = direction }.OutsideLength);
    }
}
