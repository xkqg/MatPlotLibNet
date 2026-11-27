// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The Skia and MAUI surfaces each wrote the sequence a shape is painted in on a canvas (the fill, the hatch over it, then the
/// outline, each only when it shows). The sequence is written once, over a shape the surface hands over with the three layers it knows how
/// to paint; these tests hold the order and when each layer is painted.</summary>
public class CanvasShapeExtensionsTests
{
    /// <summary>A shape that writes down the layers it is asked to paint.</summary>
    private readonly struct Recording(List<string> layers) : ICanvasShape
    {
        public void Fill(in ShapeStyle style) => layers.Add("fill");

        public void Hatch(in ShapeStyle style) => layers.Add("hatch");

        public void Stroke(StrokeStyle outline) => layers.Add($"stroke {outline.Thickness}");
    }

    private static List<string> LayersOf(ShapeStyle style)
    {
        var layers = new List<string>();
        new Recording(layers).PaintIn(style);
        return layers;
    }

    [Fact]
    public void AFilledHatchedOutlinedShape_IsFilled_ThenHatched_ThenOutlined() =>
        Assert.Equal(["fill", "hatch", "stroke 2"],
            LayersOf(new ShapeStyle(Colors.Blue, Colors.Black, 2) { Hatch = HatchPattern.Cross }));

    [Fact]
    public void AFilledShape_IsOnlyFilled() =>
        Assert.Equal(["fill"], LayersOf(ShapeStyle.Filled(Colors.Blue)));

    [Fact]
    public void AnOutlinedShape_IsOnlyOutlined_AndAHatchNeedsAFill() =>
        Assert.Equal(["stroke 1.5"], LayersOf(ShapeStyle.Outlined(Colors.Black, 1.5) with { Hatch = HatchPattern.Cross }));

    [Fact]
    public void AShapeWithNothingThatShows_PaintsNothing() =>
        Assert.Empty(LayersOf(new ShapeStyle(null, null, 0)));
}
