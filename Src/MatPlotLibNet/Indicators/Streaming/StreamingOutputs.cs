// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>The output lines of a streaming indicator and what every streaming indicator does with them: one point per input on
/// every line at the same x, the style of the first line as the style of the indicator, and the latest value of the first line.
/// Both streaming bases hold one, so the way outputs are written is stated once.</summary>
internal sealed class StreamingOutputs
{
    private readonly StreamingLineSeries[] _series;

    /// <summary>Takes the given lines; the first is the line the indicator is named and styled by.</summary>
    internal StreamingOutputs(StreamingLineSeries[] series) => _series = series;

    /// <summary>One line of the given capacity.</summary>
    internal StreamingOutputs(int capacity) : this([new StreamingLineSeries(capacity)]) { }

    /// <summary>The lines, in the order the indicator names them.</summary>
    internal IReadOnlyList<StreamingLineSeries> Series => _series;

    /// <summary>How many lines.</summary>
    internal int Count => _series.Length;

    /// <summary>The label of the first line.</summary>
    internal string? Label { get => _series[0].Label; set => _series[0].Label = value; }

    /// <summary>The color of the first line.</summary>
    internal Color? Color { get => _series[0].Color; set => _series[0].Color = value; }

    /// <summary>The width of the first line.</summary>
    internal double LineWidth { get => _series[0].LineWidth; set => _series[0].LineWidth = value; }

    /// <summary>Adds the values of one input, the first to the first line and so on, all at <paramref name="x"/>. The row is
    /// complete before the first line is touched, so no line is ever a point ahead of another.</summary>
    internal void AppendRow(double x, ReadOnlySpan<double> row)
    {
        for (int k = 0; k < _series.Length; k++) _series[k].AppendPoint(x, row[k]);
    }

    /// <summary>The latest value of the first line, or NaN when it has none.</summary>
    internal double Latest()
    {
        var values = _series[0].CreateSnapshot().YData;
        return values.Length > 0 ? values[^1] : double.NaN;
    }
}
