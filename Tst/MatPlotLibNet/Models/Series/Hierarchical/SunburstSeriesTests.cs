// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SunburstSeries"/> default properties and construction.</summary>
public class SunburstSeriesTests
{
    private static TreeNode SampleTree => new()
    {
        Label = "Root",
        Children = [
            new TreeNode { Label = "A", Value = 30 },
            new TreeNode { Label = "B", Value = 70 }
        ]
    };

    /// <summary>Verifies that the constructor stores the root node.</summary>
    [Fact]
    public void Constructor_StoresRoot()
    {
        var root = SampleTree;
        var series = new SunburstSeries(root);
        Assert.Same(root, series.Root);
    }

    /// <summary>Verifies that InnerRadius defaults to 0.</summary>
    [Fact]
    public void DefaultInnerRadius_Is0()
    {
        var series = new SunburstSeries(SampleTree);
        Assert.Equal(0.0, series.InnerRadius);
    }

    /// <summary>A wedge wears the colour its node names; a node that names none takes the next colour of the theme's cycle.</summary>
    [Fact]
    public void AWedge_WearsTheColourItsNodeNames_AndTheCycleOtherwise()
    {
        var named = Color.FromHex("#123456");
        var root = new TreeNode
        {
            Label = "Root",
            Children = [new TreeNode { Label = "A", Value = 30, Color = named }, new TreeNode { Label = "B", Value = 70 }],
        };
        var recording = new Tests.Rendering.RecordingRenderContext();

        new ChartRenderer().Render(Plt.Create().WithSize(400, 400).AddSubPlot(1, 1, 1, ax => ax.Sunburst(root)).Build(), recording);

        var fills = recording.OfKind("DrawPath").Select(call => (Color?)((dynamic)call.Args!).fill).ToList();
        Assert.Contains(named, fills.OfType<Color>());
        Assert.Contains(fills, fill => fill is { } colour && colour != named);
    }

    /// <summary>Verifies that ShowLabels defaults to true.</summary>
    [Fact]
    public void DefaultShowLabels_IsTrue()
    {
        var series = new SunburstSeries(SampleTree);
        Assert.True(series.ShowLabels);
    }

}
