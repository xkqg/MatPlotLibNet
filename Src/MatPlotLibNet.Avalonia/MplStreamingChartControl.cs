// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using LayoutResult = MatPlotLibNet.Rendering.LayoutResult;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia <see cref="Control"/> that renders a <see cref="StreamingFigure"/> with automatic
/// re-rendering when streamed data arrives. Subscribes to <see cref="StreamingFigure.RenderRequested"/>
/// and marshals invalidation to the UI thread via <see cref="Dispatcher"/>.
/// </summary>
public sealed class MplStreamingChartControl : Control, IInteractiveChart
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> styled property.</summary>
    public static readonly StyledProperty<StreamingFigure?> StreamingFigureProperty =
        AvaloniaProperty.Register<MplStreamingChartControl, StreamingFigure?>(nameof(StreamingFigure));

    /// <summary>Identifies the <see cref="IsInteractive"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MplStreamingChartControl, bool>(nameof(IsInteractive), defaultValue: false);

    private readonly InteractionHost _host = new();
    private StreamingFigure? _subscribedFigure;

    static MplStreamingChartControl()
    {
        AffectsRender<MplStreamingChartControl>(StreamingFigureProperty);
        FocusableProperty.OverrideDefaultValue<MplStreamingChartControl>(true);
    }

    /// <summary>Initializes a new streaming chart control.</summary>
    public MplStreamingChartControl()
    {
        _host.InvalidateRequested += InvalidateVisual;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
    }

    /// <summary>Gets or sets whether the reader can pan, zoom and rotate the chart. A chart the reader has moved stays
    /// where they put it while data keeps arriving, until Home returns it to the scale mode that follows the data.</summary>
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

    /// <inheritdoc />
    BrushSelectState? IInteractiveChart.ActiveBrushSelect => _host.ActiveBrushSelect;

    /// <summary>Called on the render thread after each paint; the layout is handed to the host on the UI thread, where the
    /// pointer and key events arrive.</summary>
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    void IInteractiveChart.OnRenderCompleted(Figure figure, LayoutResult layoutResult) =>
        Dispatcher.UIThread.Post(() => _host.Update(figure, ChartLayout.Create(figure, layoutResult)));

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
    [ExcludeFromCodeCoverage(Justification = "Draws through an Avalonia drawing context, which only a running Avalonia application gives.")]
    public override void Render(DrawingContext context)
    {
        var sf = StreamingFigure;
        if (sf is null) return;
        sf.ApplyAxisScaling();
        context.Custom(new MplChartDrawOperation(new Rect(Bounds.Size), sf.Figure, this));
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

    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    private void OnRenderRequested() =>
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        base.OnDetachedFromVisualTree(e);
    }
}
