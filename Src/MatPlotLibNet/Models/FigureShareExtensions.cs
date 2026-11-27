// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Joins the subplots of a figure that share a scale: the one walk from the key a subplot asked for to the subplot of that
/// key, which the builder and the reader of a document both end in, and the join by the place a document names for a partner that
/// has no key.</summary>
internal static class FigureShareExtensions
{
    extension(Figure figure)
    {
        /// <summary>Joins every subplot that asked to share a scale (<see cref="Axes.ShareXKey"/>, <see cref="Axes.ShareYKey"/>) to the
        /// subplot of that key (<see cref="Axes.Key"/>; of two subplots with one key, the later one). A key no subplot has joins
        /// nothing.</summary>
        internal void ShareByKeys()
        {
            Dictionary<string, Axes>? keyed = null;
            foreach (var axes in figure.SubPlots)
            {
                if (axes.Key is { } key) (keyed ??= [])[key] = axes;
            }

            if (keyed is null) return;
            foreach (var axes in figure.SubPlots)
            {
                if (axes.ShareXKey is { } x && keyed.TryGetValue(x, out var xTarget)) axes.ShareXWith = xTarget;
                if (axes.ShareYKey is { } y && keyed.TryGetValue(y, out var yTarget)) axes.ShareYWith = yTarget;
            }
        }

        /// <summary>Joins <paramref name="axes"/> to the subplots at the places a document names for the partners that have no key to
        /// name them by (<c>Figure.AddSubPlot(..., sharex, sharey)</c>, matplotlib's <c>add_subplot(sharex=ax)</c>), once every subplot
        /// stands; a place no subplot holds joins nothing.</summary>
        /// <param name="axes">The subplot that shares.</param>
        /// <param name="xPlace">The place among the figure's subplots of the one whose X scale it shares; <see langword="null"/> for none.</param>
        /// <param name="yPlace">The place of the one whose Y scale it shares; <see langword="null"/> for none.</param>
        internal void ShareByPlace(Axes axes, int? xPlace, int? yPlace)
        {
            if (figure.SubplotAt(xPlace) is { } x) axes.ShareXWith = x;
            if (figure.SubplotAt(yPlace) is { } y) axes.ShareYWith = y;
        }

        private Axes? SubplotAt(int? place) => place is int i && (uint)i < (uint)figure.SubPlots.Count ? figure.SubPlots[i] : null;
    }
}
