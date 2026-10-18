// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Root of the stacked event hierarchy driving server-authoritative interactive charts.
/// Every event is self-applying: <see cref="ApplyTo"/> mutates the figure in place. Adding a new
/// interaction type means adding a new subclass — no switch, no visitor, no registry update.</summary>
public abstract record FigureInteractionEvent(string ChartId, int AxesIndex)
{
    /// <summary>Mutates <paramref name="figure"/> to reflect this interaction.</summary>
    public abstract void ApplyTo(Figure figure);

    /// <summary>Resolves the target <see cref="Axes"/> for this event. Shared helper for subclasses.</summary>
    protected Axes TargetAxes(Figure figure) => figure.SubPlots[AxesIndex];

    /// <summary>The target axes together with every axes linked to it by <c>ShareX</c>, in either direction and
    /// through any number of links. They show one stretch of the X axis, so a pan or zoom of one is a pan or zoom
    /// of all: before this, <c>ShareX</c> shared only the automatic scale and the lanes of a stacked timeline
    /// drifted apart at the first scroll.</summary>
    protected IReadOnlyList<Axes> XLinkedAxes(Figure figure)
    {
        var group = new List<Axes> { TargetAxes(figure) };
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
