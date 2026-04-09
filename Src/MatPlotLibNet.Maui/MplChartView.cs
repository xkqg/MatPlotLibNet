// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Maui;

/// <summary>A MAUI <see cref="GraphicsView"/> that renders a MatPlotLibNet <see cref="Figure"/> natively.</summary>
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

    /// <summary>Gets or sets the <see cref="Models.Figure"/> to render in this view.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    private static void OnFigureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MplChartView view)
            view.Invalidate();
    }

    /// <summary>Initializes a new chart view with the default drawable.</summary>
    public MplChartView()
    {
        Drawable = new MplChartDrawable(this);
    }
}

internal sealed class MplChartDrawable : IDrawable
{
    private readonly MplChartView _view;

    public MplChartDrawable(MplChartView view) => _view = view;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_view.Figure is null) return;

        var context = new MauiGraphicsRenderContext(canvas);
        ChartServices.Renderer.Render(_view.Figure, context);
    }
}
