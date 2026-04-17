// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia <see cref="Control"/> that renders a <see cref="StreamingFigure"/> with automatic
/// re-rendering when streamed data arrives. Subscribes to <see cref="StreamingFigure.RenderRequested"/>
/// and marshals invalidation to the UI thread via <see cref="Dispatcher"/>.
/// </summary>
public sealed class MplStreamingChartControl : Control
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> styled property.</summary>
    public static readonly StyledProperty<StreamingFigure?> StreamingFigureProperty =
        AvaloniaProperty.Register<MplStreamingChartControl, StreamingFigure?>(nameof(StreamingFigure));

    private StreamingFigure? _subscribedFigure;

    static MplStreamingChartControl()
    {
        AffectsRender<MplStreamingChartControl>(StreamingFigureProperty);
    }

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
        {
            Unsubscribe();
            if (change.NewValue is StreamingFigure sf)
                Subscribe(sf);
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var sf = StreamingFigure;
        if (sf is null) return;
        sf.ApplyAxisScaling();
        context.Custom(new MplChartDrawOperation(new Rect(Bounds.Size), sf.Figure, null));
    }

    private void Subscribe(StreamingFigure sf)
    {
        _subscribedFigure = sf;
        sf.RenderRequested += OnRenderRequested;
    }

    private void Unsubscribe()
    {
        if (_subscribedFigure is not null)
        {
            _subscribedFigure.RenderRequested -= OnRenderRequested;
            _subscribedFigure = null;
        }
    }

    private void OnRenderRequested() =>
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        base.OnDetachedFromVisualTree(e);
    }
}
