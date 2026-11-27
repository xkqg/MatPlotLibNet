// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Maui;

/// <summary>A MAUI <see cref="GraphicsView"/> that renders a <see cref="StreamingFigure"/> with
/// automatic re-rendering when streamed data arrives. Subscribes to
/// <see cref="StreamingFigure.RenderRequested"/> and marshals invalidation via
/// <see cref="MainThread"/>. Unsubscribes when the <see cref="StreamingFigure"/> changes and
/// when the view's platform handler is removed, so the view never outlives its subscription. The wiring is the one every
/// native streaming control composes (the library's streaming follower); this view hands it the main thread and its
/// invalidate.</summary>
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

    private readonly StreamingFigureFollower _follower;

    /// <summary>Initializes a new streaming chart view.</summary>
    public MplStreamingChartView() => _follower = new StreamingFigureFollower(PostToMainThread, Invalidate);

    /// <inheritdoc />
    /// <remarks>The follower's redraw: the limits follow the data, then the view is drawn.</remarks>
    private protected override void Repaint() => _follower.Redraw();

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    private static void OnStreamingFigureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (MplStreamingChartView)bindable;
        var streaming = newValue as StreamingFigure;
        view._follower.Follow(streaming);
        // The view draws the figure the streaming figure wraps; one let go of leaves the last picture where it was.
        if (streaming is not null) view.Figure = streaming.Figure;
    }

    /// <inheritdoc />
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler is null)
        {
            _follower.Follow(null);
        }
    }
}
