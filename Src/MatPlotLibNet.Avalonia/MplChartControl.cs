// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia control that renders a MatPlotLibNet <see cref="Models.Figure"/> natively via SkiaSharp: the figure set through
/// <see cref="Figure"/>, or each frame of an <see cref="AnimationSource"/>. Set <see cref="MplChartSurface.IsInteractive"/> to
/// <c>true</c> to enable pan / zoom / reset / brush-select interaction.
/// </summary>
public sealed class MplChartControl : MplChartSurface
{
    /// <summary>Identifies the <see cref="Figure"/> styled property.</summary>
    public static readonly StyledProperty<Figure?> FigureProperty =
        AvaloniaProperty.Register<MplChartControl, Figure?>(nameof(Figure));

    /// <summary>Identifies the <see cref="MplChartSurface.IsInteractive"/> styled property (the surface's own).</summary>
    public static new readonly StyledProperty<bool> IsInteractiveProperty = MplChartSurface.IsInteractiveProperty;

    static MplChartControl() => AffectsRender<MplChartControl>(FigureProperty);

    private readonly AnimationSourceFollower _animation;

    /// <summary>Initializes a new chart control.</summary>
    public MplChartControl() => _animation = new AnimationSourceFollower(PostAtRenderPriority, ShowFrame);

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this control.</summary>
    public Figure? Figure
    {
        get => GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>Gets or sets the <see cref="IAnimationSource"/> whose frames the control shows.</summary>
    public IAnimationSource? AnimationSource
    {
        get => _animation.Source;
        set => _animation.Follow(value);
    }

    /// <inheritdoc />
    internal override Figure? Shown => Figure;

    /// <summary>The assignment of a frame is posted together with the repaint it asks for.</summary>
    [ExcludeFromCodeCoverage(Justification = "Sets a styled property, which only the UI thread of a running Avalonia application may; the frame it is handed is held by the follower tests.")]
    private void ShowFrame(Figure fig)
    {
        Figure = fig;
        InvalidateVisual();
    }
}
