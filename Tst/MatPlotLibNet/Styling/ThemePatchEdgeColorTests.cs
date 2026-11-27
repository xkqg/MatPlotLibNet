// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A theme's patch edge colour is one value: <see cref="Theme.PatchEdgeColor"/> reads and writes the colour of its patch look, the
/// one matplotlib's <c>patch.edgecolor</c> names. matplotlib 3.11.2 (python): <c>rcParams['patch.edgecolor']</c> is 'black' under the
/// default style and 'k' under the classic one; with the classic style and <c>patch.edgecolor = 'g'</c>, <c>ax.hist([1, 2, 2, 3])[2][0]</c>
/// and <c>ax.bar([1], [1]).patches[0]</c> are edged (0, 0.5, 0, 1) (<c>patch.force_edgecolor</c> on); under the default style
/// (<c>patch.force_edgecolor</c> off) they have no edge whatever the colour.</summary>
public class ThemePatchEdgeColorTests
{
    private static readonly Color Green = Color.FromHex("#008000");

    [Fact]
    public void ABuiltInTheme_NamesThePatchEdgeColourItsLookDrawsWith()
    {
        Assert.Equal(Colors.Black, Theme.MatplotlibV2.PatchEdgeColor);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.PatchEdgeColor);
        Assert.Null(Theme.Default.PatchEdgeColor);
        Assert.Equal(Theme.MatplotlibV2.Looks.Patch.EdgeColor, Theme.MatplotlibV2.PatchEdgeColor);
    }

    [Fact]
    public void AThemeThatSetsThePatchEdgeColour_DrawsWithIt_AndItsLookSaysTheSame()
    {
        var classic = new Theme("classic, green edges", Theme.MatplotlibClassic) { PatchEdgeColor = Green };
        var v2 = new Theme("default, green edges", Theme.MatplotlibV2) { PatchEdgeColor = Green };

        Assert.Equal(Green, classic.Looks.Patch.EdgeColor);
        Assert.Equal(Green, v2.Looks.Patch.EdgeColor);
        Assert.Equal(Green, EdgeOfTheFirstBin(classic));
        Assert.Null(EdgeOfTheFirstBin(v2));
    }

    [Fact]
    public void TheJson_WritesThePatchEdgeColourOnce_AndReadsItBackIntoTheLook()
    {
        var serializer = new ChartSerializer();
        var theme = new Theme("classic, green edges", Theme.MatplotlibClassic) { PatchEdgeColor = Green };
        var json = serializer.ToJson(Plt.Create().WithTheme(theme).Plot([1.0, 2.0], [1.0, 2.0]).Build());

        Assert.Contains("\"patchEdgeColor\"", json);
        Assert.DoesNotContain("\"surfaceEdgeColor\"", json);
        var back = serializer.FromJson(json).Theme;
        Assert.Equal(Green, back.PatchEdgeColor);
        Assert.Equal(Green, back.Looks.Patch.EdgeColor);
    }

    private static Color? EdgeOfTheFirstBin(Theme theme)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Hist([1.0, 2, 2, 3], 3)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx.Of<DrawRectangleCall>().First(c => c.Fill is Color { A: 255 } f && f != theme.AxesBackground && f != theme.Background).Stroke;
    }
}
