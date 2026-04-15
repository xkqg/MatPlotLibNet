// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Fluent options bag for per-chart handler registration introduced in v1.2.2.
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
/// <para>This options type is the only public surface a user needs to learn to react to
/// browser events in .NET code. No handler registry, no interface to implement, no reflection —
/// just two callbacks.</para>
/// </remarks>
public sealed class ChartSessionOptions
{
    internal Func<BrushSelectEvent, ValueTask>? BrushSelectHandler { get; private set; }
    internal Func<HoverEvent, ValueTask<string?>>? HoverHandler { get; private set; }

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
}
