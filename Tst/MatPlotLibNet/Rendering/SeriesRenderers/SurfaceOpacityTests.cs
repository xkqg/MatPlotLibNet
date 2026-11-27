// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>How opaque a 3-D surface is drawn when its author set no alpha. matplotlib 3.11, mpl_toolkits/mplot3d/axes3d.py:
/// <c>plot_surface</c> and <c>plot_trisurf</c> build an <c>art3d.Poly3DCollection(polys, **kwargs)</c> and give it no alpha of
/// their own, so a surface is opaque unless the call names one, in the default style and the classic one alike. The matplotlib
/// themes draw it so; every other theme keeps the 0.8 the library has always drawn. A value the author gave, 0.8 among them,
/// is drawn as it is under every theme, and <see cref="SurfaceSeries.Alpha"/> still reads 0.8 for a surface that was given
/// none.</summary>
public sealed class SurfaceOpacityTests
{
    private static readonly Rect Plot = new(0, 0, 400, 400);

    /// <summary>The opacity the faces are drawn at: the opacity in force (full until one is set) when the first polygon is drawn, with
    /// the frame's panes hidden so the faces are the only polygons.</summary>
    private static double DrawnOpacity(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        axes.Pane3D = new Pane3DConfig { Visible = false };
        new ThreeDAxesRenderer(axes, Plot, ctx, theme).Render();
        double opacity = 1.0;
        foreach (var call in ctx.Calls)
        {
            if (call is SetOpacityCall set) opacity = set.Opacity;
            if (call is DrawPolygonCall) return opacity;
        }

        Assert.Fail("no face was drawn");
        return opacity;
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void ASurfaceGivenNoAlpha_IsOpaque_UnderAMatplotlibTheme(string theme)
    {
        Assert.Equal(1.0, DrawnOpacity(SampleSeries.SurfaceAxes(), theme.AsTheme()));
        Assert.Equal(1.0, DrawnOpacity(SampleSeries.TrisurfAxes(), theme.AsTheme()));
    }

    [Fact]
    public void ASurfaceGivenNoAlpha_KeepsItsPointEight_UnderEveryOtherTheme()
    {
        Assert.Equal(0.8, DrawnOpacity(SampleSeries.SurfaceAxes(), Theme.Default));
        Assert.Equal(0.8, DrawnOpacity(SampleSeries.TrisurfAxes(), Theme.Dark));
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(0.5)]
    public void AnAlphaTheAuthorGave_IsDrawnAsItIs_UnderEveryTheme(double alpha)
    {
        Assert.Equal(alpha, DrawnOpacity(SampleSeries.SurfaceAxes(s => s.Alpha = alpha), Theme.MatplotlibV2));
        Assert.Equal(alpha, DrawnOpacity(SampleSeries.TrisurfAxes(s => s.Alpha = alpha), Theme.MatplotlibClassic));
        Assert.Equal(alpha, DrawnOpacity(SampleSeries.SurfaceAxes(s => s.Alpha = alpha), Theme.Default));
    }

    [Fact]
    public void TheAlphaOfASurfaceGivenNone_StillReadsPointEight()
    {
        Assert.Equal(0.8, ((SurfaceSeries)SampleSeries.SurfaceAxes().Series[0]).Alpha);
        Assert.Equal(0.8, ((Trisurf3DSeries)SampleSeries.TrisurfAxes().Series[0]).Alpha);
    }

    /// <summary>The JSON carries the alpha that was given, 0.8 included, and nothing for a surface that was given none, so a
    /// figure read back keeps following its theme and a figure that named 0.8 keeps it.</summary>
    [Fact]
    public void TheJson_CarriesAGivenAlpha_AndNoneForASurfaceGivenNone()
    {
        Assert.Null(((SurfaceSeries)SampleSeries.SurfaceAxes().Series[0]).ToSeriesDto().Alpha);
        Assert.Null(((Trisurf3DSeries)SampleSeries.TrisurfAxes().Series[0]).ToSeriesDto().Alpha);
        Assert.Equal(0.8, ((SurfaceSeries)SampleSeries.SurfaceAxes(s => s.Alpha = 0.8).Series[0]).ToSeriesDto().Alpha);
        Assert.Equal(0.8, ((Trisurf3DSeries)SampleSeries.TrisurfAxes(s => s.Alpha = 0.8).Series[0]).ToSeriesDto().Alpha);

        var given = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        SurfaceSeries.FromSeriesDto(given, ((SurfaceSeries)SampleSeries.SurfaceAxes(s => s.Alpha = 0.8).Series[0]).ToSeriesDto());
        Assert.Equal(0.8, DrawnOpacity(given, Theme.MatplotlibV2));

        var none = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        Trisurf3DSeries.FromSeriesDto(none, ((Trisurf3DSeries)SampleSeries.TrisurfAxes().Series[0]).ToSeriesDto());
        Assert.Equal(1.0, DrawnOpacity(none, Theme.MatplotlibV2));
    }

    /// <summary>The theme's surface opacity is the theme's, so it comes back from JSON with the theme and a theme derived from
    /// a matplotlib one keeps it.</summary>
    [Fact]
    public void TheThemesSurfaceAlpha_SurvivesJsonAndADerivedTheme()
    {
        Assert.Equal(1.0, Theme.MatplotlibV2.Looks.Patch.Alpha);
        Assert.Equal(1.0, Theme.MatplotlibClassic.Looks.Patch.Alpha);
        Assert.Null(Theme.Default.Looks.Patch.Alpha);
        Assert.Equal(1.0, Theme.MatplotlibClassic.ThroughDto().Looks.Patch.Alpha);
        Assert.Equal(1.0, Theme.MatplotlibV2.Cloned().Looks.Patch.Alpha);
        Assert.Null(ThemeDto.From(Theme.Default).SurfaceAlpha);
    }
}
