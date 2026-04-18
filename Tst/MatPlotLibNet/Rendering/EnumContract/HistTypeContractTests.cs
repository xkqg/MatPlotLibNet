// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — <see cref="HistType"/> (Bar / Step / StepFilled) must produce
/// distinct SVG — Bar emits <c>&lt;rect&gt;</c> per bin, Step emits a
/// <c>&lt;polyline&gt;</c> outline, StepFilled emits a filled polygon.
/// </summary>
public class HistTypeContractTests
{
    [Fact]
    public void EveryHistType_ProducesDistinctSvg()
    {
        EnumOutputContract.EveryValueRendersDistinctOutput<HistType>(RenderWithType);
    }

    private static string RenderWithType(HistType type)
    {
        var rng = new Random(42);
        double[] data = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() + rng.NextDouble()).ToArray();
        return Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hist(data, 20, s => { s.HistType = type; s.Color = Colors.Teal; }))
            .ToSvg();
    }
}
