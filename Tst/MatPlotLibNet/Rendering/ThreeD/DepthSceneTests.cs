// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>A 3-D axes is one scene: every series hands its marks to the scene's one depth queue, which draws them back to front among
/// the marks of every other series, each run of a series' marks inside the group of that series.</summary>
public sealed class DepthSceneTests
{
    private static readonly double[] One = [0.0];

    private static string SvgOf(Action<AxesBuilder> axes) => Plt.Create().AddSubPlot(1, 1, 1, axes).Build().ToSvg();

    /// <summary>The faces of a labelled bar series are drawn inside its group, as the marks of every other series are, so a legend
    /// toggle or a hover that finds the series by its group reaches its faces; they were drawn after an empty group.</summary>
    [Fact]
    public void TheFacesOfABarSeries_AreDrawnInsideItsGroup()
    {
        string svg = SvgOf(ax => ax.Bar3D(One, One, [2.0], s => s.Label = "bars"));

        var group = svg.Each("g").FirstOrDefault(element => (string?)element.Attribute("class") == "series" && (string?)element.Attribute("aria-label") == "bars");
        Assert.True(group is not null, "the series has a group");
        Assert.Equal("polygon", group!.Elements().First().Name.LocalName);
        Assert.Equal(1, svg.CountOf("aria-label=\"bars\""));
    }

    /// <summary>A point that stands in front of a bar is drawn after the bar's faces, whatever order the two series were added in: the
    /// point's series hands its marks to the same queue as the bar's. It was drawn at its own turn, under every face of a bar added
    /// after it.</summary>
    [Fact]
    public void APointInFrontOfABar_IsDrawnAfterItsFaces_ThoughItWasAddedFirst()
    {
        string svg = SvgOf(ax => ax
            .Scatter3D([0.5], [-1.0], [0.5])
            .Bar3D(One, One, [1.0]));

        int point = svg.IndexOf("<circle", StringComparison.Ordinal);
        int lastFace = svg.LastIndexOf("<polygon", StringComparison.Ordinal);

        Assert.True(point > 0, "the point is drawn");
        Assert.True(lastFace < point, "the point in front is drawn after every face of the bar behind it");
    }

    /// <summary>Two labelled series whose faces interleave: every face stands inside the group of its own series (a group opened
    /// around each run of its faces), and the faces of a series drawn at an opacity stand in an opacity group inside the series'
    /// group, closed before the series' group is, so the markup nests and each run carries its series' opacity.</summary>
    [Fact]
    public void InterleavedSeries_EachRunStandsInItsOwnGroup_AtItsOwnOpacity()
    {
        var cells = new bool[2, 1, 1];
        cells[0, 0, 0] = cells[1, 0, 0] = true;
        string svg = SvgOf(ax => ax
            .Voxels(cells, s => { s.Label = "cubes"; s.Alpha = 0.6; })
            .Bar3D([0.5, 1.5], [1.2, -0.8], [1.5, 0.5], s => { s.Label = "bars"; s.BarWidth = 0.4; })
            .WithPane3D(p => p with { Visible = false }));

        var doc = svg.Xml();
        var faces = doc.Each("polygon").ToList();
        string? LabelOf(System.Xml.Linq.XElement face) =>
            face.Ancestors().FirstOrDefault(g => g.Attribute("aria-label") is not null)?.Attribute("aria-label")!.Value;

        Assert.All(faces.Where(f => LabelOf(f) is null), f => Assert.Fail("a face stands outside every series group"));
        Assert.Contains(faces, f => LabelOf(f) == "cubes");
        Assert.Contains(faces, f => LabelOf(f) == "bars");
        Assert.True(svg.CountOf("aria-label=\"cubes\"") + svg.CountOf("aria-label=\"bars\"") > 2,
            "the faces interleave, so at least one series is drawn in more than one run");
        Assert.All(faces.Where(f => LabelOf(f) == "cubes"), f => Assert.Equal("0.6", f.Parent!.Attribute("opacity")?.Value));
        Assert.All(faces.Where(f => LabelOf(f) == "bars"), f => Assert.Null(f.Ancestors().FirstOrDefault(g => g.Attribute("opacity") is not null)));
    }

    /// <summary>The labels of the series groups, in the order the document holds them.</summary>
    private static List<string> GroupLabels(string svg) =>
        [.. svg.Elements("g").Where(group => group["class"] == "series" && group["aria-label"] is not null).Select(group => group["aria-label"]!)];

    private static readonly double[] Nothing = [];
    private static readonly double[] Missing = [double.NaN, double.NaN];
    private static readonly double[] Helix = [0.0, 0.5, 1.0, 1.5];

    /// <summary>A labelled series with nothing it can draw (no values, or none that can be placed) still writes its group, where it
    /// stands among the series, as every series drawn at its turn does: a legend toggle or a hover finds every series it lists.</summary>
    [Fact]
    public void ASeriesWithNothingToDraw_StillWritesItsGroup_WhereItStandsAmongTheSeries()
    {
        string svg = SvgOf(ax => ax
            .Scatter3D(Nothing, Nothing, Nothing, s => s.Label = "empty")
            .Scatter3D(Missing, Missing, Missing, s => s.Label = "missing")
            .Plot3D(Helix, Helix, Helix, s => s.Label = "line"));

        Assert.Equal(["empty", "missing", "line"], GroupLabels(svg));
    }

    /// <summary>The group of a series with nothing to draw, added after a series with marks, stands after that series' group.</summary>
    [Fact]
    public void ASeriesWithNothingToDraw_AddedLast_WritesItsGroupLast()
    {
        string svg = SvgOf(ax => ax
            .Plot3D(Helix, Helix, Helix, s => s.Label = "line")
            .Scatter3D(Nothing, Nothing, Nothing, s => s.Label = "empty"));

        Assert.Equal(["line", "empty"], GroupLabels(svg));
    }

    /// <summary>Every 3-D kind, labelled and given nothing it can draw, writes its group, and writes it once.</summary>
    [Theory]
    [InlineData("scatter")]
    [InlineData("line")]
    [InlineData("bar")]
    [InlineData("planar")]
    [InlineData("stem")]
    [InlineData("trisurf")]
    [InlineData("quiver")]
    [InlineData("surface")]
    [InlineData("wireframe")]
    [InlineData("contour")]
    [InlineData("voxels")]
    public void EveryKind_WithNothingToDraw_WritesItsGroupOnce(string kind)
    {
        var holes = new double[2, 2] { { double.NaN, double.NaN }, { double.NaN, double.NaN } };
        double[] two = [0.0, 1.0];
        string svg = SvgOf(ax => _ = kind switch
        {
            "scatter" => ax.Scatter3D(Nothing, Nothing, Nothing, s => s.Label = kind),
            "line" => ax.Plot3D(Nothing, Nothing, Nothing, s => s.Label = kind),
            "bar" => ax.Bar3D(Nothing, Nothing, Nothing, s => s.Label = kind),
            "planar" => ax.PlanarBar3D(Nothing, Nothing, Nothing, s => s.Label = kind),
            "stem" => ax.Stem3D(Nothing, Nothing, Nothing, s => s.Label = kind),
            "trisurf" => ax.Trisurf(Nothing, Nothing, Nothing, s => s.Label = kind),
            "quiver" => ax.Quiver3D(Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, s => s.Label = kind),
            "surface" => ax.Surface(two, two, holes, s => s.Label = kind),
            "wireframe" => ax.Wireframe(two, two, holes, s => s.Label = kind),
            "contour" => ax.Contour3D(two, two, holes, s => s.Label = kind),
            _ => ax.Voxels(new bool[2, 2, 2], s => s.Label = kind),
        });

        Assert.Equal([kind], GroupLabels(svg));
    }

    /// <summary>A kind drawn as one whole drawing (stems, a wireframe) that can place none of its points hands the scene no mark, so its
    /// group stands where the series stands, after the series added before it, not first as a mark at no depth.</summary>
    [Theory]
    [InlineData("stem")]
    [InlineData("wireframe")]
    public void AWholeDrawingThatPlacesNothing_WritesItsGroupWhereItsSeriesStands(string kind)
    {
        var holes = new double[2, 2] { { double.NaN, double.NaN }, { double.NaN, double.NaN } };
        string svg = SvgOf(ax =>
        {
            ax.Plot3D(Helix, Helix, Helix, s => s.Label = "line");
            _ = kind == "stem"
                ? ax.Stem3D(Missing, Missing, Missing, s => s.Label = kind)
                : ax.Wireframe([0.0, 1.0], [0.0, 1.0], holes, s => s.Label = kind);
        });

        Assert.Equal(["line", kind], GroupLabels(svg));
    }

    /// <summary>A flat series added to a 3-D axes (its marks are not projected) is drawn at its turn inside its own group, as on any
    /// axes; only the series drawn in the scene hand their group to the scene's queue.</summary>
    [Fact]
    public void AFlatSeriesOnA3DAxes_IsDrawnInsideItsGroup()
    {
        string svg = SvgOf(ax => ax
            .Scatter3D(Helix, Helix, Helix, s => s.Label = "points")
            .Plot([0.0, 1.0], [1.0, 0.0], s => s.Label = "flat"));

        var doc = svg.Xml();
        var flat = doc.Each("g").SingleOrDefault(e => e.Attribute("aria-label")?.Value == "flat");
        Assert.NotNull(flat);
        Assert.Contains(flat!.Descendants(), e => e.Name.LocalName is "polyline" or "path" or "line");
        Assert.Equal(["flat", "points"], GroupLabels(svg));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A 3-D contour of a flat surface crosses none of its levels: it traces no line and hands the scene no mark.
        // A 3-D contour of a flat surface crosses none of its levels: it traces no line and hands the scene no mark.
        new DrawnToken("AContourOfAFlatSurface_DrawsNoLine", () => SvgOf(ax => ax.Contour3D([0.0, 1.0], [0.0, 1.0], new double[,] { { 2, 2 }, { 2, 2 } }, s => s.Color = MatPlotLibNet.Styling.Color.FromHex("#123456")))) { Absent = ["#123456"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A series whose marks are drawn in its own order (a wireframe) stands in the scene as one mark at its nearest point, as
    /// matplotlib orders a collection: a surface added after it no longer paints over it when the wireframe stands in front.</summary>
    [Fact]
    public void AWireframeInFrontOfASurface_IsDrawnAfterIt_ThoughItWasAddedFirst()
    {
        double[] x = [0.0, 1.0];
        string svg = SvgOf(ax => ax
            .Wireframe(x, [-2.0, -1.0], new double[,] { { 0.5, 0.5 }, { 0.5, 0.5 } })
            .Surface(x, [0.0, 1.0], new double[,] { { 0.0, 0.0 }, { 1.0, 1.0 } }));

        int lastFace = svg.LastIndexOf("<polygon", StringComparison.Ordinal);
        int wire = svg.IndexOf("<polyline", lastFace < 0 ? 0 : lastFace, StringComparison.Ordinal);

        Assert.True(lastFace > 0, "the surface is drawn");
        Assert.True(wire > lastFace, "the wireframe in front is drawn after the surface behind it");
    }
}
