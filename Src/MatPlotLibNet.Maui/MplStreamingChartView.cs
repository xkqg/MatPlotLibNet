// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Maui;

/// <summary>A MAUI <see cref="GraphicsView"/> that renders a <see cref="StreamingFigure"/> with
/// automatic re-rendering when streamed data arrives. Subscribes to
/// <see cref="StreamingFigure.RenderRequested"/> and marshals invalidation via
/// <see cref="MainThread"/>. Unsubscribes when the <see cref="StreamingFigure"/> changes and
/// when the view's platform handler is removed, so the view never outlives its subscription.</summary>
public class MplStreamingChartView : MplChartView
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> bindable property.</summary>
    public static readonly BindableProperty StreamingFigureProperty =
        BindableProperty.Create(
            nameof(StreamingFigure),
            typeof(StreamingFigure),
            typeof(MplStreamingChartView),
            defaultValue: null,
            propertyChanged: OnStreamingFigureChanged);

    private StreamingFigure? _subscribedFigure;

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    private static void OnStreamingFigureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (MplStreamingChartView)bindable;
        view.Unsubscribe();

        if (newValue is StreamingFigure sf)
        {
            view.Subscribe(sf);
        }
    }

    private void Subscribe(StreamingFigure sf)
    {
        _subscribedFigure = sf;
        Figure = sf.Figure;
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

    private void OnRenderRequested()
    {
        _subscribedFigure?.ApplyAxisScaling();
        MainThread.BeginInvokeOnMainThread(Invalidate);
    }

    /// <inheritdoc />
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler is null)
        {
            Unsubscribe();
        }
    }
}
