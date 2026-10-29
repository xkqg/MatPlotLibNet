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

    /// <summary>The target axes together with every axes linked to it by <c>ShareX</c>, in either direction and
    /// through any number of links. They show one stretch of the X axis, so a pan or zoom of one is a pan or zoom
    /// of all: before this, <c>ShareX</c> shared only the automatic scale and the lanes of a stacked timeline
    /// drifted apart at the first scroll. Empty when the event names no subplot.</summary>
    protected IReadOnlyList<Axes> XLinkedAxes(Figure figure)
    {
        if (!TryTargetAxes(figure, out var target)) return [];

        var group = new List<Axes> { target };
        for (int i = 0; i < group.Count; i++)
        {
            foreach (var other in figure.SubPlots)
            {
                if (!group.Contains(other) && (other.ShareXWith == group[i] || group[i].ShareXWith == other))
                {
                    group.Add(other);
                }
            }
        }

        return group;
    }
}
