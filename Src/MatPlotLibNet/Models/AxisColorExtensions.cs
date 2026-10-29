// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>The edge of the axes a spine stands on. It keys the one rule that colours an axis with the spine on its
/// edge (<see cref="AxisColorExtensions"/>): the left Y axis stands on <see cref="Left"/>, the first right Y axis on
/// <see cref="Right"/>, the bottom X axis on <see cref="Bottom"/>, the secondary X axis on <see cref="Top"/>.</summary>
internal enum SpineEdge
{
    /// <summary>The top edge: the secondary X axis.</summary>
    Top,

    /// <summary>The bottom edge: the X axis.</summary>
    Bottom,

    /// <summary>The left edge: the primary Y axis.</summary>
    Left,

    /// <summary>The right edge: the first right Y axis.</summary>
    Right,
}

/// <summary>The one rule that colours an axis: its tick marks, its tick labels and its title, and, when the axis has a
/// spine, the spine on its edge. Every colour verb (<c>SetYAxisColor</c> and <c>SetXAxisColor</c> on every builder)
/// calls <see cref="ApplyAxisColor(Axes, Axis, SpineEdge?, Color)"/>; none keeps a copy of the body.</summary>
internal static class AxisColorExtensions
{
    extension(Axis axis)
    {
        /// <summary>Colours the tick marks, the tick labels and the title of this axis with
        /// <paramref name="color"/>, keeping every other tick and label setting it already carries; a missing
        /// <see cref="Axis.LabelStyle"/> is created.</summary>
        /// <param name="color">The colour all three take.</param>
        internal void ApplyAxisColor(Color color)
        {
            axis.MajorTicks = axis.MajorTicks with { Color = color, LabelColor = color };
            axis.LabelStyle = (axis.LabelStyle ?? new TextStyle()) with { Color = color };
        }
    }

    extension(Axes axes)
    {
        /// <summary>Colours <paramref name="axis"/> (its tick marks, tick labels and title) and, when the axis stands on
        /// a spine, that spine, keeping everything else the axis and the spine already carry.</summary>
        /// <param name="axis">The axis to colour; one of this axes' own.</param>
        /// <param name="spine">The edge whose spine takes the colour too, or <see langword="null"/> for an axis that
        /// has no spine of its own: a Y axis past the first on its side draws ticks, tick labels and a title only.</param>
        /// <param name="color">The colour the axis, and the spine when there is one, take.</param>
        internal void ApplyAxisColor(Axis axis, SpineEdge? spine, Color color)
        {
            axis.ApplyAxisColor(color);
            if (spine is { } edge) axes.Spines = axes.Spines.WithEdgeColor(edge, color);
        }
    }

    extension(SpinesConfig spines)
    {
        /// <summary>These spines with the one on <paramref name="edge"/> stroked in <paramref name="color"/>.</summary>
        /// <param name="edge">Which spine.</param>
        /// <param name="color">Its colour.</param>
        internal SpinesConfig WithEdgeColor(SpineEdge edge, Color color) => edge switch
        {
            SpineEdge.Top => spines with { Top = spines.Top with { Color = color } },
            SpineEdge.Bottom => spines with { Bottom = spines.Bottom with { Color = color } },
            SpineEdge.Left => spines with { Left = spines.Left with { Color = color } },
            _ => spines with { Right = spines.Right with { Color = color } },
        };
    }
}
