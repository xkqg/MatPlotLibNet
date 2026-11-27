// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>L.9 — <c>AxesBuilder.WithColorMap(string)</c> throws on unknown
/// names instead of silently returning the unchanged builder.</summary>
public class AxesBuilderColorMapStrictModeTests
{
    [Fact]
    public void WithColorMap_UnknownName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Plt.Create()
               .AddSubPlot(1, 1, 1, ax => ax
                   .Heatmap(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } })
                   .WithColorMap("definitely-not-a-real-colormap"))
               .Build());

        // Exception message should list the registered names so users can correct
        // the typo immediately.
        Assert.Contains("viridis", ex.Message);
    }

}
