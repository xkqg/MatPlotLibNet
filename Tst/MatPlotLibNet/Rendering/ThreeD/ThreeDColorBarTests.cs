// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class ThreeDColorBarTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("SurfaceWithColorBar_LabelRendered", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } },
                    s => s.ColorMap = ColorMaps.Plasma)
                .WithColorBar(cb => cb with { Label = "Intensity" }))
            .Build()
            .ToSvg(), "Intensity"),
        // Should contain colorbar gradient elements.
        new DrawnToken("SurfaceWithColorMap_AndColorBar_ProducesGradientInSvg", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } },
                    s => s.ColorMap = ColorMaps.Viridis)
                .WithColorBar())
            .Build()
            .ToSvg(), "colorbar") { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("SurfaceWithoutColorBar_NoGradient", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build()
            .ToSvg()) { Absent = ["colorbar"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
