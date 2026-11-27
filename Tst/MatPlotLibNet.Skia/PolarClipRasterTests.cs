// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A line of a polar axes that runs past a radial limit the caller set is cut off at the circle in the PNG, as matplotlib
/// clips it to the axes patch: past the circle the figure shows, and the line is not flattened onto the rim either.</summary>
public class PolarClipRasterTests
{
    [Fact]
    public void ALinePastTheOuterRing_IsCutOffAtTheCircle()
    {
        // A thick black line from the centre out along the 45-degree spoke to r 5, on a scale that ends at r 2: past the circle it
        // runs through the corner of the square round it, which a clip to that square would keep.
        var figure = Plt.Create().WithSize(400, 400).AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([0.0, 5.0], [Math.PI / 4, Math.PI / 4], s => { s.Color = Colors.Black; s.LineWidth = 6; })
            .SetYLim(0, 2)).Build();
        var circle = figure.ToSvg().Elements("circle")[0];
        double cx = circle.Number("cx"), cy = circle.Number("cy"), r = circle.Number("r");
        using var picture = SKBitmap.Decode(figure.ToPng());
        SKColor At(double distance) =>
            picture.GetPixel((int)Math.Round(cx + distance * Math.Sqrt(0.5)), (int)Math.Round(cy - distance * Math.Sqrt(0.5)));

        // Inside the circle the line is drawn; five pixels past it the figure's own colour shows.
        Assert.Equal(SKColors.Black, At(r * 0.9));
        var outside = At(r + 5);
        Assert.True(outside.Red > 200 && outside.Green > 200 && outside.Blue > 200, $"past the circle the pixel is {outside}");
    }
}
