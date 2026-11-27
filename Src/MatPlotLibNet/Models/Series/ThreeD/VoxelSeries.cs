// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D voxel plot where filled cubes are placed according to a boolean mask.</summary>
public sealed class VoxelSeries : ChartSeries, IHasColor, IHasAlpha, IEdgedPatch, ISeriesKind<VoxelSeries>
{
    /// <summary>Under the library's own themes a face is outlined by a faint black hairline (80 of 255); under the matplotlib themes the
    /// theme decides, as it does for any patch (matplotlib's <c>voxels</c> is a <c>Poly3DCollection</c> per voxel with no edge colour of its
    /// own: black at 1 pt under the classic style's <c>patch.force_edgecolor</c>, none under the default style).</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.HalfPixel(PatchLook.RestingSurfaceEdgeColor);

    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.ThreeD;

    /// <inheritdoc />
    /// <remarks><c>Axes3D.voxels</c>, which asks the patch cycle and makes a <c>Poly3DCollection</c> of each voxel.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.PatchFaces3D;

    /// <summary>3D boolean mask indicating which voxels are filled.
    /// Dimensions are [X, Y, Z] with ranges derived from array dimensions.</summary>
    public bool[,,] Filled { get; }

    /// <summary>Fill color for the rendered cube faces.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    private const double RestingAlpha = 0.8;
    /// <summary>Opacity of the cube faces. Range [0, 1], default <see cref="RestingAlpha"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    /// <summary>Initializes a new voxel series with the specified 3D boolean mask.</summary>
    public VoxelSeries(bool[,,] filled)
    {
        Filled = filled;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int xDim = Filled.GetLength(0);
        int yDim = Filled.GetLength(1);
        int zDim = Filled.GetLength(2);

        if (xDim == 0 || yDim == 0 || zDim == 0)
            return default;

        return new(0, xDim, 0, yDim, ZMin: 0, ZMax: zDim);
    }

    static string ISeriesKind<VoxelSeries>.Kind => "voxels";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        VoxelData = FilledToList(),
        Color = Color,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Label = Label
    };

    /// <summary>The reader of a <see cref="VoxelSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static VoxelSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var filled = VoxelDataToArray(dto.VoxelData);
        var s = axes.Voxels(filled);
        if (dto.Color is { } color) s.Color = color;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        return s;
    }

    /// <summary>Converts a nested boolean list from JSON back to a rectangular 3D mask array.</summary>
    private static bool[,,] VoxelDataToArray(List<List<List<bool>>>? data)
    {
        if (data is null || data.Count == 0)
            return new bool[1, 1, 1];

        int xDim = data.Count;
        int yDim = data[0].Count;
        int zDim = yDim > 0 ? data[0][0].Count : 0;

        var result = new bool[xDim, yDim, zDim];
        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                for (int z = 0; z < zDim; z++)
                    result[x, y, z] = data[x][y][z];

        return result;
    }


    /// <inheritdoc />
    /// <remarks>One row per FILLED cell: an empty cell is the absence of data, and a table of mostly-false
    /// would bury the shape it is supposed to describe.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        for (int x = 0; x < Filled.GetLength(0); x++)
        {
            for (int y = 0; y < Filled.GetLength(1); y++)
            {
                for (int z = 0; z < Filled.GetLength(2); z++)
                {
                    if (Filled[x, y, z])
                    {
                        rows.Add([DataCell.FromNumber(x), DataCell.FromNumber(y), DataCell.FromNumber(z)]);
                    }
                }
            }
        }

        return new ChartDataTable(null, [ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.Y), new("z")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Converts the 3D boolean mask to a nested list structure for JSON serialization.</summary>
    private List<List<List<bool>>> FilledToList()
    {
        int xDim = Filled.GetLength(0);
        int yDim = Filled.GetLength(1);
        int zDim = Filled.GetLength(2);
        var result = new List<List<List<bool>>>(xDim);
        for (int x = 0; x < xDim; x++)
        {
            var plane = new List<List<bool>>(yDim);
            for (int y = 0; y < yDim; y++)
            {
                var row = new List<bool>(zDim);
                for (int z = 0; z < zDim; z++) row.Add(Filled[x, y, z]);
                plane.Add(row);
            }
            result.Add(plane);
        }
        return result;
    }
}
