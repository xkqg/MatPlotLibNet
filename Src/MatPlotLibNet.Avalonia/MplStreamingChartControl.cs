// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Threading;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia control that renders a <see cref="StreamingFigure"/> with automatic
/// re-rendering when streamed data arrives. Subscribes to <see cref="StreamingFigure.RenderRequested"/>
/// and marshals invalidation to the UI thread via <see cref="Dispatcher"/>, through the wiring every native streaming control
/// composes (the library's streaming follower), and lets go of the figure when it leaves the visual tree.
/// </summary>
public sealed class MplStreamingChartControl : MplChartSurface
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> styled property.</summary>
    public static readonly StyledProperty<StreamingFigure?> StreamingFigureProperty =
        AvaloniaProperty.Register<MplStreamingChartControl, StreamingFigure?>(nameof(StreamingFigure));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> styled property (the surface's own).</summary>
    public static new readonly StyledProperty<bool> IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    private readonly StreamingFigureFollower _follower;

    static MplStreamingChartControl() => AffectsRender<MplStreamingChartControl>(StreamingFigureProperty);

    /// <summary>Initializes a new streaming chart control.</summary>
    public MplStreamingChartControl() => _follower = new StreamingFigureFollower(PostAtRenderPriority, InvalidateVisual);

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StreamingFigureProperty)
            _follower.Follow(change.NewValue as StreamingFigure);
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
