// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule for where the bars of an axes stand in a draw: first the stack, then the groups, since a bar that
/// stands on a stack takes no place in a group.</summary>
internal static class BarArrangementExtensions
{
    /// <summary>How wide a group of bars side by side is, in category slots: snug within a bar's resting width (<see cref="BarSeries.BarWidth"/>, matplotlib's).</summary>
    private const double GroupWidth = 0.7;

    extension(Axes axes)
    {
        /// <summary>Stands every bar series of the axes where this draw puts it. In a stacked axes of more than one layer each
        /// bar with no foot of its own stands on the layers added before it (<see cref="StackableExtensions"/>); a bar with a
        /// foot of its own (<see cref="IStackable.StackBaseline"/>) floats on it. Then, in each orientation apart, the bars with
        /// no foot step aside for each other when there is more than one, the way matplotlib's grouped bars do; a vertical bar
        /// is never grouped with a horizontal one. Everything written here is render state, worked out afresh on every call and
        /// cleared where it no longer holds, so a bar that left a stack or a group keeps nothing of it, and nothing the user
        /// set is changed. The stack is worked out only when the bars are stacked, and nothing is allocated for an axes without
        /// bars. The drawing and the marks a legend keeps clear of both arrange the bars here, so they stand where they are
        /// drawn.</summary>
        internal void ArrangeBars()
        {
            var series = axes.Series;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] is not BarSeries bar) continue;
                bar.StackedBottoms = null;
                bar.BarGroupOffset = 0;
                bar.BarGroupWidth = null;
            }

            if (axes.BarMode == BarMode.Stacked)
                StandOnTheStack(series.StackLayers());

            int upright = 0, lying = 0;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] is BarSeries { Bottoms: null } bar)
                {
                    if (!bar.Orientation.ValuesAlongX) upright++;
                    else lying++;
                }
            }

            if (upright < 2 && lying < 2) return;
            int uprightIndex = 0, lyingIndex = 0;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] is not BarSeries { Bottoms: null } bar) continue;
                if (!bar.Orientation.ValuesAlongX) StepAside(bar, uprightIndex++, upright);
                else StepAside(bar, lyingIndex++, lying);
            }
        }
    }

    /// <summary>Stands each bar among <paramref name="layers"/> on the layers before it; one layer is no stack.</summary>
    /// <param name="layers">The layers of the stack (<see cref="StackableExtensions.StackLayers"/>).</param>
    private static void StandOnTheStack(List<IStackable> layers)
    {
        if (layers.Count < 2) return;
        var feet = layers.StackedFeet();
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] is BarSeries bar) bar.StackedBottoms = feet[i];
        }
    }

    /// <summary>Moves a bar to its place in a group of <paramref name="count"/> bars and narrows it to its share of the group;
    /// a group of one leaves the bar as it is.</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="index">Its place in the group, in the order the series were added.</param>
    /// <param name="count">How many bars of its orientation the group holds.</param>
    private static void StepAside(BarSeries bar, int index, int count)
    {
        if (count < 2) return;
        double width = GroupWidth / count;
        bar.BarGroupOffset = (index - (count - 1) / 2.0) * width;
        bar.BarGroupWidth = width;
    }
}
