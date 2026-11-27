// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The rubber band of a drag a held key opens: a left press on an axes with <c>key</c> held opens it, it follows the pointer,
/// and the release hands it back as one <see cref="Drag"/>, unless the drag is shorter than <c>floor</c>. The zoom rectangle (Ctrl),
/// the span (Alt) and the brush (Shift) each hold one; the key and the floor are what they differ by.</summary>
/// <param name="key">The key that must be held for a press to open the drag.</param>
/// <param name="floor">The least a drag must cover to count.</param>
internal sealed class DragTrack(ModifierKeys key, DragFloor floor)
{
    /// <summary>The data point under the press that opened the drag.</summary>
    private Point _startData;

    /// <summary>Whether a drag is in progress.</summary>
    internal bool IsActive { get; private set; }

    /// <summary>The axes the drag started in.</summary>
    internal int AxesIndex { get; private set; }

    /// <summary>The pixel the drag started at.</summary>
    internal Point Start { get; private set; }

    /// <summary>The pixel the pointer is at now.</summary>
    internal Point Current { get; private set; }

    /// <summary>What the drag shows while it is in progress, made by <paramref name="make"/> from the pixel it started at, the pixel the
    /// pointer is at now and its axes: the state the zoom rectangle, the span and the brush each hand to the control that draws the band.</summary>
    /// <typeparam name="TState">The state a modifier shows.</typeparam>
    /// <param name="make">Makes the state from the start pixel's X and Y, the current pixel's X and Y, and the axes index.</param>
    /// <returns>The state, or <see langword="null"/> when no drag is in progress.</returns>
    internal TState? Showing<TState>(Func<double, double, double, double, int, TState> make) where TState : struct =>
        IsActive ? make(Start.X, Start.Y, Current.X, Current.Y, AxesIndex) : null;

    /// <summary>Whether <paramref name="press"/> opens the drag: <c>key</c> is held and it is a left press on an axes.</summary>
    /// <param name="layout">The layout of the rendered figure.</param>
    /// <param name="press">The press.</param>
    /// <returns><see langword="true"/> when the press opens the drag.</returns>
    internal bool Opens(IChartLayout layout, PointerInputArgs press) => (press.Modifiers & key) != 0 && layout.IsLeftPressOnAxes(press);

    /// <summary>Starts the drag at <paramref name="press"/>; a press no data lies under starts nothing.</summary>
    /// <param name="layout">The layout of the rendered figure.</param>
    /// <param name="press">The press.</param>
    internal void Begin(IChartLayout layout, PointerInputArgs press)
    {
        if (layout.PressAt(press.X, press.Y) is not { } at) return;

        IsActive = true;
        AxesIndex = at.AxesIndex;
        Start = Current = new Point(press.X, press.Y);
        _startData = new Point(at.Data.DataX, at.Data.DataY);
    }

    /// <summary>Follows the pointer while a drag is in progress.</summary>
    /// <param name="move">The pointer's move.</param>
    internal void MoveTo(PointerInputArgs move)
    {
        if (IsActive) Current = new Point(move.X, move.Y);
    }

    /// <summary>Ends the drag at <paramref name="release"/>.</summary>
    /// <param name="layout">The layout of the rendered figure.</param>
    /// <param name="release">The release.</param>
    /// <returns>The drag, or <see langword="null"/> when none was in progress, no data lies under the release, or the drag is
    /// shorter than its floor.</returns>
    internal Drag? End(IChartLayout layout, PointerInputArgs release)
    {
        if (!IsActive) return null;
        IsActive = false;

        if (layout.PixelToData(release.X, release.Y, AxesIndex) is not { } end) return null;

        var drag = new Drag(AxesIndex, Start, new Point(release.X, release.Y), _startData, new Point(end.DataX, end.DataY));
        return floor.Counts(drag) ? drag : null;
    }
}

/// <summary>A finished drag: the axes it was made in, the pixels it started and ended at, and the data under each.</summary>
/// <param name="AxesIndex">The axes the drag was made in.</param>
/// <param name="StartPixel">The pixel it started at.</param>
/// <param name="EndPixel">The pixel it ended at.</param>
/// <param name="StartData">The data point under its start.</param>
/// <param name="EndData">The data point under its end.</param>
internal readonly record struct Drag(int AxesIndex, Point StartPixel, Point EndPixel, Point StartData, Point EndData)
{
    /// <summary>The box the drag spans in data, low ends first, whichever way it was drawn.</summary>
    internal DataRange DataBox => DataRange.Between(StartData, EndData);
}

/// <summary>The least a drag must cover to count, in pixels along each axis: a drag shorter than <see cref="X"/> across and shorter
/// than <see cref="Y"/> down is a click that wandered, not a selection.</summary>
/// <param name="X">The pixels across below which a drag may not count.</param>
/// <param name="Y">The pixels down below which a drag may not count.</param>
internal readonly record struct DragFloor(double X, double Y)
{
    /// <summary>The pixels a drag must cover along an axis that has a floor.</summary>
    private const double Pixels = 2;

    /// <summary>A box counts once it covers <see cref="Pixels"/> along either axis: the zoom rectangle.</summary>
    internal static DragFloor Box { get; } = new(Pixels, Pixels);

    /// <summary>A range along X counts once it covers <see cref="Pixels"/> across, whatever it covers down: the span.</summary>
    internal static DragFloor AlongX { get; } = new(Pixels, double.PositiveInfinity);

    /// <summary>Every drag counts: the brush.</summary>
    internal static DragFloor None { get; } = new(0, 0);

    /// <summary>Whether <paramref name="drag"/> covers enough to count.</summary>
    /// <param name="drag">The drag.</param>
    /// <returns><see langword="false"/> when it is shorter than <see cref="X"/> across and shorter than <see cref="Y"/> down.</returns>
    internal bool Counts(Drag drag) =>
        !(Math.Abs(drag.EndPixel.X - drag.StartPixel.X) < X && Math.Abs(drag.EndPixel.Y - drag.StartPixel.Y) < Y);
}
