// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>What the SVG transform writes beside the markup: the script that sends a chart's gestures to the server. It is emitted
/// only when <c>Figure.ServerInteraction</c> is set, and then <c>data-chart-id</c> lands on the root SVG; the local client-side scripts (zoom and pan, legend toggle) are replaced by it,
/// never doubled. Each branch is gated by its own flag and nothing of it is emitted without it, because a script that is not asked
/// for is weight in every byte of every chart that does not want it; the branch is told by the call it makes to the hub, the script
/// carries no comment to mark it with. The hub method names are a wire contract between this script and <c>ChartHub</c>, so
/// renaming either breaks a chart no compiler ever looks at.</summary>
public class SvgTransformTests
{
    private const string SignalRMarker = "mplSignalRInteraction";
    private const string BrushSelectMarker = "invoke('OnBrushSelect'";
    private const string HoverMarker = "invoke('OnHover'";
    private const string DataCursorMarker = "invoke('OnDataCursor'";

    private static string Served(string id, Action<ServerInteractionBuilder> configure) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0], [3.0, 4.0], s => s.Label = "load"))
            .WithServerInteraction(id, configure)
            .ToSvg();

    /// <summary>What a figure carries for the flags it turns on: the script and its branches, the chart id, and the handlers that
    /// every chart in server mode has.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ServerInteractionFalse_Default_ScriptIsNotEmitted", () => Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).ToSvg())
        {
            Absent = [SignalRMarker, "data-chart-id", BrushSelectMarker, HoverMarker, DataCursorMarker],
        },
        new DrawnToken("ServerInteractionTrue_ScriptIsEmitted_WithTheChartId_AndNoBranchNobodyAskedFor", () => Served("live-7", i => i.EnableZoom()),
            SignalRMarker, "data-chart-id=\"live-7\"")
        {
            Absent = [BrushSelectMarker, HoverMarker, DataCursorMarker],
        },
        // Existing script (client-side viewBox manipulation) must not be emitted in server mode: the two would double-handle the
        // same wheel and drag events. The dispatcher replaces it.
        new DrawnToken("ServerInteractionTrue_LocalZoomPanScript_IsSuppressed", () => Served("c", i => i.EnableZoom().EnablePan()), "OnZoom", "OnPan")
        {
            Absent = ["vb.slice()"],
        },
        // The legend toggle script is the one that marks an entry pressed; the server script sends the click instead.
        new DrawnToken("ServerInteractionTrue_LocalLegendToggleScript_IsSuppressed", () => Served("c", i => i.EnableLegendToggle())) { Absent = ["aria-pressed"] },
        new DrawnToken("ServerInteractionFalse_WithZoomPan_StillEmitsLocalScript", () => Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).WithZoomPan().ToSvg(), "vb.slice()")
        {
            Absent = [SignalRMarker],
        },
        new DrawnToken("TheConnectionIsTheOneTheScriptStubsAndTheChartIdIsOnTheRoot", () => Served("test-chart-1", i => i.EnableZoom().EnablePan().EnableReset()),
            "__mpl_signalr_connection", "data-chart-id=\"test-chart-1\"", "OnReset"),
        // Opting into brush-select leaves the zoom and pan handlers in place (they are emitted whenever the figure is in server
        // mode) and the Escape key cancels a brush without a dispatch.
        new DrawnToken("BrushSelectBranch_IsEmitted_WhenEnableBrushSelectIsSet_AndAddsToTheHandlersOfEveryChart", () => Served("c", i => i.EnableBrushSelect()),
            BrushSelectMarker, "OnZoom", "OnPan", "Escape")
        {
            Absent = [HoverMarker, DataCursorMarker],
        },
        new DrawnToken("HoverBranch_IsEmitted_WhenEnableHoverIsSet", () => Served("c", i => i.EnableHover()), HoverMarker)
        {
            Absent = [BrushSelectMarker, DataCursorMarker],
        },
        // The name is a wire contract between this script and ChartHub. Every series group carries data-series-index and an aria-label,
        // which is how a click knows which series it landed on without the page holding any of the data.
        new DrawnToken("DataCursorBranch_CallsTheHubMethodByItsAgreedName_AndFindsTheSeriesFromTheGroupTheMarkerSitsIn", () => Served("c", i => i.EnableDataCursor()),
            DataCursorMarker, "'OnDataCursor'", "data-series-index", "closest('[data-series-index]')")
        {
            Absent = [BrushSelectMarker, HoverMarker],
        },
        new DrawnToken("All_TurnsEveryBranchOn", () => Served("c", i => i.All()), BrushSelectMarker, HoverMarker, DataCursorMarker),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    public static TheoryData<int> EveryCombination { get; } = new(Enumerable.Range(0, (int)SvgTransform.ServerScriptBranches.All + 1));

    /// <summary>A script is put together once per combination of branches: drawing again hands out the same string, and it holds
    /// exactly the branches asked for, each told by the hub method it calls or the message it hears.</summary>
    [Theory]
    [MemberData(nameof(EveryCombination))]
    public void EveryCombinationOfBranches_IsPutTogetherOnce_WithTheBranchesAskedFor(int combination)
    {
        var branches = (SvgTransform.ServerScriptBranches)combination;

        var script = SvgTransform.ServerInteractionScript(branches);

        Assert.Same(script, SvgTransform.ServerInteractionScript(branches));
        Assert.Equal(branches.HasFlag(SvgTransform.ServerScriptBranches.BrushSelect), script.Contains("'OnBrushSelect'", StringComparison.Ordinal));
        Assert.Equal(branches.HasFlag(SvgTransform.ServerScriptBranches.Hover), script.Contains("'OnHover'", StringComparison.Ordinal));
        Assert.Equal(branches.HasFlag(SvgTransform.ServerScriptBranches.DataCursor), script.Contains("'OnDataCursor'", StringComparison.Ordinal));
        Assert.Equal(branches.HasFlag(SvgTransform.ServerScriptBranches.ViewerCursors), script.Contains("'OnCursor'", StringComparison.Ordinal));
    }

    [Fact]
    public void AFigure_CarriesTheBranchesItAsksFor()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableBrushSelect().EnableDataCursor()).Build();

        Assert.Same(SvgTransform.ServerInteractionScript(SvgTransform.ServerScriptBranches.BrushSelect | SvgTransform.ServerScriptBranches.DataCursor),
            SvgTransform.ServerInteractionScriptFor(figure));
    }
}
