// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;
using Xunit.Sdk;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The corners of a 3-D face are joined as the series is to matplotlib, in the drawn SVG: <c>bar3d</c> and <c>voxels</c> are a
/// <c>Poly3DCollection</c> (round under the classic style, where they are edged), a bar on a 3-D axes is a <c>Patch3D</c> (a mitre). The
/// face is styled through the resolved edge of its series, which carries the artist, not through the bare edge, which does not.</summary>
public class ThreeDFaceJoinTests
{
    private static IReadOnlyList<SvgElement> EdgedFaces(string kind)
    {
        var svg = Plt.Create().WithTheme(Theme.MatplotlibClassic).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(DrawnSeriesFixtures.Instance(kind))).Build().ToSvg();
        return [.. svg.Elements("polygon").Where(polygon => polygon["stroke"] == "#000000")];
    }

    [Theory]
    [InlineData(nameof(Bar3DSeries))]
    [InlineData(nameof(VoxelSeries))]
    public void AFaceOfACollection_IsJoinedRound_UnderTheClassicStyle(string kind)
    {
        var faces = EdgedFaces(kind);

        Assert.NotEmpty(faces);
        Assert.All(faces, face => Assert.Equal("round", face["stroke-linejoin"]));
    }

    [Fact]
    public void AFaceOfAPlanarBar_KeepsItsMitre_UnderTheClassicStyle()
    {
        var faces = EdgedFaces(nameof(PlanarBar3DSeries));

        Assert.NotEmpty(faces);
        Assert.All(faces, face => Assert.Null(face["stroke-linejoin"]));
    }
}
