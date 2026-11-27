// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>A voxel's faces are shaded by the light as a 3-D bar's are: both are boxes (<c>Cuboid</c>). matplotlib 3.11.2,
/// <c>Axes3D.voxels(filled, facecolors='#1f77b4')</c> (<c>shade=True</c>, <c>lightsource=None</c>) builds a
/// <c>Poly3DCollection(faces, shade=True, lightsource=None)</c> per voxel, the same <c>art3d._shade_colors</c> as
/// <c>bar3d(..., color='#1f77b4')</c>; read with python after a draw, <c>np.round(get_facecolor() * 255)</c> gives the same six
/// colours for both, all opaque: [17, 63, 96], [13, 50, 75], [27, 105, 159], [13, 50, 75], [27, 105, 159], [24, 91, 138].</summary>
public sealed class VoxelShadingTests
{
    private static readonly string[] MatplotlibShades = ["#113F60", "#0D324B", "#1B699F", "#185B8A"];

    private static List<SvgElement> FaceFillsOf(Action<AxesBuilder> axes)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                axes(ax);
                ax.WithPane3D(p => p with { Visible = false });
            })
            .Build().ToSvg();
        return [.. svg.Elements("polygon")];
    }

    [Fact]
    public void TheFacesOfAVoxel_AreShadedByTheLight_AsMatplotlibShadesThem()
    {
        var cell = new bool[1, 1, 1];
        cell[0, 0, 0] = true;

        var faces = FaceFillsOf(ax => ax.Voxels(cell, s => s.Color = Color.FromHex("#1F77B4")));
        var fills = faces.Select(face => face["fill"]!).Distinct().Order().ToArray();

        Assert.Equal(MatplotlibShades.Order().ToArray(), fills);
        Assert.DoesNotContain(faces, face => face["fill-opacity"] is not null);
    }

    [Fact]
    public void AVoxelAndABarOfOneColour_WearTheSameShades()
    {
        var cell = new bool[1, 1, 1];
        cell[0, 0, 0] = true;
        var colour = Color.FromHex("#D62728");

        string FillsOf(Action<AxesBuilder> axes) =>
            string.Join(' ', FaceFillsOf(axes).Select(face => face["fill"]!).Distinct().Order());

        Assert.Equal(FillsOf(ax => ax.Bar3D([0.0], [0.0], [1.0], s => s.Color = colour)), FillsOf(ax => ax.Voxels(cell, s => s.Color = colour)));
    }

    /// <summary>Only the faces between a filled cell and an empty one (or the edge of the grid) are drawn, as matplotlib's voxels draw
    /// them ("we only draw faces between a voxel and an empty space, to avoid drawing internal faces", axes3d.py). Read with python
    /// after a draw, <c>sum(len(c.get_paths()) for c in ax.voxels(v).values())</c> is 10 for two cells side by side and 24 for a
    /// filled block of two by two by two.</summary>
    [Theory]
    [InlineData(2, 1, 1, 10)]
    [InlineData(2, 2, 2, 24)]
    [InlineData(1, 1, 1, 6)]
    public void OnlyTheFacesThatBorderEmptySpace_AreDrawn(int nx, int ny, int nz, int faces)
    {
        var block = new bool[nx, ny, nz];
        for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
                for (int z = 0; z < nz; z++)
                    block[x, y, z] = true;

        Assert.Equal(faces, FaceFillsOf(ax => ax.Voxels(block)).Count);
    }
}
