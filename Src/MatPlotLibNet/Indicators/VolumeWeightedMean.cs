// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>A volume-weighted mean price, kept as two running sums: the sum of price times volume and the sum of volume. The one
/// place that mean is worked out: the cumulative <see cref="Vwap"/>, <see cref="Streaming.StreamingVwap"/> and the rolling
/// VWAP of <see cref="VwapZScore"/> all read it, so bars they share give the same mean to the bit. The sums are added in the
/// order the bars come, and a mean of no volume reads the fallback the caller names (a price is the volume-weighted mean of
/// itself).</summary>
internal struct VolumeWeightedMean
{
    private double _priceVolume;
    private double _volume;

    /// <summary>The volume added so far.</summary>
    internal readonly double Volume => _volume;

    /// <summary>Adds one bar.</summary>
    /// <param name="price">The bar's price.</param>
    /// <param name="volume">The bar's volume.</param>
    internal void Add(double price, double volume)
    {
        _priceVolume += price * volume;
        _volume += volume;
    }

    /// <summary>The mean so far: the sum of price times volume over the sum of volume, or <paramref name="fallback"/> when the
    /// volume is not above 0 (nothing added, bars of no volume only, or a volume that is not a number).</summary>
    /// <param name="fallback">What to read when there is no volume to weigh by.</param>
    internal readonly double Value(double fallback) => _volume > 0 ? _priceVolume / _volume : fallback;

    /// <summary>Forgets every bar added.</summary>
    internal void Reset()
    {
        _priceVolume = 0;
        _volume = 0;
    }
}
