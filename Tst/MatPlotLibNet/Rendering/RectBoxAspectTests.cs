// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule that fits a rectangle to a box aspect.</summary>
public class RectBoxAspectTests
{
    [Fact]
    public void ARectangleTooTallForTheAspect_LosesHeight_AndStaysCentred()
    {
        var fitted = new Rect(10, 20, 400, 300).WithBoxAspect(0.5);

        Assert.Equal(new Rect(10, 20 + 50, 400, 200), fitted);
    }

    [Fact]
    public void ARectangleTooWideForTheAspect_LosesWidth_AndStaysCentred()
    {
        var fitted = new Rect(10, 20, 400, 200).WithBoxAspect(1.0);

        Assert.Equal(new Rect(110, 20, 200, 200), fitted);
    }

    [Fact]
    public void ARectangleOfTheShapeAlready_IsUnchanged()
    {
        Assert.Equal(new Rect(0, 0, 300, 150), new Rect(0, 0, 300, 150).WithBoxAspect(0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABoxAspectThatIsNotPositiveAndFinite_LeavesTheRectangleAlone(double aspect)
    {
        Assert.Equal(new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4).WithBoxAspect(aspect));
    }
}
