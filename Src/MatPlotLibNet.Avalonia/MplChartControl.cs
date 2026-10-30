// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia <see cref="Control"/> that renders a MatPlotLibNet <see cref="Figure"/> natively
/// via SkiaSharp. Set <see cref="IsInteractive"/> to <c>true</c> to enable pan / zoom / reset /
/// brush-select interaction without any JavaScript or SignalR dependency.
/// </summary>
public sealed class MplChartControl : Control, IInteractiveChart
{
    /// <summary>Identifies the <see cref="Figure"/> styled property.</summary>
    public static readonly StyledProperty<Figure?> FigureProperty =
        AvaloniaProperty.Register<MplChartControl, Figure?>(nameof(Figure));

    /// <summary>Identifies the <see cref="IsInteractive"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MplChartControl, bool>(nameof(IsInteractive), defaultValue: false);

    static MplChartControl()
    {
        AffectsRender<MplChartControl>(FigureProperty);
        FocusableProperty.OverrideDefaultValue<MplChartControl>(true);
    }

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this control.</summary>
    public Figure? Figure
    {
        get => GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>
    /// Gets or sets whether local pan / zoom / reset interaction is enabled.
    /// When <c>true</c> the control captures pointer and keyboard events and mutates
    /// the figure's axis limits directly, then calls <c>InvalidateVisual</c>.
    /// </summary>
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
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

    private readonly InteractionHost _host = new();
    private IAnimationSource? _animationSource;

    /// <summary>Initializes a new chart control.</summary>
    public MplChartControl()
    {
        _host.InvalidateRequested += InvalidateVisual;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
    }

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

    /// <summary>When set, the interaction controller routes events through this sink (typically
    /// created via <see cref="SignalREventSink.Create"/>) instead of applying them locally.
    /// Set via <see cref="ServerInteractionExtensions.WithServerInteraction"/>.</summary>
    internal Action<FigureInteractionEvent>? ServerEventSink
    {
        get => _host.ServerEventSink;
        set => _host.ServerEventSink = value;
    }

    /// <summary>Returns the active brush select state for overlay rendering, or <c>null</c>.</summary>
    internal BrushSelectState? ActiveBrushSelect => _host.ActiveBrushSelect;

    /// <inheritdoc />
    BrushSelectState? IInteractiveChart.ActiveBrushSelect => _host.ActiveBrushSelect;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    void IInteractiveChart.OnRenderCompleted(Figure figure, MatPlotLibNet.Rendering.LayoutResult layoutResult) =>
        OnRenderCompleted(figure, layoutResult);

    /// <summary>Raised when the reader clicks a data point, with the point they clicked: the series it belongs to,
    /// its value, where it is on screen and which axes it is in. Subscribe to open a detail panel, select a row in a
    /// grid beside the chart, or navigate. It needs <see cref="IsInteractive"/>, and it fires whether the chart runs
    /// locally or through a server sink.</summary>
    public event Action<PinnedAnnotation>? DataPointClicked;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Draws through an Avalonia drawing context, which only a running Avalonia application gives.")]
    public override void Render(DrawingContext context)
    {
        var figure = Figure;
        if (figure is null) return;
        context.Custom(new MplChartDrawOperation(new Rect(Bounds.Size), figure, this));
    }

    /// <summary>
    /// Called by <see cref="MplChartDrawOperation"/> (render thread) after each render pass.
    /// Marshals the layout update back to the UI thread so all interaction state is
    /// read and written from the same thread as pointer/keyboard events.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    internal void OnRenderCompleted(Figure figure, MatPlotLibNet.Rendering.LayoutResult layoutResult)
    {
        if (!IsInteractive) return;
        Dispatcher.UIThread.Post(() => Attach(figure, ChartLayout.Create(figure, layoutResult)));
    }

    /// <summary>Gives the host the figure that was painted and its layout: the first paint of a figure builds the
    /// controller for it, a paint of another figure builds a new one, a repaint only replaces the layout. Separate from
    /// the render callback above because that one runs on the dispatcher and this is the part worth testing.</summary>
    internal InteractionHost Attach(Figure figure, ChartLayout layout)
    {
        _host.Update(figure, layout);
        return _host;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerPressed(AvaloniaInputAdapter.ToPointerArgs(e, this));
        e.Handled = true;
        Focus();
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerMoved(AvaloniaInputAdapter.ToPointerArgs(e, this));

        var tooltip = _host.ActiveTooltip;
        ToolTip.SetTip(this, tooltip?.Text);
        ToolTip.SetIsOpen(this, tooltip is not null);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerReleased(AvaloniaInputAdapter.ToPointerArgs(e, this));
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleScroll(AvaloniaInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleKeyDown(AvaloniaInputAdapter.ToKeyArgs(e));
    }

    /// <summary>A frame arrives on the animation's own thread, and a styled property may only be set on the UI thread,
    /// so the assignment is posted together with the repaint it asks for.</summary>
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    private void OnAnimationFrameReady(object? sender, Figure fig) =>
        Dispatcher.UIThread.Post(() =>
        {
            Figure = fig;
            InvalidateVisual();
        }, DispatcherPriority.Render);
}
