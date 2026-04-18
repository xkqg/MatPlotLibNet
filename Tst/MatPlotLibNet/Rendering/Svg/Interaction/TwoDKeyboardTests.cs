// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.1 of the v1.7.2 follow-on plan — keyboard parity for 2D
/// zoom/pan/reset. The code handler in
/// <c>SvgInteractivityScript.cs</c> (L209-221) is in place and correct; this
/// Theory pins its behaviour end-to-end via the Jint harness so keyboard
/// users (WCAG 2.1 AA) and matplotlib-muscle-memory users get the expected
/// viewBox mutations every time.
///
/// <para>Matplotlib reference: <c>backend_bases.py:NavigationToolbar2</c>
/// keymap has <c>home = h, r, home</c>; <c>zoom</c> / <c>pan</c> shortcuts
/// are widget-driven (not keybound by default). We adopt
/// <c>+ / = / - / Arrows / Home</c> for browser-SVG parity with the more
/// discoverable set matplotlib's WebAgg backend actually uses.</para></summary>
public class TwoDKeyboardTests
{
    /// <summary>Reads the current viewBox as `[x, y, w, h]`.</summary>
    private static double[] Vb(InteractionScriptHarness h) =>
        h.GetAttribute("svg", "viewBox")!
            .Split(' ')
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();

    private static InteractionScriptHarness Build2D() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithZoomPan()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

    public static IEnumerable<object[]> KeyboardZoomCases() =>
    [
        // key, expectedWidthRatio, expectedHeightRatio (dx: -5% shift when zooming in)
        ["+", 0.9, 0.9],
        ["=", 0.9, 0.9],
        ["-", 1.1, 1.1],
    ];

    [Theory]
    [MemberData(nameof(KeyboardZoomCases))]
    public void KeyboardZoom_AppliesExpectedViewBoxScale(string key, double wRatio, double hRatio)
    {
        using var h = Build2D();
        var before = Vb(h);

        h.Simulate("svg", "keydown", e => { e.key = key; });

        var after = Vb(h);
        Assert.Equal(wRatio, after[2] / before[2], 3);
        Assert.Equal(hRatio, after[3] / before[3], 3);
    }

    public static IEnumerable<object[]> KeyboardPanCases() =>
    [
        // key, expectedDxSign, expectedDySign  (−1 = viewBox[0 or 1] decreases, +1 = increases)
        ["ArrowLeft",  -1,  0],
        ["ArrowRight",  1,  0],
        ["ArrowUp",     0, -1],
        ["ArrowDown",   0,  1],
    ];

    [Theory]
    [MemberData(nameof(KeyboardPanCases))]
    public void KeyboardPan_ShiftsViewBox(string key, int dxSign, int dySign)
    {
        using var h = Build2D();
        var before = Vb(h);

        h.Simulate("svg", "keydown", e => { e.key = key; });

        var after = Vb(h);
        if (dxSign != 0) Assert.True(Math.Sign(after[0] - before[0]) == dxSign, $"key={key} expected vb[0] sign {dxSign}, got {after[0] - before[0]}");
        else             Assert.Equal(before[0], after[0], 6);
        if (dySign != 0) Assert.True(Math.Sign(after[1] - before[1]) == dySign, $"key={key} expected vb[1] sign {dySign}, got {after[1] - before[1]}");
        else             Assert.Equal(before[1], after[1], 6);
    }

    [Fact]
    public void HomeKey_RestoresOriginalViewBox()
    {
        using var h = Build2D();
        var original = Vb(h);

        // Pan + zoom off the original.
        h.Simulate("svg", "keydown", e => { e.key = "ArrowRight"; });
        h.Simulate("svg", "keydown", e => { e.key = "+"; });
        Assert.NotEqual(original[0], Vb(h)[0]);
        Assert.NotEqual(original[2], Vb(h)[2]);

        h.Simulate("svg", "keydown", e => { e.key = "Home"; });

        var restored = Vb(h);
        for (int i = 0; i < 4; i++)
            Assert.Equal(original[i], restored[i], 6);
    }

    [Fact]
    public void DoubleClick_RestoresOriginalViewBox()
    {
        using var h = Build2D();
        var original = Vb(h);

        h.Simulate("svg", "keydown", e => { e.key = "ArrowRight"; });
        h.Simulate("svg", "keydown", e => { e.key = "+"; });
        Assert.NotEqual(original[2], Vb(h)[2]);

        h.Simulate("svg", "dblclick", _ => { });

        var restored = Vb(h);
        for (int i = 0; i < 4; i++)
            Assert.Equal(original[i], restored[i], 6);
    }
}
