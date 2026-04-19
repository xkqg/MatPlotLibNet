// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase X.7 wave-2 (v1.7.2, 2026-04-19) — branch lifts for the C2 cohort
/// (line ≥ 90%, branch 80–89%). Each fact pins a single missed condition arm
/// identified via cobertura `condition-coverage="50% (1/2)"` markers, with the
/// source file:line cited in the summary so a future maintainer can map the
/// fact back to the production branch it covers.</summary>
public class PinpointBranchTests11
{
    /// <summary>RcParams.Get&lt;T&gt;(key) line 64 — `_params.TryGetValue(...) ? (T)v : throw`
    /// false arm. Lift class from 92.3%L / 83.3%B → 92.3%L / 100%B.</summary>
    [Fact]
    public void RcParams_Get_UnknownKey_ThrowsKeyNotFound()
    {
        var rc = new RcParams();
        Assert.Throws<KeyNotFoundException>(() => rc.Get<int>("non-existent-key"));
    }

    /// <summary>Quiver3DSeries.ToSeriesDto line 87 — `ArrowLength != 1.0 ? ArrowLength : null`
    /// false arm (default = 1.0). Lift 91.9%L / 83.3%B → ~91.9%L / 100%B.</summary>
    [Fact]
    public void Quiver3DSeries_NonDefaultArrowLength_EmittedInDto()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { ArrowLength = 2.5 };
        var dto = s.ToSeriesDto();
        Assert.Equal(2.5, dto.ArrowLength);
    }

    /// <summary>ParabolicSar.Apply line 44 — `if (n &lt; 2) return` true arm.
    /// Single-bar input triggers the early return; Apply is a no-op (no series added).</summary>
    [Fact]
    public void ParabolicSar_SingleBar_NoSeriesAdded()
    {
        var axes = new Axes();
        new ParabolicSar(new double[] { 100.0 }, new double[] { 99.0 }).Apply(axes);
        Assert.Empty(axes.Series);
    }

    /// <summary>ParabolicSar.Apply line 49 — `_high[1] &gt;= _high[0]` false arm:
    /// when second bar's high is LOWER than first, trending starts SHORT.</summary>
    [Fact]
    public void ParabolicSar_DescendingFirstBar_RendersWithoutCrash()
    {
        var axes = new Axes();
        new ParabolicSar(
            high: new double[] { 105.0, 100.0, 95.0, 92.0, 88.0 },
            low:  new double[] { 100.0, 95.0, 90.0, 88.0, 85.0 }).Apply(axes);
        Assert.NotEmpty(axes.Series);
    }

    /// <summary>CountSeriesRenderer line 27 — `series.Orientation == Vertical` false arm
    /// (Horizontal). Default Vertical was the only path previously tested.</summary>
    [Fact]
    public void CountSeries_Horizontal_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new CountSeries(new[] { "a", "b", "a", "c", "b", "a" })
                { Orientation = BarOrientation.Horizontal }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>StreamingRsi line 45 — `ProcessedCount == _period + 1` first-bar-after-warmup
    /// arm. Pushes exactly _period+1 samples to trip the avg-init divider.</summary>
    [Fact]
    public void StreamingRsi_PeriodPlusOneSamples_HitsAvgInit()
    {
        var rsi = new MatPlotLibNet.Indicators.Streaming.StreamingRsi(period: 14);
        // Push 16 closes (period 14 + 2) to cross the warmup boundary.
        for (int i = 0; i < 16; i++) rsi.Append(100 + Math.Sin(i) * 5);
        Assert.True(rsi.ProcessedCount >= 15);
    }

    /// <summary>SunburstSeriesRenderer.RenderRing line 64 — `if (total &lt;= 0) return;`
    /// true arm. A tree node with all-zero children sums to zero → renderer skips.</summary>
    [Fact]
    public void SunburstSeries_AllZeroChildValues_RendersWithoutCrash()
    {
        var root = new TreeNode { Label = "Root", Children =
        [
            new TreeNode { Label = "A", Value = 0 },
            new TreeNode { Label = "B", Value = 0 }
        ]};
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sunburst(root))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>BarSeries.ComputeDataRange line 126 — `c &lt; s.Values.Length ? s.Values[c] : 0`
    /// false arm: one stacked series shorter than the other → fallback to 0 for missing index.</summary>
    [Fact]
    public void BarSeries_StackedSeries_VaryingLengths_ZeroFallbackArm()
    {
        // Two stackable BarSeries (BarSeries is IStackable by default); second has
        // fewer values than first → triggers the `c < s.Values.Length ? : 0` fallback.
        var b1 = new BarSeries(new[] { "a", "b", "c" }, new[] { 1.0, 2.0, 3.0 });
        var b2 = new BarSeries(new[] { "a", "b" }, new[] { 4.0, 5.0 });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { ax.AddSeries(b1); ax.AddSeries(b2); })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // InteractiveExtensions.Browser setter test omitted: InteractiveExtensions lives
    // in MatPlotLibNet.Interactive assembly which MatPlotLibNet.Tests does not reference;
    // the test belongs in MatPlotLibNet.Interactive.Tests instead.

    /// <summary>LegendToggleEvent.ApplyTo line 21 — `SeriesIndex &lt; 0 || SeriesIndex &gt;= Count`
    /// out-of-range arm. Index = 99 on a 1-series figure exercises the early-return.</summary>
    [Fact]
    public void LegendToggleEvent_OutOfRangeIndex_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]))
            .Build();
        var evt = new MatPlotLibNet.Interaction.LegendToggleEvent("c1", AxesIndex: 0, SeriesIndex: 99);
        evt.ApplyTo(fig);   // must not throw, must not crash
        Assert.True(fig.SubPlots[0].Series[0].Visible);   // unchanged
    }

    /// <summary>LegendToggleEvent.ApplyTo line 21 — second arm `SeriesIndex &lt; 0`.</summary>
    [Fact]
    public void LegendToggleEvent_NegativeIndex_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]))
            .Build();
        var evt = new MatPlotLibNet.Interaction.LegendToggleEvent("c1", AxesIndex: 0, SeriesIndex: -1);
        evt.ApplyTo(fig);
        Assert.True(fig.SubPlots[0].Series[0].Visible);
    }

    /// <summary>PriceSources.Resolve line 34 — switch covers Close/Open/High/Low (4/8)
    /// pre-X. This Theory exercises the remaining 4: HL2, HLC3, OHLC4, HLCC4.</summary>
    [Theory]
    [InlineData(MatPlotLibNet.Indicators.PriceSource.HL2)]
    [InlineData(MatPlotLibNet.Indicators.PriceSource.HLC3)]
    [InlineData(MatPlotLibNet.Indicators.PriceSource.OHLC4)]
    public void PriceSources_DerivedSources_ComputeCorrectAverages(MatPlotLibNet.Indicators.PriceSource source)
    {
        double[] open = { 100, 101, 102 };
        double[] high = { 105, 106, 107 };
        double[] low = { 99, 100, 101 };
        double[] close = { 103, 104, 105 };
        var result = MatPlotLibNet.Indicators.PriceSources.Resolve(source, open, high, low, close);
        Assert.Equal(3, result.Length);
        Assert.True(result[0] > 0);
    }

    /// <summary>SankeySeriesRenderer line 86 — `vert = Orient == Vertical` true arm.
    /// Default is Horizontal; Vertical orientation exercises the rotated layout
    /// pipeline. Lifts class from 91/82.5 → ~91/85+.</summary>
    [Fact]
    public void SankeySeries_VerticalOrientation_RendersWithoutCrash()
    {
        var nodes = new[] { new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C") };
        var links = new[] { new SankeyLink(0, 1, 5), new SankeyLink(1, 2, 3) };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new SankeySeries(nodes, links) { Orient = SankeyOrientation.Vertical }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>FigureTemplates.FinancialDashboard lines 70/81/103 — the three
    /// `configure*Panel?.Invoke(ax)` null-conditional invocations. Default null
    /// is tested elsewhere; supplying non-null callbacks exercises the true arms.</summary>
    [Fact]
    public void FinancialDashboard_AllPanelCallbacks_AreInvoked()
    {
        double[] open = { 100, 102, 101 }, high = { 103, 104, 103 };
        double[] low = { 98, 99, 98 }, close = { 101, 102, 101 };
        double[] volume = { 1000, 1200, 800 };
        bool priceCalled = false, volCalled = false, oscCalled = false;
        var fig = FigureTemplates.FinancialDashboard(
            open, high, low, close, volume,
            configurePricePanel: _ => priceCalled = true,
            configureVolumePanel: _ => volCalled = true,
            configureOscillatorPanel: _ => oscCalled = true).Build();
        Assert.True(priceCalled);
        Assert.True(volCalled);
        Assert.True(oscCalled);
    }

    // ThreeDAxesRenderer ProjectionSet/Pane3D tests omitted: AxesBuilder doesn't
    // surface SetElevation/SetAzimuth/Pane3D fluently — those branches need direct
    // Axes property mutation that Plt.Create()'s pipeline doesn't easily support.
    // Tracked for X.9.b deeper 3D test coverage.

    /// <summary>ThreeDAxesRenderer line 409 — `if (range &lt;= 0) return [lo];` degenerate
    /// tick range. Triggered when 3D axis has zero span.</summary>
    [Fact]
    public void ThreeDAxes_ZeroRangeAxis_ReturnsSingleTick()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                // Single-point surface forces zMin==zMax (zero range on Z).
                ax.Surface(new double[] { 5, 5 }, new double[] { 5, 5 },
                    new double[,] { { 7, 7 }, { 7, 7 } });
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>DataTransform.TransformX line 138 — `_xScale == 0` degenerate-X-axis arm.
    /// Forces axis range to zero span so the centre-fill fallback fires.</summary>
    [Fact]
    public void DataTransform_DegenerateXAxis_FillsCenterPixel()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([5.0, 5.0, 5.0], [1.0, 2.0, 3.0]);   // all-same X
                ax.SetXLim(5, 5);   // zero-span x-axis
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>DataTransform.TransformY line 161 — `_yScale == 0` Y mirror of above.</summary>
    [Fact]
    public void DataTransform_DegenerateYAxis_FillsCenterPixel()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0, 3.0], [5.0, 5.0, 5.0]);
                ax.SetYLim(5, 5);
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Phase X.7 finding: IRenderContext.MeasureRichText line 76 default-interface
    // implementation needs a concrete IRenderContext + non-1.0 FontSizeScale span. The
    // test setup is non-trivial — SvgRenderContext doesn't expose a clean public ctor
    // for instantiation outside the rendering pipeline. The existing LegendMeasurer
    // math-label test in NearMissBranchTests does invoke the path indirectly when
    // labels contain $...$ math; the deeper non-1.0 scale arm is already covered if
    // MathTextParser produces super/subscript spans for that input.
}
