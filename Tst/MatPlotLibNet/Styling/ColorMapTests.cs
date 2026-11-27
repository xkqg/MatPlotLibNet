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
        // matplotlib 3.11.2: colormaps['hsv'](0.0, bytes=True) is (255, 0, 0) and (1.0) is (255, 0, 23), a hue of 351 degrees: the
        // hue wheel stops a step short of red, as the library's own map did not.
        Assert.True(diff <= (map.Name == "hsv" ? 24 : 3),
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

    [Theory]
    [InlineData(0.0, -0.5)]
    [InlineData(1.0, 1.5)]
    public void GetColor_ClampsBelow0AndAbove1(double atTheEnd, double beyond)
    {
        Assert.Equal(ColorMaps.Viridis.GetColor(atTheEnd), ColorMaps.Viridis.GetColor(beyond));
    }

    // The look of each map at its endpoints is a row of ColorMapEndpointTests.

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

    [Theory]
    [InlineData(0.0, -0.5)]
    [InlineData(1.0, 1.5)]
    public void ListedColorMap_AnInputOutsideZeroToOne_Clamped(double atTheEnd, double beyond)
    {
        var cm = new ListedColorMap("test", [new Color(10, 0, 0), new Color(100, 0, 0)]);
        Assert.Equal(cm.GetColor(atTheEnd), cm.GetColor(beyond));
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

    /// <summary>Both factories hold their stops to the one rule: two or more, at positions that strictly increase, refused by the
    /// name of the parameter that holds them.</summary>
    [Theory]
    [InlineData(true, new[] { 0.5 })]
    [InlineData(true, new[] { 0.0, 0.0, 1.0 })]
    [InlineData(false, new[] { 0.5 })]
    [InlineData(false, new[] { 0.5, 0.0, 1.0 })]
    public void LinearColorMap_StopsThatAreNotTwoOrMoreRising_AreRefused(bool fromList, double[] positions)
    {
        ColorStop[] stops = [.. positions.Select(p => new ColorStop(p, new Color(0, 0, 0)))];

        var refused = Assert.Throws<ArgumentException>(() => fromList ? LinearColorMap.FromList("refused stops", stops) : LinearColorMap.FromPositions("test", stops));

        Assert.Equal(fromList ? "colors" : "stops", refused.ParamName);
    }

    // --- B4: Extreme value support ---

    /// <summary>Either kind of map takes an under, an over (the linear one) and a bad colour as it is given and hands it back.</summary>
    [Theory]
    [InlineData(true, "under")]
    [InlineData(true, "over")]
    [InlineData(true, "bad")]
    [InlineData(false, "under")]
    [InlineData(false, "bad")]
    public void AColorMap_ExtremeColor_CanBeSet(bool linear, string extreme)
    {
        var color = new Color(11, 22, 33);
        Color[] stops = [new Color(0, 0, 0), new Color(255, 255, 255)];
        IColorMap cm = (linear, extreme) switch
        {
            (true, "under") => new LinearColorMap("test", stops) { UnderColor = color },
            (true, "over") => new LinearColorMap("test", stops) { OverColor = color },
            (true, _) => new LinearColorMap("test", stops) { BadColor = color },
            (false, "under") => new ListedColorMap("test", stops) { UnderColor = color },
            _ => new ListedColorMap("test", stops) { BadColor = color },
        };

        Assert.Equal(color, extreme switch { "under" => cm.GetUnderColor(), "over" => cm.GetOverColor(), _ => cm.GetBadColor() });
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
}
