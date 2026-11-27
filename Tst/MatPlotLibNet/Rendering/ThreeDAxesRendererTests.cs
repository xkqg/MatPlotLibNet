// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What <see cref="ThreeDAxesRenderer"/> draws of the 3-D axes: the three axis labels, the panes (hidden, or in the colours
/// given) and the vertex data a viewer rotates the scene with.</summary>
public class ThreeDAxesRendererTests
{
    private static string Render3D(Action<AxesBuilder> configure, Action<global::MatPlotLibNet.Models.Figure>? postBuild = null)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build();
        postBuild?.Invoke(fig);
        return fig.ToSvg();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render3D_WithAxisLabels_RendersAllThreeLabels", () => Render3D(ax => ax
            .Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9])
            .SetXLabel("X axis")
            .SetYLabel("Y axis")
            .SetZLabel("Z axis")), ">X axis<", ">Y axis<", ">Z axis<"),
        new DrawnToken("Render3D_WithPaneHidden_SkipsPaneDrawing", () => Render3D(ax => ax
            .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
            .WithPane3D(p => p with { Visible = false }))) { Absent = ["mpl-pane"] },
        new DrawnToken("Render3D_WithCustomPaneColors_UsesPaneColors", () => Render3D(ax => ax
            .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
            .WithPane3D(p => p with
            {
                FloorColor = Colors.Cyan,
                LeftWallColor = Colors.Green,
                RightWallColor = Colors.Yellow,
            })), "<polygon"),
        new DrawnToken("Render3D_WithEmit3DVertexData_EmitsDataV3DAttributes", () => Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].Emit3DVertexData = true; }), "data-v3d"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>An axis of the 3-D axes whose major ticks are hidden draws neither their marks nor their labels: the figure writes fewer
    /// texts than with them shown.</summary>
    [Fact]
    public void HiddenMajorTicks_AreNotDrawn()
    {
        static int Texts(bool shown) => Render3D(ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]), fig =>
        {
            var z = fig.SubPlots[0].ZAxis;
            z.MajorTicks = z.MajorTicks with { Visible = shown };
        }).Split("<text").Length;

        Assert.True(Texts(shown: false) < Texts(shown: true));
    }
}
