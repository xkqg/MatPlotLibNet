// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Fluent builder for <see cref="PropCycler"/>.</summary>
/// <remarks>
/// Any property arrays left unset receive single-element defaults:
/// Color → <see cref="Colors.Tab10Blue"/>, LineStyle → <see cref="LineStyle.Solid"/>,
/// MarkerStyle → <see cref="MarkerStyle.None"/>, LineWidth → 1.5.
/// </remarks>
public sealed class PropCyclerBuilder
{
    private Color[]       _colors       = [];
    private LineStyle[]   _lineStyles   = [];
    private MarkerStyle[] _markerStyles = [];
    private double[]      _lineWidths   = [];

    /// <summary>Sets the colors to cycle through.</summary>
    /// <param name="colors">One or more colors.</param>
    /// <returns>This builder for chaining.</returns>
    public PropCyclerBuilder WithColors(params Color[] colors) { _colors = colors; return this; }

    /// <summary>Sets the line styles to cycle through.</summary>
    /// <param name="styles">One or more line styles.</param>
    /// <returns>This builder for chaining.</returns>
    public PropCyclerBuilder WithLineStyles(params LineStyle[] styles) { _lineStyles = styles; return this; }

    /// <summary>Sets the marker styles to cycle through.</summary>
    /// <param name="markers">One or more marker styles.</param>
    /// <returns>This builder for chaining.</returns>
    public PropCyclerBuilder WithMarkerStyles(params MarkerStyle[] markers) { _markerStyles = markers; return this; }

    /// <summary>Sets the line widths (in points) to cycle through.</summary>
    /// <param name="widths">One or more line widths.</param>
    /// <returns>This builder for chaining.</returns>
    public PropCyclerBuilder WithLineWidths(params double[] widths) { _lineWidths = widths; return this; }

    /// <summary>Builds and returns the configured <see cref="PropCycler"/>.</summary>
    /// <returns>A new <see cref="PropCycler"/> instance.</returns>
    public PropCycler Build() => new(_colors, _lineStyles, _markerStyles, _lineWidths);
}
