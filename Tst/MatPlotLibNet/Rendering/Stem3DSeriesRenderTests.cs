// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="Stem3DSeries"/> rendering.</summary>
public class Stem3DSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 1.0, 0.0];
    private static readonly double[] Z = [1.0, 2.0, 1.5];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Each stem is a line and each head a marker.
        new DrawnToken("Stem3D_SvgContainsLinesAndCircles", () => TestFigures.OfOneVerb(ax => ax.Stem3D(X, Y, Z)).ToSvg(), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
