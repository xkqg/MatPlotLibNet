// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a horizontal price level (support/resistance) drawn across the full axes width.</summary>
public sealed class HorizontalLevel : IChangeTracked, IDrawingTool
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this level changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>The price the level stands at, a Y value in data units.</summary>
    public double Value { get; }

    /// <inheritdoc cref="ReferenceLine.Color"/>
    public Color? Color { get; set => Change(ref field, value); }
    /// <summary>The dash pattern of the line. Dashed by default.</summary>
    public LineStyle LineStyle { get; set => Change(ref field, value); } = RestingLineStyle;

    /// <summary>The dash pattern a level is drawn in when it is told none.</summary>
    internal const LineStyle RestingLineStyle = LineStyle.Dashed;
    /// <summary>The width of the line, in pixels. Default 1.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = 1.0;
    /// <summary>Text drawn above the right end of the line, in the line's colour. <see langword="null"/> (the default)
    /// draws none.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>Creates a horizontal level at the given price.</summary>
    /// <param name="value">The price the level stands at, a Y value in data units.</param>
    public HorizontalLevel(double value)
    {
        Value = value;
    }

    void IDrawingTool.AddTo(AxesOverlays overlays) => overlays.HorizontalLevels.Add(this);
    void IDrawingTool.RemoveFrom(AxesOverlays overlays) => overlays.HorizontalLevels.Remove(this);
}
