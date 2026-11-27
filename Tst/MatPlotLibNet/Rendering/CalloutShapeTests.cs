// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The outline of the box behind an annotation's text: one <see cref="CalloutShape"/> for each <see cref="BoxStyle"/>,
/// chosen in one place. What each outline draws is in <see cref="CalloutBoxTests"/>, through the box that draws it.</summary>
public class CalloutShapeTests
{
    [Theory]
    [InlineData(BoxStyle.None, typeof(NoCallout))]
    [InlineData(BoxStyle.Square, typeof(SquareCallout))]
    [InlineData(BoxStyle.Round, typeof(RoundCallout))]
    [InlineData(BoxStyle.RoundTooth, typeof(RoundToothCallout))]
    [InlineData(BoxStyle.Sawtooth, typeof(SawtoothCallout))]
    [InlineData((BoxStyle)99, typeof(NoCallout))]
    public void EveryStyle_HasItsOutline(BoxStyle style, Type shape) => Assert.IsType(shape, CalloutShape.Of(style));

    /// <summary>A rounded box is drawn every frame a legend under the matplotlib themes is (its fancy frame), so its outline is made as
    /// one array of exactly its segments: ten for a straight bottom edge, ten and a line per tooth for a toothed one.</summary>
    [Theory]
    [InlineData(BoxStyle.Round, 10)]
    [InlineData(BoxStyle.RoundTooth, 15)]
    public void ARoundedBoxOutline_IsOneArrayOfExactlyItsSegments(BoxStyle style, int segments)
    {
        var outline = ((RoundedCallout)CalloutShape.Of(style)).Outline(new Rect(0, 0, 40, 20), 4);

        Assert.Equal(segments, Assert.IsType<PathSegment[]>(outline).Length);
    }
}
