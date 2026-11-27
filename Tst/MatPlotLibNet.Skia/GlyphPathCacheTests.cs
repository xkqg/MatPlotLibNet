// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A label's outline is made once. The same text in the same face at the same size always gives the same
/// outline, and shaping it again on every draw was 45 % of a frame while a 3-D surface was turned beside three panels,
/// so the provider keeps what it made: a second ask hands back the very string the first one made, a colour change
/// reuses it (colour is not part of an outline), and a size, a weight or a fresh font resolution makes a new one.</summary>
public sealed class GlyphPathCacheTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    [Fact]
    public void TheSameTextAndFont_AskedTwice_GiveTheStringMadeTheFirstTime()
    {
        var provider = new SkiaGlyphPathProvider();

        string? first = provider.GetPathData("Latency (ms)", Font13);
        string? second = provider.GetPathData("Latency (ms)", Font13);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AKeptOutline_IsWhatAProviderThatNeverSawTheTextMakes()
    {
        var provider = new SkiaGlyphPathProvider();
        string? kept = provider.GetPathData("12:00:30", Font13);
        _ = provider.GetPathData("12:00:30", Font13);

        string? fresh = new SkiaGlyphPathProvider().GetPathData("12:00:30", Font13);

        Assert.Equal(fresh, kept);
        Assert.NotSame(fresh, kept);
    }

    [Fact]
    public void AnEqualFont_ThatIsAnotherObject_FindsTheKeptOutline()
    {
        var provider = new SkiaGlyphPathProvider();
        string? first = provider.GetPathData("Ta", Font13);

        string? second = provider.GetPathData("Ta", new Font { Family = "DejaVu Sans", Size = 13 });

        Assert.Same(first, second);
    }

    [Fact]
    public void AnotherColour_ReusesTheOutline()
    {
        var provider = new SkiaGlyphPathProvider();
        string? plain = provider.GetPathData("Ta", Font13);

        string? red = provider.GetPathData("Ta", Font13 with { Color = Colors.Red });

        Assert.Same(plain, red);
    }

    [Theory]
    [InlineData(26, FontWeight.Normal, "Ta")]
    [InlineData(13, FontWeight.Bold, "Ta")]
    [InlineData(13, FontWeight.Normal, "aT")]
    public void AnotherSizeWeightOrText_IsAnotherOutline_TheOneAFreshProviderMakes(double size, FontWeight weight, string text)
    {
        var provider = new SkiaGlyphPathProvider();
        string? baseline = provider.GetPathData("Ta", Font13);
        var font = Font13 with { Size = size, Weight = weight };

        string? changed = provider.GetPathData(text, font);

        Assert.NotEqual(baseline, changed);
        Assert.Equal(new SkiaGlyphPathProvider().GetPathData(text, font), changed);
    }

    [Fact]
    public void AnEmptyText_IsStillNoOutline_AndIsNotKept()
    {
        var provider = new SkiaGlyphPathProvider();

        Assert.Null(provider.GetPathData("", Font13));
        Assert.Equal(0, provider.Shaped.Count);
    }

    [Fact]
    public void AFullStore_StartsOver_AndStillAnswersRight()
    {
        var provider = new SkiaGlyphPathProvider();
        for (int i = 0; i < ShapedTextStore.Capacity; i++)
            _ = provider.GetPathData(i.ToString(System.Globalization.CultureInfo.InvariantCulture), Font13);
        Assert.Equal(ShapedTextStore.Capacity, provider.Shaped.Count);

        string? past = provider.GetPathData("past the capacity", Font13);

        Assert.Equal(1, provider.Shaped.Count);
        Assert.Equal(new SkiaGlyphPathProvider().GetPathData("past the capacity", Font13), past);
    }

    [Fact]
    public void ManyThreadsAskingAtOnce_AllGetTheSameOutline()
    {
        var provider = new SkiaGlyphPathProvider();
        var outlines = new string?[64];

        Parallel.For(0, outlines.Length, i => outlines[i] = provider.GetPathData("concurrent", Font13));

        Assert.All(outlines, o => Assert.Same(outlines[0], o));
        Assert.Equal(1, provider.Shaped.Count);
    }
}

