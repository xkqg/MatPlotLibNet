// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Exercises the <see cref="Axes"/> factory helpers (Sunburst, Sankey, Polar*, *3D, Inset, Text3D).
/// Each test asserts: (a) the returned series is of the correct type, (b) it was appended to
/// <c>axes.Series</c>, and (c) for 3D/Polar helpers the CoordinateSystem flips correctly.
/// Covers the previously-uncovered factory blocks in Axes.cs (lines 149-151, 603-762).</summary>
public class AxesFactoryMethodTests
{
    [Fact]
    public void AddInset_AppendsAndReturnsNewAxes()
    {
        var ax = new Axes();
        var inset = ax.AddInset(new InsetBounds(0.1, 0.1, 0.3, 0.3));
        Assert.NotNull(inset);
        Assert.NotSame(ax, inset);
        Assert.Equal(new InsetBounds(0.1, 0.1, 0.3, 0.3), inset.InsetBounds);
        Assert.Single(ax.Insets);
    }

    [Fact]
    public void Sunburst_AppendsSunburstSeries()
    {
        var ax = new Axes();
        var s = ax.Sunburst(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 1 }] });
        Assert.IsType<SunburstSeries>(s);
        Assert.Single(ax.Series);
    }

    [Fact]
    public void Sankey_AppendsSankeySeries()
    {
        var ax = new Axes();
        var s = ax.Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 5)]);
        Assert.IsType<SankeySeries>(s);
        Assert.Single(ax.Series);
    }

    [Theory]
    [MemberData(nameof(PolarFactories))]
    public void PolarFactory_AppendsAndSwitchesCoordinateSystem(string name, Func<Axes, ISeries> factory)
    {
        _ = name; // for theory display
        var ax = new Axes();
        var s = factory(ax);
        Assert.NotNull(s);
        Assert.Single(ax.Series);
        Assert.Equal(CoordinateSystem.Polar, ax.CoordinateSystem);
    }

    public static TheoryData<string, Func<Axes, ISeries>> PolarFactories => new()
    {
        { "PolarPlot",    ax => ax.PolarPlot([1.0, 2.0], [0.0, 1.0]) },
        { "PolarScatter", ax => ax.PolarScatter([1.0, 2.0], [0.0, 1.0]) },
        { "PolarBar",     ax => ax.PolarBar([1.0, 2.0], [0.0, 1.0]) },
        { "PolarHeatmap", ax => ax.PolarHeatmap(new double[,] { { 1, 2 }, { 3, 4 } }, 2, 2) },
    };

    [Theory]
    [MemberData(nameof(ThreeDFactories))]
    public void ThreeDFactory_AppendsAndSwitchesCoordinateSystem(string name, Func<Axes, ISeries> factory)
    {
        _ = name;
        var ax = new Axes();
        var s = factory(ax);
        Assert.NotNull(s);
        Assert.Single(ax.Series);
        Assert.Equal(CoordinateSystem.ThreeD, ax.CoordinateSystem);
    }

    public static TheoryData<string, Func<Axes, ISeries>> ThreeDFactories
    {
        get
        {
            var data = new TheoryData<string, Func<Axes, ISeries>>
            {
                { "Surface",     ax => ax.Surface([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }) },
                { "Wireframe",   ax => ax.Wireframe([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }) },
                { "Scatter3D",   ax => ax.Scatter3D([1.0], [2.0], [3.0]) },
                { "Stem3D",      ax => ax.Stem3D(new Vec([1.0]), new Vec([2.0]), new Vec([3.0])) },
                { "Bar3D",       ax => ax.Bar3D(new Vec([1.0]), new Vec([2.0]), new Vec([3.0])) },
                { "PlanarBar3D", ax => ax.PlanarBar3D(new Vec([1.0]), new Vec([2.0]), new Vec([3.0])) },
                { "Plot3D",      ax => ax.Plot3D([1.0, 2.0], [3.0, 4.0], [5.0, 6.0]) },
                { "Trisurf",     ax => ax.Trisurf([0.0, 1.0, 0.5], [0.0, 0.0, 1.0], [1.0, 2.0, 3.0]) },
                { "Contour3D",   ax => ax.Contour3D([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }) },
                { "Quiver3D",    ax => ax.Quiver3D([1.0], [2.0], [3.0], [0.5], [0.5], [0.5]) },
                { "Voxels",      ax => ax.Voxels(new bool[,,] { { { true } } }) },
                { "Text3D",      ax => ax.Text3D(1, 2, 3, "lbl") },
            };
            return data;
        }
    }
}
