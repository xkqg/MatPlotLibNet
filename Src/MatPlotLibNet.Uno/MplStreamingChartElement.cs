// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Uno;

/// <summary>
/// A Uno Platform element that renders a <see cref="StreamingFigure"/> with
/// automatic re-rendering when streamed data arrives. Subscribes to
/// <see cref="StreamingFigure.RenderRequested"/> and marshals invalidation via
/// <see cref="DispatcherQueue"/>, through the wiring every native streaming control composes (the library's streaming
/// follower), and lets go of the figure when it is unloaded, even if its StreamingFigure never changed.
/// </summary>
public sealed class MplStreamingChartElement : MplChartSurface
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> dependency property. The figure followed is the one bound now; the one
    /// bound before is let go of.</summary>
    public static readonly DependencyProperty StreamingFigureProperty =
        DependencyProperty.Register(nameof(StreamingFigure), typeof(StreamingFigure),
            typeof(MplStreamingChartElement),
            new PropertyMetadata(null, static (d, e) => ((MplStreamingChartElement)d)._follower.Follow(e.NewValue as StreamingFigure)));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> dependency property (the surface's own).</summary>
    public static new readonly DependencyProperty IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    private readonly StreamingFigureFollower _follower;

    /// <summary>Initializes a new streaming chart element.</summary>
    public MplStreamingChartElement() =>
        _follower = new StreamingFigureFollower(work => DispatcherQueue.Post(work), Invalidate);

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <inheritdoc />
    internal override Figure? Shown => StreamingFigure?.Figure;

    /// <inheritdoc />
    /// <remarks>The follower's redraw: the limits follow the data, then the element is drawn.</remarks>
    private protected override void Repaint() => _follower.Redraw();

    /// <inheritdoc />
    internal override void Release()
    {
        _follower.Follow(null);
        base.Release();
    }
}
