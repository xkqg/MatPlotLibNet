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

    [Fact]
    public void ALeafWithAColorValue_IsColouredThroughTheMap()
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

        string svg = Render(root, s => { s.VMin = 0; s.VMax = 100; });

        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Colors.Blue.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Without explicit limits the map spans the leaves' own ColorValue range — the convention every
    /// normalizable series shares (<c>VMin</c>/<c>VMax</c> null ⇒ data min/max).</summary>
    [Fact]
    public void WithoutLimits_TheRangeIsTheLeavesOwn()
    {
        var root = new TreeNode
        {
            Children =
            [
                new() { Label = "a", Value = 10, ColorValue = 40 },
                new() { Label = "b", Value = 10, ColorValue = 60 },
            ]
        };

        string svg = Render(root);

        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Colors.Blue.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnExplicitNodeColour_StillWins()
    {
        var root = new TreeNode
        {
            Children = [new() { Label = "a", Value = 10, ColorValue = 100, Color = Colors.Green }]
        };

        string svg = Render(root, s => { s.VMin = 0; s.VMax = 100; });

        Assert.Contains(Colors.Green.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Colors.Blue.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ALeafWithoutAColorValue_KeepsTheIndexRamp()
    {
        var root = new TreeNode { Children = [new() { Label = "a", Value = 10 }, new() { Label = "b", Value = 10 }] };

        string svg = Render(root);

        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Colors.Blue.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheNormalizer_IsHonoured()
    {
        var root = new TreeNode
        {
            Children = [new() { Label = "a", Value = 10, ColorValue = 50 }]
        };

        // A boundary normalizer with one bin below 75 puts 50 at fraction 0 → the map's first stop.
        string svg = Render(root, s =>
        {
            s.VMin = 0;
            s.VMax = 100;
            s.Normalizer = new BoundaryNormalizer([0, 75, 100]);
        });

        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

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
