// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The theme's rule for the ticks of an axis that names no locator (<see cref="AxisTickRule"/>): the ladder it lays them by
/// and whether it rounds numbers, read once off the theme's looks (<see cref="ThemeLooks.TickRule"/>).</summary>
public class AxisTickRuleTests
{
    [Fact]
    public void TheLibrarysOwnThemes_NameNoLadder_AndRoundNoNumbers()
    {
        var rule = Theme.Default.Looks.TickRule;

        Assert.Null(rule.Ladder);
        Assert.False(rule.RoundNumbers);
        Assert.Equal(AxisTickRule.Library, rule);
    }

    [Fact]
    public void TheDefaultStyle_LaysItsLadderAlongTheAxis_AndRoundsNoNumbers()
    {
        var rule = Theme.MatplotlibV2.Looks.TickRule;

        Assert.Equal(TickLadder.DefaultStyle.Steps, rule.Ladder!.Value.Steps);
        Assert.True(rule.Ladder.Value.BinsByLength);
        Assert.False(rule.RoundNumbers);
    }

    /// <summary>matplotlib's classic style sets <c>axes.autolimit_mode</c> to <c>'round_numbers'</c>, which its <c>MaxNLocator</c> reads
    /// in <c>_raw_ticks</c>, and its classic mode, which its <c>LogLocator</c> reads: the library holds both as the one rule.</summary>
    [Fact]
    public void TheClassicStyle_LaysItsNineBins_AndRoundsNumbers()
    {
        var rule = Theme.MatplotlibClassic.Looks.TickRule;

        Assert.Equal(TickLadder.Classic.Steps, rule.Ladder!.Value.Steps);
        Assert.Equal(9, rule.Ladder.Value.Bins);
        Assert.True(rule.RoundNumbers);
        Assert.Equal(TickLadder.Classic.Steps, rule.FallbackLadder.Steps);
    }

    [Theory]
    [InlineData("RoundNumbers", true)]
    [InlineData("Data", false)]
    [InlineData(null, false)]
    public void TheRuleRoundsNumbers_OnlyWhereTheRangeEndsAtRoundNumbers(string? mode, bool rounds)
    {
        AutolimitMode? autolimit = mode is null ? null : Enum.Parse<AutolimitMode>(mode);
        var looks = ThemeLooks.Library with { Range = AxisRangeLook.Library with { Autolimit = autolimit } };

        Assert.Equal(rounds, looks.TickRule.RoundNumbers);
    }

    /// <summary>A theme that names no ladder (a hand-written theme that rounds numbers, a library theme) lays the ticks a log or logit
    /// scale falls back to by the default style's <c>AutoLocator</c>, as it did.</summary>
    [Fact]
    public void ARuleWithNoLadder_FallsBackToTheDefaultStylesLadder()
    {
        var rule = new AxisTickRule(null, RoundNumbers: true);

        Assert.Equal(TickLadder.DefaultStyle.Steps, rule.FallbackLadder.Steps);
        Assert.True(rule.FallbackLadder.BinsByLength);
    }
}
