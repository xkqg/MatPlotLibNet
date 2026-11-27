// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A scatter marker's size and shape that the author gives are drawn as given; ones left alone are the theme's. The
/// scatter keeps what was given apart from what it reads at rest, so a size of 36 (6 across for a polar, ternary or 3-D scatter)
/// that was given cannot be taken for the one a new scatter reads. matplotlib 3.11.2 under the classic style:
/// <c>scatter(x, y).get_sizes()</c> is [20.] and <c>scatter(x, y, s=36).get_sizes()</c> is [36.]; <c>Axes3D.scatter(xs, ys, zs)</c>
/// is 20 and <c>s=36</c> is 36 in both styles. A marker the author names wins over the theme's cycler, as an explicit colour
/// does.</summary>
public class GivenMarkerTests
{
    private const double Pt = 100.0 / 72.0;
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static XDocument Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(400, 300).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static List<double> Radii(XDocument svg, Color fill) =>
        [.. svg.Each("circle").Where(e => (string?)e.Attribute("fill") == fill.ToHex()
                && !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))
            .Select(e => e.Number("r"))];

    [Fact]
    public void Classic_AScatterGivenThirtySix_IsThirtySixPointsSquared_AndOneLeftAloneIsTwenty()
    {
        var given = Radii(Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { MarkerSize = 36 })), Colors.Blue);
        var unset = Radii(Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y))), Colors.Blue);

        Assert.Equal(3, given.Count);
        Assert.All(given, r => Assert.Equal(6 * Pt / 2, r, 1));
        Assert.All(unset, r => Assert.Equal(Math.Sqrt(20) * Pt / 2, r, 1));
    }

    [Fact]
    public void Classic_APolarAndATernaryScatterGivenSix_AreSixPointsAcross()
    {
        var polar = Radii(Svg(Theme.MatplotlibClassic, ax => ax.PolarScatter([0.5, 1.0], [0.0, 1.0], s => s.MarkerSize = 6)), Colors.Blue);
        var ternary = Radii(Svg(Theme.MatplotlibClassic,
            ax => ax.TernaryScatter([0.2, 0.5], [0.5, 0.3], [0.3, 0.2], s => s.MarkerSize = 6)), Colors.Blue);

        Assert.Equal(2, polar.Count);
        Assert.Equal(2, ternary.Count);
        Assert.All(polar, r => Assert.Equal(6 * Pt / 2, r, 1));
        Assert.All(ternary, r => Assert.Equal(6 * Pt / 2, r, 1));
    }

    [Fact]
    public void V2_AThreeDScatterGivenSix_IsSixPointsAcross_AndOneLeftAloneIsTwentyPointsSquared()
    {
        var blue = Theme.MatplotlibV2.CycleColors[0];

        var given = Radii(Svg(Theme.MatplotlibV2, ax => ax.Scatter3D(X, Y, [1.0, 3.0, 2.0], s => s.MarkerSize = 6)), blue);
        var unset = Radii(Svg(Theme.MatplotlibV2, ax => ax.Scatter3D(X, Y, [1.0, 3.0, 2.0])), blue);

        Assert.Equal(3, given.Count);
        Assert.All(given, r => Assert.Equal(6 * Pt / 2, r, 1));
        Assert.All(unset, r => Assert.Equal(Math.Sqrt(20) * Pt / 2, r, 1));
    }

    [Fact]
    public void Classic_TheLegendHandleOfAScatterGivenThirtySix_IsThirtySixPointsSquared()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.AddSeries(new ScatterSeries(X, Y) { MarkerSize = 36, Label = "a" }).WithLegend());

        var legend = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("class") == "legend");
        // classic shows three markers in a scatter's handle (legend.scatterpoints), each of the size the plot draws
        var handles = legend.Each("circle").ToList();
        Assert.Equal(3, handles.Count);
        Assert.All(handles, handle => Assert.Equal(6 * Pt / 2, handle.Number("r"), 1));
    }

    /// <summary>A circle the author names stays a circle on a slot whose cycler hands out squares.</summary>
    [Fact]
    public void ACircleTheAuthorNames_WinsOverTheCyclersMarker()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithMarkerStyles(MarkerStyle.Square).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

        static int CirclesIn(XDocument svg) => svg.Each("circle").Count(e => (string?)e.Attribute("fill") == Colors.Red.ToHex() && !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"));

        Assert.Equal(3, CirclesIn(Svg(theme, ax => ax.Scatter(X, Y, s => s.Marker = MarkerStyle.Circle))));
        Assert.Equal(0, CirclesIn(Svg(theme, ax => ax.Scatter(X, Y))));
    }

    [Fact]
    public void AScatterLeftAlone_WritesNoSizeOrMarker_AndAGivenOneWritesBoth_AndReadsThemBackAsGiven()
    {
        var unset = new ScatterSeries(X, Y).ToSeriesDto();
        var given = new ScatterSeries(X, Y) { MarkerSize = 36, Marker = MarkerStyle.Circle }.ToSeriesDto();

        Assert.Null(unset.MarkerSize);
        Assert.Null(unset.Marker);
        Assert.Equal(36, given.MarkerSize);
        Assert.Equal(MarkerStyle.Circle, given.Marker);

        var back = ScatterSeries.FromSeriesDto(new Axes(), given);
        Assert.Equal(36, back.GivenMarkerSize);
        Assert.Equal(MarkerStyle.Circle, back.GivenMarker);
        Assert.Null(ScatterSeries.FromSeriesDto(new Axes(), unset).GivenMarkerSize);
    }

    [Fact]
    public void APolarTernaryAnd3DScatterLeftAlone_WriteNoSize_AndGivenSix_WriteIt()
    {
        Assert.Null(new PolarScatterSeries(X, Y).ToSeriesDto().MarkerSize);
        Assert.Null(new TernaryScatterSeries(X, Y, X).ToSeriesDto().MarkerSize);
        Assert.Null(new Scatter3DSeries(X, Y, X).ToSeriesDto().MarkerSize);
        Assert.Equal(6, new PolarScatterSeries(X, Y) { MarkerSize = 6 }.ToSeriesDto().MarkerSize);
        Assert.Equal(6, new TernaryScatterSeries(X, Y, X) { MarkerSize = 6 }.ToSeriesDto().MarkerSize);
        Assert.Equal(6, new Scatter3DSeries(X, Y, X) { MarkerSize = 6 }.ToSeriesDto().MarkerSize);
    }
}
