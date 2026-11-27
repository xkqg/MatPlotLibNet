// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>A horizontal reference line and a horizontal level are drawn alike: one line across the plot, and a label at its right end
/// aligned to the right.</summary>
public class HorizontalLinePartsTests
{
    private static readonly Rect Plot = PartFactory.Plot;

    private static RecordingRenderContext ReferenceLines(Axes axes) =>
        PartFactory.Drawn(ctx => PartFactory.ReferenceLines(axes, Plot, ctx, Theme.Default, Plot.Linear(), new DataRange(0, 10, 0, 10)));

    private static RecordingRenderContext Levels(Axes axes) => PartFactory.Drawn(axes, PartFactory.Tools);

    public static TheoryData<Action> TheLines => new()
    {
        () =>
        {
            var ax = new Axes();
            ax.AxHLine(5);
            ReferenceLines(ax).AssertOneLineAcross(Plot);
        },
        () =>
        {
            var ax = new Axes();
            ax.AddLevel(5.0);
            Levels(ax).AssertOneLineAcross(Plot);
        },
    };

    [Theory]
    [MemberData(nameof(TheLines))]
    public void Render_AHorizontalLine_DrawsFullWidthLine(Action check) => check();

    public static TheoryData<Action> TheLabelledLines => new()
    {
        () =>
        {
            var ax = new Axes();
            var l = ax.AxHLine(5);
            l.Label = "Threshold";
            ReferenceLines(ax).AssertOneLabelAlignedRight();
        },
        () =>
        {
            var ax = new Axes();
            var l = ax.AddLevel(5.0);
            l.Label = "Support";
            Levels(ax).AssertOneLabelAlignedRight();
        },
    };

    [Theory]
    [MemberData(nameof(TheLabelledLines))]
    public void Render_AHorizontalLineWithLabel_RightAligned(Action check) => check();
}
