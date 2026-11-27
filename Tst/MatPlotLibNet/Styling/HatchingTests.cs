// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A pattern and the colour of its strokes that a series, a patch or a tree node names are one value, <see cref="HatchFill"/>,
/// read as <c>Hatching</c> and set as <c>LayHatching</c>; each wire form takes and gives the same value and keeps its own bytes.</summary>
public class HatchingTests
{
    private static readonly HatchFill Named = new(HatchPattern.Cross, Colors.Red);

    // ── the wire: one read rule, beside the write rule ────────────────────────────────────────────────────────

    /// <summary>What a document carries reads back as one value: a pattern that is absent is no pattern, its colour as it is; the read
    /// half of <see cref="HatchFill.PatternOnTheWire"/>, which every wire form (a series, a patch, a tree node) asks.</summary>
    [Fact]
    public void WhatTheWireCarries_ReadsBackAsOneHatching_AnAbsentPatternBeingNone()
    {
        Assert.Equal(new HatchFill(HatchPattern.None, Colors.Red), HatchFill.FromTheWire(null, Colors.Red));
        Assert.Equal(Named, HatchFill.FromTheWire(HatchPattern.Cross, Colors.Red));
        Assert.Equal(HatchFill.None, HatchFill.FromTheWire((Named with { Pattern = HatchPattern.None }).PatternOnTheWire, null));
    }

    // ── the models ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void APatch_ReadsItsPatternAndColourAsOneHatching()
    {
        var patch = new Rectangle(new(0, 0), 2, 1) { Hatch = HatchPattern.Cross, HatchColor = Colors.Red };

        Assert.Equal(Named, patch.Hatching);
    }

    [Fact]
    public void APatch_TakesAHatching_AsItsPatternAndColour()
    {
        var patch = new Rectangle(new(0, 0), 2, 1);

        patch.LayHatching(Named);

        Assert.Equal(HatchPattern.Cross, patch.Hatch);
        Assert.Equal(Colors.Red, patch.HatchColor);
    }

    [Fact]
    public void ATreeNode_ReadsItsPatternAndColourAsOneHatching()
    {
        var node = new TreeNode { Hatch = HatchPattern.Cross, HatchColor = Colors.Red };

        Assert.Equal(Named, node.Hatching);
        Assert.Equal(HatchFill.None, new TreeNode().Hatching);
    }

    // ── the wire forms ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASeriesDto_WritesNoHatchBytes_ForNoHatching()
    {
        var dto = new SeriesDto { Hatching = HatchFill.None };

        Assert.Null(dto.Hatch);
        Assert.Null(dto.HatchColor);
        Assert.Equal(HatchFill.None, dto.Hatching);
    }

    [Fact]
    public void ASeriesDto_CarriesAHatching_AsItsTwoFields()
    {
        var dto = new SeriesDto { Hatching = Named };

        Assert.Equal(HatchPattern.Cross, dto.Hatch);
        Assert.Equal(Colors.Red, dto.HatchColor);
        Assert.Equal(Named, dto.Hatching);
    }

    [Fact]
    public void APatchDto_WritesNoPatternForNone_AndTheColourAsNamed()
    {
        var plain = new Rectangle(new(0, 0), 2, 1).Describe();
        var hatched = new Rectangle(new(0, 0), 2, 1) { Hatch = HatchPattern.Cross, HatchColor = Colors.Red }.Describe();

        Assert.Null(plain.Hatch);
        Assert.Null(plain.HatchColor);
        Assert.Equal(HatchPattern.Cross, hatched.Hatch);
        Assert.Equal(Colors.Red, hatched.HatchColor);
    }

    [Fact]
    public void APatch_KeepsItsHatching_ThroughItsDocument()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(
            new Rectangle(new(0, 0), 2, 1) { Hatch = HatchPattern.Cross, HatchColor = Colors.Red })).Build();

        var back = figure.RoundTripped();

        var patch = Assert.Single(back.SubPlots[0].Patches);
        Assert.Equal(Named, patch.Hatching);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("APatchWithNoHatching_WritesNoHatchKey", () =>
        {
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(new Rectangle(new(0, 0), 2, 1))).Build();
            return new ChartSerializer().ToJson(figure);
        }) { Absent = ["hatch"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ATreeNodeDto_AlwaysWritesItsPattern_AndCarriesAHatchingBothWays()
    {
        var node = new TreeNode { Label = "a", Value = 1, Hatch = HatchPattern.Cross, HatchColor = Colors.Red };

        var dto = node.TreeNodeToDto();
        var back = dto.DtoToTreeNode();

        Assert.Equal(Named, dto.Hatching);
        Assert.Equal(HatchPattern.Cross, dto.Hatch);
        Assert.Equal(Colors.Red, dto.HatchColor);
        Assert.Equal(Named, back.Hatching);
        Assert.Equal(HatchPattern.None, new TreeNode { Label = "b" }.TreeNodeToDto().Hatch);
    }
}
