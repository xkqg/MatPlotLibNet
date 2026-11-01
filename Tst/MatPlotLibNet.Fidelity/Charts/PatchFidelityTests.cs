// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Patches in data space against matplotlib's <c>add_patch</c>: a rectangle, a polygon, an arrow, an arc, a circle, an
/// ellipse and a wedge (and, on the log axis, a ring cut from a wedge), each at the same numbers in the generator
/// (<c>tools/mpl_reference/generate.py</c>). Every colour is written out, so the picture does not lean on the style's patch
/// defaults, and both axes have set limits: the library rounds an automatic range to a step chosen by a target count, matplotlib to
/// one that depends on the size of the axes, a difference in every chart that has nothing to do with a patch. The linear axes lists
/// three of the patches in its legend; the log axes bends the curves through the scale.</summary>
public class PatchFidelityTests : FidelityTest
{
    private static readonly Color Black = Colors.Black;
    private static readonly Color Blue = Colors.Tab10Blue;
    private static readonly Color Orange = Colors.Tab10Orange;
    private static readonly Color Green = Colors.Tab10Green;
    private static readonly Color Red = Color.FromHex("#d62728");
    private static readonly Color Purple = Color.FromHex("#9467bd");
    private static readonly Color Brown = Color.FromHex("#8c564b");
    private static readonly Color Pink = Color.FromHex("#e377c2");
    private static readonly Color Cyan = Color.FromHex("#17becf");

    /// <summary>The patches both axes draw, all at positive coordinates so the log axis can place them. With <paramref name="labelled"/>
    /// three of them carry a label, the rows of the legend of the linear axes; this library lists a labelled patch without being asked,
    /// where matplotlib lists nothing until <c>legend()</c> is called, so the log axes, which has no legend, leaves them unnamed.</summary>
    private static List<Patch> Shapes(bool labelled) =>
    [
        new Rectangle(new DataPoint(1.2, 1), 1.0, 2.0) { Angle = 20, FaceColor = Blue, EdgeColor = Black, Label = labelled ? "rectangle" : null },
        new Polygon([new DataPoint(3, 1), new DataPoint(5, 1.5), new DataPoint(4.5, 4), new DataPoint(3.4, 3)])
            { FaceColor = Orange, EdgeColor = Black },
        new Arrow(new DataPoint(6, 1), 3, 2.5) { Width = 0.35, FaceColor = Green, EdgeColor = Black },
        new Arc(new DataPoint(16, 2.5), 8, 2.4) { Theta1 = 0, Theta2 = 270, EdgeColor = Red, EdgeWidth = 2 },
        new Circle(new DataPoint(2, 7), 1.2) { FaceColor = Purple, EdgeColor = Black, Alpha = 0.6 },
        new Ellipse(new DataPoint(6, 7), 4, 2) { Angle = 30, FaceColor = Brown, EdgeColor = Black, Label = labelled ? "ellipse" : null },
        new Wedge(new DataPoint(11, 7), 2.0, 30, 300) { FaceColor = Pink, EdgeColor = Black, Label = labelled ? "wedge" : null },
    ];

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 40, Ssim = 0.75, DeltaE = 42)]   // measured on Windows (classic 35, v2 22 over 1.5; the dominant colours 30 and 65 over 1.7) plus a margin of 10 percent, SSIM less 0.04
    public void Patches_OnALinearAxesWithALegend_MatchMatplotlib(string themeId)
    {
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Patches in data space");
                foreach (var patch in Shapes(labelled: true)) ax.AddPatch(patch);
                ax.SetXLim(0, 21).SetYLim(0, 10).WithLegend(LegendPosition.UpperRight);
            })
            .Build();
        AssertFidelity(figure, "patches_linear");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 35, Ssim = 0.80, DeltaE = 42)]   // measured on Windows (classic 30, v2 17 over 1.5), plus a margin of 10 percent, SSIM less 0.04. The dominant colours measure 1, but the fifth and sixth of v2 are an orange and a pink 18 pixels apart: a platform that swaps them reads 65 over 1.7
    public void Patches_OnALogXAxis_MatchMatplotlib(string themeId)
    {
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Patches on a log axis");
                foreach (var patch in Shapes(labelled: false)) ax.AddPatch(patch);
                ax.AddPatch(new Wedge(new DataPoint(17, 7), 2.5, 40, 320) { Width = 0.8, FaceColor = Cyan, EdgeColor = Black });
                ax.SetXScale(AxisScale.Log).SetXLim(0.4, 25).SetYLim(0, 10);
            })
            .Build();
        AssertFidelity(figure, "patches_log");
    }
}
