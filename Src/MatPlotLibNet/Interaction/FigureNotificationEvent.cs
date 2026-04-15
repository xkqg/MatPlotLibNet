// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Intermediate tier shared by every event that OBSERVES the figure without mutating
/// it — brush-select, hover, and future notification-only events. Sibling tier to
/// <see cref="AxisRangeEvent"/>, which covers mutation events for axis limits.</summary>
/// <remarks>
/// <para>v1.2.0 assumed every <see cref="FigureInteractionEvent"/> mutates the figure:
/// <c>ZoomEvent</c>, <c>PanEvent</c>, <c>ResetEvent</c>, <c>LegendToggleEvent</c> all rewrite
/// axis limits or series visibility and trigger a re-render + publish. v1.2.2 introduces the
/// first non-mutating category: the server observes the event, routes it to a user-registered
/// handler, and either fires a callback (<see cref="BrushSelectEvent"/>) or returns a
/// caller-only response (<see cref="HoverEvent"/>). The figure is never re-rendered.</para>
///
/// <para><see cref="ApplyTo"/> is <c>sealed override</c> here — any concrete subclass inherits
/// the no-op automatically and cannot accidentally mutate. This is the structural guarantee
/// that makes "notification events don't touch the figure" a compile-time property rather
/// than a runtime convention.</para>
///
/// <para>Routing discrimination: <c>ChartSession.DrainAsync</c> does one <c>is</c> test against
/// this type to decide whether to skip the publish step. No switch chain, no visitor.</para>
/// </remarks>
public abstract record FigureNotificationEvent(string ChartId, int AxesIndex)
    : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <summary>Notification events never rewrite figure state. Sealed here so concrete
    /// subclasses cannot override — the semantic guarantee of this tier.</summary>
    public sealed override void ApplyTo(Figure figure)
    {
        // Intentionally empty — notification events observe, they do not mutate.
    }
}
