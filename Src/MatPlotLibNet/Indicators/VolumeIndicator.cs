// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Volume indicator. Displays trading volume as a bar chart in a subplot panel.</summary>
/// <remarks>Best placed in a separate subplot below the price chart. Automatically sets Y-axis minimum to zero.</remarks>
public sealed class VolumeIndicator : Indicator<SignalResult>
{
    private readonly double[] _volumes;
    private readonly string[]? _labels;

    /// <summary>Creates a new Volume indicator.</summary>
    /// <param name="volumes">The volume data for each period.</param>
    /// <param name="labels">Optional category labels (e.g., dates).</param>
    public VolumeIndicator(double[] volumes, string[]? labels = null)
    {
        _volumes = volumes;
        _labels = labels;
        Label = "Volume";
    }

    /// <inheritdoc />
    public override SignalResult Compute() => new(_volumes);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var categories = _labels ?? new string[_volumes.Length];
        if (_labels is null)
            for (int i = 0; i < _volumes.Length; i++) categories[i] = i.ToString();

        var series = axes.Bar(categories, _volumes);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        axes.YAxis.Min = 0;
    }
}
