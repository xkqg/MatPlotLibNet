// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;

namespace MatPlotLibNet.Wpf;

/// <summary>A WPF <see cref="SKElement"/> that renders a MatPlotLibNet <see cref="Figure"/>
/// natively via SkiaSharp. Set <see cref="IsInteractive"/> to <c>true</c> to enable
/// pan / zoom / 3D rotation / brush-select interaction.</summary>
public sealed class MplChartControl : SKElement
{
    /// <summary>Identifies the <see cref="Figure"/> dependency property.</summary>
    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register(nameof(Figure), typeof(Figure), typeof(MplChartControl),
            new PropertyMetadata(null, static (d, _) => ((MplChartControl)d).OnFigureChanged()));

    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplChartControl),
            new PropertyMetadata(false));

    private readonly InteractionHost _host = new();
    private IAnimationSource? _animationSource;

    /// <summary>When set, the host routes interaction events through this sink (typically created via
    /// <see cref="SignalREventSink.Create"/>) instead of applying them locally. Set via
    /// <see cref="ServerInteractionExtensions.WithServerInteraction"/>.</summary>
    internal Action<FigureInteractionEvent>? ServerEventSink
    {
        get => _host.ServerEventSink;
        set => _host.ServerEventSink = value;
    }

    /// <summary>Raised when the reader clicks a data point, with the point they clicked: the series it belongs to,
    /// its value, where it is on screen and which axes it is in. Subscribe to open a detail panel, select a row in a
    /// grid beside the chart, or navigate. It needs <see cref="IsInteractive"/>.</summary>
    public event Action<PinnedAnnotation>? DataPointClicked;

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

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this control.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>Gets or sets whether local interaction is enabled.</summary>
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

    /// <summary>Creates a chart control: it can take the keyboard focus (so the keys of the interaction work), repaints when the
    /// interaction host asks it to, and raises <see cref="DataPointClicked"/> when a data point is clicked.</summary>
    public MplChartControl()
    {
        Focusable = true;
        _host.InvalidateRequested += InvalidateVisual;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
    }

    /// <inheritdoc />
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var figure = Figure;
        if (figure is null) return;

        var ctx = new SkiaRenderContext(e.Surface.Canvas);
        ChartRendererDefault.Current.Render(figure, ctx);

        if (IsInteractive)
        {
            var layoutResult = ChartRendererDefault.Current.ComputeLayout(figure, ctx);
            _host.Update(figure, ChartLayout.Create(figure, layoutResult));
        }

        // The rubber band of a brush-select drag in progress.
        if (_host.ActiveBrushSelect is { } band)
        {
            e.Surface.Canvas.DrawBrushSelect(band);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerPressed(WpfInputAdapter.ToPressedArgs(e, this));
        Focus();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerMoved(WpfInputAdapter.ToMovedArgs(e, this));
        ToolTip = _host.ActiveTooltip?.Text;
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerReleased(WpfInputAdapter.ToReleasedArgs(e, this));
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleScroll(WpfInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleKeyDown(WpfInputAdapter.ToKeyArgs(e));
    }

    /// <summary>A control handed a different figure repaints; the host sees the new figure with that paint and aims the
    /// controller at it, so pan and zoom act on the figure now on screen.</summary>
    private void OnFigureChanged() => InvalidateVisual();

    private void OnAnimationFrameReady(object? sender, Figure fig) =>
        Dispatcher.InvokeAsync(() => Figure = fig);
}
