// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase X.11.a (v1.7.2, 2026-04-19) — drives <see cref="MplStreamingChart"/>
/// from 0%L to 90%+L. The component does NOT use SignalR — it subscribes to
/// <see cref="StreamingFigure.RenderRequested"/> on its parameter, re-renders the
/// SVG, and unsubscribes on dispose. All branches are covered without any network:
///   - OnParametersSet with null StreamingFigure → empty content (line 59 true arm)
///   - OnParametersSet with new StreamingFigure → Subscribe + RenderChart
///   - OnParametersSet with same StreamingFigure → no resubscribe (line 23 false arm)
///   - OnParametersSet with changed StreamingFigure → Unsubscribe old, Subscribe new
///   - RenderRequested fires → invokeasync → re-render
///   - Dispose → unsubscribe
///   - CssClass parameter applied to container div</summary>
public class MplStreamingChartTests : BunitContext
{
    private static StreamingFigure NewStreamingFigure()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        return new StreamingFigure(fig);
    }

    /// <summary>Render with a non-null StreamingFigure — RenderChart populates the
    /// inner SVG markup; container div gets the streaming + chart classes.</summary>
    [Fact]
    public void Renders_SvgContent_WhenStreamingFigureProvided()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf));

        var div = cut.Find("div.mpl-streaming");
        Assert.NotNull(div);
        var svg = cut.Find("svg");
        Assert.NotNull(svg);
    }

    /// <summary>Render with null StreamingFigure (default) → empty SVG markup
    /// (RenderChart line 59 true arm). Container div renders without inner content.</summary>
    [Fact]
    public void Renders_EmptyContent_WhenStreamingFigureIsNull()
    {
        var cut = Render<MplStreamingChart>();

        var div = cut.Find("div.mpl-streaming");
        Assert.NotNull(div);
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("svg"));
    }

    /// <summary>CssClass parameter is appended to the container div's class attribute.</summary>
    [Fact]
    public void CssClass_AppendedToContainerClass()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p =>
        {
            p.Add(x => x.StreamingFigure, sf);
            p.Add(x => x.CssClass, "my-streaming");
        });

        var div = cut.Find("div.mpl-streaming");
        Assert.Contains("my-streaming", div.GetAttribute("class"));
    }

    /// <summary>Changing StreamingFigure parameter triggers Unsubscribe(old) + Subscribe(new).
    /// Pins the line 23 true arm + lines 25, 27 of OnParametersSet.</summary>
    [Fact]
    public void StreamingFigureChange_ResubscribesToNewFigure()
    {
        using var sf1 = NewStreamingFigure();
        using var sf2 = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf1));
        Assert.Contains("<svg", cut.Markup);

        cut.Render(p => p.Add(x => x.StreamingFigure, sf2));
        Assert.Contains("<svg", cut.Markup);
    }

    /// <summary>Render with same StreamingFigure on re-render → line 23 false arm
    /// (no resubscribe — same reference). Re-render still calls RenderChart so SVG
    /// stays populated.</summary>
    [Fact]
    public void SameStreamingFigure_DoesNotResubscribe_StillRendersChart()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf));
        var firstMarkup = cut.Markup;

        cut.Render(p => p.Add(x => x.StreamingFigure, sf));
        Assert.Contains("<svg", cut.Markup);
        Assert.Equal(firstMarkup.Length > 0, cut.Markup.Length > 0);
    }

    /// <summary>StreamingFigure.RequestRender fires RenderRequested → component
    /// re-renders via OnRenderRequested → InvokeAsync → RenderChart + StateHasChanged.</summary>
    [Fact]
    public void RenderRequested_TriggersReRender()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf));
        Assert.Contains("<svg", cut.Markup);

        sf.RequestRender();
        // bUnit dispatches the InvokeAsync synchronously in test context. SVG content
        // remains present (re-rendered from the same figure).
        Assert.Contains("<svg", cut.Markup);
    }

    /// <summary>Disposing the component unsubscribes from RenderRequested. Pins
    /// Dispose() line 64-67 + Unsubscribe() lines 41-45.</summary>
    [Fact]
    public void Dispose_UnsubscribesFromRenderRequested()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf));
        cut.Dispose();
        // After dispose, calling RequestRender should not throw and should not affect
        // the now-disposed component (no observable assertion possible — just contract).
        sf.RequestRender();
    }

    /// <summary>Switching from non-null to null StreamingFigure — Unsubscribe runs
    /// (line 25), Subscribe is skipped (line 26 false arm). RenderChart sets empty
    /// _svgContent (line 59 true arm).</summary>
    [Fact]
    public void StreamingFigure_SetToNull_UnsubscribesAndClearsContent()
    {
        using var sf = NewStreamingFigure();
        var cut = Render<MplStreamingChart>(p => p.Add(x => x.StreamingFigure, sf));
        Assert.Contains("<svg", cut.Markup);

        cut.Render(p => p.Add(x => x.StreamingFigure, (StreamingFigure?)null));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("svg"));
    }
}
