// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class HistTypeOutputContractTests : EnumOutputContractTests<HistType>
{
    protected override Func<HistType, string> RenderDelegate => type =>
    {
        var rng = new Random(42);
        double[] data = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() + rng.NextDouble()).ToArray();
        return Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hist(data, 20, s => { s.HistType = type; s.Color = Colors.Teal; }))
            .ToSvg();
    };
}
