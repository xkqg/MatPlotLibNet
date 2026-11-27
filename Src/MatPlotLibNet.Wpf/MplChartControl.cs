// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Wpf;

/// <summary>A WPF control that renders a MatPlotLibNet <see cref="Models.Figure"/> natively via SkiaSharp: the figure set through
/// <see cref="Figure"/>, or each frame of an <see cref="AnimationSource"/>. Set <see cref="MplChartSurface.IsInteractive"/> to
/// <c>true</c> to enable pan / zoom / 3D rotation / brush-select interaction.</summary>
public sealed class MplChartControl : MplChartSurface
{
    /// <summary>Identifies the <see cref="Figure"/> dependency property. A control handed a different figure repaints; the host
    /// sees the new figure with that paint and aims at it, so pan and zoom act on the figure now on screen.</summary>
    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register(nameof(Figure), typeof(Figure), typeof(MplChartControl),
            new PropertyMetadata(null, static (d, _) => ((MplChartControl)d).InvalidateVisual()));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> dependency property (the surface's own).</summary>
    public static new readonly DependencyProperty IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    private readonly AnimationSourceFollower _animation;

    /// <summary>Creates a chart control.</summary>
    public MplChartControl() =>
        // A frame arrives on the animation's own thread and a dependency property may only be set on the dispatcher's.
        _animation = new AnimationSourceFollower(work => Dispatcher.InvokeAsync(work), fig => Figure = fig);

    /// <summary>Gets or sets the <see cref="IAnimationSource"/> whose frames the control shows.</summary>
    public IAnimationSource? AnimationSource
    {
        get => _animation.Source;
        set => _animation.Follow(value);
    }

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this control.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <inheritdoc />
    internal override Figure? Shown => Figure;
}
