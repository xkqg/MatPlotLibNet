// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>An opacity a theme's look holds (a 3-D surface's, a legend frame's) is refused outside 0 to 1 by the one rule every setting
/// reads (<c>ThrowIfNotOpacity</c>), so a theme document that carries one is refused when it is read, as a figure document is. matplotlib
/// 3.11.2 (printed): <c>rcParams['legend.framealpha'] = 1.5</c> is taken, and the legend it makes raises <c>ValueError: alpha (1.5) is
/// outside 0-1 range</c>, as <c>Rectangle(alpha=1.5)</c> does.</summary>
public class LookOpacityTests
{
    public static TheoryData<double> NotOpacities { get; } = new(1.5, -0.2, double.NaN, double.PositiveInfinity);

    [Theory]
    [MemberData(nameof(NotOpacities))]
    public void ALegendLook_RefusesAFrameOpacityOutside0To1(double alpha) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new LegendLook { FrameAlpha = alpha });

    [Theory]
    [MemberData(nameof(NotOpacities))]
    public void APatchLook_RefusesASurfaceOpacityOutside0To1(double alpha) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PatchLook { Alpha = alpha });

    [Theory]
    [MemberData(nameof(NotOpacities))]
    public void AThemeDocument_ThatCarriesSuchAnOpacity_IsRefusedWhenItIsRead(double alpha)
    {
        var written = ThemeDto.From(Theme.MatplotlibV2);

        Assert.Throws<ArgumentOutOfRangeException>(() => (written with { LegendFrameAlpha = alpha }).ToTheme());
        Assert.Throws<ArgumentOutOfRangeException>(() => (written with { SurfaceAlpha = alpha }).ToTheme());

        // A document that names no legend font scale is read as the library's legend look; its frame opacity is still read, never
        // passed over in silence.
        var library = ThemeDto.From(Theme.Default);
        Assert.Throws<ArgumentOutOfRangeException>(() => (library with { LegendFrameAlpha = alpha }).ToTheme());
        Assert.Throws<ArgumentOutOfRangeException>(() => (library with { SurfaceAlpha = alpha }).ToTheme());
        if (double.IsFinite(alpha))
        {
            string number = alpha.ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (string key in new[] { "legendFrameAlpha", "surfaceAlpha" })
            {
                string json = $$"""{"width":400,"height":300,"theme":{"{{key}}":{{number}}},"subPlots":[]}""";
                Assert.Throws<ArgumentOutOfRangeException>(() => new ChartSerializer().FromJson(json));
            }
        }
    }

    [Fact]
    public void AnOpacityFrom0To1_AndNoneAtAll_AreTaken()
    {
        Assert.Equal(0.25, new LegendLook { FrameAlpha = 0.25 }.FrameAlpha);
        Assert.Null(new PatchLook { Alpha = null }.Alpha);
        Assert.Equal(1.0, new PatchLook { Alpha = 1.0 }.Alpha);
        var theme = (ThemeDto.From(Theme.MatplotlibV2) with { LegendFrameAlpha = 0.6, SurfaceAlpha = 0.9 }).ToTheme();
        Assert.Equal(0.6, theme.Looks.Legend.FrameAlpha);
        Assert.Equal(0.9, theme.Looks.Patch.Alpha);
    }
}
