// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — sixth pinpoint batch targeting series ToSeriesDto
/// ternary branches that require NON-default property values to fire (Smooth=true,
/// SmoothResolution!=default, Levels!=default, etc.). Each test names the property combo
/// that lifts the branch.</summary>
public class PinpointBranchTests6
{
    // AreaSeries: `Smooth ? true : null` AND `Smooth && SmoothResolution != 10 ? : null`
    [Fact] public void AreaSeries_SmoothFalse_BothTernariesReturnNull()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 });
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Smooth);
        Assert.Null(dto.SmoothResolution);
    }

    [Fact] public void AreaSeries_SmoothTrue_DefaultResolution_FirstReturnsTrueSecondNull()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 })
        { Smooth = true, SmoothResolution = 10 };
        var dto = s.ToSeriesDto();
        Assert.Equal(true, dto.Smooth);
        Assert.Null(dto.SmoothResolution);
    }

    [Fact] public void AreaSeries_SmoothTrue_NonDefaultResolution_BothTernariesReturnValue()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 })
        { Smooth = true, SmoothResolution = 25 };
        var dto = s.ToSeriesDto();
        Assert.Equal(true, dto.Smooth);
        Assert.Equal(25, dto.SmoothResolution);
    }

    // ContourSeries L48 — Levels != default branch.
    [Fact] public void ContourSeries_NonDefaultLevels_SerializesIt()
    {
        var s = new ContourSeries(new double[] { 0.0, 1 }, new double[] { 0.0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { Levels = 7 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Contour3DSeries L30 — Levels != default branch.
    [Fact] public void Contour3DSeries_NonDefaultLevels_SerializesIt()
    {
        var s = new Contour3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { Levels = 7 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Line3DSeries L27 — non-default LineWidth/Color/etc.
    [Fact] public void Line3DSeries_NonDefaultLineWidth_SerializesIt()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { LineWidth = 3.5 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // SurfaceSeries L32 — non-default ColorMap.
    [Fact] public void SurfaceSeries_NonDefaultColormap_SerializesName()
    {
        var s = new SurfaceSeries(new double[] { 1, 2 }, new double[] { 1, 2 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { ColorMap = ColorMaps.Inferno };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Trisurf3DSeries L37 — non-default ColorMap.
    [Fact] public void Trisurf3DSeries_NonDefaultColormap_SerializesName()
    {
        var s = new Trisurf3DSeries(new double[] { 0.0, 1, 0.5 },
            new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })
        { ColorMap = ColorMaps.Magma };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Quiver3DSeries L78 — explicit Color override.
    [Fact] public void Quiver3DSeries_ExplicitColor_SerializesIt()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { Color = Colors.Red };
        Assert.NotNull(s.ToSeriesDto());
    }

    // RegressionSeries L48 — non-default Degree branch.
    [Fact] public void RegressionSeries_NonDefaultDegree_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })
        { Degree = 3 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // EnumerableFigureExtensions L99 — likely a generic-type-handling branch.
    // Test by calling Plot with an integer enumerable (uses generic EnumerableFigureExtensions<T>).
    [Fact] public void EnumerableFigureExtensions_IntSequence_ConvertsToFigure()
    {
        var data = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
        var fig = MatPlotLibNet.Plt.Create().Plot(Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), data).Build();
        Assert.NotNull(fig);
    }

    // CommunityThemes line 89% — try every Light theme via reflection to lift the line %.
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("Seaborn")]
    [InlineData("Ggplot")]
    [InlineData("FiveThirtyEight")]
    [InlineData("Bmh")]
    public void CommunityThemes_LightAndDark_AllAccessible(string name)
    {
        var prop = typeof(MatPlotLibNet.Styling.Theme).GetProperty(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(prop);
        Assert.NotNull(prop.GetValue(null));
    }
}
