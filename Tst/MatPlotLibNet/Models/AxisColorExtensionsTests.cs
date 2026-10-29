// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies the one rule that colours an axis and the spine standing on its edge: the four colour verbs
/// (<c>SetYAxisColor</c> and <c>SetXAxisColor</c> on the builder, the right Y and the top X on their builders) all call
/// it, so what it does to an edge is the same for each of them.</summary>
public class AxisColorExtensionsTests
{
    private static readonly Color Mark = Color.FromHex("#E4007C");

    private static readonly string[] Edges = ["Top", "Bottom", "Left", "Right"];

    public static TheoryData<string> EdgeNames { get; } = new(Edges);

    /// <summary>The colour each of the four spines carries, keyed by the edge it stands on.</summary>
    private static Dictionary<string, Color?> SpineColours(Axes axes) => new()
    {
        ["Top"] = axes.Spines.Top.Color,
        ["Bottom"] = axes.Spines.Bottom.Color,
        ["Left"] = axes.Spines.Left.Color,
        ["Right"] = axes.Spines.Right.Color,
    };

    [Theory]
    [MemberData(nameof(EdgeNames))]
    public void ApplyAxisColor_WithAnEdge_ColoursTheAxisAndExactlyThatSpine(string edge)
    {
        var axes = new Axes();
        var axis = new Axis();

        axes.ApplyAxisColor(axis, Enum.Parse<SpineEdge>(edge), Mark);

        Assert.Equal(Mark, axis.MajorTicks.Color);
        Assert.Equal(Mark, axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axis.LabelStyle!.Color);
        foreach (var (name, colour) in SpineColours(axes))
        {
            Assert.Equal(name == edge ? (Color?)Mark : null, colour);
        }
    }

    [Fact]
    public void ApplyAxisColor_WithoutAnEdge_ColoursTheAxisAndLeavesEverySpineUntouched()
    {
        var axes = new Axes();
        var axis = new Axis();

        axes.ApplyAxisColor(axis, spine: null, Mark);

        Assert.Equal(Mark, axis.MajorTicks.Color);
        Assert.Equal(Mark, axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axis.LabelStyle!.Color);
        Assert.Equal(new SpinesConfig(), axes.Spines);
    }

    [Fact]
    public void ApplyAxisColor_KeepsEverythingElseTheSpineAndTheAxisAlreadyCarry()
    {
        var axes = new Axes { Spines = new SpinesConfig { Left = new SpineConfig { LineWidth = 3, Visible = false } } };
        var axis = new Axis
        {
            MajorTicks = new TickConfig { Length = 9, LabelRotation = 45 },
            LabelStyle = new TextStyle { FontSize = 17 },
        };

        axes.ApplyAxisColor(axis, SpineEdge.Left, Mark);

        Assert.Equal(9, axis.MajorTicks.Length);
        Assert.Equal(45, axis.MajorTicks.LabelRotation);
        Assert.Equal(17, axis.LabelStyle!.FontSize);
        Assert.Equal(3, axes.Spines.Left.LineWidth);
        Assert.False(axes.Spines.Left.Visible);
        Assert.Equal(Mark, axes.Spines.Left.Color);
    }
}
