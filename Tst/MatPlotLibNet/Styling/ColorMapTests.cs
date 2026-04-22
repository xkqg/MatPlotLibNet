// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="ColorMaps"/> behavior.</summary>
public class ColorMapTests
{
    // --- Helpers ---

    private static int Brightness(Color c) => c.R + c.G + c.B;
    private static int Saturation(Color c) => Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));

    // --- MemberData ---

    public static IEnumerable<object[]> AllMaps =>
    [
        // Perceptually-uniform
        [ColorMaps.Viridis],
        [ColorMaps.Plasma],
        [ColorMaps.Inferno],
        [ColorMaps.Magma],
        [ColorMaps.Coolwarm],
        [ColorMaps.Blues],
        [ColorMaps.Reds],
        // Sequential
        [SequentialColorMaps.Cividis],
        [SequentialColorMaps.Greens],
        [SequentialColorMaps.Oranges],
        [SequentialColorMaps.Purples],
        [SequentialColorMaps.Greys],
        [SequentialColorMaps.YlOrBr],
        [SequentialColorMaps.YlOrRd],
        [SequentialColorMaps.OrRd],
        [SequentialColorMaps.PuBu],
        [SequentialColorMaps.YlGn],
        [SequentialColorMaps.BuGn],
        // Diverging
        [DivergingColorMaps.RdBu],
        [DivergingColorMaps.RdYlGn],
        [DivergingColorMaps.RdYlBu],
        [DivergingColorMaps.BrBG],
        [DivergingColorMaps.PiYG],
        [DivergingColorMaps.Spectral],
        // Sequential batch 1
        [SequentialColorMaps.Hot],
        [SequentialColorMaps.Copper],
        [SequentialColorMaps.Bone],
        [SequentialColorMaps.BuPu],
        [SequentialColorMaps.GnBu],
        [SequentialColorMaps.PuRd],
        [SequentialColorMaps.RdPu],
        [SequentialColorMaps.YlGnBu],
        [SequentialColorMaps.PuBuGn],
        // Perceptually-uniform batch 4
        [ColorMaps.Turbo],
        [ColorMaps.Jet],
        // Sequential batch 4
        [SequentialColorMaps.Cubehelix],
        // Cyclic batch 4
        [CyclicColorMaps.Hsv],
        // Diverging batch 2
        [DivergingColorMaps.PuOr],
        [DivergingColorMaps.Seismic],
        [DivergingColorMaps.Bwr],
        // Cyclic
        [CyclicColorMaps.Twilight],
        [CyclicColorMaps.TwilightShifted],
        // Qualitative
        [QualitativeColorMaps.Tab10],
        [QualitativeColorMaps.Tab20],
        [QualitativeColorMaps.Set1],
        [QualitativeColorMaps.Set2],
        [QualitativeColorMaps.Set3],
        [QualitativeColorMaps.Pastel1],
        // Qualitative batch 3
        [QualitativeColorMaps.Pastel2],
        [QualitativeColorMaps.Dark2],
        [QualitativeColorMaps.Accent],
        [QualitativeColorMaps.Paired],
    ];

    // Cyclic maps intentionally have equal start/end, so exclude from distinct-endpoints test
    public static IEnumerable<object[]> NonCyclicMaps =>
        AllMaps.Where(m => !((IColorMap)m[0]).Name.StartsWith("twilight") && ((IColorMap)m[0]).Name != "hsv");

    public static IEnumerable<object[]> SequentialMaps =>
    [
        [ColorMaps.Blues],
        [ColorMaps.Reds],
        [SequentialColorMaps.Cividis],
        [SequentialColorMaps.Greens],
        [SequentialColorMaps.Oranges],
        [SequentialColorMaps.Purples],
        [SequentialColorMaps.Greys],
        [SequentialColorMaps.YlOrBr],
        [SequentialColorMaps.YlOrRd],
        [SequentialColorMaps.OrRd],
        [SequentialColorMaps.PuBu],
        [SequentialColorMaps.YlGn],
        [SequentialColorMaps.BuGn],
        [SequentialColorMaps.Hot],
        [SequentialColorMaps.Copper],
        [SequentialColorMaps.Bone],
        [SequentialColorMaps.BuPu],
        [SequentialColorMaps.GnBu],
        [SequentialColorMaps.PuRd],
        [SequentialColorMaps.RdPu],
        [SequentialColorMaps.YlGnBu],
        [SequentialColorMaps.PuBuGn],
        [SequentialColorMaps.Cubehelix],
    ];

    public static IEnumerable<object[]> DivergingMaps =>
    [
        [ColorMaps.Coolwarm],
        [DivergingColorMaps.RdBu],
        [DivergingColorMaps.RdYlGn],
        [DivergingColorMaps.RdYlBu],
        [DivergingColorMaps.BrBG],
        [DivergingColorMaps.PiYG],
        [DivergingColorMaps.Spectral],
        [DivergingColorMaps.PuOr],
        [DivergingColorMaps.Seismic],
        [DivergingColorMaps.Bwr],
    ];

    public static IEnumerable<object[]> CyclicMaps =>
    [
        [CyclicColorMaps.Twilight],
        [CyclicColorMaps.TwilightShifted],
        [CyclicColorMaps.Hsv],
    ];

    public static IEnumerable<object[]> QualitativeMaps =>
    [
        [QualitativeColorMaps.Tab10],
        [QualitativeColorMaps.Tab20],
        [QualitativeColorMaps.Set1],
        [QualitativeColorMaps.Set2],
        [QualitativeColorMaps.Set3],
        [QualitativeColorMaps.Pastel1],
        [QualitativeColorMaps.Pastel2],
        [QualitativeColorMaps.Dark2],
        [QualitativeColorMaps.Accent],
        [QualitativeColorMaps.Paired],
    ];

    // --- Universal theories ---

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_Name_IsLowercaseNonEmpty(IColorMap map)
    {
        Assert.NotEmpty(map.Name);
        Assert.Equal(map.Name, map.Name.ToLowerInvariant());
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_IsRegistered(IColorMap map)
    {
        Assert.NotNull(ColorMapRegistry.Get(map.Name));
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_ReversedIsRegistered(IColorMap map)
    {
        Assert.NotNull(ColorMapRegistry.Get(map.Name + "_r"));
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_AtZero_HaveFullOpacity(IColorMap map)
    {
        Assert.Equal(255, map.GetColor(0.0).A);
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_AtOne_HaveFullOpacity(IColorMap map)
    {
        Assert.Equal(255, map.GetColor(1.0).A);
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_FullRangeOpacity(IColorMap map)
    {
        for (double v = 0; v <= 1.0; v += 0.1)
            Assert.Equal(255, map.GetColor(v).A);
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_Clamp_BelowZero(IColorMap map)
    {
        Assert.Equal(map.GetColor(0.0), map.GetColor(-0.5));
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_Clamp_AboveOne(IColorMap map)
    {
        Assert.Equal(map.GetColor(1.0), map.GetColor(1.5));
    }

    [Theory]
    [MemberData(nameof(NonCyclicMaps))]
    public void AllMaps_ReturnDifferentColorsAtEndpoints(IColorMap map)
    {
        Assert.NotEqual(map.GetColor(0.0), map.GetColor(1.0));
    }

    // --- Category-specific theories ---

    [Theory]
    [MemberData(nameof(SequentialMaps))]
    public void SequentialMaps_MonotonicBrightness(IColorMap map)
    {
        // Sequential maps are monotone in brightness (light-to-dark or dark-to-light).
        // Sample 5 points and verify direction is consistent, with tolerance for interpolation noise.
        const int tolerance = 15;
        double[] pts = [0.0, 0.25, 0.5, 0.75, 1.0];
        int[] brightness = pts.Select(v => Brightness(map.GetColor(v))).ToArray();

        bool increasing = brightness[4] > brightness[0];
        for (int i = 0; i < brightness.Length - 1; i++)
        {
            if (increasing)
                Assert.True(brightness[i + 1] >= brightness[i] - tolerance,
                    $"{map.Name}: expected brightness to increase at t={pts[i + 1]:F2}, got {brightness[i + 1]} < {brightness[i]} - {tolerance}");
            else
                Assert.True(brightness[i + 1] <= brightness[i] + tolerance,
                    $"{map.Name}: expected brightness to decrease at t={pts[i + 1]:F2}, got {brightness[i + 1]} > {brightness[i]} + {tolerance}");
        }
    }

    [Theory]
    [MemberData(nameof(DivergingMaps))]
    public void DivergingMaps_MidpointLessSaturatedThanExtremes(IColorMap map)
    {
        int satMid = Saturation(map.GetColor(0.5));
        int satExtreme = Math.Max(Saturation(map.GetColor(0.0)), Saturation(map.GetColor(1.0)));
        Assert.True(satMid < satExtreme,
            $"{map.Name}: midpoint saturation {satMid} should be less than extremes {satExtreme}");
    }

    [Theory]
    [MemberData(nameof(CyclicMaps))]
    public void CyclicMaps_StartApproximatelyEqualsEnd(IColorMap map)
    {
        var c0 = map.GetColor(0.0);
        var c1 = map.GetColor(1.0);
        int diff = Math.Abs(c0.R - c1.R) + Math.Abs(c0.G - c1.G) + Math.Abs(c0.B - c1.B);
        Assert.True(diff <= 3,
            $"{map.Name}: start and end colors should be approximately equal (diff={diff})");
    }

    [Theory]
    [MemberData(nameof(QualitativeMaps))]
    public void QualitativeMaps_HasDistinctColors(IColorMap map)
    {
        var colors = Enumerable.Range(0, 10).Select(i => map.GetColor(i / 9.0)).ToList();
        var unique = colors.Distinct().Count();
        Assert.True(unique >= 8,
            $"{map.Name}: expected at least 8 distinct colors from 10 samples, got {unique}");
    }

    // --- Interpolation ---

    [Fact]
    public void Interpolation_Midpoint_DiffersFromEndpoints()
    {
        var c0 = ColorMaps.Viridis.GetColor(0.0);
        var cMid = ColorMaps.Viridis.GetColor(0.5);
        var c1 = ColorMaps.Viridis.GetColor(1.0);
        Assert.NotEqual(c0, cMid);
        Assert.NotEqual(c1, cMid);
    }

    // --- Clamping (kept for specificity) ---

    [Fact]
    public void GetColor_ClampsBelow0()
    {
        Assert.Equal(ColorMaps.Viridis.GetColor(0.0), ColorMaps.Viridis.GetColor(-0.5));
    }

    [Fact]
    public void GetColor_ClampsAbove1()
    {
        Assert.Equal(ColorMaps.Viridis.GetColor(1.0), ColorMaps.Viridis.GetColor(1.5));
    }

    // --- Batch 1: Sequential endpoint tests ---

    [Fact]
    public void Hot_AtZero_IsBlack()
    {
        var c = SequentialColorMaps.Hot.GetColor(0.0);
        Assert.True(c.R < 20 && c.G < 20 && c.B < 20, $"Hot at 0 should be near-black, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Hot_AtOne_IsWhite()
    {
        var c = SequentialColorMaps.Hot.GetColor(1.0);
        Assert.True(c.R > 240 && c.G > 240 && c.B > 240, $"Hot at 1 should be near-white, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Copper_AtZero_IsBlack()
    {
        var c = SequentialColorMaps.Copper.GetColor(0.0);
        Assert.True(c.R < 30 && c.G < 30 && c.B < 30, $"Copper at 0 should be near-black, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Copper_AtOne_HasCopperTone()
    {
        var c = SequentialColorMaps.Copper.GetColor(1.0);
        Assert.True(c.R > c.G && c.G >= c.B, $"Copper at 1 should have R>G>=B (copper tone), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Bone_AtZero_IsBlack()
    {
        var c = SequentialColorMaps.Bone.GetColor(0.0);
        Assert.True(c.R < 30 && c.G < 30 && c.B < 30, $"Bone at 0 should be near-black, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Bone_AtOne_IsLightBluish()
    {
        var c = SequentialColorMaps.Bone.GetColor(1.0);
        Assert.True(c.R > 200 && c.G > 200 && c.B > 200, $"Bone at 1 should be near-white, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void BuPu_AtZero_IsLight()
    {
        var c = SequentialColorMaps.BuPu.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"BuPu at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void BuPu_AtOne_IsDarkPurple()
    {
        var c = SequentialColorMaps.BuPu.GetColor(1.0);
        Assert.True(c.B > c.G, $"BuPu at 1 should have blue>green (purple), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void GnBu_AtZero_IsLight()
    {
        var c = SequentialColorMaps.GnBu.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"GnBu at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void GnBu_AtOne_IsDarkBlue()
    {
        var c = SequentialColorMaps.GnBu.GetColor(1.0);
        Assert.True(c.B > c.R, $"GnBu at 1 should have blue>red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void PuRd_AtZero_IsLight()
    {
        var c = SequentialColorMaps.PuRd.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"PuRd at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void PuRd_AtOne_IsDarkRed()
    {
        var c = SequentialColorMaps.PuRd.GetColor(1.0);
        Assert.True(c.R > c.B, $"PuRd at 1 should have red>blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void RdPu_AtZero_IsLight()
    {
        var c = SequentialColorMaps.RdPu.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"RdPu at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void RdPu_AtOne_IsDarkPurple()
    {
        var c = SequentialColorMaps.RdPu.GetColor(1.0);
        Assert.True(c.B > c.G, $"RdPu at 1 should have blue>green (purple), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void YlGnBu_AtZero_IsLight()
    {
        var c = SequentialColorMaps.YlGnBu.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"YlGnBu at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void YlGnBu_AtOne_IsDarkBlue()
    {
        var c = SequentialColorMaps.YlGnBu.GetColor(1.0);
        Assert.True(c.B > c.R, $"YlGnBu at 1 should have blue>red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void PuBuGn_AtZero_IsLight()
    {
        var c = SequentialColorMaps.PuBuGn.GetColor(0.0);
        Assert.True(Brightness(c) > 700, $"PuBuGn at 0 should be light, brightness={Brightness(c)}");
    }

    [Fact]
    public void PuBuGn_AtOne_IsDarkGreen()
    {
        var c = SequentialColorMaps.PuBuGn.GetColor(1.0);
        Assert.True(c.G >= c.R, $"PuBuGn at 1 should have green>=red, got ({c.R},{c.G},{c.B})");
    }

    // --- Batch 2: Diverging endpoint tests ---

    [Fact]
    public void PuOr_AtZero_IsDarkPurple()
    {
        var c = DivergingColorMaps.PuOr.GetColor(0.0);
        Assert.True(c.B >= c.G, $"PuOr at 0 should have blue>=green (purple), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void PuOr_AtOne_IsOrange()
    {
        var c = DivergingColorMaps.PuOr.GetColor(1.0);
        Assert.True(c.R > c.B, $"PuOr at 1 should have red>blue (orange), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Seismic_AtZero_IsDarkBlue()
    {
        var c = DivergingColorMaps.Seismic.GetColor(0.0);
        Assert.True(c.B > c.R, $"Seismic at 0 should be blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Seismic_AtOne_IsDarkRed()
    {
        var c = DivergingColorMaps.Seismic.GetColor(1.0);
        Assert.True(c.R > c.B, $"Seismic at 1 should be red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Bwr_AtZero_IsBlue()
    {
        var c = DivergingColorMaps.Bwr.GetColor(0.0);
        Assert.True(c.B > c.R && c.B > c.G, $"Bwr at 0 should be blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Bwr_AtHalf_IsWhite()
    {
        var c = DivergingColorMaps.Bwr.GetColor(0.5);
        Assert.True(c.R > 240 && c.G > 240 && c.B > 240, $"Bwr at 0.5 should be near-white, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Bwr_AtOne_IsRed()
    {
        var c = DivergingColorMaps.Bwr.GetColor(1.0);
        Assert.True(c.R > c.B && c.R > c.G, $"Bwr at 1 should be red, got ({c.R},{c.G},{c.B})");
    }

    // --- Batch 3: Qualitative endpoint tests ---

    [Fact]
    public void Pastel2_AtZero_IsGreenish()
    {
        var c = QualitativeColorMaps.Pastel2.GetColor(0.0);
        Assert.True(c.G >= c.R, $"Pastel2 at 0 should be greenish (#B3E2CD), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Dark2_AtZero_IsTeal()
    {
        var c = QualitativeColorMaps.Dark2.GetColor(0.0);
        Assert.True(c.G > c.R, $"Dark2 at 0 should have green>red (teal #1B9E77), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Accent_AtZero_IsGreen()
    {
        var c = QualitativeColorMaps.Accent.GetColor(0.0);
        Assert.True(c.G > c.R && c.G > c.B, $"Accent at 0 should have green dominant (#7FC97F), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Paired_AtZero_IsLightBlue()
    {
        var c = QualitativeColorMaps.Paired.GetColor(0.0);
        Assert.True(c.B >= c.R, $"Paired at 0 should be light blue (#A6CEE3), got ({c.R},{c.G},{c.B})");
    }

    // --- Batch 4: Special colormap endpoint tests ---

    [Fact]
    public void Turbo_AtZero_IsDarkBlue()
    {
        var c = ColorMaps.Turbo.GetColor(0.0);
        Assert.True(c.B > c.R, $"Turbo at 0 should be dark blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Turbo_AtOne_IsDarkRed()
    {
        var c = ColorMaps.Turbo.GetColor(1.0);
        Assert.True(c.R > c.B, $"Turbo at 1 should be dark red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Jet_AtZero_IsDarkBlue()
    {
        var c = ColorMaps.Jet.GetColor(0.0);
        Assert.True(c.B > c.R, $"Jet at 0 should be dark blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Jet_AtHalf_IsGreenish()
    {
        var c = ColorMaps.Jet.GetColor(0.5);
        Assert.True(c.G > c.B, $"Jet at 0.5 should be greenish, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Jet_AtOne_IsDarkRed()
    {
        var c = ColorMaps.Jet.GetColor(1.0);
        Assert.True(c.R > c.B, $"Jet at 1 should be dark red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Hsv_AtZero_IsRed()
    {
        var c = CyclicColorMaps.Hsv.GetColor(0.0);
        Assert.True(c.R > 200 && c.G < 50 && c.B < 50, $"Hsv at 0 should be red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Hsv_AtOne_IsRed()
    {
        var c = CyclicColorMaps.Hsv.GetColor(1.0);
        Assert.True(c.R > 200 && c.G < 50 && c.B < 50, $"Hsv at 1 should be red (cyclic), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Cubehelix_AtZero_IsBlack()
    {
        var c = SequentialColorMaps.Cubehelix.GetColor(0.0);
        Assert.True(Brightness(c) < 60, $"Cubehelix at 0 should be near-black, brightness={Brightness(c)}");
    }

    [Fact]
    public void Cubehelix_AtOne_IsWhite()
    {
        var c = SequentialColorMaps.Cubehelix.GetColor(1.0);
        Assert.True(Brightness(c) > 650, $"Cubehelix at 1 should be near-white, brightness={Brightness(c)}");
    }

    // --- Semantic endpoint tests ---

    [Fact]
    public void Viridis_AtZero_ReturnsDarkColor()
    {
        Assert.True(ColorMaps.Viridis.GetColor(0.0).R < 128);
    }

    [Fact]
    public void Viridis_AtOne_ReturnsLightColor()
    {
        Assert.True(ColorMaps.Viridis.GetColor(1.0).R > 128);
    }

    [Fact]
    public void Blues_AtZero_ReturnsLightColor()
    {
        Assert.True(ColorMaps.Blues.GetColor(0.0).R > 200);
    }

    [Fact]
    public void Blues_AtOne_ReturnsBlueColor()
    {
        var c = ColorMaps.Blues.GetColor(1.0);
        Assert.True(c.B > c.R && c.B > c.G);
    }

    [Fact]
    public void Reds_AtZero_ReturnsLightColor()
    {
        Assert.True(ColorMaps.Reds.GetColor(0.0).R > 200);
    }

    [Fact]
    public void Coolwarm_AtHalf_DiffersFromEndpoints()
    {
        var mid = ColorMaps.Coolwarm.GetColor(0.5);
        Assert.NotEqual(ColorMaps.Coolwarm.GetColor(0.0), mid);
        Assert.NotEqual(ColorMaps.Coolwarm.GetColor(1.0), mid);
    }

    [Fact]
    public void AllColors_HaveFullOpacity()
    {
        for (double v = 0; v <= 1.0; v += 0.1)
            Assert.Equal(255, ColorMaps.Viridis.GetColor(v).A);
    }

    // --- B2: ListedColorMap ---

    [Fact]
    public void ListedColorMap_AtZero_ReturnsFirstColor()
    {
        var cm = new ListedColorMap("test", [new Color(10, 20, 30), new Color(100, 110, 120), new Color(200, 210, 220)]);
        Assert.Equal(new Color(10, 20, 30), cm.GetColor(0.0));
    }

    [Fact]
    public void ListedColorMap_AtOne_ReturnsLastColor()
    {
        var cm = new ListedColorMap("test", [new Color(10, 20, 30), new Color(100, 110, 120), new Color(200, 210, 220)]);
        Assert.Equal(new Color(200, 210, 220), cm.GetColor(1.0));
    }

    [Fact]
    public void ListedColorMap_Discrete_NotInterpolated()
    {
        // Tab10 has 10 colors. v=0.05 → bin 0; v=0.15 → bin 1. They must differ, not blend.
        var map = QualitativeColorMaps.Tab10;
        var c0 = map.GetColor(0.05);
        var c1 = map.GetColor(0.15);
        // Same bin at 0 and 0.05
        Assert.Equal(map.GetColor(0.0), c0);
        // Different bin
        Assert.NotEqual(c0, c1);
    }

    [Fact]
    public void ListedColorMap_IndexBoundary_Tab10()
    {
        // Tab10: 10 colors, bin width = 0.1
        // v=0.0 → index 0; v=0.1 → index 1; v=0.9 → index 9; v=1.0 → index 9
        var map = QualitativeColorMaps.Tab10;
        Assert.Equal(map.GetColor(0.0), map.GetColor(0.05));  // same bin
        Assert.NotEqual(map.GetColor(0.0), map.GetColor(0.15)); // next bin
        Assert.Equal(map.GetColor(1.0), map.GetColor(0.95));  // last bin
    }

    [Fact]
    public void ListedColorMap_NegativeInput_Clamped()
    {
        var cm = new ListedColorMap("test", [new Color(10, 0, 0), new Color(100, 0, 0)]);
        Assert.Equal(cm.GetColor(0.0), cm.GetColor(-0.5));
    }

    [Fact]
    public void ListedColorMap_OverOneInput_Clamped()
    {
        var cm = new ListedColorMap("test", [new Color(10, 0, 0), new Color(100, 0, 0)]);
        Assert.Equal(cm.GetColor(1.0), cm.GetColor(1.5));
    }

    [Fact]
    public void ListedColorMap_Name_Preserved()
    {
        var cm = new ListedColorMap("mycmap", [new Color(0, 0, 0)]);
        Assert.Equal("mycmap", cm.Name);
    }

    [Theory]
    [MemberData(nameof(QualitativeMaps))]
    public void QualitativeMaps_AreListedColorMap(IColorMap map)
    {
        Assert.IsType<ListedColorMap>(map);
    }

    [Theory]
    [MemberData(nameof(QualitativeMaps))]
    public void QualitativeMaps_AreDiscreteNotInterpolated(IColorMap map)
    {
        // Same value twice → same color (deterministic)
        Assert.Equal(map.GetColor(0.0), map.GetColor(0.0));
        // Bin 0 and bin 1 differ (for any map with ≥2 colors)
        // Use values in distinct bins (0.0 → bin 0; halfway into bin 1)
        int colorCount = Enumerable.Range(0, 20)
            .Select(i => map.GetColor(i / 19.0))
            .Distinct().Count();
        Assert.True(colorCount >= 2, $"{map.Name}: expected at least 2 distinct colors");
    }

    // --- B5: Additional colormaps ---

    public static IEnumerable<object[]> AdditionalMaps =>
    [
        [AdditionalColorMaps.Gray],
        [AdditionalColorMaps.Spring],
        [AdditionalColorMaps.Summer],
        [AdditionalColorMaps.Autumn],
        [AdditionalColorMaps.Winter],
        [AdditionalColorMaps.Cool],
        [AdditionalColorMaps.AfmHot],
        [AdditionalColorMaps.PRGn],
        [AdditionalColorMaps.RdGy],
        [AdditionalColorMaps.Rainbow],
        [AdditionalColorMaps.Ocean],
        [AdditionalColorMaps.Terrain],
        [AdditionalColorMaps.CMRmap],
    ];

    [Theory]
    [MemberData(nameof(AdditionalMaps))]
    public void AdditionalMaps_AreRegistered(IColorMap map)
    {
        Assert.NotNull(ColorMapRegistry.Get(map.Name));
    }

    [Theory]
    [MemberData(nameof(AdditionalMaps))]
    public void AdditionalMaps_ReversedIsRegistered(IColorMap map)
    {
        Assert.NotNull(ColorMapRegistry.Get(map.Name + "_r"));
    }

    [Theory]
    [MemberData(nameof(AdditionalMaps))]
    public void AdditionalMaps_FullOpacity(IColorMap map)
    {
        for (double v = 0; v <= 1.0; v += 0.25)
            Assert.Equal(255, map.GetColor(v).A);
    }

    [Fact]
    public void Gray_AtZero_IsBlack()
    {
        var c = AdditionalColorMaps.Gray.GetColor(0.0);
        Assert.True(c.R < 20 && c.G < 20 && c.B < 20, $"Gray at 0 should be near-black, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Gray_AtOne_IsWhite()
    {
        var c = AdditionalColorMaps.Gray.GetColor(1.0);
        Assert.True(c.R > 240 && c.G > 240 && c.B > 240, $"Gray at 1 should be near-white, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Spring_AtZero_IsMagenta()
    {
        var c = AdditionalColorMaps.Spring.GetColor(0.0);
        Assert.True(c.R > 200 && c.G < 10, $"Spring at 0 should be magenta (R high, G=0), got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Spring_AtOne_IsYellow()
    {
        var c = AdditionalColorMaps.Spring.GetColor(1.0);
        Assert.True(c.R > 200 && c.G > 200, $"Spring at 1 should be yellow, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Summer_AtZero_IsGreenish()
    {
        var c = AdditionalColorMaps.Summer.GetColor(0.0);
        Assert.True(c.G > c.R, $"Summer at 0 should have G>R, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Autumn_AtZero_IsRed()
    {
        var c = AdditionalColorMaps.Autumn.GetColor(0.0);
        Assert.True(c.R > 200 && c.G < 10, $"Autumn at 0 should be red, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Autumn_AtOne_IsYellow()
    {
        var c = AdditionalColorMaps.Autumn.GetColor(1.0);
        Assert.True(c.R > 200 && c.G > 200, $"Autumn at 1 should be yellow, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Winter_AtZero_IsBluish()
    {
        var c = AdditionalColorMaps.Winter.GetColor(0.0);
        Assert.True(c.B > c.R, $"Winter at 0 should have B>R, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Cool_AtZero_IsCyan()
    {
        var c = AdditionalColorMaps.Cool.GetColor(0.0);
        Assert.True(c.G > 200 && c.B > 200 && c.R < 10, $"Cool at 0 should be cyan, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Cool_AtOne_IsMagenta()
    {
        var c = AdditionalColorMaps.Cool.GetColor(1.0);
        Assert.True(c.R > 200 && c.B > 200, $"Cool at 1 should be magenta, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void AfmHot_AtZero_IsDark()
    {
        var c = AdditionalColorMaps.AfmHot.GetColor(0.0);
        Assert.True(c.R + c.G + c.B < 30, $"AfmHot at 0 should be dark, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void PRGn_AtZero_IsPurple()
    {
        var c = AdditionalColorMaps.PRGn.GetColor(0.0);
        Assert.True(c.R > 50 && c.B > 50, $"PRGn at 0 should be purplish, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void PRGn_AtOne_IsGreen()
    {
        var c = AdditionalColorMaps.PRGn.GetColor(1.0);
        Assert.True(c.G > c.R && c.G > c.B, $"PRGn at 1 should be green, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void RdGy_AtZero_IsRed()
    {
        var c = AdditionalColorMaps.RdGy.GetColor(0.0);
        Assert.True(c.R > c.B, $"RdGy at 0 should be reddish, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void Rainbow_HasFullSpectrum()
    {
        // Blue at 0, red at 1
        var c0 = AdditionalColorMaps.Rainbow.GetColor(0.0);
        var c1 = AdditionalColorMaps.Rainbow.GetColor(1.0);
        Assert.True(c0.B > c0.R, $"Rainbow at 0 should be blue, got ({c0.R},{c0.G},{c0.B})");
        Assert.True(c1.R > c1.B, $"Rainbow at 1 should be red, got ({c1.R},{c1.G},{c1.B})");
    }

    [Fact]
    public void Ocean_AtZero_IsDeepBlue()
    {
        var c = AdditionalColorMaps.Ocean.GetColor(0.0);
        Assert.True(c.B > c.R, $"Ocean at 0 should be blue, got ({c.R},{c.G},{c.B})");
    }

    [Fact]
    public void CMRmap_AtZero_IsDark()
    {
        var c = AdditionalColorMaps.CMRmap.GetColor(0.0);
        Assert.True(c.R + c.G + c.B < 30, $"CMRmap at 0 should be dark, got ({c.R},{c.G},{c.B})");
    }

    // --- B3: LinearColorMap ---

    [Fact]
    public void LinearColorMap_IsPublic()
    {
        Assert.True(typeof(LinearColorMap).IsPublic);
    }

    [Fact]
    public void LinearColorMap_EvenlySpaced_AtZero_ReturnsFirstStop()
    {
        var cm = new LinearColorMap("test", [new Color(0, 0, 0), new Color(255, 255, 255)]);
        Assert.Equal(new Color(0, 0, 0), cm.GetColor(0.0));
    }

    [Fact]
    public void LinearColorMap_EvenlySpaced_AtOne_ReturnsLastStop()
    {
        var cm = new LinearColorMap("test", [new Color(0, 0, 0), new Color(255, 255, 255)]);
        Assert.Equal(new Color(255, 255, 255), cm.GetColor(1.0));
    }

    [Fact]
    public void LinearColorMap_FromPositions_Midpoint_InterpolatesCorrectly()
    {
        // Stops: (0.0, black), (0.5, red), (1.0, white)
        // v=0.25 → halfway between black(0,0,0) and red(255,0,0) → R≈127, G=0, B=0
        var white = new Color(255, 255, 255);
        var red = new Color(255, 0, 0);
        var black = new Color(0, 0, 0);
        var cm = LinearColorMap.FromPositions("test",
        [
            new(0.0, black),
            new(0.5, red),
            new(1.0, white),
        ]);
        var c = cm.GetColor(0.25);
        Assert.InRange(c.R, (byte)125, (byte)129); // ~127 ± rounding
        Assert.Equal((byte)0, c.G);
        Assert.Equal((byte)0, c.B);
    }

    [Fact]
    public void LinearColorMap_FromPositions_AtPositionBoundary_ReturnsExact()
    {
        var red = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var cm = LinearColorMap.FromPositions("test", [new(0.0, red), new(1.0, blue)]);
        Assert.Equal(red, cm.GetColor(0.0));
        Assert.Equal(blue, cm.GetColor(1.0));
    }

    [Fact]
    public void LinearColorMap_FromPositions_UnorderedStops_Throws()
    {
        var c = new Color(0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            LinearColorMap.FromPositions("test", [new(0.5, c), new(0.0, c), new(1.0, c)]));
    }

    [Fact]
    public void LinearColorMap_FromPositions_SingleStop_Throws()
    {
        var c = new Color(0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            LinearColorMap.FromPositions("test", [new(0.5, c)]));
    }

    // --- B4: Extreme value support ---

    [Fact]
    public void IColorMap_GetUnderColor_Default_IsNull()
    {
        IColorMap cm = ColorMaps.Viridis;
        Assert.Null(cm.GetUnderColor());
    }

    [Fact]
    public void IColorMap_GetOverColor_Default_IsNull()
    {
        IColorMap cm = ColorMaps.Viridis;
        Assert.Null(cm.GetOverColor());
    }

    [Fact]
    public void IColorMap_GetBadColor_Default_IsNull()
    {
        IColorMap cm = ColorMaps.Viridis;
        Assert.Null(cm.GetBadColor());
    }

    [Fact]
    public void LinearColorMap_UnderColor_CanBeSet()
    {
        IColorMap cm = new LinearColorMap("test", [new Color(0, 0, 0), new Color(255, 255, 255)])
        {
            UnderColor = new Color(0, 0, 200)
        };
        Assert.Equal(new Color(0, 0, 200), cm.GetUnderColor());
    }

    [Fact]
    public void LinearColorMap_OverColor_CanBeSet()
    {
        IColorMap cm = new LinearColorMap("test", [new Color(0, 0, 0), new Color(255, 255, 255)])
        {
            OverColor = new Color(200, 0, 0)
        };
        Assert.Equal(new Color(200, 0, 0), cm.GetOverColor());
    }

    [Fact]
    public void LinearColorMap_BadColor_CanBeSet()
    {
        IColorMap cm = new LinearColorMap("test", [new Color(0, 0, 0), new Color(255, 255, 255)])
        {
            BadColor = new Color(128, 128, 128)
        };
        Assert.Equal(new Color(128, 128, 128), cm.GetBadColor());
    }

    [Fact]
    public void ListedColorMap_UnderColor_CanBeSet()
    {
        IColorMap cm = new ListedColorMap("test", [new Color(255, 0, 0), new Color(0, 255, 0)])
        {
            UnderColor = new Color(0, 0, 255)
        };
        Assert.Equal(new Color(0, 0, 255), cm.GetUnderColor());
    }

    [Fact]
    public void ListedColorMap_BadColor_CanBeSet()
    {
        IColorMap cm = new ListedColorMap("test", [new Color(255, 0, 0)])
        {
            BadColor = new Color(50, 50, 50)
        };
        Assert.Equal(new Color(50, 50, 50), cm.GetBadColor());
    }

    [Fact]
    public void ReversedColorMap_SwapsUnderAndOver()
    {
        var inner = new LinearColorMap("base", [new Color(0, 0, 0), new Color(255, 255, 255)])
        {
            UnderColor = new Color(0, 0, 255),
            OverColor = new Color(255, 0, 0)
        };
        IColorMap reversed = new ReversedColorMap(inner);
        Assert.Equal(new Color(255, 0, 0), reversed.GetUnderColor()); // was over
        Assert.Equal(new Color(0, 0, 255), reversed.GetOverColor());  // was under
    }

    [Fact]
    public void ReversedColorMap_BadColor_Unchanged()
    {
        var inner = new LinearColorMap("base", [new Color(0, 0, 0), new Color(255, 255, 255)])
        {
            BadColor = new Color(128, 128, 128)
        };
        IColorMap reversed = new ReversedColorMap(inner);
        Assert.Equal(new Color(128, 128, 128), reversed.GetBadColor());
    }

    // --- LinearColorMap.FromList ---

    [Fact]
    public void FromList_TwoStops_EndpointsExact()
    {
        var red  = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var map  = LinearColorMap.FromList("fl_test_two",
            [new(0.0, red), new(1.0, blue)]);
        Assert.Equal(red,  map.GetColor(0.0));
        Assert.Equal(blue, map.GetColor(1.0));
    }

    [Fact]
    public void FromList_ThreeStops_MidpointInterpolates()
    {
        var black = new Color(0,   0,   0);
        var white = new Color(255, 255, 255);
        var red   = new Color(255, 0,   0);
        // stops at 0, 0.5, 1 — midpoint between black and red is at value=0.25
        var map = LinearColorMap.FromList("fl_test_three",
            [new(0.0, black), new(0.5, red), new(1.0, white)]);
        var c = map.GetColor(0.25);
        Assert.InRange(c.R, (byte)125, (byte)129);
        Assert.Equal((byte)0, c.G);
        Assert.Equal((byte)0, c.B);
    }

    [Fact]
    public void FromList_AutoNormalizes_WhenRangeNot01()
    {
        var black = new Color(0,   0,   0);
        var white = new Color(255, 255, 255);
        // positions 10 and 20 should be normalized to 0 and 1
        var map = LinearColorMap.FromList("fl_test_norm",
            [new(10.0, black), new(20.0, white)]);
        Assert.Equal(black, map.GetColor(0.0));
        Assert.Equal(white, map.GetColor(1.0));
    }

    [Fact]
    public void FromList_AutoRegisters_InRegistry()
    {
        LinearColorMap.FromList("fl_test_reg",
            [new(0.0, new Color(0, 0, 0)), new(1.0, new Color(255, 255, 255))]);
        Assert.NotNull(ColorMapRegistry.Get("fl_test_reg"));
        Assert.NotNull(ColorMapRegistry.Get("fl_test_reg_r"));
    }

    [Fact]
    public void FromList_SingleStop_Throws()
    {
        var c = new Color(0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            LinearColorMap.FromList("fl_test_single", [new(0.0, c)]));
    }

    [Fact]
    public void FromList_NonIncreasingPositions_Throws()
    {
        var c = new Color(0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            LinearColorMap.FromList("fl_test_nonincr",
                [new(0.0, c), new(0.0, c), new(1.0, c)]));
    }
}
