// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class DepthResortTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("RotationScript_ContainsResortDepth", () => Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Bar3D([0.0, 1.0], [0.0, 1.0], [3.0, 5.0],
                    s => s.Color = Colors.Tomato))
            .Build()
            .ToSvg(), "resortDepth"),
        new DrawnToken("RotationScript_ContainsAvgViewZ", () => Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build()
            .ToSvg(), "avgViewZ"),
        // resortDepth(b) is called inside reprojectAll() body — the basis arg `b` was
        // added in Phase B.4 of v1.7.2 follow-on (full matplotlib projection port; viewZ
        // now needs the camera basis to compute depth).
        // the call inside reprojectAll
        // the function definition
        new DrawnToken("RotationScript_CallsResortAfterReproject", () => Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Scatter3D([0.0], [0.0], [0.0]))
            .Build()
            .ToSvg(), "resortDepth(b);", "function resortDepth(b)"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
