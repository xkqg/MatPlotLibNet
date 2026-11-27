// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An opacity is a state of the surface a figure is drawn on, and a draw that sets one has to leave the surface as it
/// found it. Two things hold that: a 3-D series whose faces are drawn later, sorted by depth among the faces of every series of
/// the axes, carries its opacity with each face, and every stage of a figure (what lies under the subplots, each subplot, what
/// lies above them) starts at full opacity whatever the stage before it left behind.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class OpacityStageTests
{
    private static Figure Voxels(double alpha) =>
        Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Voxels(new bool[,,] { { { true } } }, s => s.Alpha = alpha))
            .Build();

    [Fact]
    public void AVoxelFace_IsDrawnAtTheOpacityOfItsSeries()
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(Voxels(0.5), recording);

        double current = 1.0;
        var opacities = new List<double>();
        foreach (var call in recording.Calls)
        {
            if (call is SetOpacityCall set) current = set.Opacity;
            else if (call is DrawPolygonCall) opacities.Add(current);
        }

        Assert.Equal(6, opacities.Count(o => o == 0.5));
        Assert.Equal(1.0, current);
    }

    [Fact]
    public void AVoxelChart_InSvg_DrawsItsFacesInsideAnOpacityGroup()
    {
        string svg = Voxels(0.5).ToSvg();

        int open = svg.IndexOf("<g opacity=\"0.5\">", StringComparison.Ordinal);
        Assert.True(open >= 0);
        int close = svg.IndexOf("</g>", open, StringComparison.Ordinal);
        string group = svg[open..close];
        Assert.Equal(6, group.Split("<polygon").Length - 1);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnOpaqueVoxelChart_WritesNoOpacityGroup", () => Voxels(1.0).ToSvg()) { Absent = ["<g opacity="] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A polar renderer that draws as usual and then leaves the surface at half opacity, as a renderer someone
    /// registers might.</summary>
    private sealed class LeavesHalfOpacity(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : AxesRenderer(axes, plotArea, ctx, theme)
    {
        public override void Render() => Ctx.SetOpacity(0.5);
    }

    /// <summary>The first stage, the figure's background and title, starts at full opacity too: a surface handed over at another
    /// opacity draws the whole figure at full opacity, not its background alone at the opacity it held.</summary>
    [Fact]
    public void ASurfaceHandedOverAtAnotherOpacity_DrawsTheBackgroundAtFullOpacity()
    {
        var figure = Plt.Create().WithSize(400, 300).WithTitle("t").AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [1.0, 2])).Build();
        var recording = new RecordingRenderContext();
        recording.SetOpacity(0.4);

        new ChartRenderer().Render(figure, recording);

        double current = 0.4;
        double atBackground = double.NaN;
        foreach (var call in recording.Calls.Skip(1))
        {
            if (call is SetOpacityCall set) current = set.Opacity;
            else if (call is DrawRectangleCall) { atBackground = current; break; }
        }

        Assert.Equal(1.0, atBackground);
    }

    /// <summary>What a renderer that leaves half opacity behind did to the figure drawn after it: the opacity every line was drawn at, and
    /// the opacity of the text above the subplots.</summary>
    private readonly record struct OpacityLeft(IReadOnlyList<double> AtLines, double AtText);

    /// <summary>A figure of two subplots and a text above them, the one at <paramref name="polarIndex"/> drawn by a polar renderer that
    /// leaves half opacity behind, and the opacity it leaves what is drawn after it at.</summary>
    private static OpacityLeft DrawnAfterAPolarAxesThatLeavesHalfOpacity(int polarIndex)
    {
        AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new LeavesHalfOpacity(a, p, c, t));
        try
        {
            var figure = Plt.Create().WithSize(400, 300)
                .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 2, 1]))
                .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1, 2], [1.0, 2, 1]))
                .Build();
            figure.SubPlots[polarIndex].CoordinateSystem = CoordinateSystem.Polar;
            figure.AddText("above", 0.5, 0.95);
            var recording = new RecordingRenderContext();

            new ChartRenderer().Render(figure, recording);

            double current = 1.0;
            var atLines = new List<double>();
            double atText = double.NaN;
            foreach (var call in recording.Calls)
            {
                if (call is SetOpacityCall set) current = set.Opacity;
                else if (call is DrawLinesCall) atLines.Add(current);
                else if (call is DrawTextCall { Text: "above" }) atText = current;
            }

            return new OpacityLeft(atLines, atText);
        }
        finally
        {
            AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new PolarAxesRenderer(a, p, c, t));
        }
    }

    /// <summary>The last subplot is followed by what lies above the subplots (the figure's texts), which starts at full opacity whatever
    /// a renderer someone registered left behind there.</summary>
    [Fact]
    public void WhatTheLastSubplotLeavesBehind_DoesNotReachWhatLiesAboveIt() =>
        Assert.Equal(1.0, DrawnAfterAPolarAxesThatLeavesHalfOpacity(1).AtText);

    [Fact]
    public void WhatARendererLeavesBehind_DoesNotReachTheNextSubplotNorWhatLiesAboveThem()
    {
        var left = DrawnAfterAPolarAxesThatLeavesHalfOpacity(0);

        Assert.NotEmpty(left.AtLines);
        Assert.All(left.AtLines, o => Assert.Equal(1.0, o));
        Assert.Equal(1.0, left.AtText);
    }
}
