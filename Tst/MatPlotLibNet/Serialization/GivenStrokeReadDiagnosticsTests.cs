// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A dash name in a figure file that is no <see cref="LineStyle"/> (a figure written by a newer version, a hand-made one) is
/// announced on the chart diagnostics by every kind that holds a given stroke, and the series keeps the dash it had: one reader of the
/// stroke for every kind, where six kinds once dropped the name without a word.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class GivenStrokeReadDiagnosticsTests
{
    [Theory]
    [MemberData(nameof(GivenStrokeHolderTests.KindNames), MemberType = typeof(GivenStrokeHolderTests))]
    public void ADashNameThatIsNoLineStyle_IsAnnounced_AndTheDashIsNotGiven(string kind)
    {
        var dto = GivenStrokeHolderTests.Kinds[kind].Make().ToSeriesDto() with { LineStyle = "wavy", LineWidth = 2 };
        var (read, received) = DiagnosticsCapture.Of(() => SeriesRegistry.Create(dto.Type!, new Axes(), dto));

        var diagnostic = Assert.Single(received);
        Assert.Contains("LineStyle 'wavy'", diagnostic.Message);
        Assert.Equal(GivenStroke.RestingStyle, read!.GetType().GetProperty("LineStyle")!.GetValue(read));
        Assert.Equal(2.0, read.GetType().GetProperty("LineWidth")!.GetValue(read));
    }
}
