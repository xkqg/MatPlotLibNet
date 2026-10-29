// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Council fix B9: <c>SeriesRegistry.Create</c> returns <see langword="null"/> for an
/// unknown series-type discriminator, and <c>ChartSerializer.AddSeriesFromDto</c> used to drop that
/// series with no observable signal — the chart element silently vanished on deserialize. The wire
/// compat contract (old JSON with an unrecognised kind must still load, just without that series) is
/// pinned unchanged; this only adds an observable <see cref="ChartDiagnostics"/> signal naming the
/// unknown discriminator.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class ChartSerializerUnknownDiscriminatorDiagnosticsTests
{
    private static readonly ChartSerializer S = new();

    [Fact]
    public void FromJson_UnknownDiscriminator_EmitsDiagnostic()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "series":[{"type":"__bogus_kind__","label":"skip"}]
            }]
        }
        """;

        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var fig = S.FromJson(json);

            // Lenient default is pinned: the figure still loads, the unknown series is just skipped.
            Assert.Empty(fig.SubPlots[0].Series);

            // But the drop is no longer silent.
            Assert.NotNull(received);
            Assert.Equal("ChartSerializer", received!.Value.Source);
            Assert.Contains("__bogus_kind__", received.Value.Message);
            Assert.Null(received.Value.Exception);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    /// <summary>A secondary-axis series with no "type" field at all (dto.Type is null, not merely unrecognised) —
    /// AxesSerializationExtensions.CreateOnSecondarySeat's own "unknown" fallback, the branch the kind-named test
    /// above does not reach.</summary>
    [Fact]
    public void FromJson_ARightSlotZeroSeriesWithNoTypeField_EmitsDiagnosticNamingItUnknown()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{},
                "secondarySeries":[{"label":"skip"}]
            }]
        }
        """;

        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var fig = S.FromJson(json);

            Assert.Empty(fig.SubPlots[0].YAxes.Right[0].Series);
            Assert.NotNull(received);
            Assert.Contains("'unknown'", received!.Value.Message);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    /// <summary>The same signal on the secondary-axis restore path (ROADMAP.md row 99): before the fix this switch
    /// ended in <c>_ =&gt; null</c> with no diagnostic at all — a bar on the right-hand axis vanished silently.</summary>
    [Fact]
    public void FromJson_UnknownDiscriminator_OnSecondaryAxis_EmitsDiagnostic()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{},
                "secondarySeries":[{"type":"__bogus_kind__","label":"skip"}]
            }]
        }
        """;

        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var fig = S.FromJson(json);

            Assert.Empty(fig.SubPlots[0].YAxes.Right[0].Series);
            Assert.NotNull(received);
            Assert.Equal("ChartSerializer", received!.Value.Source);
            Assert.Contains("__bogus_kind__", received.Value.Message);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    /// <summary>A tick locator, tick formatter, normaliser or colour map of a kind this build does not know (JSON from a
    /// later release, or a typo) is skipped the same way: the figure loads, the axis or series keeps its own default, and
    /// the skip is reported. A reversed map of an unknown map is no map either.</summary>
    [Theory]
    [InlineData("""{"xAxis":{"tickLocator":{"type":"__spiral__","base":2}},"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}""", "tick locator")]
    [InlineData("""{"xAxis":{"tickFormatter":{"type":"__roman__"}},"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}""", "tick formatter")]
    [InlineData("""{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"normalizer":{"type":"__cubic__"}}]}""", "normaliser")]
    [InlineData("""{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"colorMap":{"type":"__swirl__"}}]}""", "colour map")]
    [InlineData("""{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"colorMap":{"type":"reversed","map":{"type":"__swirl__"}}}]}""", "colour map")]
    public void FromJson_AnUnknownLocatorFormatterOrNormaliserKind_IsSkippedWithADiagnostic(string subPlot, string what)
    {
        string json = $$"""{"width":400,"height":300,"subPlots":[{{subPlot}}]}""";
        var received = new List<ChartDiagnostic>();
        void Handler(ChartDiagnostic d) => received.Add(d);
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var axes = S.FromJson(json).SubPlots[0];

            Assert.Null(axes.XAxis.TickLocator);
            Assert.Null(axes.XAxis.TickFormatter);
            Assert.Null((axes.Series[0] as MatPlotLibNet.Models.Series.HeatmapSeries)?.Normalizer);
            Assert.Null((axes.Series[0] as MatPlotLibNet.Models.Series.HeatmapSeries)?.ColorMap);
            Assert.Contains(received, d => d.Source == "ChartSerializer" && d.Message.Contains(what, StringComparison.Ordinal));
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }
}
