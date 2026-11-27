// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="ImageSeries"/> interpolation and blend modes produce correct SVG output.</summary>
public class ImageInterpolationRenderTests
{
    private static readonly double[,] Data = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Default ImageSeries (nearest) renders to SVG with rect elements.
        new DrawnToken("Image_Nearest_RendersSvg", () => Plt.Create()
            .Image(Data, s => s.Interpolation = "nearest")
            .ToSvg(), "<rect"),
        // Alpha < 1 produces an opacity attribute in the SVG.
        new DrawnToken("Image_Alpha_ProducesOpacity", () => Plt.Create()
            .Image(Data, s => s.Alpha = 0.5)
            .ToSvg(), "opacity"),
        // Bicubic interpolation renders without error.
        new DrawnToken("Image_Bicubic_RendersSvg", () => Plt.Create()
            .Image(Data, s => s.Interpolation = "bicubic")
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Bilinear interpolation produces more rect elements than nearest (upsampled).</summary>
    [Fact]
    public void Image_Bilinear_ProducesMoreRects()
    {
        string nearest = Plt.Create().Image(Data, s => s.Interpolation = "nearest").ToSvg();
        string bilinear = Plt.Create().Image(Data, s => s.Interpolation = "bilinear").ToSvg();

        int nearestCount  = nearest.CountOf("<rect");
        int bilinearCount = bilinear.CountOf("<rect");
        Assert.True(bilinearCount > nearestCount,
            $"Bilinear ({bilinearCount}) should produce more rects than nearest ({nearestCount})");
    }
}
