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
            new PropertyMetadata(null, static (d, _) => ((MplChartControl)d).InvalidateVisual()));

    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplChartControl),
            new PropertyMetadata(false));

    private InteractionController? _controller;
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

    public MplChartControl()
    {
        Focusable = true;
    }

    /// <inheritdoc />
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var figure = Figure;
        if (figure is null) return;

        var ctx = new SkiaRenderContext(e.Surface.Canvas);
        ChartServices.Renderer.Render(figure, ctx);

        if (IsInteractive)
        {
            var layoutResult = ChartServices.Renderer.ComputeLayout(figure, ctx);
            var layout = ChartLayout.Create(figure, layoutResult);
            if (_controller is null)
            {
                _controller = InteractionController.CreateLocal(figure, layout);
                _controller.InvalidateRequested += InvalidateVisual;
            }
            else
            {
                _controller.UpdateLayout(layout);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (_controller is null || !IsInteractive) return;
        var pos = e.GetPosition(this);
        _controller.HandlePointerPressed(new PointerInputArgs(pos.X, pos.Y,
            e.ChangedButton == MouseButton.Right ? PointerButton.Right : PointerButton.Left,
            GetModifiers(), e.ClickCount));
        Focus();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_controller is null || !IsInteractive) return;
        var pos = e.GetPosition(this);
        _controller.HandlePointerMoved(new PointerInputArgs(pos.X, pos.Y,
            PointerButton.Left, GetModifiers()));
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_controller is null || !IsInteractive) return;
        var pos = e.GetPosition(this);
        _controller.HandlePointerReleased(new PointerInputArgs(pos.X, pos.Y,
            e.ChangedButton == MouseButton.Right ? PointerButton.Right : PointerButton.Left,
            GetModifiers()));
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_controller is null || !IsInteractive) return;
        var pos = e.GetPosition(this);
        _controller.HandleScroll(new ScrollInputArgs(pos.X, pos.Y, 0, e.Delta / 120.0, GetModifiers()));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandleKeyDown(new KeyInputArgs(e.Key.ToString(), GetModifiers()));
    }

    private void OnAnimationFrameReady(object? sender, Figure fig) =>
        Dispatcher.InvokeAsync(() => Figure = fig);

    private static MatPlotLibNet.Interaction.ModifierKeys GetModifiers()
    {
        var mods = MatPlotLibNet.Interaction.ModifierKeys.None;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            mods |= MatPlotLibNet.Interaction.ModifierKeys.Shift;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            mods |= MatPlotLibNet.Interaction.ModifierKeys.Ctrl;
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            mods |= MatPlotLibNet.Interaction.ModifierKeys.Alt;
        return mods;
    }
}
