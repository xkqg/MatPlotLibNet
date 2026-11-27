// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>The faces of a surface as far as the camera does not move them: for every drawn patch its colour after the
/// light, and, made the first time a draw asks for it, the vertex data the browser turns it with. Turning the surface moves
/// where each face lands and the order faces are painted in, and nothing here, so a surface drawn again under a new camera
/// reuses these and only projects and sorts.</summary>
/// <remarks>A face is one patch over a block of the grid, as matplotlib's <c>plot_surface</c> draws it: the block between two
/// drawn rows and two drawn columns, walked along its perimeter, coloured by the mean height of the perimeter's points over
/// the range from the lowest to the highest mean of the patches drawn (not the range of the data). With
/// a stride of 1 every block is one cell and its perimeter its four corners. The faces are made from a copy of what they
/// depend on: the grid and the heights, the strides, the colour map, the normalizer, the light and the data box. A draw
/// compares that copy with what it is about to draw and makes the faces again on the first difference, so a height changed
/// in place is drawn as a fresh surface would draw it. The grid and the heights are compared bit for bit. Whether a draw
/// writes vertex data is not part of the copy: an SVG turned in the browser writes it and a native draw does not, and the
/// two in turn share one set of faces.</remarks>
internal sealed class SurfaceFaces
{
    /// <summary>One patch of the grid: the block from row <see cref="Row"/> to <see cref="RowEnd"/> and column
    /// <see cref="Col"/> to <see cref="ColEnd"/>, whether its data can be drawn at all and the colour it is filled with.</summary>
    internal readonly record struct Face(int Row, int Col, int RowEnd, int ColEnd, bool Finite, Color Fill)
    {
        /// <summary>How many grid points lie on the block's edge: twice the rows and the columns it spans.</summary>
        internal int PerimeterLength => 2 * ((RowEnd - Row) + (ColEnd - Col));

        /// <summary>Writes the points on the block's edge into <paramref name="into"/> (at least
        /// <see cref="PerimeterLength"/> long) in the order matplotlib's <c>cbook._array_perimeter</c> walks it: the first
        /// row left to right, the last column top to bottom, the last row right to left, the first column bottom to top.
        /// For one cell: its corners (r, c), (r, c+1), (r+1, c+1), (r+1, c).</summary>
        /// <returns>How many points were written: <see cref="PerimeterLength"/>.</returns>
        internal int Perimeter(Span<GridPoint> into)
        {
            int k = 0;
            for (int c = Col; c < ColEnd; c++) into[k++] = new(Row, c);
            for (int r = Row; r < RowEnd; r++) into[k++] = new(r, ColEnd);
            for (int c = ColEnd; c > Col; c--) into[k++] = new(RowEnd, c);
            for (int r = RowEnd; r > Row; r--) into[k++] = new(r, Col);
            return k;
        }
    }

    /// <summary>A point of the grid, by its row and column.</summary>
    internal readonly record struct GridPoint(int Row, int Col);

    /// <summary>The rows and the columns one patch spans.</summary>
    internal readonly record struct Strides(int Row, int Col)
    {
        // plot_surface: rstride = kwargs.pop('rstride', 10), cstride likewise; rcount = kwargs.pop('rcount', 50).
        private const int GivenElsewhere = 10;
        private const int MostPatchesASide = 50;

        /// <summary>The strides matplotlib's <c>plot_surface</c> draws <paramref name="series"/> with (matplotlib 3.11,
        /// mpl_toolkits/mplot3d/axes3d.py): a stride the author set (above 0) is kept, and when either is set the other is
        /// 10; with neither set, <paramref name="themeStride"/> (classic mode: strides over counts) or, without one, as many
        /// rows and columns as keep each side to 50 patches: <c>int(max(np.ceil(rows / rcount), 1))</c>.</summary>
        internal static Strides Of(SurfaceSeries series, int? themeStride)
        {
            if (series.RowStride > 0 || series.ColStride > 0)
                return new(series.RowStride > 0 ? series.RowStride : GivenElsewhere,
                    series.ColStride > 0 ? series.ColStride : GivenElsewhere);
            if (themeStride is { } stride) return new(stride, stride);
            return new(Math.Max(1, (series.Z.GetLength(0) + MostPatchesASide - 1) / MostPatchesASide),
                Math.Max(1, (series.Z.GetLength(1) + MostPatchesASide - 1) / MostPatchesASide));
        }
    }

    // The grid and the heights are arrays the series holds for its whole life, so their lengths never change; only
    // their values can, and those are what is copied.
    private readonly double[] _x, _y, _z;
    private readonly Strides _strides;
    private readonly ColorMapping _mapping;
    private readonly ILightSource? _light;
    private readonly Box3D _box;
    private string?[]? _vertexData;

    /// <summary>The patches in the order the grid is walked: row by row, block by block.</summary>
    internal Face[] Faces { get; }

    private SurfaceFaces(SurfaceSeries series, Projection3D projection, FaceInputs inputs)
    {
        _x = (double[])series.X.Clone();
        _y = (double[])series.Y.Clone();
        _z = HeightsOf(series).ToArray();
        _strides = inputs.Strides;
        _mapping = inputs.Mapping;
        _light = inputs.Light;
        _box = projection.DataBox;
        Faces = Make(series, inputs);
    }

    /// <summary>What a draw asks the faces to be made with, apart from the data itself: the strides, how the means become colours (the
    /// series' <c>ColorMappingUnder</c>) and the light.</summary>
    internal readonly record struct FaceInputs(Strides Strides, ColorMapping Mapping, ILightSource? Light);

    /// <summary>The faces <paramref name="kept"/> holds when they were made from exactly this data and these inputs, or
    /// newly made faces otherwise.</summary>
    internal static SurfaceFaces For(SurfaceFaces? kept, SurfaceSeries series, Projection3D projection, FaceInputs inputs) =>
        kept is not null && kept.Matches(series, projection, inputs) ? kept : new SurfaceFaces(series, projection, inputs);

    private bool Matches(SurfaceSeries series, Projection3D projection, FaceInputs inputs) =>
        inputs.Strides == _strides
        && ReferenceEquals(inputs.Mapping.Map, _mapping.Map) && ReferenceEquals(inputs.Mapping.Normalizer, _mapping.Normalizer)
        && Equals(inputs.Mapping.Look, _mapping.Look)
        && Equals(inputs.Light, _light) && projection.DataBox.Equals(_box)
        && SameBits(series.X, _x) && SameBits(series.Y, _y) && SameBits(HeightsOf(series), _z);

    private static ReadOnlySpan<double> HeightsOf(SurfaceSeries series) =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, double>(ref MemoryMarshal.GetArrayDataReference(series.Z)), series.Z.Length);

    private static bool SameBits(ReadOnlySpan<double> now, ReadOnlySpan<double> then) =>
        MemoryMarshal.Cast<double, long>(now).SequenceEqual(MemoryMarshal.Cast<double, long>(then));

    /// <summary>The vertex data the browser turns each face with, in the order of <see cref="Faces"/> (<see langword="null"/>
    /// for a patch that is not drawn): the points of its perimeter placed in the data box, which the camera does not move.
    /// Made the first time a draw asks for it and kept with the faces.</summary>
    internal string?[] VertexData(SurfaceSeries series, Projection3D proj) => _vertexData ??= MakeVertexData(series, proj, Faces);

    /// <summary>The longest perimeter a walk is written for without a heap buffer: a block of up to 16 by 16 cells.</summary>
    internal const int MostOnTheStack = 64;

    /// <summary>The most points any of <paramref name="faces"/> has on its edge: the first block's, as only the blocks
    /// ending on the last row or column are smaller. A surface has faces only on a grid of at least 2 by 2, so there is a
    /// first.</summary>
    internal static int LongestPerimeter(Face[] faces) => faces[0].PerimeterLength;

    private static string?[] MakeVertexData(SurfaceSeries series, Projection3D proj, Face[] faces)
    {
        var x = series.X;
        var y = series.Y;
        var z = series.Z;
        var data = new string?[faces.Length];
        int longest = LongestPerimeter(faces);
        Span<GridPoint> walk = longest <= MostOnTheStack ? stackalloc GridPoint[MostOnTheStack] : new GridPoint[longest];
        // A face's wire is its points, each followed by a space but the last, written into one buffer the whole surface shares.
        const int PointRoom = Normalized3DPoint.PointLength + 1;
        Span<char> text = longest <= MostOnTheStack ? stackalloc char[MostOnTheStack * PointRoom] : new char[longest * PointRoom];
        for (int i = 0; i < faces.Length; i++)
        {
            if (!faces[i].Finite) continue;
            int written = 0;
            for (int k = 0, n = faces[i].Perimeter(walk); k < n; k++)
            {
                var p = walk[k];
                var v = proj.Normalize(x[p.Col], y[p.Row], z[p.Row, p.Col]);
                if (k > 0) text[written++] = ' ';
                written += v.FormatWire(text[written..], Normalized3DPoint.MarkDigits);
            }
            data[i] = new string(text[..written]);
        }

        return data;
    }

    /// <summary>The range the patches of <paramref name="series"/> are coloured over when they are mapped by <paramref name="mapping"/>:
    /// the means of the patches drawn at the theme's strides (<see cref="Strides.Of"/>), the colour range of a surface.</summary>
    /// <param name="series">The surface.</param>
    /// <param name="mapping">The map, the normaliser and the theme the surface is drawn under.</param>
    /// <returns>The range.</returns>
    internal static Numerics.MinMaxRange ColourRangeOf(SurfaceSeries series, ColorMapping mapping) =>
        mapping.RangeOf(Walk(series, Strides.Of(series, mapping.Theme.Looks.Cube.SurfaceStride), mapping.RangeScale.LowestPlaced,
            out _, out _));

    /// <summary>Walks the patches of <paramref name="series"/> at <paramref name="strides"/>: each patch, whether it can be drawn, and the
    /// mean height of the points on its edge (<c>avg_z = ps[:, 2].mean()</c>, which picks its colour).</summary>
    /// <param name="series">The surface.</param>
    /// <param name="strides">The rows and columns a patch spans.</param>
    /// <param name="lowest">The lowest mean the colour scale places (matplotlib's LogNorm takes the smallest value above 0 as vmin).</param>
    /// <param name="faces">The patches, uncoloured.</param>
    /// <param name="means">The mean height of each patch that can be drawn.</param>
    /// <returns>The range of the means the scale places: <c>polyc.set_array(avg_z)</c> with no limits given autoscales the norm over the
    /// means of the patches drawn, so the lowest patch takes the map's first colour and the highest its last; <see langword="null"/>
    /// when none.</returns>
    private static Range1D? Walk(SurfaceSeries series, Strides strides, double lowest, out Face[] faces, out double[] means)
    {
        var x = series.X;
        var y = series.Y;
        var z = series.Z;
        int lastRow = z.GetLength(0) - 1, lastCol = z.GetLength(1) - 1;
        int rs = strides.Row, cs = strides.Col;
        // row_inds = list(range(0, rows-1, rstride)) + [rows-1]: one block from each drawn row to the next, the last
        // ending on the last row.
        faces = new Face[((lastRow + rs - 1) / rs) * ((lastCol + cs - 1) / cs)];
        means = new double[faces.Length];
        var range = Range1D.Empty;
        int longest = 2 * (Math.Min(rs, lastRow) + Math.Min(cs, lastCol));
        Span<GridPoint> walk = longest <= MostOnTheStack ? stackalloc GridPoint[MostOnTheStack] : new GridPoint[longest];
        int f = 0;
        for (int r = 0; r < lastRow; r += rs)
            for (int c = 0; c < lastCol; c += cs, f++)
            {
                var face = new Face(r, c, Math.Min(r + rs, lastRow), Math.Min(c + cs, lastCol), true, default);
                int n = face.Perimeter(walk);
                double sum = 0;
                bool finite = true;
                for (int k = 0; k < n && finite; k++)
                {
                    var p = walk[k];
                    double h = z[p.Row, p.Col];
                    // A patch with a value that is not finite projects to a point that cannot be placed, and is never drawn.
                    finite = double.IsFinite(x[p.Col]) && double.IsFinite(y[p.Row]) && double.IsFinite(h);
                    sum += h;
                }

                faces[f] = finite ? face : face with { Finite = false };
                if (!finite) continue;
                means[f] = sum / n;
                range = range.IncludingFrom(means[f], lowest);
            }

        return range.OrNone();
    }

    private static Face[] Make(SurfaceSeries series, FaceInputs inputs)
    {
        var x = series.X;
        var y = series.Y;
        var z = series.Z;
        var range = Walk(series, inputs.Strides, inputs.Mapping.RangeScale.LowestPlaced, out var faces, out var means);
        var scale = inputs.Mapping.Over(inputs.Mapping.RangeOf(range));
        for (int f = 0; f < faces.Length; f++)
        {
            if (!faces[f].Finite) continue;
            var fill = scale.ColorOf(means[f]);
            if (inputs.Light is { } light)
            {
                int r = faces[f].Row, c = faces[f].Col, r1 = faces[f].RowEnd, c1 = faces[f].ColEnd;
                var (nx, ny, nz) = Vec3.FaceNormal(new(x[c], y[r], z[r, c]), new(x[c1], y[r], z[r, c1]), new(x[c], y[r1], z[r1, c]));
                fill = light.Shade(fill, nx, ny, nz);
            }

            faces[f] = faces[f] with { Fill = fill };
        }

        return faces;
    }
}