// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Golden-JSON corpus gate for the upcoming <see cref="ChartSerializer"/> refactor. One
/// checked-in golden file per registered <see cref="SeriesRegistry"/> discriminator (every one
/// <see cref="SeriesRegistry.Discriminators"/> lists, so a new series type is held to a golden file by the Theory itself) pins
/// the exact byte-for-byte wire shape the current serializer produces. A refactor that changes
/// field order, naming, null-omission, or default-materialization for any series type will fail
/// this Theory immediately, instead of surfacing as a silent wire-format drift discovered later
/// by a downstream consumer.
///
/// <para>Two invariants are checked per kind:</para>
/// <list type="number">
///   <item>(a) <c>ToJson(fig)</c> reproduces the golden file byte-for-byte (ordinal compare,
///   trailing-newline-normalized) — proves native construction serializes exactly as pinned.</item>
///   <item>(b) <c>ToJson(FromJson(golden))</c> reproduces the golden file — proves the golden is
///   a round-trip <b>fixpoint</b>, i.e. deserializing and re-serializing never drifts further.</item>
/// </list>
///
/// <para>Regeneration: set the <c>MPL_REGEN_GOLDEN=1</c> environment variable to make this Theory
/// WRITE the golden file instead of asserting against it (standard approval-test shape). Default
/// (no env var) asserts. Golden files live under <c>Serialization/Golden/&lt;discriminator&gt;.json</c>
/// relative to the test assembly and are copied there at build time via the csproj
/// <c>&lt;Content CopyToOutputDirectory&gt;</c> item — see <see cref="BytePins"/>.</para>
/// </summary>
[Collection("ChartSerializerGlobalState")]
public class ChartSerializerGoldenCorpusTests
{
    private static readonly ChartSerializer S = new();

    /// <summary>Discriminators whose native construction does NOT reproduce the round-trip
    /// fixpoint on the first pass — some optional field materializes a different default (or is
    /// dropped/normalized) only after passing through <c>FromJson</c> once. For these kinds the
    /// checked-in golden is the STABILIZED form <c>ToJson(FromJson(ToJson(fig)))</c>, and the
    /// figure used for assertion (a) is likewise pre-stabilized by one round trip before the
    /// final <c>ToJson</c> call — see <see cref="BuildStabilizedIfNeeded"/>.
    /// Empirically determined by running this Theory in <c>MPL_REGEN_GOLDEN=1</c> mode and then
    /// observing which kinds failed assertion (b) on the very next assert-mode run.</summary>
    /// <remarks>Empirical result (2026-07-04): the set is EMPTY. All 76 discriminators'
    /// native constructions already reproduce the round-trip fixpoint on the first pass — no
    /// kind's golden needed the extra stabilization round trip. Verified by generating every
    /// golden as the raw <c>ToJson(fig)</c> (via <c>MPL_REGEN_GOLDEN=1</c>) and then observing
    /// that the very next assert-mode run passed both assertion (a) and (b) for all 76 kinds
    /// with zero failures — if any kind had needed stabilization, assertion (b) would have
    /// failed for it on that run and its name would have been added here.</remarks>
    private static readonly HashSet<string> StabilizedKinds = new(StringComparer.Ordinal);

    /// <summary>The discriminators a satellite package registers when its assembly loads into the process (the Geo package's
    /// polygon): they are not the core's and are not built here.</summary>
    private static readonly HashSet<string> SatelliteKinds = new(["geo_polygon"], StringComparer.Ordinal);

    /// <summary>Every discriminator the registry knows, so a series type registered tomorrow is held to a golden file
    /// without an edit here: the Theory has no hand-kept list that could fall behind the registry.</summary>
    public static TheoryData<string> Discriminators { get; } = new(SeriesRegistry.Discriminators.Where(kind => !SatelliteKinds.Contains(kind)));

    private static Figure BuildStabilizedIfNeeded(string discriminator, Figure fig)
    {
        if (StabilizedKinds.Contains(discriminator))
        {
            return fig.RoundTripped();
        }
        return fig;
    }

    // ── Theory: every registered discriminator gets a golden file ───────────

    [Theory]
    [MemberData(nameof(Discriminators))]
    public void Golden_MatchesCheckedInFile_AndRoundTripFixpoints(string discriminator)
    {
        var rawFig = BuildFigure(discriminator);
        var assertionSubject = BuildStabilizedIfNeeded(discriminator, rawFig);
        var json = S.ToJson(assertionSubject);
        Assert.Contains($"\"type\":\"{discriminator}\"", json, StringComparison.Ordinal);

        // MPL_REGEN_GOLDEN=1 writes the stabilized form; otherwise the checked-in file is read.
        string golden = BytePins.Pinned("", discriminator, StabilizedKinds.Contains(discriminator)
            ? S.ToJson(rawFig.RoundTripped())
            : S.ToJson(rawFig));

        // (a) Native construction serializes byte-for-byte identical to the pinned golden.
        Assert.Equal(golden, json);

        // (b) Round-trip fixpoint: deserializing the golden and re-serializing reproduces it.
        string fixpointJson = S.ToJson(S.FromJson(golden));
        Assert.Equal(golden, fixpointJson);
    }

    // ── Native figure construction per discriminator ─────────────────────────

    private static Figure BuildFigure(string discriminator) => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.AddSampleSeries(discriminator))
        .Build();
}
