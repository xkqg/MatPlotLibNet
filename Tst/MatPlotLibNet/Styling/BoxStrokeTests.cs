// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A line of a box plot becomes the stroke or the outline it is drawn with in one place: the colour its author named, else the
/// look's, else the colour of the box, at the look's width and dash.</summary>
public sealed class BoxStrokeTests
{
    [Fact]
    public void ANamedColour_WinsOverTheLooks_AndTheLooksOverTheBoxs()
    {
        var look = new BoxStroke(Colors.Red, 2, LineStyle.Dashed);

        Assert.Equal(new StrokeStyle(Colors.Blue, 2, LineStyle.Dashed), look.StrokeOver(Colors.Blue, Colors.Green));
        Assert.Equal(new StrokeStyle(Colors.Red, 2, LineStyle.Dashed), look.StrokeOver(null, Colors.Green));
        Assert.Equal(new StrokeStyle(Colors.Green, 2, LineStyle.Dashed), (look with { Color = null }).StrokeOver(null, Colors.Green));
    }

    [Fact]
    public void AnOutline_IsTheSameColourAtTheLooksWidth_AndNoFill()
    {
        var look = new BoxStroke(null, 1.5);

        Assert.Equal(ShapeStyle.Outlined(Colors.Green, 1.5), look.OutlineOver(null, Colors.Green));
        Assert.Equal(ShapeStyle.Outlined(Colors.Blue, 1.5), look.OutlineOver(Colors.Blue, Colors.Green));
    }

    [Fact]
    public void ACap_IsASolidLineOfItsColourAndWidth()
    {
        var caps = new BoxCaps(Colors.Black, 0.5, 1.25);

        Assert.Equal(StrokeStyle.Solid(Colors.Black, 1.25), caps.Line.StrokeOver(null, Colors.Green));
    }
}
