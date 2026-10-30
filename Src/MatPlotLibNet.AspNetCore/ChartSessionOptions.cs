// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Fluent options bag for per-chart handler registration.
/// Passed to <see cref="FigureRegistry.Register(string, Models.Figure, System.Action{ChartSessionOptions})"/>
/// to attach callbacks for non-mutating notification events — brush-select (fire-and-forget)
/// and hover (request-response via caller publisher).</summary>
/// <remarks>
/// <para>Each chart can register its own handlers; the same user application can host different
/// figures with different tooltip formatters or different selection-reaction logic. Handlers
/// are stored by reference on the <c>ChartSession</c> and fired synchronously from the
/// session's drain task — same thread-model guarantee as mutation events (one session =
/// one reader, no locking needed).</para>
///
/// <para>This options type is the public surface a user needs to react to browser events in .NET code. Two callbacks
/// (a brush, a hover) are the short way; <see cref="Handle"/> takes an <see cref="Interaction.IChartEventHandler"/> that
/// receives every interaction event (a zoom, a pan, a legend toggle, a reset, the rows a brush covers), with the viewer it came
/// from, so one class can react to a chart wherever it is shown. No handler registry and no reflection.</para>
/// </remarks>
public sealed class ChartSessionOptions
{
    internal Func<BrushSelectEvent, ValueTask>? BrushSelectHandler { get; private set; }
    internal Func<HoverEvent, ValueTask<string?>>? HoverHandler { get; private set; }
    internal Func<DataCursorEvent, ValueTask>? DataCursorHandler { get; private set; }
    internal IChartEventHandler? EventHandler { get; private set; }

    /// <summary>Register one handler for everything a viewer does to the chart: every event, whichever kind, once, after
    /// the callback written for its kind (<see cref="OnBrushSelect"/>, <see cref="OnHover"/>, <see cref="OnDataCursor"/>)
    /// has run. Unlike those three it also sees the events that change the figure: a zoom, a pan, a legend toggle, a
    /// reset. It runs inline, so the next event waits for it; a handler that throws costs only its event, which is counted
    /// in the session's fault count.</summary>
    public ChartSessionOptions Handle(IChartEventHandler handler)
    {
        EventHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>Register a callback invoked when the user Shift+drags a rubber-band rectangle
    /// over the plot area. The callback receives the data-space rectangle and runs fire-and-forget
    /// — the figure is not re-rendered. Use this to log selections, filter a dataset, trigger a
    /// downstream query, or any other observational action.</summary>
    public ChartSessionOptions OnBrushSelect(Func<BrushSelectEvent, ValueTask> handler)
    {
        BrushSelectHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>Register a callback invoked when the user hovers over the plot area. The
    /// callback receives the data-space point and returns an HTML fragment (or <c>null</c> for
    /// "no tooltip"); the fragment is delivered back to the originating client only via
    /// <see cref="ICallerPublisher"/>, not broadcast to the group. Use this to compute tooltip
    /// content from live server state — authenticated lookups, per-user data, async queries.</summary>
    public ChartSessionOptions OnHover(Func<HoverEvent, ValueTask<string?>> handler)
    {
        HoverHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>Register a callback invoked when the reader clicks a data point. The callback receives the
    /// point they clicked — the series it belongs to, where on the axes it sits, and where on screen — and runs
    /// fire-and-forget: the figure is not re-rendered, because a click is a question about the data rather than
    /// a change to it. Use it to open a detail panel, select a row beside the chart, or start a query.</summary>
    public ChartSessionOptions OnDataCursor(Func<DataCursorEvent, ValueTask> handler)
    {
        DataCursorHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }
}
