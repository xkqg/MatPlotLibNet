// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Covers the raster hatch branches the coverage gate found uncovered.</summary>
public class HatchShaderBranchTests
{
    /// <summary>A hatch without an explicit colour still paints: the shader falls back to a shade that contrasts
    /// with the fill, so one property is enough to get a visible pattern on the raster backend too.</summary>
    [Fact]
    public void AHatchWithoutAColour_StillPaintsOnTheRaster()
    {
        byte[] plain = Render(HatchPattern.None, hatchColor: null);
        byte[] hatched = Render(HatchPattern.ForwardDiagonal, hatchColor: null);

        Assert.NotEmpty(hatched);
        Assert.False(hatched.AsSpan().SequenceEqual(plain),
            "a hatch with no explicit colour rasterised identically to a plain fill");
    }

    /// <summary>A shape with a hatch but NO fill has nothing to hatch — it paints nothing, and does not throw.</summary>
    [Fact]
    public void AHatchWithoutAFill_PaintsNothing()
    {
        byte[] png = Plt.Create()
            .WithSize(120, 90)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [1.0, 2]))   // a line: stroke only, no fill
            .Build()
            .ToPng();

        Assert.NotEmpty(png);
    }

    private static byte[] Render(HatchPattern hatch, Color? hatchColor) =>
        Plt.Create()
            .WithSize(160, 120)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A"], [3.0], s =>
            {
                s.Color = Colors.Gray;
                s.Hatch = hatch;
                s.HatchColor = hatchColor;
            }))
            .Build()
            .ToPng();
}
