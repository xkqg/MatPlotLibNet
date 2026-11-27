// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using ModifierKeys = MatPlotLibNet.Interaction.ModifierKeys;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;

namespace MatPlotLibNet.Wpf;

/// <summary>What a WPF chart control is, apart from where its figure comes from: an <see cref="SKElement"/> that paints the figure
/// natively through SkiaSharp and, with <see cref="IsInteractive"/> set, lets the reader pan, zoom, turn a 3-D chart and brush-select
/// through its <see cref="InteractionHost"/>. <see cref="MplChartControl"/> shows a <see cref="Figure"/> and
/// <see cref="MplStreamingChartControl"/> a streaming figure; only the library derives from it.</summary>
public abstract class MplChartSurface : SKElement, IServerInteractive
{
    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplChartSurface), new PropertyMetadata(false));

    private readonly InteractionHost _host;
    private readonly SkiaChartPainter _painter = new();

    /// <summary>Only the library makes a surface. It can take the keyboard focus (so the keys of the interaction work), is painted
    /// again when its host asks it to, and lets go of what it holds when it is unloaded.</summary>
    private protected MplChartSurface()
    {
        Focusable = true;
        _host = new InteractionHost(() => IsInteractive, Repaint);
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
        Unloaded += (_, _) => Release();
    }

    /// <summary>Gets or sets whether the reader can work the chart: pan, zoom, turn a 3-D chart and brush-select. Off by default.</summary>
    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
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

    /// <summary>The host the gestures go through; <c>WithServerInteraction</c> sends them to a server through it.</summary>
    internal InteractionHost Host => _host;

    /// <inheritdoc />
    InteractionHost IServerInteractive.Host => _host;

    /// <summary>The figure the surface shows now, or <see langword="null"/> for none: the one thing each leaf says of itself.</summary>
    internal abstract Figure? Shown { get; }

    /// <summary>How the surface is painted again after a gesture changed the figure: its invalidate. A streaming control sets the
    /// limits from the data first.</summary>
    private protected virtual void Repaint() => InvalidateVisual();

    /// <summary>What the surface lets go of when it is unloaded: what its painter recorded of the figure, so shown again it paints
    /// from scratch. A streaming control lets go of its figure too, which outlives it.</summary>
    internal virtual void Release() => _painter.Dispose();

    /// <inheritdoc />
    /// <remarks>The paint only draws: it writes nothing into the figure (a streaming control set the limits from the data before
    /// it asked to be drawn). It draws only the subplots that changed since the last paint (the others are replayed as they were
    /// recorded), with the rubber band of a brush-select drag in progress over them, and hands the layout of that draw to the host.</remarks>
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        if (Shown is not { } figure) return;

        _host.Painted(figure, _painter.Paint(e.Surface.Canvas, figure, _host.ActiveBrushSelect));
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_host.Listening) return;
        _host.HandlePointerPressed(ToPressedArgs(e, this));
        Focus();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_host.Listening) return;
        _host.HandlePointerMoved(ToMovedArgs(e, this));
        ToolTip = _host.ActiveTooltip?.Text;
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_host.Listening) return;
        _host.HandlePointerReleased(ToReleasedArgs(e, this));
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!_host.Listening) return;
        _host.HandleScroll(ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_host.Listening) return;
        _host.HandleKeyDown(ToKeyArgs(e));
    }

    /// <summary>Converts a mouse-down event.</summary>
    private static PointerInputArgs ToPressedArgs(MouseButtonEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return new PointerInputArgs(pos.X, pos.Y, ButtonOf(e.ChangedButton), Modifiers(), e.ClickCount);
    }

    /// <summary>Converts a mouse-move event.</summary>
    private static PointerInputArgs ToMovedArgs(MouseEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        var button = PointerButton.MovedWith(
            e.LeftButton == MouseButtonState.Pressed,
            e.RightButton == MouseButtonState.Pressed,
            e.MiddleButton == MouseButtonState.Pressed);
        return new PointerInputArgs(pos.X, pos.Y, button, Modifiers());
    }

    /// <summary>Converts a mouse-up event.</summary>
    private static PointerInputArgs ToReleasedArgs(MouseButtonEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return new PointerInputArgs(pos.X, pos.Y, ButtonOf(e.ChangedButton), Modifiers());
    }

    /// <summary>Converts a wheel event; one notch of the wheel is a delta of 1.</summary>
    private static ScrollInputArgs ToScrollArgs(MouseWheelEventArgs e, IInputElement relativeTo)
    {
        var pos = e.GetPosition(relativeTo);
        return ScrollInputArgs.OfWheel(pos.X, pos.Y, e.Delta, Modifiers());
    }

    /// <summary>Converts a key-down event.</summary>
    private static KeyInputArgs ToKeyArgs(KeyEventArgs e) => new(e.Key.ToString(), Modifiers());

    private static PointerButton ButtonOf(MouseButton button) => button switch
    {
        MouseButton.Right => PointerButton.Right,
        MouseButton.Middle => PointerButton.Middle,
        _ => PointerButton.Left,
    };

    private static ModifierKeys Modifiers()
    {
        var held = Keyboard.Modifiers;
        return ModifierKeys.Held(
            (held & WpfModifierKeys.Shift) != 0,
            (held & WpfModifierKeys.Control) != 0,
            (held & WpfModifierKeys.Alt) != 0);
    }
}
