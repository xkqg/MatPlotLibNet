// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>The view each axes of a figure had before the reader first moved it: the limits it was declared with
/// (<see langword="null"/> when the axes scales itself), the limits of every Y axis stacked on either side, and the
/// camera of a 3-D axes. Home returns to it.
/// <para>A browser, a native control and a server each used to work out "the original view" in their own way, and each
/// worked it out from a picture that had already been zoomed: the first repaint after a zoom became the new original.
/// The figure is the only thing that outlives every repaint, so it keeps the view, and an interaction that changes a
/// view (<see cref="Interaction.AxisRangeEvent"/>, <see cref="Interaction.PanEvent"/>,
/// <see cref="Interaction.Rotate3DEvent"/>) asks it to remember the axes it is about to move. Remembering is lazy and
/// once: the first change stores, later ones find the axes already stored, so the second zoom of a journey does not
/// become the new start.</para>
/// <para>It is state of the session, not of the drawing: it is not part of the JSON contract and no setting can change
/// it, so a figure read back from JSON starts with nothing remembered.</para></summary>
internal sealed class FigureHome
{
    private readonly Dictionary<Axes, AxesView> _views = [];

    /// <summary>Stores the declared view of <paramref name="axes"/> unless it is already stored.</summary>
    /// <param name="axes">The axes about to be moved.</param>
    internal void Remember(Axes axes)
    {
        if (!_views.ContainsKey(axes))
        {
            _views[axes] = AxesView.Of(axes);
        }
    }

    /// <summary>Whether the first view of <paramref name="axes"/> is stored, so something that moves the axes by itself
    /// (a stream that follows its newest data) leaves it where the reader put it.</summary>
    /// <param name="axes">The axes asked about.</param>
    internal bool Remembers(Axes axes) => _views.ContainsKey(axes);

    /// <summary>Puts the stored view back on <paramref name="axes"/> and forgets it.</summary>
    /// <param name="axes">The axes to return.</param>
    /// <returns><see langword="false"/> when no view of it is stored: the reader never moved it.</returns>
    internal bool GoHome(Axes axes)
    {
        if (!_views.Remove(axes, out var view))
        {
            return false;
        }

        view.RestoreOn(axes);
        return true;
    }

    /// <summary>The limits of one axis; <see langword="null"/> is a limit the axis works out for itself.</summary>
    private readonly record struct AxisLimits(double? Min, double? Max)
    {
        internal static AxisLimits Of(Axis axis) => new(axis.Min, axis.Max);

        internal void RestoreOn(Axis axis)
        {
            axis.Min = Min;
            axis.Max = Max;
        }
    }

    /// <summary>Everything a gesture moves on one axes.</summary>
    private readonly record struct AxesView(
        AxisLimits X,
        AxisLimits[] Left,
        AxisLimits[] Right,
        double Azimuth,
        double Elevation,
        double? CameraDistance)
    {
        internal static AxesView Of(Axes axes) => new(
            AxisLimits.Of(axes.XAxis),
            [.. axes.YAxes.Left.Select(slot => AxisLimits.Of(slot.Axis))],
            [.. axes.YAxes.Right.Select(slot => AxisLimits.Of(slot.Axis))],
            axes.Azimuth,
            axes.Elevation,
            axes.CameraDistance);

        internal void RestoreOn(Axes axes)
        {
            X.RestoreOn(axes.XAxis);
            Restore(Left, axes.YAxes.Left);
            Restore(Right, axes.YAxes.Right);
            axes.Azimuth = Azimuth;
            axes.Elevation = Elevation;
            axes.CameraDistance = CameraDistance;
            axes.Projection = null;   // the camera moved, the projection is built again
        }

        /// <summary>Slots that were added after the view was stored have no stored limits and are left as they are.</summary>
        private static void Restore(AxisLimits[] stored, YAxisStack stack)
        {
            for (int i = 0; i < stored.Length && i < stack.Count; i++)
            {
                stored[i].RestoreOn(stack[i].Axis);
            }
        }
    }
}
