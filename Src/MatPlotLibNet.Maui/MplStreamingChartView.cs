// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>The figure raises this on a thread-pool thread. The axis limits are written on the main thread, where the
    /// view draws and where a gesture changes them, so there is one writer and a frame never draws half-written limits.</summary>
    [ExcludeFromCodeCoverage(Justification = "Runs on MAUI's main thread, which a unit test has no loop for; what it does there is held through the figure and the view it calls.")]
    private void OnRenderRequested() =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _subscribedFigure?.ApplyAxisScaling();
            Invalidate();
        });

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
