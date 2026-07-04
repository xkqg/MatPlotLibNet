// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Golden-JSON corpus gate for the upcoming <see cref="ChartSerializer"/> refactor. One
/// checked-in golden file per registered <see cref="SeriesRegistry"/> discriminator (all 76,
/// enumerated from the exact <c>Register("...")</c> calls in <c>SeriesRegistry.cs</c>) pins
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
/// <c>&lt;Content CopyToOutputDirectory&gt;</c> item — see <see cref="GoldenPath"/>.</para>
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

    private static string GoldenDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "Serialization", "Golden");

    private static string GoldenPath(string discriminator) =>
        Path.Combine(GoldenDirectory(), $"{discriminator}.json");

    private static Figure BuildStabilizedIfNeeded(string discriminator, Figure fig)
    {
        if (StabilizedKinds.Contains(discriminator))
        {
            return S.FromJson(S.ToJson(fig));
        }
        return fig;
    }

    // ── Theory: every registered discriminator gets a golden file ───────────

    [Theory]
    [InlineData("line")]
    [InlineData("scatter")]
    [InlineData("bar")]
    [InlineData("histogram")]
    [InlineData("pie")]
    [InlineData("box")]
    [InlineData("violin")]
    [InlineData("hexbin")]
    [InlineData("regression")]
    [InlineData("kde")]
    [InlineData("heatmap")]
    [InlineData("image")]
    [InlineData("histogram2d")]
    [InlineData("stem")]
    [InlineData("contour")]
    [InlineData("contourf")]
    [InlineData("area")]
    [InlineData("step")]
    [InlineData("ecdf")]
    [InlineData("stackedarea")]
    [InlineData("errorbar")]
    [InlineData("candlestick")]
    [InlineData("quiver")]
    [InlineData("streamplot")]
    [InlineData("radar")]
    [InlineData("donut")]
    [InlineData("bubble")]
    [InlineData("ohlcbar")]
    [InlineData("waterfall")]
    [InlineData("funnel")]
    [InlineData("gantt")]
    [InlineData("gauge")]
    [InlineData("progressbar")]
    [InlineData("stattile")]
    [InlineData("statetimeline")]
    [InlineData("sparkline")]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    [InlineData("dendrogram")]
    [InlineData("clustermap")]
    [InlineData("pairgrid")]
    [InlineData("networkgraph")]
    [InlineData("relativerotation")]
    [InlineData("sankey")]
    [InlineData("polarline")]
    [InlineData("polarscatter")]
    [InlineData("polarbar")]
    [InlineData("surface")]
    [InlineData("wireframe")]
    [InlineData("scatter3d")]
    [InlineData("rugplot")]
    [InlineData("stripplot")]
    [InlineData("eventplot")]
    [InlineData("brokenbar")]
    [InlineData("count")]
    [InlineData("pcolormesh")]
    [InlineData("residual")]
    [InlineData("pointplot")]
    [InlineData("swarmplot")]
    [InlineData("spectrogram")]
    [InlineData("table")]
    [InlineData("tricontour")]
    [InlineData("tripcolor")]
    [InlineData("quiverkey")]
    [InlineData("barbs")]
    [InlineData("stem3d")]
    [InlineData("bar3d")]
    [InlineData("signal-xy")]
    [InlineData("signal")]
    [InlineData("polarheatmap")]
    [InlineData("line3d")]
    [InlineData("trisurf")]
    [InlineData("contour3d")]
    [InlineData("quiver3d")]
    [InlineData("voxels")]
    [InlineData("text3d")]
    public void Golden_MatchesCheckedInFile_AndRoundTripFixpoints(string discriminator)
    {
        var rawFig = BuildFigure(discriminator);
        var assertionSubject = BuildStabilizedIfNeeded(discriminator, rawFig);
        var json = S.ToJson(assertionSubject);
        Assert.Contains($"\"type\":\"{discriminator}\"", json, StringComparison.Ordinal);

        string path = GoldenPath(discriminator);
        if (Environment.GetEnvironmentVariable("MPL_REGEN_GOLDEN") == "1")
        {
            Directory.CreateDirectory(GoldenDirectory());
            string toWrite = StabilizedKinds.Contains(discriminator)
                ? S.ToJson(S.FromJson(S.ToJson(rawFig)))
                : S.ToJson(rawFig);
            File.WriteAllText(path, toWrite);
            return;
        }

        Assert.True(File.Exists(path),
            $"Golden file missing for discriminator '{discriminator}': {path}. " +
            "Regenerate with the MPL_REGEN_GOLDEN=1 environment variable.");
        string golden = File.ReadAllText(path).TrimEnd('\r', '\n');

        // (a) Native construction serializes byte-for-byte identical to the pinned golden.
        Assert.Equal(golden, json);

        // (b) Round-trip fixpoint: deserializing the golden and re-serializing reproduces it.
        string fixpointJson = S.ToJson(S.FromJson(golden));
        Assert.Equal(golden, fixpointJson);
    }

    // ── Native figure construction per discriminator ─────────────────────────

    private static Figure BuildFigure(string discriminator) => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => AddSeriesByKind(ax, discriminator))
        .Build();

    private static readonly double[,] Grid2X2 = { { 1.0, 2.0 }, { 3.0, 4.0 } };
    private static readonly double[,] Grid2X2Alt = { { 0.0, 1.0 }, { 1.0, 0.0 } };

    private static void AddSeriesByKind(AxesBuilder ax, string kind)
    {
        switch (kind)
        {
            case "line": ax.Plot([1.0, 2, 3], [4.0, 5, 6]); break;
            case "scatter": ax.Scatter([1.0, 2, 3], [4.0, 5, 6]); break;
            case "bar": ax.Bar(["A", "B"], [1.0, 2.0]); break;
            case "histogram": ax.Hist([1.0, 2, 3, 4, 5]); break;
            case "pie": ax.Pie([30.0, 70.0]); break;
            case "box": ax.BoxPlot([[1.0, 2, 3]]); break;
            case "violin": ax.Violin([[1.0, 2, 3]]); break;
            case "hexbin": ax.Hexbin([1.0, 2, 3], [1.0, 2, 3]); break;
            case "regression": ax.Regression([1.0, 2, 3], [1.0, 2, 3]); break;
            case "kde": ax.Kde([1.0, 2, 3, 4, 5]); break;
            case "heatmap": ax.Heatmap(Grid2X2); break;
            case "image": ax.Image(Grid2X2); break;
            case "histogram2d": ax.Histogram2D([1.0, 2, 3], [1.0, 2, 3]); break;
            case "stem": ax.Stem([1.0, 2], [3.0, 4]); break;
            case "contour": ax.Contour([1.0, 2], [1.0, 2], Grid2X2); break;
            case "contourf": ax.Contourf([1.0, 2], [1.0, 2], Grid2X2); break;
            case "area": ax.FillBetween([1.0, 2], [3.0, 4]); break;
            case "step": ax.Step([1.0, 2], [3.0, 4]); break;
            case "ecdf": ax.Ecdf([1.0, 2, 3]); break;
            case "stackedarea": ax.StackPlot([1.0, 2], [[1.0, 2], [3.0, 4]]); break;
            case "errorbar": ax.ErrorBar([1.0, 2], [3.0, 4], [0.1, 0.2], [0.1, 0.2]); break;
            case "candlestick": ax.Candlestick([10.0, 11], [12.0, 13], [9.0, 10], [11.0, 12]); break;
            case "quiver": ax.Quiver([0.0, 1], [0.0, 1], [1.0, 1], [1.0, 1]); break;
            case "streamplot": ax.Streamplot([0.0, 1.0], [0.0, 1.0], Grid2X2Alt, Grid2X2Alt); break;
            case "radar": ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0]); break;
            case "donut": ax.Donut([30.0, 70.0]); break;
            case "bubble": ax.Bubble([1.0, 2], [3.0, 4], [10.0, 20]); break;
            case "ohlcbar": ax.OhlcBar([10.0, 12], [15.0, 17], [8.0, 10], [13.0, 15]); break;
            case "waterfall": ax.Waterfall(["A", "B"], [10.0, -5.0]); break;
            case "funnel": ax.Funnel(["A", "B"], [100.0, 50.0]); break;
            case "gantt": ax.Gantt(["Task A", "Task B"], [0.0, 2.0], [3.0, 5.0]); break;
            case "gauge": ax.Gauge(0.7); break;
            case "progressbar": ax.ProgressBar(0.5); break;
            case "stattile": ax.StatTile(42.0); break;
            case "statetimeline":
                ax.StateTimeline([new StateSegment(0, 1, "A", Colors.Red), new StateSegment(1, 2, "B", Colors.Blue)]);
                break;
            case "sparkline": ax.Sparkline([1.0, 2, 3, 4, 5]); break;
            case "treemap": ax.Treemap(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }); break;
            case "sunburst": ax.Sunburst(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }); break;
            case "dendrogram":
                ax.Dendrogram(new TreeNode
                {
                    Label = "Root",
                    Value = 1,
                    Children = [new TreeNode { Label = "A", Value = 0 }, new TreeNode { Label = "B", Value = 0 }],
                });
                break;
            case "clustermap": ax.Clustermap(Grid2X2); break;
            case "pairgrid": ax.PairGrid([[1.0, 2, 3], [4.0, 5, 6]]); break;
            case "networkgraph":
                ax.NetworkGraph(
                    [new GraphNode("a", X: 1, Y: 2, ColorScalar: 0.3, SizeScalar: 1.5, Label: "Alpha"),
                     new GraphNode("b", X: 3, Y: 4, ColorScalar: 0.7, SizeScalar: 2.0)],
                    [new GraphEdge("a", "b", Weight: 1.5, IsDirected: true)]);
                break;
            case "relativerotation":
                ax.RelativeRotation(
                    [[100.0, 101.0, 102.0], [100.0, 99.0, 98.0]],
                    [100.0, 100.0, 100.0],
                    ["ETH", "BNB"]);
                break;
            case "sankey": ax.Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]); break;
            case "polarline": ax.PolarPlot([1.0, 2], [0.0, 1.5]); break;
            case "polarscatter": ax.PolarScatter([1.0, 2], [0.0, 1.5]); break;
            case "polarbar": ax.PolarBar([1.0, 2], [0.0, 1.5]); break;
            case "surface": ax.Surface([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
            case "wireframe": ax.Wireframe([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
            case "scatter3d": ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            case "rugplot": ax.Rugplot([1.0, 2, 3]); break;
            case "stripplot": ax.Stripplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
            case "eventplot": ax.Eventplot([[1.0, 2, 3]]); break;
            case "brokenbar": ax.BrokenBarH([[new BarRange(1, 2), new BarRange(4, 1)]]); break;
            case "count": ax.Countplot(["A", "A", "B"]); break;
            case "pcolormesh": ax.Pcolormesh([0.0, 1, 2], [0.0, 1, 2], Grid2X2); break;
            case "residual": ax.Residplot([1.0, 2, 3, 4], [1.5, 2.1, 2.8, 4.2]); break;
            case "pointplot": ax.Pointplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
            case "swarmplot": ax.Swarmplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
            case "spectrogram":
                ax.Spectrogram(Enumerable.Range(0, 64).Select(i => Math.Sin(i * 0.5)).ToArray(), 1000);
                break;
            case "table": ax.Table([["a", "b"], ["c", "d"]]); break;
            case "tricontour": ax.Tricontour([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3]); break;
            case "tripcolor": ax.Tripcolor([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3]); break;
            case "quiverkey": ax.QuiverKey(0.5, 0.9, 1.0, "key"); break;
            case "barbs": ax.Barbs([0.0], [0.0], [10.0], [45.0]); break;
            case "stem3d": ax.Stem3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            case "bar3d": ax.Bar3D([0.0, 1.0], [0.0, 1.0], [1.0, 2.0]); break;
            case "signal-xy": ax.SignalXY([0.0, 1.0, 2.0], [1.0, 2.0, 3.0]); break;
            case "signal": ax.Signal([1.0, 2.0, 3.0], 1.0, 0.0); break;
            case "polarheatmap": ax.PolarHeatmap(Grid2X2, 4, 2); break;
            case "line3d": ax.Plot3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            case "trisurf": ax.Trisurf([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 4]); break;
            case "contour3d": ax.Contour3D([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
            case "quiver3d": ax.Quiver3D([0.0], [0.0], [0.0], [1.0], [1.0], [1.0]); break;
            case "voxels":
                var filled = new bool[2, 2, 2];
                filled[0, 0, 0] = true;
                filled[1, 1, 1] = true;
                ax.Voxels(filled);
                break;
            case "text3d": ax.Text3D(1, 2, 3, "hello"); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "unknown discriminator");
        }
    }
}
