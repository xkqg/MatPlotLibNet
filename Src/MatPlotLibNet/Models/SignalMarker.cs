// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the direction of a trading signal.</summary>
public enum SignalDirection
{
    /// <summary>A buy signal, rendered as an upward triangle below the price point.</summary>
    Buy,

    /// <summary>A sell signal, rendered as a downward triangle above the price point.</summary>
    Sell
}

/// <summary>Represents a buy or sell signal marker at a specific data coordinate on the chart.</summary>
public sealed class SignalMarker
{
    public double X { get; }

    public double Y { get; }

    public SignalDirection Direction { get; }

    public Color? Color { get; set; }

    public double Size { get; set; } = 12;

    public string? Label { get; set; }

    /// <summary>Creates a new signal marker at the given data coordinates.</summary>
    public SignalMarker(double x, double y, SignalDirection direction)
    {
        X = x;
        Y = y;
        Direction = direction;
    }
}
