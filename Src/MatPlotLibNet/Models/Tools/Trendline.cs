// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a trendline drawn between two data-coordinate points on an axes.</summary>
/// <remarks>Under the matplotlib themes the two points range both axes, as matplotlib's <c>axline</c> adds them to the data limits, so
/// the automatic range shows them; under the library's own themes the line is drawn over the range the rest of the axes gives.</remarks>
public sealed class Trendline : IChangeTracked, IDrawingTool
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this trendline changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>The X data coordinate of the first endpoint.</summary>
    public double X1 { get; }
    /// <summary>The Y data coordinate of the first endpoint.</summary>
    public double Y1 { get; }
    /// <summary>The X data coordinate of the second endpoint.</summary>
    public double X2 { get; }
    /// <summary>The Y data coordinate of the second endpoint.</summary>
    public double Y2 { get; }

    /// <inheritdoc cref="ReferenceLine.Color"/>
    public Color? Color { get; set => Change(ref field, value); }
    /// <summary>The dash pattern of the line. Solid by default.</summary>
    public LineStyle LineStyle { get; set => Change(ref field, value); } = RestingLineStyle;

    /// <summary>The dash pattern a trendline is drawn in when it is told none.</summary>
    internal const LineStyle RestingLineStyle = LineStyle.Solid;
    /// <summary>The width of the line, in pixels. Default 1.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = 1.0;
    /// <summary>Text drawn at the end of the line that is drawn, in the line's colour. <see langword="null"/> (the default)
    /// draws none.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>When true, the line is extended beyond both endpoints across the whole plot, as matplotlib's <c>axline</c> draws
    /// it: the straight line through the two points as the axes draw them, so on a log axis it is straight on the screen and
    /// runs through both points. An upright one runs from the bottom edge to the top; one through a single point twice draws
    /// nothing.</summary>
    public bool IsExtended { get; set => Change(ref field, value); }

    /// <summary>Creates a trendline between two data-coordinate points.</summary>
    /// <param name="x1">The X data coordinate of the first endpoint.</param>
    /// <param name="y1">The Y data coordinate of the first endpoint.</param>
    /// <param name="x2">The X data coordinate of the second endpoint.</param>
    /// <param name="y2">The Y data coordinate of the second endpoint.</param>
    public Trendline(double x1, double y1, double x2, double y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    void IDrawingTool.AddTo(AxesOverlays overlays) => overlays.Trendlines.Add(this);
    void IDrawingTool.RemoveFrom(AxesOverlays overlays) => overlays.Trendlines.Remove(this);
}
