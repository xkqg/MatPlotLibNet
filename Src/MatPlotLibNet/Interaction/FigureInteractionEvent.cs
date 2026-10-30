// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Root of the stacked event hierarchy driving server-authoritative interactive charts.
/// Every event is self-applying: <see cref="ApplyTo"/> mutates the figure in place. Adding a new
/// interaction type means adding a new subclass — no switch, no visitor, no registry update.</summary>
/// <remarks>An event comes from a client over the wire, so the subplot it names may not exist any more (the server's figure
/// changed after the client drew) or never did (a crafted index). Every event the library ships drops itself then: it
/// changes nothing and does not throw, because one stale event must not end the loop that applies the events behind it. An
/// event written outside the library gets the same answer from <see cref="TryTargetAxes"/>.</remarks>
public abstract record FigureInteractionEvent(string ChartId, int AxesIndex)
{
    /// <summary>The identity of the viewer who did this: for a browser, the connection the server received it on. The hub
    /// writes it over whatever the payload carried (<see cref="StampedBy"/>), so a client cannot speak for another
    /// viewer; <see langword="null"/> for an event that did not come over a connection, as on a native control.</summary>
    public string? ViewerId { get; init; }

    /// <summary>A short name for that viewer ("Viewer 2"), in the order the viewers joined the chart; written together with
    /// <see cref="ViewerId"/>.</summary>
    public string? ViewerName { get; init; }

    /// <summary>Whether this event moves the view the viewers of a chart share (a zoom, a pan, a reset or a rotation),
    /// as opposed to one that changes what the chart shows (a legend toggle, a drawing) or only observes. A server
    /// names the author of the last event that did, because the others are looking at the view it left.</summary>
    internal virtual bool ChangesView => false;

    /// <summary>The viewer of this event as an <see cref="IChartEventHandler"/> is told of them.</summary>
    internal ChartEventContext ViewerContext => new(ViewerId, ViewerName);

    /// <summary>A copy of this event with the viewer set, over any viewer the event already named. The server calls it on
    /// every event it receives from a connection, because the identity of the sender is the connection, not a field the
    /// sender fills in.</summary>
    /// <param name="viewerId">The connection the event was received on.</param>
    /// <param name="viewerName">The name the chart gave that connection.</param>
    /// <returns>The same kind of event, stamped.</returns>
    internal FigureInteractionEvent StampedBy(string viewerId, string? viewerName) =>
        this with { ViewerId = viewerId, ViewerName = viewerName };

    /// <summary>Mutates <paramref name="figure"/> to reflect this interaction.</summary>
    public abstract void ApplyTo(Figure figure);

    /// <summary>Applies this event and says how it ended: <see langword="true"/> when it was applied,
    /// <see langword="false"/> when it was dropped because it names a subplot the figure does not have. The caller that
    /// drains events counts the second kind, so a dropped event leaves a trace. An event that overrides only
    /// <see cref="ApplyTo"/> is applied by it and reports <see langword="true"/>; the events of the library override this
    /// as well, and their <see cref="ApplyTo"/> is this method with the answer left out.</summary>
    /// <param name="figure">The figure the event is applied to.</param>
    /// <returns><see langword="false"/> when the event named a subplot <paramref name="figure"/> lacks and changed nothing.</returns>
    internal virtual bool TryApplyTo(Figure figure)
    {
        ApplyTo(figure);
        return true;
    }

    /// <summary>Resolves the target <see cref="Axes"/> for this event. Shared helper for subclasses.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="AxesIndex"/> names no subplot of <paramref name="figure"/>;
    /// <see cref="TryTargetAxes"/> answers with <see langword="false"/> instead.</exception>
    protected Axes TargetAxes(Figure figure) => figure.SubPlots[AxesIndex];

    /// <summary>Resolves the target <see cref="Axes"/> for this event without throwing: <see langword="false"/> when
    /// <see cref="AxesIndex"/> is below 0 or past the last subplot of <paramref name="figure"/>, the event then being
    /// dropped by whoever asked.</summary>
    /// <param name="figure">The figure the event is applied to.</param>
    /// <param name="axes">The subplot at <see cref="AxesIndex"/>, or <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when <paramref name="axes"/> was found.</returns>
    protected bool TryTargetAxes(Figure figure, [NotNullWhen(true)] out Axes? axes)
    {
        bool found = (uint)AxesIndex < (uint)figure.SubPlots.Count;
        axes = found ? figure.SubPlots[AxesIndex] : null;
        return found;
    }

    /// <summary>Resolves the target <see cref="Axes"/> of an event that moves the view (a zoom, a pan, a reset), without
    /// throwing: <see langword="false"/> when there is no such subplot, and also when the axes has a fixed frame
    /// (<see cref="Axes.HasFixedFrame"/>, the triangle of a ternary diagram), where there is nothing to move. The ONE place
    /// that rule is applied, so a modifier, a server and a script all get the same answer and none of them asks the coordinate
    /// system for itself.</summary>
    /// <param name="figure">The figure the event is applied to.</param>
    /// <param name="axes">The subplot at <see cref="AxesIndex"/>, or <see langword="null"/> when there is none or its frame is fixed.</param>
    /// <returns><see langword="true"/> when <paramref name="axes"/> was found and its view can move.</returns>
    protected bool TryTargetMovableAxes(Figure figure, [NotNullWhen(true)] out Axes? axes)
    {
        if (!TryTargetAxes(figure, out axes)) return false;
        if (!axes.HasFixedFrame) return true;

        axes = null;
        return false;
    }

    /// <summary>The target axes together with every axes linked to it by <c>ShareX</c>, in either direction and
    /// through any number of links. They show one stretch of the X axis, so a pan or zoom of one is a pan or zoom
    /// of all: before this, <c>ShareX</c> shared only the automatic scale and the lanes of a stacked timeline
    /// drifted apart at the first scroll. An axes whose frame is fixed (<see cref="Axes.HasFixedFrame"/>) is not carried
    /// along. Empty when the event names no subplot.</summary>
    protected IReadOnlyList<Axes> XLinkedAxes(Figure figure)
    {
        if (!TryTargetAxes(figure, out var target)) return [];

        var group = new List<Axes> { target };
        for (int i = 0; i < group.Count; i++)
        {
            foreach (var other in figure.SubPlots)
            {
                if (!other.HasFixedFrame && !group.Contains(other) && (other.ShareXWith == group[i] || group[i].ShareXWith == other))
                {
                    group.Add(other);
                }
            }
        }

        return group;
    }
}
