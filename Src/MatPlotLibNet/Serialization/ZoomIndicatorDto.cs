// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="ZoomIndicator"/>, the <c>zoomIndicator</c> of an inset: each setting only when
/// it differs from a new indicator's. The indicator is a setting by being there (an inset without one draws none), so an
/// indicator at its defaults is written as an empty object, never left out; an axes without one writes no field at all, so a
/// figure with none writes the bytes it wrote before the field existed. A 1.27.0 reader skips the field and draws no indicator.</summary>
internal sealed record ZoomIndicatorDto
{
    /// <summary>The colour of the rectangle and the connectors; absent for the theme's neutral grey.</summary>
    public Color? Color { get; init; }

    /// <summary>The opacity of the rectangle and the connectors; absent for 0.5.</summary>
    public double? Alpha { get; init; }

    /// <summary>The width of their lines in pixels; absent for 1.</summary>
    public double? LineWidth { get; init; }

    /// <summary>The dash pattern of their lines; absent for solid.</summary>
    public LineStyle? LineStyle { get; init; }

    /// <summary>Whether the connectors are drawn; absent for <see langword="true"/>.</summary>
    public bool? Connectors { get; init; }

    private static readonly ZoomIndicator Rest = new();

    /// <summary>The JSON's form of <paramref name="zoom"/>; <see langword="null"/> when there is none.</summary>
    internal static ZoomIndicatorDto? From(ZoomIndicator? zoom) => zoom is null ? null : new()
    {
        Color = zoom.Color,
        Alpha = zoom.Alpha.OrNullAtRest(Rest.Alpha),
        LineWidth = zoom.LineWidth.OrNullAtRest(Rest.LineWidth),
        LineStyle = zoom.LineStyle.OrNullAtRest(Rest.LineStyle),
        Connectors = zoom.Connectors.OrNullAtRest(Rest.Connectors),
    };

    /// <summary>The indicator this record describes, each field it leaves out at a new indicator's value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The opacity is outside 0 to 1, or the line width is negative or not
    /// finite.</exception>
    internal ZoomIndicator ToZoomIndicator() => new()
    {
        Color = Color,
        Alpha = Alpha ?? Rest.Alpha,
        LineWidth = LineWidth ?? Rest.LineWidth,
        LineStyle = LineStyle ?? Rest.LineStyle,
        Connectors = Connectors ?? Rest.Connectors,
    };
}
