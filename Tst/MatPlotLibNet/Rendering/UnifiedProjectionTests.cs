// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that the unified Projection3D is threaded through to all 3D series renderers.</summary>
public class UnifiedProjectionTests
{
    /// <summary>A different elevation or azimuth on the axes draws different polygon coordinates.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThreeDAxes_UsesAxesElevationAndAzimuth(bool elevation)
    {
        var (x, y, z) = SampleSurface.ThreeByThree();

        var axesDefault = new Axes();
        axesDefault.Surface(x, y, z);

        var axesCustom = elevation ? new Axes { Elevation = 60 } : new Axes { Azimuth = 0 };
        axesCustom.Surface(x, y, z);

        string svgDefault = axesDefault.RenderedSvg(new Rect(10, 10, 380, 280));
        string svgCustom = axesCustom.RenderedSvg(new Rect(10, 10, 380, 280));

        Assert.NotEqual(svgDefault, svgCustom);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ThreeD_PerspectiveProjection_RendersSvg", () =>
        {
            var (x, y, z) = SampleSurface.ThreeByThree();
            var axes = new Axes { CameraDistance = 5.0 };
            axes.Surface(x, y, z);
            return axes.RenderedSvg(new Rect(10, 10, 380, 280));
        }, "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
