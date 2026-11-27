// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Interaction;

/// <summary>What a native streaming control does with the <see cref="StreamingFigure"/> it is given, in one place: listen to
/// the figure, let go of the one it is taken off of, set the axis limits from the data before the first draw, and when the
/// figure asks to be drawn (on a pool thread) move to the control's thread, set the limits, then draw. A gesture sets them too:
/// the control gives its <see cref="InteractionHost"/> the follower's <see cref="Redraw"/> as its repaint, so an axes the reader
/// moved stays where they put it and Home hands it back to its scale mode at once.
/// <para>The WPF, Avalonia, Uno and MAUI controls derive from four different base classes and differ only in how they reach
/// their thread and how they draw, so each composes one follower and hands it those two. The limits are written on the UI
/// thread only, where the control draws and where a gesture changes them: one writer, and a paint never sees half-written
/// limits. Nothing here is thread-safe beyond that: the control calls it on its UI thread.</para></summary>
internal sealed class StreamingFigureFollower
{
    private readonly UiThreadDispatch _dispatch;
    private readonly Action _draw;

    /// <summary>Creates the follower of one control.</summary>
    /// <param name="dispatch">How the control runs work on its UI thread.</param>
    /// <param name="draw">How the control asks to be drawn again (an invalidate).</param>
    internal StreamingFigureFollower(UiThreadDispatch dispatch, Action draw)
    {
        _dispatch = dispatch;
        _draw = draw;
    }

    /// <summary>The figure followed now, or <see langword="null"/>.</summary>
    internal StreamingFigure? Figure { get; private set; }

    /// <summary>Follows <paramref name="figure"/> and lets go of the one followed before. <see langword="null"/> only lets
    /// go, which a control does when it is torn down: the figure outlives the control, and would otherwise keep asking a dead
    /// one to draw.</summary>
    /// <param name="figure">The figure the control shows now, or <see langword="null"/>.</param>
    internal void Follow(StreamingFigure? figure)
    {
        if (Figure is { } old)
        {
            old.RenderRequested -= OnRenderRequested;
            Figure = null;
        }

        if (figure is null) return;

        Figure = figure;
        figure.RenderRequested += OnRenderRequested;
        // The first draw shows the data that is there already, within the limits its scale modes ask for.
        figure.ApplyAxisScaling();
    }

    /// <summary>The figure asked to be drawn (data arrived), on a pool thread: the rest happens on the control's thread.</summary>
    private void OnRenderRequested() => _dispatch(Redraw);

    /// <summary>On the UI thread, after data arrived or a gesture changed the figure (the control gives its host this as its
    /// repaint): the limits follow the data that is there now (an axes the reader moved stays where they put it), then the control
    /// is drawn. Before the draw, not in it: a paint writes nothing into the figure. With no figure followed (a request still
    /// waiting when the figure was let go of, a gesture after it) it only draws.</summary>
    internal void Redraw()
    {
        Figure?.ApplyAxisScaling();
        _draw();
    }
}
