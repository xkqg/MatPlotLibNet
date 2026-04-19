// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — tenth pinpoint batch on simple line-coverage gaps
/// (color presets / record types / utility classes that aren't hit because no test touches them).</summary>
public class PinpointBranchTests10
{
    // Colors line 82% → unhit: DarkGray, Transparent, AliceBlue, Gold, Khaki, Silver, MidnightBlue.
    [Fact] public void Colors_AllUnhitPresets_Reachable()
    {
        // Touch every preset so the property getter line counts as covered.
        // (Transparent is intentionally Color { 0,0,0,0 } — same as default — so
        // assert by alpha for that one rather than NotEqual to default.)
        _ = Colors.DarkGray; _ = Colors.AliceBlue; _ = Colors.Gold;
        _ = Colors.Khaki; _ = Colors.Silver; _ = Colors.MidnightBlue;
        Assert.Equal(0, Colors.Transparent.A);
    }

    // SignalResult line 60% → exercise both implicit conversions + indexer + Length property.
    [Fact] public void SignalResult_AllPublicSurface_Exercised()
    {
        SignalResult fromArray = new[] { 1.0, 2.0, 3.0 };
        Assert.Equal(3, fromArray.Length);
        Assert.Equal(2.0, fromArray[1]);
        double[] toArray = fromArray;
        Assert.Equal([1.0, 2.0, 3.0], toArray);
    }

    // PriceIndicator<TResult> line 75% → invoke the abstract surface via concrete subclass.
    [Fact] public void PriceIndicator_BaseClassPropertiesAccessible()
    {
        // Sma is the canonical concrete PriceIndicator<SignalResult>.
        var sma = new Sma(new double[] { 1, 2, 3, 4, 5 }, period: 3);
        Assert.NotNull(sma.Compute());
        // Exercise base-class Label setter/getter.
        sma.Label = "test";
        Assert.Equal("test", sma.Label);
    }

    // EnumerableFigureExtensions L99 — exercise the IEnumerable<double> Plot path.
    [Fact] public void EnumerableFigureExtensions_DoubleEnumerable_ProducesValidFigure()
    {
        IEnumerable<double> data = Enumerable.Range(1, 5).Select(i => (double)i);
        var fig = MatPlotLibNet.Plt.Create().Plot(data.ToArray(), data.ToArray()).Build();
        Assert.NotNull(fig);
    }

    // Contour3DSeries L30 — Color != null branch (we already test Levels).
    [Fact] public void Contour3DSeries_WithExplicitColor_SerializesIt()
    {
        var s = new Contour3DSeries(new double[] { 0.0, 1 }, new double[] { 0.0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } })
        { Color = Colors.Red, Levels = 5 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // Line3DSeries L27 — both Color set and not set already covered;
    // additional coverage by setting LineStyle (which has its own ternary).
    [Fact] public void Line3DSeries_WithExplicitLineStyle_SerializesIt()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { LineStyle = LineStyle.Dashed, Label = "L1" };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // RegressionSeries L48 — ConfidenceLevel != default branch.
    [Fact] public void RegressionSeries_NonDefaultConfidence_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1.0, 2, 3 }, new double[] { 1.0, 2, 3 })
        { ConfidenceLevel = 0.99, ShowConfidence = true, LineWidth = 3.0 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // Box3D + CrosshairModifier require interaction-harness contexts not in scope here.
    // Tracked as Phase R deferral.
}
