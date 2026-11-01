// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="SignalMarker"/>, an entry of a subplot's <c>signals</c>: where the triangle
/// stands and which way it points, and each setting only when it differs from a new signal's. A subplot without a signal
/// writes no list, so every figure without one writes the bytes it wrote before the signals reached the JSON. A 1.28.0 reader
/// skips the list.</summary>
internal sealed record SignalMarkerDto
{
    /// <summary>The X data coordinate of the signal.</summary>
    public double X { get; init; }

    /// <summary>The Y data coordinate of the signal, the price point the triangle stands next to.</summary>
    public double Y { get; init; }

    /// <summary>A buy signal (an upward triangle below the point) or a sell signal (a downward one above it).</summary>
    public SignalDirection Direction { get; init; }

    /// <summary>The colour of the triangle; absent for green for a buy and red for a sell.</summary>
    public Color? Color { get; init; }

    /// <summary>The size of the triangle in pixels; absent for 12.</summary>
    public double? Size { get; init; }

    /// <summary>A name for the signal, which the chart does not draw; absent for none.</summary>
    public string? Label { get; init; }

    private static readonly SignalMarker Rest = new(0, 0, SignalDirection.Buy);

    /// <summary>The JSON's form of <paramref name="signal"/>.</summary>
    /// <param name="signal">The signal.</param>
    internal static SignalMarkerDto From(SignalMarker signal) => new()
    {
        X = signal.X,
        Y = signal.Y,
        Direction = signal.Direction,
        Color = signal.Color,
        Size = signal.Size.OrNullAtRest(Rest.Size),
        Label = signal.Label,
    };

    /// <summary>Adds the signal this record describes to <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var signal = axes.AddSignal(X, Y, Direction);
        signal.Color = Color;
        signal.Size = Size ?? Rest.Size;
        signal.Label = Label;
    }
}
