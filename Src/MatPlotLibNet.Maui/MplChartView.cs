// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Maui;

/// <summary>A MAUI <see cref="GraphicsView"/> that renders a MatPlotLibNet <see cref="Figure"/> natively. With
/// <see cref="IsInteractive"/> set, a finger works the chart as a mouse does the other native heads: a drag does what
/// <see cref="DragAction"/> says (a pan unless told otherwise), a pinch zooms, a tap picks the point under it, a double tap
/// goes Home, and a pointer over the chart shows the tooltip of the point under it. The gestures reach the figure through the
/// same <see cref="InteractionHost"/> the other heads use, by way of <see cref="TouchGestureTranslator"/>.</summary>
public class MplChartView : GraphicsView
{
    /// <summary>Identifies the <see cref="Figure"/> bindable property.</summary>
    public static readonly BindableProperty FigureProperty =
        BindableProperty.Create(
            nameof(Figure),
            typeof(Figure),
            typeof(MplChartView),
            defaultValue: null,
            propertyChanged: OnFigureChanged);

    /// <summary>Identifies the <see cref="IsInteractive"/> bindable property.</summary>
    public static readonly BindableProperty IsInteractiveProperty =
        BindableProperty.Create(
            nameof(IsInteractive),
            typeof(bool),
            typeof(MplChartView),
            defaultValue: false);

    /// <summary>Identifies the <see cref="DragAction"/> bindable property.</summary>
    public static readonly BindableProperty DragActionProperty =
        BindableProperty.Create(
            nameof(DragAction),
            typeof(DragGestureAction),
            typeof(MplChartView),
            defaultValue: DragGestureAction.Pan,
            propertyChanged: OnDragActionChanged);

    /// <summary>Gets or sets the <see cref="Models.Figure"/> to render in this view.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>Gets or sets whether a finger works the chart. Off by default, so a chart on a scrolling page does not take
    /// the drag that was meant to scroll it.</summary>
    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    /// <summary>Told of every gesture of the reader once it has reached the figure (or the server sink), whichever kind it
    /// is: the same <see cref="IChartEventHandler"/> a server session takes, so one class reacts to a chart wherever it is
    /// shown. It needs <see cref="IsInteractive"/>.</summary>
    public IChartEventHandler? ChartEventHandler
    {
        get => _host.Handler;
        set => _host.Handler = value;
    }

    /// <summary>Raised when <see cref="ChartEventHandler"/> fails. The gesture has been applied and the next one is taken as
    /// usual, so the failure is reported here and never thrown into the control.</summary>
    public event Action<Exception>? ChartEventHandlerFaulted;

    /// <summary>Gets or sets what a one-finger drag does: pan, zoom into a rectangle, select a band or a span, or turn a 3-D
    /// chart. A finger has no modifier keys, so the view says. A drag in progress keeps the action it began with.</summary>
    public DragGestureAction DragAction
    {
        get => (DragGestureAction)GetValue(DragActionProperty);
        set => SetValue(DragActionProperty, value);
    }

    /// <summary>Raised when the reader taps a data point, with the point they tapped: the series it belongs to, its place in
    /// that series and its data position.</summary>
    public event Action<PinnedAnnotation>? DataPointClicked;

    private static void OnFigureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MplChartView view)
            view.Invalidate();
    }

    private static void OnDragActionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MplChartView view)
            view._touch.DragAction = (DragGestureAction)newValue;
    }

    private IAnimationSource? _animationSource;

    /// <summary>Gets or sets the animation source whose <see cref="IAnimationSource.FrameReady"/>
    /// event drives figure updates and visual invalidation.</summary>
    public IAnimationSource? AnimationSource
    {
        get => _animationSource;
        set
        {
            if (_animationSource is not null)
                _animationSource.FrameReady -= OnAnimationFrameReady;
            _animationSource = value;
            if (_animationSource is not null)
                _animationSource.FrameReady += OnAnimationFrameReady;
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Runs on MAUI's main thread, which a unit test has no loop for; what it does there is held through the figure and the view it calls.")]
    private void OnAnimationFrameReady(object? sender, Figure fig)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Figure = fig;
            Invalidate();
        });
    }

    private readonly InteractionHost _host = new();
    private readonly TouchGestureTranslator _touch;
    private PointF? _fingerDown;
    private bool _dragging;
    private PointF _dragOrigin;
    private double _dragTotalX;
    private double _dragTotalY;

    /// <summary>The host that the drawable gives each paint's layout to, and that the gestures act through.</summary>
    internal InteractionHost Host => _host;

    /// <summary>Initializes a new chart view with the default drawable.</summary>
    public MplChartView()
    {
        _touch = new TouchGestureTranslator(_host);
        Drawable = new MplChartDrawable(this);
        _host.InvalidateRequested += Invalidate;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(pan);

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinch);

        var tap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnDoubleTapped;
        GestureRecognizers.Add(doubleTap);

        StartInteraction += OnFingerDown;
        StartHoverInteraction += OnPointerMoved;
        MoveHoverInteraction += OnPointerMoved;
        EndHoverInteraction += OnPointerLeft;
    }

    /// <summary>Whether a gesture can act: the view is interactive and a paint has given the host a figure.</summary>
    private bool Listening => IsInteractive && _host.IsAttached;

    /// <summary>The pan recognizer reports how far the finger has gone, not where it began, and the graphics view reports
    /// where it went down; the two together are the drag the translator takes. A pan the view saw no finger go down for is
    /// not one it can place.</summary>
    private void OnFingerDown(object? sender, TouchEventArgs e) =>
        _fingerDown = e.Touches.Length > 0 ? e.Touches[0] : null;

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!Listening) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started when _fingerDown is { } down:
                _dragging = true;
                _dragOrigin = down;
                _dragTotalX = _dragTotalY = 0;
                _touch.DragStarted(down.X, down.Y);
                break;

            case GestureStatus.Running when _dragging:
                _dragTotalX = e.TotalX;
                _dragTotalY = e.TotalY;
                _touch.DragMoved(_dragOrigin.X, _dragOrigin.Y, _dragTotalX, _dragTotalY);
                break;

            // The end of a pan carries no totals, so the drag ends where the last move left it.
            case GestureStatus.Completed when _dragging:
                _dragging = false;
                _touch.DragEnded(_dragOrigin.X, _dragOrigin.Y, _dragTotalX, _dragTotalY);
                break;

            case GestureStatus.Canceled when _dragging:
                _dragging = false;
                _touch.DragCancelled();
                break;
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (!Listening) return;

        switch (e.Status)
        {
            case GestureStatus.Running:
                // The origin is a share of the view; the host works in the view's pixels.
                _touch.Pinched(e.Scale, e.ScaleOrigin.X * Width, e.ScaleOrigin.Y * Height);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _touch.PinchEnded();
                break;
        }
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Listening && e.GetPosition(this) is { } at)
            _touch.Tapped(at.X, at.Y);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Listening && e.GetPosition(this) is { } at)
            _touch.DoubleTapped(at.X, at.Y);
    }

    private void OnPointerMoved(object? sender, TouchEventArgs e)
    {
        if (!Listening || e.Touches.Length == 0) return;

        _touch.Hovered(e.Touches[0].X, e.Touches[0].Y);
        ShowTooltip(_host.ActiveTooltip?.Text);
    }

    private void OnPointerLeft(object? sender, EventArgs e) => ShowTooltip(null);

    /// <summary>The tooltip of the point under the pointer; none when there is no such point or the pointer has left.</summary>
    private void ShowTooltip(string? text)
    {
        if (text is null)
            ClearValue(ToolTipProperties.TextProperty);
        else
            ToolTipProperties.SetText(this, text);
    }
}

internal sealed class MplChartDrawable : IDrawable
{
    private readonly MplChartView _view;

    public MplChartDrawable(MplChartView view) => _view = view;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_view.Figure is not { } figure) return;

        var context = new MauiGraphicsRenderContext(canvas);
        ChartRendererDefault.Current.Render(figure, context);

        if (!_view.IsInteractive) return;

        // What this paint drew is what the next gesture acts on; the host aims at the figure and reads the layout.
        var layout = ChartRendererDefault.Current.ComputeLayout(figure, context);
        _view.Host.Update(figure, ChartLayout.Create(figure, layout));

        if (_view.Host.ActiveBrushSelect is { } band)
            canvas.DrawBrushSelect(band);
    }
}
