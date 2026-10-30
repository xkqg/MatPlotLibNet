// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the direction of a trading signal.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum SignalDirection
{
    /// <summary>A buy signal, rendered as an upward triangle below the price point.</summary>
    Buy = 0,

    /// <summary>A sell signal, rendered as a downward triangle above the price point.</summary>
    Sell = 1,
}

/// <summary>Represents a buy or sell signal marker at a specific data coordinate on the chart.</summary>
public sealed class SignalMarker
{
    /// <summary>The X data coordinate of the signal.</summary>
    public double X { get; }

    /// <summary>The Y data coordinate of the signal, the price point the triangle stands next to.</summary>
    public double Y { get; }

    /// <summary>Whether this is a buy signal (an upward triangle below the point) or a sell signal (a downward triangle
    /// above it).</summary>
    public SignalDirection Direction { get; }

    /// <summary>The colour of the triangle. <see langword="null"/> (the default) draws a buy signal in green and a sell
    /// signal in red.</summary>
    public Color? Color { get; set; }

    /// <summary>The size of the triangle, in pixels: it is this tall and this wide, and its tip stands this far from the
    /// price point. Default 12.</summary>
    public double Size { get; set; } = 12;

    /// <summary>A name for the signal. The chart does not draw it. <see langword="null"/> (the default) means none.</summary>
    public string? Label { get; set; }

    /// <summary>Creates a new signal marker at the given data coordinates.</summary>
    /// <param name="x">The X data coordinate of the signal.</param>
    /// <param name="y">The Y data coordinate of the signal.</param>
    /// <param name="direction">Whether the signal is a buy or a sell.</param>
    public SignalMarker(double x, double y, SignalDirection direction)
    {
        X = x;
        Y = y;
        Direction = direction;
    }
}
