// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The rows of the classification map, every indicator with a streaming form. Each family of rows is a part of this class
/// in a file of its own; this part joins them.</summary>
public static partial class StreamingRows
{
    /// <summary>Every row, in the order of the parts.</summary>
    public static IReadOnlyList<StreamingRow> All { get; } = [.. Shipped(), .. Windowed(), .. Filters(), .. Machines(), .. Entropy()];

    /// <summary>The name of each row, as the theory takes it (a theory takes data xunit can describe, so it takes the name).</summary>
    public static TheoryData<string> Names() => new(All.Select(r => r.Name));

    /// <summary>The values of several series at one bar: the row a multivariate form is given (the close, the range, the volume in hundreds).</summary>
    public static double[] FeatureRow(OracleBars bars, int i) => [bars.Close[i], bars.High[i] - bars.Low[i], bars.Volume[i] / 100];

    /// <summary>The rows of the bars, as the batch multivariate indicators are given them.</summary>
    public static double[][] FeatureRows(OracleBars bars) => [.. Enumerable.Range(0, bars.Count).Select(i => FeatureRow(bars, i))];

    /// <summary>The row of an indicator.</summary>
    public static StreamingRow Of(string name) => All.Single(r => r.Name == name);
}
