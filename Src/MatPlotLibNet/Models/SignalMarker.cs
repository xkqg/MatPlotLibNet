// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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
    /// <summary>Gets the X data coordinate.</summary>
    public double X { get; }

    /// <summary>Gets the Y data coordinate (price level).</summary>
    public double Y { get; }

    /// <summary>Gets the signal direction (Buy or Sell).</summary>
    public SignalDirection Direction { get; }

    /// <summary>Gets or sets the marker color. Defaults to green for Buy, red for Sell.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the marker size in pixels.</summary>
    public double Size { get; set; } = 12;

    /// <summary>Gets or sets an optional text label displayed near the marker.</summary>
    public string? Label { get; set; }

    /// <summary>Creates a new signal marker at the given data coordinates.</summary>
    public SignalMarker(double x, double y, SignalDirection direction)
    {
        X = x;
        Y = y;
        Direction = direction;
    }
}
