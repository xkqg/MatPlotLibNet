// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SunburstSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class SunburstSeriesTests
{
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

        var fills = recording.Of<Tests.Rendering.DrawPathCall>().Select(call => call.Fill).ToList();
        Assert.Contains(named, fills.OfType<Color>());
        Assert.Contains(fills, fill => fill is { } colour && colour != named);
    }
}
