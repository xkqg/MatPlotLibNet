// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>What the library adds to a normaliser (<see cref="INormalizerRule"/>, internal beside the public <see cref="INormalizer"/>):
/// each normaliser answers for itself which normaliser a theme reads it as, what it masks, where the middle of a band stands, which scale
/// its range and its contour levels are taken over and whether a colour bar names decades; a normaliser the library does not know
/// answers as a linear one. No site tests which type a normaliser is.</summary>
public class NormalizerRuleTests
{
    [Fact]
    public void MatplotlibsLogNorm_MasksZeroAndBelow_OverARangeThatHasALogarithm()
    {
        var rule = LogNorm.Instance.Rule;

        Assert.True(rule.MasksOver(1));
        Assert.False(rule.MasksOver(0));
        Assert.True(rule.Masks(0, 1));
        Assert.True(rule.Masks(-2, 1));
        Assert.False(rule.Masks(3, 1));
        Assert.False(rule.Masks(0, 0));
    }

    [Fact]
    public void EveryOtherNormaliser_MasksNothing()
    {
        foreach (var normalizer in new INormalizer[] { LinearNormalizer.Instance, new LogNormalizer(), new TestNormalizer((_, _, _) => 0.5), new BoundaryNormalizer([0, 1, 2]) })
        {
            Assert.False(normalizer.Rule.MasksOver(1));
            Assert.False(normalizer.Rule.Masks(0, 1));
        }
    }

    /// <summary>matplotlib 3.11.2 <c>_process_layers</c>: a log-scaled filled contour colours a band at <c>sqrt(lower) * sqrt(upper)</c>.</summary>
    [Fact]
    public void TheMiddleOfABand_IsGeometricUnderTheLogNorm_AndArithmeticOtherwise()
    {
        Assert.Equal(Math.Sqrt(10) * Math.Sqrt(1000), LogNorm.Instance.Rule.MiddleOf(10, 1000));
        Assert.Equal(505, new LogNormalizer().Rule.MiddleOf(10, 1000));
        Assert.Equal(505, new TestNormalizer((_, _, _) => 0.5).Rule.MiddleOf(10, 1000));
        Assert.Equal(-0.5, LogNorm.Instance.Rule.MiddleOf(-1, 0));
    }

    [Fact]
    public void TheRangeIsTakenOverTheLogScale_UnderTheLogNormOnly_AndTheLevelsUnderEitherLogNormaliser()
    {
        Assert.Same(ScaleRule.Log, LogNorm.Instance.Rule.RangeScale);
        Assert.Same(ScaleRule.Linear, new LogNormalizer().Rule.RangeScale);
        Assert.Same(ScaleRule.Linear, LinearNormalizer.Instance.Rule.RangeScale);
        Assert.Same(ScaleRule.Log, LogNorm.Instance.Rule.LevelScale);
        Assert.Same(ScaleRule.Log, new LogNormalizer().Rule.LevelScale);
        Assert.Same(ScaleRule.Linear, new TestNormalizer((_, _, _) => 0.5).Rule.LevelScale);
    }

    [Fact]
    public void ALogNormalizer_IsReadAsTheLogNorm_UnderAThemeThatColoursSo()
    {
        var named = new LogNormalizer();

        Assert.Same(LogNorm.Instance, named.Rule.ReadUnder(Theme.MatplotlibV2.Looks.ColorMap));
        Assert.Null(named.Rule.ReadUnder(Theme.Default.Looks.ColorMap));
        Assert.Null(LinearNormalizer.Instance.Rule.ReadUnder(Theme.MatplotlibV2.Looks.ColorMap));
    }

    [Fact]
    public void ABoundaryNormalizer_IsThinnedForABarThatStepsPerColour()
    {
        var bands = new BoundaryNormalizer([0, 1, 2, 3]);
        var stepping = new ColorBarLook { StepPerColour = true };

        Assert.IsType<BoundaryNormalizer>(bands.Rule.ForColorBarUnder(stepping));
        Assert.NotSame(bands, bands.Rule.ForColorBarUnder(stepping));
        Assert.Null(bands.Rule.ForColorBarUnder(new ColorBarLook()));
        Assert.Null(LinearNormalizer.Instance.Rule.ForColorBarUnder(stepping));
    }

    [Fact]
    public void ABarNamesDecades_UnderTheLogNorm_OverARangeAbove0()
    {
        Assert.True(LogNorm.Instance.Rule.NamesDecadesOver(0.5));
        Assert.False(LogNorm.Instance.Rule.NamesDecadesOver(0));
        Assert.False(new LogNormalizer().Rule.NamesDecadesOver(0.5));
    }
}
