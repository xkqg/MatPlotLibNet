// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The outline a 3-D surface's cells are drawn with when its author named no edge colour. matplotlib 3.11,
/// mpl_toolkits/mplot3d/axes3d.py: <c>plot_surface</c> and <c>plot_trisurf</c> build an <c>art3d.Poly3DCollection</c> and name
/// no edge colour, so <c>collections.Collection._set_edgecolor(None)</c> decides: <c>if (mpl.rcParams['patch.force_edgecolor']
/// or self._edge_default or ...): c = self._get_default_edgecolor() else: c = 'none'</c>. The default style leaves
/// <c>patch.force_edgecolor</c> False, so a surface has no outline (measured: <c>get_edgecolor()</c> is empty after a draw, for a
/// colour map and a solid colour alike); the classic style sets it True, so the outline is <c>patch.edgecolor</c>, black, at
/// <c>patch.linewidth</c>, 1 pt (1.39 px at 100 dpi). An edge colour the caller names is drawn at <c>patch.linewidth</c> in
/// both, and so is a wireframe the caller turns on: <see cref="SurfaceSeries.ShowWireframe"/> set to true outlines the cells in
/// <c>patch.edgecolor</c>, black in both styles, as <c>patch.force_edgecolor</c> would. Every other theme keeps the thin outline
/// the library has always drawn, black at alpha 80 and 0.5 px.</summary>
public sealed class SurfaceEdgeTests
{
    private const double OnePoint = 100.0 / 72.0;

    private static readonly Rect Plot = new(0, 0, 400, 400);

    /// <summary>The outline of one face: its stroke colour, none for no outline, and its width.</summary>
    private readonly record struct Edge(Color? Stroke, double Width);

    /// <summary>The outline of every face: the polygons drawn with the frame's panes hidden, so the faces are the only ones.</summary>
    private static Edge[] FaceEdges(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        axes.Pane3D = new Pane3DConfig { Visible = false };
        new ThreeDAxesRenderer(axes, Plot, ctx, theme).Render();
        var faces = ctx.Of<DrawPolygonCall>()
            .Select(c => new Edge(c.Stroke, c.StrokeThickness)).ToArray();
        Assert.NotEmpty(faces);
        return faces;
    }

    private static void AllDrawn(Axes axes, Theme theme, Color? stroke, double width) =>
        Assert.All(FaceEdges(axes, theme), e => Assert.Equal(new Edge(stroke, width), e));

    [Fact]
    public void UnderTheMatplotlibV2Theme_ASurfaceHasNoOutline()
    {
        AllDrawn(SampleSeries.SurfaceAxes(), Theme.MatplotlibV2, null, 0);
        AllDrawn(SampleSeries.TrisurfAxes(), Theme.MatplotlibV2, null, 0);
    }

    [Fact]
    public void UnderTheMatplotlibClassicTheme_ASurfaceIsOutlinedInBlack_OnePointWide()
    {
        AllDrawn(SampleSeries.SurfaceAxes(), Theme.MatplotlibClassic, Colors.Black, OnePoint);
        AllDrawn(SampleSeries.TrisurfAxes(), Theme.MatplotlibClassic, Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderEveryOtherTheme_ASurfaceKeepsItsThinTranslucentOutline()
    {
        AllDrawn(SampleSeries.SurfaceAxes(), Theme.Default, Colors.Black.WithAlpha(80), 0.5);
        AllDrawn(SampleSeries.TrisurfAxes(), Theme.Dark, Colors.Black.WithAlpha(80), 0.5);
    }

    [Fact]
    public void AnEdgeColourTheAuthorNamed_IsDrawn_AtTheThemesWidth()
    {
        AllDrawn(SampleSeries.SurfaceAxes(s => s.EdgeColor = Colors.Red), Theme.MatplotlibV2, Colors.Red, OnePoint);
        AllDrawn(SampleSeries.TrisurfAxes(s => s.EdgeColor = Colors.Red), Theme.MatplotlibClassic, Colors.Red, OnePoint);
        AllDrawn(SampleSeries.SurfaceAxes(s => s.EdgeColor = Colors.Red), Theme.Default, Colors.Red, 0.5);
    }

    [Fact]
    public void AWireframeTheAuthorTurnedOn_IsDrawnInTheThemesEdgeColour_UnderTheMatplotlibV2ThemeToo()
    {
        AllDrawn(SampleSeries.SurfaceAxes(s => s.ShowWireframe = true), Theme.MatplotlibV2, Colors.Black, OnePoint);
        AllDrawn(SampleSeries.TrisurfAxes(s => s.ShowWireframe = true), Theme.MatplotlibV2, Colors.Black, OnePoint);
        AllDrawn(SampleSeries.SurfaceAxes(s => s.ShowWireframe = true), Theme.Default, Colors.Black.WithAlpha(80), 0.5);
    }

    [Fact]
    public void TheWireframeOfASurfaceTurnedNeitherWay_StillReadsTrue()
    {
        Assert.True(((SurfaceSeries)SampleSeries.SurfaceAxes().Series[0]).ShowWireframe);
        Assert.True(((Trisurf3DSeries)SampleSeries.TrisurfAxes().Series[0]).ShowWireframe);
    }

    /// <summary>The JSON carries a wireframe that was turned on or off, and nothing for one turned neither way, so a figure read
    /// back keeps following its theme and one that asked for an outline keeps it.</summary>
    [Fact]
    public void TheJson_CarriesAWireframeTurnedEitherWay_AndNoneForOneLeftAlone()
    {
        Assert.Null(((SurfaceSeries)SampleSeries.SurfaceAxes().Series[0]).ToSeriesDto().ShowWireframe);
        Assert.Null(((Trisurf3DSeries)SampleSeries.TrisurfAxes().Series[0]).ToSeriesDto().ShowWireframe);
        Assert.True(((SurfaceSeries)SampleSeries.SurfaceAxes(s => s.ShowWireframe = true).Series[0]).ToSeriesDto().ShowWireframe);
        Assert.True(((Trisurf3DSeries)SampleSeries.TrisurfAxes(s => s.ShowWireframe = true).Series[0]).ToSeriesDto().ShowWireframe);
        Assert.False(((SurfaceSeries)SampleSeries.SurfaceAxes(s => s.ShowWireframe = false).Series[0]).ToSeriesDto().ShowWireframe);

        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Surface([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } },
                s => s.ShowWireframe = true))
            .Build();
        var again = figure.RoundTripped().SubPlots[0];

        AllDrawn(again, Theme.MatplotlibV2, Colors.Black, OnePoint);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [InlineData("default")]
    public void ASurfaceWithoutItsWireframe_HasNoOutline_UnderEveryTheme(string id)
    {
        var theme = id.AsTheme();

        AllDrawn(SampleSeries.SurfaceAxes(s => { s.ShowWireframe = false; s.EdgeColor = Colors.Red; }), theme, null, 0);
        AllDrawn(SampleSeries.TrisurfAxes(s => s.ShowWireframe = false), theme, null, 0);
    }

    [Fact]
    public void TheEdgeRuleOfEachTheme()
    {
        Assert.True(Theme.MatplotlibV2.Looks.Patch.LikeCollections);
        Assert.False(Theme.MatplotlibV2.Looks.Patch.ForceEdge);
        Assert.True(Theme.MatplotlibClassic.Looks.Patch.ForceEdge);
        Assert.False(Theme.Default.Looks.Patch.LikeCollections);
        Assert.True(Theme.Default.Looks.Patch.ForceEdge);
        Assert.Equal(Colors.Black, Theme.MatplotlibV2.Looks.Patch.EdgeColor);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.Looks.Patch.EdgeColor);
        Assert.Equal(OnePoint, Theme.MatplotlibV2.Looks.Patch.EdgeWidth);
        Assert.Equal(OnePoint, Theme.MatplotlibClassic.Looks.Patch.EdgeWidth);
        Assert.Null(Theme.Default.Looks.Patch.EdgeColor);
        Assert.Null(Theme.Dark.Looks.Patch.EdgeWidth);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void TheEdgeRule_SurvivesJson_AndAThemeMadeFromIt(string id)
    {
        var theme = id.AsTheme();
        var figure = Plt.Create().WithTheme(theme).Build();

        var again = figure.RoundTripped().Theme;
        var made = Theme.CreateFrom(theme).Build();

        Assert.Equal(theme.Looks.Patch.ForceEdge, again.Looks.Patch.ForceEdge);
        Assert.Equal(theme.Looks.Patch.EdgeColor, again.Looks.Patch.EdgeColor);
        Assert.Equal(theme.Looks.Patch.EdgeWidth, again.Looks.Patch.EdgeWidth);
        Assert.Equal(theme.Looks.Patch.ForceEdge, made.Looks.Patch.ForceEdge);
        Assert.Equal(theme.Looks.Patch.EdgeColor, made.Looks.Patch.EdgeColor);
        Assert.Equal(theme.Looks.Patch.EdgeWidth, made.Looks.Patch.EdgeWidth);
    }
}
