// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The data-cursor branch of the SignalR dispatcher script: a click on a marker becomes an
/// <c>OnDataCursor</c> call on the hub. It is gated the way every other branch is — opt in with
/// <c>EnableDataCursor()</c> and nothing is emitted without it, because a script that is not asked for is
/// weight in every byte of every chart that does not want it.</summary>
public class SvgSignalRDataCursorScriptTests
{
    private const string Marker = "mplDataCursor";

    private static string Svg(Action<MatPlotLibNet.Builders.ServerInteractionBuilder> configure) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0], [3.0, 4.0], s => s.Label = "load"))
            .WithServerInteraction("c", configure)
            .ToSvg();

    [Fact]
    public void TheBranch_IsEmitted_WhenTheDataCursorIsEnabled()
    {
        Assert.Contains(Marker, Svg(i => i.EnableDataCursor()), StringComparison.Ordinal);
    }

    [Fact]
    public void TheBranch_IsAbsent_WhenItIsNot()
    {
        Assert.DoesNotContain(Marker, Svg(i => i.EnableZoom()), StringComparison.Ordinal);
    }

    [Fact]
    public void TheBranch_CallsTheHubMethodByItsAgreedName()
    {
        // The name is a wire contract between this script and ChartHub; renaming either breaks a chart that no
        // compiler ever looks at.
        Assert.Contains("'OnDataCursor'", Svg(i => i.EnableDataCursor()), StringComparison.Ordinal);
    }

    [Fact]
    public void TheBranch_FindsTheSeriesFromTheGroupTheMarkerSitsIn()
    {
        // Every series group carries data-series-index and an aria-label; that is how a click knows which series
        // it landed on without the page holding any of the data.
        string svg = Svg(i => i.EnableDataCursor());

        Assert.Contains("data-series-index", svg, StringComparison.Ordinal);
        Assert.Contains("closest('[data-series-index]')", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void All_TurnsItOnAlongWithTheRest()
    {
        Assert.Contains(Marker, Svg(i => i.All()), StringComparison.Ordinal);
    }
}
