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
using MatPlotLibNet.Skia;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect  = MatPlotLibNet.Rendering.Rect;
using SkiaSharp;

namespace MatPlotLibNet.Avalonia;

/// <summary>What an Avalonia chart control is, apart from where its figure comes from: a <see cref="Control"/> that paints the
/// figure natively through SkiaSharp and, with <see cref="IsInteractive"/> set, lets the reader pan, zoom, reset and brush-select
/// through its <see cref="InteractionHost"/>, without any JavaScript or SignalR dependency. <see cref="MplChartControl"/> shows a
/// <see cref="Figure"/> and <see cref="MplStreamingChartControl"/> a streaming figure; only the library derives from it.</summary>
public abstract class MplChartSurface : Control, IServerInteractive
{
    /// <summary>Identifies the <see cref="IsInteractive"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MplChartSurface, bool>(nameof(IsInteractive), defaultValue: false);

    private readonly InteractionHost _host;
    private readonly SkiaChartPainter _painter = new();

    static MplChartSurface() => FocusableProperty.OverrideDefaultValue<MplChartSurface>(true);

    /// <summary>Only the library makes a surface. It takes the keyboard focus, and is painted again when its host asks it to.</summary>
    private protected MplChartSurface()
    {
        _host = new InteractionHost(() => IsInteractive, Repaint, PostToUiThread);
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
    }

    /// <summary>Gets or sets whether the reader can work the chart: pan, zoom, reset and brush-select. Off by default.</summary>
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    /// <inheritdoc cref="InteractionHost.Handler"/>
    public IChartEventHandler? ChartEventHandler
    {
        get => _host.Handler;
        set => _host.Handler = value;
    }

    /// <inheritdoc cref="InteractionHost.HandlerFaulted"/>
    public event Action<Exception>? ChartEventHandlerFaulted;

    /// <inheritdoc cref="InteractionHost.DataPointClicked"/>
    public event Action<PinnedAnnotation>? DataPointClicked;

    /// <summary>The host the gestures go through; <c>WithServerInteraction</c> sends them to a server through it, and the draw
    /// operation hands it the layout of each paint.</summary>
    internal InteractionHost Host => _host;

    /// <inheritdoc />
    InteractionHost IServerInteractive.Host => _host;

    /// <summary>The surface's painter, which keeps between paints a recording of every subplot that did not change. A new draw
    /// operation is made for every frame; the painter stays with the surface.</summary>
    internal SkiaChartPainter Painter => _painter;

    /// <summary>The figure the surface shows now, or <see langword="null"/> for none: the one thing each leaf says of itself.</summary>
    internal abstract Figure? Shown { get; }

    /// <summary>How the surface is painted again after a gesture changed the figure: its invalidate. A streaming control sets the
    /// limits from the data first.</summary>
    private protected virtual void Repaint() => InvalidateVisual();

    /// <summary>What the surface lets go of when it leaves the visual tree: what its painter recorded of the figure, so attached
    /// again it paints from scratch. A streaming control lets go of its figure too, which outlives it.</summary>
    internal virtual void Release() => _painter.Dispose();

    /// <summary>A paint runs on the render thread: the host reads its layout on the UI thread, where the pointer and key events arrive.</summary>
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the host tests.")]
    private static void PostToUiThread(Action work) => Dispatcher.UIThread.Post(work);

    /// <summary>A frame or a request to draw arrives on another thread, and a styled property may only be set on the UI thread: the
    /// work runs there, at render priority.</summary>
    [ExcludeFromCodeCoverage(Justification = "Posts to Avalonia's UI dispatcher, which needs a running Avalonia application; what it posts is held by the follower tests.")]
    private protected static void PostAtRenderPriority(Action work) => Dispatcher.UIThread.Post(work, DispatcherPriority.Render);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Avalonia calls it when the control leaves a visual tree, which only a running Avalonia application has; what it lets go of is held by the surface tests.")]
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Release();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Draws through an Avalonia drawing context, which only a running Avalonia application gives.")]
    public override void Render(DrawingContext context)
    {
        if (Shown is not { } figure) return;
        context.Custom(new MplChartDrawOperation(new Rect(Bounds.Size), figure, this));
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_host.Listening) return;
        _host.HandlePointerPressed(ToPointerArgs(e, this));
        e.Handled = true;
        Focus();
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_host.Listening) return;
        _host.HandlePointerMoved(ToPointerArgs(e, this));

        var tooltip = _host.ActiveTooltip;
        ToolTip.SetTip(this, tooltip?.Text);
        ToolTip.SetIsOpen(this, tooltip is not null);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_host.Listening) return;
        _host.HandlePointerReleased(ToPointerArgs(e, this));
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!_host.Listening) return;
        _host.HandleScroll(ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_host.Listening) return;
        _host.HandleKeyDown(ToKeyArgs(e));
    }

    /// <summary>
    /// Custom Avalonia draw operation that renders a <see cref="Figure"/> onto the Skia canvas
    /// provided by Avalonia's Skia rendering backend.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Runs on Avalonia's render thread, against a Skia lease only a running Avalonia application gives.")]
    private sealed class MplChartDrawOperation : ICustomDrawOperation
    {
        private readonly Figure _figure;
        private readonly MplChartSurface _owner;

        internal MplChartDrawOperation(Rect bounds, Figure figure, MplChartSurface owner)
        {
            Bounds = bounds;
            _figure = figure;
            _owner = owner;
        }

        /// <inheritdoc />
        public Rect Bounds { get; }

        /// <inheritdoc />
        public bool HitTest(Point p) => Bounds.Contains(p);

        /// <inheritdoc />
        public bool Equals(ICustomDrawOperation? other) => false;

        /// <inheritdoc />
        public void Dispose() { }

        /// <inheritdoc />
        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (feature is null) return;

            using var lease = feature.Lease();
            if (lease is null) return;

            // The control's painter draws only the subplots that changed since its last paint, with the band of a brush-select
            // drag in progress over them.
            // What this paint drew is what the next gesture acts on: the host reads the layout, on the UI thread where the gestures arrive.
            _owner.Host.Painted(_figure, _owner.Painter.Paint(lease.SkCanvas, _figure, _owner.Host.ActiveBrushSelect));
        }
    }

    /// <summary>The button a pointer-pressed event is for.</summary>
    /// <param name="kind">What changed about the pointer.</param>
    internal static PointerButton ButtonPressedBy(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.LeftButtonPressed   => PointerButton.Left,
        PointerUpdateKind.RightButtonPressed  => PointerButton.Right,
        PointerUpdateKind.MiddleButtonPressed => PointerButton.Middle,
        _                                     => PointerButton.None,
    };

    /// <summary>The button a pointer-released event is for.</summary>
    /// <param name="kind">What changed about the pointer.</param>
    internal static PointerButton ButtonReleasedBy(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.LeftButtonReleased   => PointerButton.Left,
        PointerUpdateKind.RightButtonReleased  => PointerButton.Right,
        PointerUpdateKind.MiddleButtonReleased => PointerButton.Middle,
        _                                      => PointerButton.None,
    };

    /// <summary>Converts a pointer-pressed event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    private static PointerInputArgs ToPointerArgs(PointerPressedEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        var btn = ButtonPressedBy(e.GetCurrentPoint(source).Properties.PointerUpdateKind);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers), e.ClickCount);
    }

    /// <summary>Converts a pointer-moved event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    private static PointerInputArgs ToPointerArgs(PointerEventArgs e, Control source)
    {
        var pos   = e.GetPosition(source);
        var props = e.GetCurrentPoint(source).Properties;
        var btn   = PointerButton.MovedWith(props.IsLeftButtonPressed, props.IsRightButtonPressed, props.IsMiddleButtonPressed);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-released event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    private static PointerInputArgs ToPointerArgs(PointerReleasedEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        var btn = ButtonReleasedBy(e.GetCurrentPoint(source).Properties.PointerUpdateKind);
        return new PointerInputArgs(pos.X, pos.Y, btn, Map(e.KeyModifiers));
    }

    /// <summary>Converts a pointer-wheel event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    private static ScrollInputArgs ToScrollArgs(PointerWheelEventArgs e, Control source)
    {
        var pos = e.GetPosition(source);
        return new ScrollInputArgs(pos.X, pos.Y, e.Delta.X, e.Delta.Y, Map(e.KeyModifiers));
    }

    /// <summary>Converts a key-down event.</summary>
    [ExcludeFromCodeCoverage(Justification = "Reads an Avalonia event argument, which only the running Avalonia input system builds; what it hands on is held by the host tests.")]
    private static KeyInputArgs ToKeyArgs(KeyEventArgs e) =>
        new(e.Key.ToString(), Map(e.KeyModifiers));

    private static ModifierKeys Map(KeyModifiers k) =>
        ModifierKeys.Held(
            (k & KeyModifiers.Shift) != 0,
            (k & KeyModifiers.Control) != 0,
            (k & KeyModifiers.Alt) != 0);
}
