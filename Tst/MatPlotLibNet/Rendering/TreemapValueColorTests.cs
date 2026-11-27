// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A treemap rect can carry TWO variables: its area (<see cref="TreeNode.Value"/>) and its colour
/// from a second value (<see cref="TreeNode.ColorValue"/>) through the series' normalizer and colour map —
/// the host-map encoding (area = size, colour = load). Without it the ramp is driven by sibling INDEX, which
/// says nothing about the data.</summary>
public class TreemapValueColorTests
{
    private static readonly IColorMap RedToBlue = new LinearColorMap("test", [Colors.Red, Colors.Blue]);

    private static string Render(TreeNode root, Action<TreemapSeries>? configure = null) =>
        Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => { s.ColorMap = RedToBlue; s.ShowLabels = false; configure?.Invoke(s); }))
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ALeafWithAColorValue_IsColouredThroughTheMap", () =>
        {
            var root = new TreeNode
            {
                Label = "fleet",
                Children =
                [
                    new() { Label = "cold", Value = 10, ColorValue = 0 },
                    new() { Label = "hot", Value = 10, ColorValue = 100 },
                ]
            };
            return Render(root, s => { s.VMin = 0; s.VMax = 100; });
        }, Colors.Red.ToHex(), Colors.Blue.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
        // Without explicit limits the map spans the leaves' own ColorValue range — the convention every normalizable series shares
        // (VMin/VMax null ⇒ data min/max).
        new DrawnToken("WithoutLimits_TheRangeIsTheLeavesOwn", () =>
        {
            var root = new TreeNode
            {
                Children =
                [
                    new() { Label = "a", Value = 10, ColorValue = 40 },
                    new() { Label = "b", Value = 10, ColorValue = 60 },
                ]
            };
            return Render(root);
        }, Colors.Red.ToHex(), Colors.Blue.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("AnExplicitNodeColour_StillWins", () =>
        {
            var root = new TreeNode
            {
                Children = [new() { Label = "a", Value = 10, ColorValue = 100, Color = Colors.Green }]
            };
            return Render(root, s => { s.VMin = 0; s.VMax = 100; });
        }, Colors.Green.ToHex()) { Absent = [Colors.Blue.ToHex()], Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("ALeafWithoutAColorValue_KeepsTheIndexRamp", () =>
        {
            var root = new TreeNode { Children = [new() { Label = "a", Value = 10 }, new() { Label = "b", Value = 10 }] };
            return Render(root);
        }, Colors.Red.ToHex(), Colors.Blue.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
        // A boundary normalizer with one bin below 75 puts 50 at fraction 0 → the map's first stop.
        new DrawnToken("TheNormalizer_IsHonoured", () =>
        {
            var root = new TreeNode
            {
                Children = [new() { Label = "a", Value = 10, ColorValue = 50 }]
            };
            return Render(root, s =>
            {
                s.VMin = 0;
                s.VMax = 100;
                s.Normalizer = new BoundaryNormalizer([0, 75, 100]);
            });
        }, Colors.Red.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The alarm palette owns the three state hues; its ramp is the ONE colour map an ops caller
    /// reaches for, so "50 % is warning, 100 % is critical" is decided in one place.</summary>
    [Fact]
    public void TheAlarmPalette_RampsRestingThroughWarningToCritical()
    {
        var palette = AlarmPalette.Default;

        IColorMap ramp = palette.Ramp;

        Assert.Equal(palette.Resting, ramp.GetColor(0));
        Assert.Equal(palette.Warning, ramp.GetColor(0.5));
        Assert.Equal(palette.Critical, ramp.GetColor(1));
    }
}
