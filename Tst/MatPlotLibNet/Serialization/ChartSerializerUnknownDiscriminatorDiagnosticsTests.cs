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
}
