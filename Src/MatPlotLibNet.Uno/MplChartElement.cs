// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Xaml;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Uno;

/// <summary>
/// A Uno Platform / WinUI 3 element that renders a MatPlotLibNet <see cref="Models.Figure"/> natively via SkiaSharp: the figure
/// set through <see cref="Figure"/>, or each frame of an <see cref="AnimationSource"/>. Set
/// <see cref="MplChartSurface.IsInteractive"/> to <c>true</c> to enable pan / zoom / reset / brush-select interaction.
/// </summary>
public sealed class MplChartElement : MplChartSurface
{
    /// <summary>Identifies the <see cref="Figure"/> dependency property.</summary>
    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register(nameof(Figure), typeof(Figure), typeof(MplChartElement),
            new PropertyMetadata(null, static (d, _) => ((MplChartElement)d).Invalidate()));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> dependency property (the surface's own).</summary>
    public static new readonly DependencyProperty IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    private readonly AnimationSourceFollower _animation;

    /// <summary>Initializes a new chart element.</summary>
    public MplChartElement() =>
        // A frame arrives on the animation's own thread, and a dependency property may only be set on the dispatcher's.
        _animation = new AnimationSourceFollower(work => DispatcherQueue.Post(work), fig => Figure = fig);

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this element.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>Gets or sets the <see cref="IAnimationSource"/> whose frames the element shows.</summary>
    public IAnimationSource? AnimationSource
    {
        get => _animation.Source;
        set => _animation.Follow(value);
    }

    /// <inheritdoc />
    internal override Figure? Shown => Figure;
}
