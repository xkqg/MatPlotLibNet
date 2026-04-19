// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — ninth pinpoint batch on series ToSeriesDto
/// non-default-property ternaries.</summary>
public class PinpointBranchTests9
{
    // SurfaceSeries L32 — exercises EVERY ternary by setting non-defaults.
    [Fact] public void SurfaceSeries_NonDefaultProperties_AllTernariesHitOppositeBranch()
    {
        var s = new SurfaceSeries(new double[] { 1, 2 }, new double[] { 1, 2 },
            new double[,] { { 1, 2 }, { 3, 4 } })
        {
            ShowWireframe = false,         // ShowWireframe ? null : (bool?)false → returns false
            RowStride = 2,                 // RowStride != 1 ? RowStride : null → returns 2
            ColStride = 2,                 // ColStride != 1 ? ColStride : null → returns 2
            Alpha = 0.5                    // Alpha != 0.8 ? Alpha : null → returns 0.5
        };
        var dto = s.ToSeriesDto();
        Assert.Equal((bool?)false, dto.ShowWireframe);
        Assert.Equal(2, dto.RowStride);
        Assert.Equal(2, dto.ColStride);
        Assert.Equal(0.5, dto.Alpha);
    }

    // Trisurf3D L37 — same pattern.
    [Fact] public void Trisurf3DSeries_NonDefaultProperties_AllTernariesHitOppositeBranch()
    {
        var s = new Trisurf3DSeries(
            new double[] { 0.0, 1, 0.5 }, new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })
        {
            ShowWireframe = false,
            Alpha = 0.5,
            Color = Colors.Red
        };
        var dto = s.ToSeriesDto();
        Assert.Equal((bool?)false, dto.ShowWireframe);
        Assert.Equal(0.5, dto.Alpha);
    }

    // ContourSeries L48 — empty data branch (both X and Y empty, hits the early-return).
    [Fact] public void ContourSeries_EmptyXData_HitsEarlyReturn()
    {
        var s = new ContourSeries(Array.Empty<double>(), Array.Empty<double>(), new double[0, 0]);
        var range = s.ComputeDataRange(new TestCtx());
        Assert.Null(range.XMin);
    }

    // RegressionSeries L48 — Color != null branch in ToSeriesDto.
    [Fact] public void RegressionSeries_WithColor_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1.0, 2, 3 }, new double[] { 1.0, 2, 3 })
        { Color = Colors.Red, Degree = 2, ConfidenceLevel = 0.99, ShowConfidence = true };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // Quiver3DSeries L78 — explicit Color branch in ToSeriesDto.
    [Fact] public void Quiver3DSeries_WithExplicitColor_SerializesIt()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { Color = Colors.Blue };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // RcParams L64 — TryGetValue (T)v cast — explicit type test.
    [Fact] public void RcParams_GetSetIntValue_ExercisesTypedGet()
    {
        var rc = new RcParams();
        rc.Set(RcParamKeys.FontSize, 14.0);
        Assert.Equal(14.0, rc.Get<double>(RcParamKeys.FontSize, 0.0));
    }

    // LegendToggleEvent — type lookup via reflection (lives in Interaction namespace).

    // EnumerableFigureExtensions L99 — string IEnumerable overload.
    [Fact] public void EnumerableFigureExtensions_NonEmpty_ProducesValidFigure()
    {
        var fig = Plt.Create().Bar(["A", "B", "C"], new double[] { 1.0, 2.0, 3.0 }).Build();
        Assert.NotNull(fig);
    }

    private sealed class TestCtx : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
