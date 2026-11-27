// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>How the step between two major ticks of a linear axis is split into minor ones (<see cref="AutoMinorLocator"/>, matplotlib's
/// <c>AutoMinorLocator</c>): in five under the library's own themes, and in matplotlib's <c>ndivs = 'auto'</c> parts under the matplotlib
/// themes, a value of the theme's tick look.</summary>
public class AutoMinorLocatorTests
{
    private static double[] Walk(AutoMinorLocator split, params double[] majors)
    {
        var values = new List<double>();
        foreach (double value in split.Between(majors)) values.Add(value);
        return [.. values];
    }

    [Fact]
    public void TheLibrarysSplit_IsFifths()
    {
        Assert.Equal(5, AutoMinorLocator.Library.Divisions);
        Assert.Equal([0.4, 0.8, 1.2, 1.6], Walk(AutoMinorLocator.Library, 0, 2).Where(value => value is > 0 and < 2));
        Assert.Equal([0.5, 1.0, 1.5], Walk(AutoMinorLocator.Auto, 0, 2).Where(value => value is > 0 and < 2));
    }

    /// <summary>matplotlib 3.11.2's <c>AutoMinorLocator.__call__</c> with <c>ndivs == 'auto'</c>: <c>majorstep_mantissa = 10 **
    /// (np.log10(majorstep) % 1)</c>, <c>ndivs = 5 if np.isclose(majorstep_mantissa, [1, 2.5, 5, 10]).any() else 4</c>; printed for the
    /// steps 1, 2, 2.5, 5, 0.2, 25 and 4: 5, 4, 5, 5, 4, 5, 4.</summary>
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 4)]
    [InlineData(2.5, 5)]
    [InlineData(5, 5)]
    [InlineData(0.2, 4)]
    [InlineData(25, 5)]
    [InlineData(4, 4)]
    public void TheAutomaticSplit_IsFiveWhereTheStepsMantissaIsRound_FourElsewhere(double step, int parts)
    {
        Assert.Null(AutoMinorLocator.Auto.Divisions);
        Assert.Equal(parts, AutoMinorLocator.Auto.PartsOf(step));
        Assert.Equal(5, AutoMinorLocator.Library.PartsOf(step));
    }

    [Fact]
    public void FewerThanTwoMajors_SplitNothing()
    {
        Assert.Empty(Walk(AutoMinorLocator.Auto, 1));
        Assert.Empty(Walk(AutoMinorLocator.Library));
    }

    [Fact]
    public void TheTickLook_SaysWhichSplit()
    {
        Assert.Equal(AutoMinorLocator.Library, Theme.Default.Looks.Ticks.MinorSplit);
        Assert.Equal(AutoMinorLocator.Auto, Theme.MatplotlibV2.Looks.Ticks.MinorSplit);
        Assert.Equal(AutoMinorLocator.Auto, Theme.MatplotlibClassic.Looks.Ticks.MinorSplit);
    }

    /// <summary>A theme read from JSON splits as matplotlib does where it says its minor ticks are matplotlib's minor locator's, the one key
    /// the split has always followed.</summary>
    [Fact]
    public void ATheme_ReadFromJson_SplitsByTheKeyItsMinorTicksFollow()
    {
        Assert.Equal(AutoMinorLocator.Auto, Theme.MatplotlibV2.ThroughDto().Looks.Ticks.MinorSplit);
        Assert.Equal(AutoMinorLocator.Library, Theme.Default.ThroughDto().Looks.Ticks.MinorSplit);
    }
}
