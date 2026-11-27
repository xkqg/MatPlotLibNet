// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Wpf;

/// <summary>
/// A WPF control that renders a <see cref="Models.Streaming.StreamingFigure"/> and redraws itself when
/// streamed data arrives.
/// </summary>
/// <remarks>
/// <para>Bind the figure once and leave it there. The ordinary <see cref="MplChartControl"/> takes a
/// <see cref="Models.Figure"/> and redraws when that property changes — which, for a dependency property,
/// means when the <em>reference</em> changes. A streaming figure keeps its identity while its data moves, so
/// driving it through that control meant handing it <see langword="null"/> and the same figure again to fake
/// a change. This control listens to the figure instead.</para>
/// <para>The figure raises <see cref="StreamingFigure.RenderRequested"/> on a thread pool thread, so the
/// request is marshalled to the dispatcher before anything touches the control. The wiring is the one every native streaming
/// control composes (the library's streaming follower); this control hands it the dispatcher and its invalidate, and lets go of
/// the figure when it is unloaded: the figure outlives the control, and would otherwise keep invalidating a dead one.</para>
/// </remarks>
public sealed class MplStreamingChartControl : MplChartSurface
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> dependency property. The figure followed is the one bound now; the one
    /// bound before is let go of.</summary>
    public static readonly DependencyProperty StreamingFigureProperty =
        DependencyProperty.Register(nameof(StreamingFigure), typeof(StreamingFigure),
            typeof(MplStreamingChartControl),
            new PropertyMetadata(null, static (d, e) => ((MplStreamingChartControl)d)._follower.Follow(e.NewValue as StreamingFigure)));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> dependency property (the surface's own).</summary>
    public static new readonly DependencyProperty IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    private readonly StreamingFigureFollower _follower;

    /// <summary>Initializes a new streaming chart control.</summary>
    public MplStreamingChartControl() =>
        _follower = new StreamingFigureFollower(work => Dispatcher.BeginInvoke(work), InvalidateVisual);

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <inheritdoc />
    internal override Figure? Shown => StreamingFigure?.Figure;

    /// <inheritdoc />
    /// <remarks>The follower's redraw: the limits follow the data, then the control is drawn.</remarks>
    private protected override void Repaint() => _follower.Redraw();

    /// <inheritdoc />
    internal override void Release()
    {
        _follower.Follow(null);
        base.Release();
    }
}
