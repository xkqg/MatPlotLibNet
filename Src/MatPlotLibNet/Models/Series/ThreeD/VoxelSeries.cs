// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D voxel plot where filled cubes are placed according to a boolean mask.</summary>
public sealed class VoxelSeries : ChartSeries, IHasColor, IHasAlpha
{
    /// <summary>3D boolean mask indicating which voxels are filled.
    /// Dimensions are [X, Y, Z] with ranges derived from array dimensions.</summary>
    public bool[,,] Filled { get; }

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.8;

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
            return new(null, null, null, null);

        return new(0, xDim, 0, yDim, ZMin: 0, ZMax: zDim);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "voxels",
        VoxelData = FilledToList(),
        Color = Color,
        Alpha = Alpha != 0.8 ? Alpha : null,
        Label = Label
    };

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
