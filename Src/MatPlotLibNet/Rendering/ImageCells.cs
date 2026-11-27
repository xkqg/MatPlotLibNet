// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>The coloured cells of an image: the colour scale its values are coloured by and the grid of cells that fills a
/// rectangle, one fill of the whole grid on a surface that paints a pixel grid under a theme that asks for one, a rectangle for each
/// cell on any other. The one owner of the grid, so an image series (<c>ImageSeriesRenderer</c>, scaled to its plot
/// area) and an image placed on the figure (<see cref="Models.FigureImage"/>, one pixel for each cell) colour and draw their
/// cells by the same rule.</summary>
/// <param name="Scale">The colour scale the values are coloured by: the image's map, normaliser and colour range, the series' or the
/// figure image's <c>ColorScaleUnder</c> (the values it spans, a missing value and an infinity left out, unless a limit is given; a
/// range of no width opened one wide).</param>
/// <param name="Alpha">The opacity of the whole image; below 1 the cells are drawn under that opacity.</param>
internal readonly record struct ImageCells(ColormapContext Scale, double Alpha)
{
    /// <summary>Draws <paramref name="pixels"/> as a grid of rectangles that fills <paramref name="bounds"/>, row by row from
    /// the top, each in the colour its value has under the colour range. A missing value takes the map's bad colour, transparent
    /// when it names none. A grid with no row or no column draws nothing, not even the opacity.</summary>
    /// <param name="ctx">The surface to draw on.</param>
    /// <param name="pixels">The values to draw, indexed [row, column]; the cells that are drawn, which may be a resampled copy of the
    /// values the range was taken from.</param>
    /// <param name="bounds">The rectangle the whole grid fills; each cell is its share of it.</param>
    /// <param name="skipTransparentCells">Whether a cell that would be painted fully transparent (a missing value under a map
    /// that names no bad colour, or any value that maps to a clear colour) is left out instead of drawn as an invisible rectangle.
    /// An image series draws it, as it always has; an image at its native size, with an element for each pixel, leaves it out.</param>
    internal void Draw(IRenderContext ctx, double[,] pixels, Rect bounds, bool skipTransparentCells = false)
    {
        int rows = pixels.GetLength(0), cols = pixels.GetLength(1);
        if (rows == 0 || cols == 0) return;
        DrawCells(ctx, new Values(this, pixels), new Point(bounds.X, bounds.Y), bounds.Width / cols, bounds.Height / rows, skipTransparentCells);
    }

    /// <summary>Draws <paramref name="pixels"/> as a grid of rectangles from the outer corner of the first cell, <paramref name="first"/>,
    /// to the outer corner of the last, <paramref name="last"/>: the first row and the first column stand at <paramref name="first"/>,
    /// so a grid whose last corner lies above or left of its first (an axis that runs up or to the left) is drawn turned that way, each
    /// cell still a rectangle of positive size. Otherwise as <see cref="Draw(IRenderContext, double[,], Rect, bool)"/>.</summary>
    /// <param name="ctx">The surface to draw on.</param>
    /// <param name="pixels">The values to draw, indexed [row, column].</param>
    /// <param name="first">Where the outer corner of the cell [0, 0] stands, in pixels.</param>
    /// <param name="last">Where the outer corner of the last cell stands, in pixels.</param>
    internal void Draw(IRenderContext ctx, double[,] pixels, Point first, Point last) => DrawBetween(ctx, new Values(this, pixels), first, last);

    /// <summary>Draws <paramref name="data"/> as an image scaled to stand from <paramref name="first"/> to <paramref name="last"/>, as
    /// matplotlib's <c>AxesImage</c> draws it: resampled by the interpolation its author named, else the theme's
    /// (<see cref="ImageLook.InterpolationOver"/>), <c>'auto'</c> and <c>'antialiased'</c> choosing by how large the image is shown
    /// (<see cref="ImageFootprint.AutoEngineName"/>); its values resampled and each coloured, or, where the look interpolates in colour at
    /// this size (<see cref="ImageLook.InterpolatesColours"/>), its colours resampled. The one rule an image series and a spectrogram
    /// drawn as an image are scaled by.</summary>
    /// <param name="ctx">The surface to draw on.</param>
    /// <param name="data">The values, indexed [row, column].</param>
    /// <param name="interpolation">The interpolation the author named; <see langword="null"/> for the theme's.</param>
    /// <param name="look">The theme's image look.</param>
    /// <param name="first">Where the outer corner of the cell [0, 0] stands, in pixels.</param>
    /// <param name="last">Where the outer corner of the last cell stands, in pixels.</param>
    internal void DrawScaled(IRenderContext ctx, double[,] data, string? interpolation, ImageLook look, Point first, Point last)
    {
        var footprint = new ImageFootprint(Math.Abs(last.X - first.X), Math.Abs(last.Y - first.Y), data.GetLength(0), data.GetLength(1));
        string name = look.InterpolationOver(interpolation);
        if (name.Equals("auto", StringComparison.OrdinalIgnoreCase) || name.Equals("antialiased", StringComparison.OrdinalIgnoreCase))
            name = footprint.AutoEngineName;
        var engine = InterpolationRegistry.Get(name) ?? NearestInterpolation.Instance;
        var grid = footprint.GridFor(engine);

        if (look.InterpolatesColours(footprint, engine))
            DrawResampledColours(ctx, data, engine, grid, first, last);
        else
            Draw(ctx, IsItsOwn(engine, grid, data) ? data : engine.Resample(data, grid.Rows, grid.Columns), first, last);

        // Nearest at the values' own size gives every value back as it is: the values are drawn, with no copy made of them.
        static bool IsItsOwn(IInterpolationEngine engine, ResampledGrid grid, double[,] data) =>
            engine is NearestInterpolation && grid.Rows == data.GetLength(0) && grid.Columns == data.GetLength(1);
    }

    /// <summary>Draws <paramref name="data"/> with its colours resampled to <paramref name="grid"/>, as matplotlib interpolates an image in
    /// its <c>'rgba'</c> stage (<c>_ImageBase._make_image</c>, 3.11.2): every value coloured first, each channel multiplied by the colour's
    /// opacity, the four channels resampled by <paramref name="engine"/>, then divided by the opacity again and cut to bytes
    /// (<see cref="ColourSample"/>). A blend of two colours is the colour between them, never the colour the map gives the value between
    /// them. A built-in engine resamples the four channels in one pass (<see cref="IColourResampler"/>); any other is asked channel by
    /// channel. The samples are borrowed for the draw and read straight into it; laid from the outer corner of the first cell to that of
    /// the last, as <see cref="Draw(IRenderContext, double[,], Point, Point)"/> lays them.</summary>
    /// <param name="ctx">The surface to draw on.</param>
    /// <param name="data">The values, indexed [row, column]: the image's own.</param>
    /// <param name="engine">The engine the colours are resampled with.</param>
    /// <param name="grid">The number of rows and columns to resample to.</param>
    /// <param name="first">Where the outer corner of the cell [0, 0] stands, in pixels.</param>
    /// <param name="last">Where the outer corner of the last cell stands, in pixels.</param>
    internal void DrawResampledColours(IRenderContext ctx, double[,] data, IInterpolationEngine engine, ResampledGrid grid, Point first, Point last)
    {
        int rows = data.GetLength(0), columns = data.GetLength(1);
        var source = _sourceSamples is { } keptSource && keptSource.Length >= rows * columns ? keptSource : _sourceSamples = new ColourSample[rows * columns];
        var target = _targetSamples is { } keptTarget && keptTarget.Length >= grid.Rows * grid.Columns ? keptTarget : _targetSamples = new ColourSample[grid.Rows * grid.Columns];
        try
        {
            var values = data.AsRows();
            for (int i = 0; i < values.Length; i++)
                source[i] = ColourSample.Premultiplied(Scale.ColorOf(values[i]));

            var from = source.AsSpan(0, rows * columns);
            var into = target.AsSpan(0, grid.Rows * grid.Columns);
            if (engine is IColourResampler resampler) resampler.ResampleColours(from, rows, columns, into, grid.Rows, grid.Columns);
            else ResampleByChannel(engine, from, rows, columns, into, grid);

            DrawBetween(ctx, new ResampledColourCells(target, grid.Rows, grid.Columns), first, last);
        }
        finally
        {

        }
    }

    /// <summary>Resamples the colours of an engine that resamples values only: each of the four channels as a grid of its own, through the
    /// public <see cref="IInterpolationEngine.Resample"/>, the same arithmetic lane by lane as an engine that resamples them together.</summary>
    private static void ResampleByChannel(IInterpolationEngine engine, ReadOnlySpan<ColourSample> source, int rows, int columns,
        Span<ColourSample> target, ResampledGrid grid)
    {
        var channels = new double[4][,];
        for (int lane = 0; lane < 4; lane++)
        {
            var channel = new double[rows, columns];
            var cells = channel.AsRows();
            for (int i = 0; i < cells.Length; i++) cells[i] = source[i].Channels[lane];
            channels[lane] = engine.Resample(channel, grid.Rows, grid.Columns);
        }

        Span<double> lanes = stackalloc double[4];
        for (int i = 0; i < target.Length; i++)
        {
            for (int lane = 0; lane < 4; lane++) lanes[lane] = channels[lane].AsRows()[i];
            target[i] = new ColourSample(System.Runtime.Intrinsics.Vector256.Create(lanes[0], lanes[1], lanes[2], lanes[3]));
        }
    }
    /// <summary>Draws <paramref name="cells"/> from the outer corner of the first cell to that of the last; a grid with no row or no
    /// column draws nothing.</summary>
    private void DrawBetween<TCells>(IRenderContext ctx, TCells cells, Point first, Point last) where TCells : struct, ICellColours
    {
        if (cells.Rows == 0 || cells.Columns == 0) return;
        DrawCells(ctx, cells, first, (last.X - first.X) / cells.Columns, (last.Y - first.Y) / cells.Rows, skipTransparentCells: false);
    }

    /// <summary>The one loop every form draws by: the cell [r, c] starts <c>c</c> cell widths and <c>r</c> cell heights from
    /// <paramref name="origin"/>; a negative width or height lays the cells the other way from it. A surface that fills a grid of
    /// cells in one pass (<see cref="RenderContextPixelGridExtensions"/>: a raster export under a theme that puts marks on its pixel
    /// grid) is handed the whole grid at once, as matplotlib draws an image, so no two cells blend where they meet; every other surface
    /// is handed a rectangle for each cell. Generic over where the colours come from, so a cell costs no call through an interface.</summary>
    private void DrawCells<TCells>(IRenderContext ctx, TCells cells, Point origin, double cellW, double cellH, bool skipTransparentCells)
        where TCells : struct, ICellColours
    {
        int rows = cells.Rows, cols = cells.Columns;
        double width = Math.Abs(cellW), height = Math.Abs(cellH);
        double leftShift = cellW < 0 ? cellW : 0, topShift = cellH < 0 ? cellH : 0;

        using var opacity = ctx.WithOpacityBelowFull(Alpha);

        if (ctx.FillsCellGrids)
        {
            FillAtOnce(ctx, cells, new Rect(origin.X + Math.Min(cols * cellW, 0), origin.Y + Math.Min(rows * cellH, 0), cols * width, rows * height),
                mirrorColumns: cellW < 0, mirrorRows: cellH < 0);
            return;
        }

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var color = cells.At(r, c);
            if (skipTransparentCells && color.IsNone) continue;

            ctx.DrawRectangle(
                new Rect(origin.X + c * cellW + leftShift, origin.Y + r * cellH + topShift, width, height),
                ShapeStyle.Filled(color));
        }
    }

    /// <summary>Hands the whole grid to a surface that fills it in one pass: every cell's colour, row by row from the top of
    /// <paramref name="area"/> and each row from its left, the cells of a grid laid the other way (<paramref name="mirrorColumns"/>,
    /// <paramref name="mirrorRows"/>) read from its far end. The colours are written into a buffer borrowed for the draw.</summary>
    private static void FillAtOnce<TCells>(IRenderContext ctx, TCells cells, Rect area, bool mirrorColumns, bool mirrorRows)
        where TCells : struct, ICellColours
    {
        int rows = cells.Rows, cols = cells.Columns;
        var buffer = System.Buffers.ArrayPool<Color>.Shared.Rent(rows * cols);
        try
        {
            for (int shownRow = 0; shownRow < rows; shownRow++)
            {
                int r = mirrorRows ? rows - 1 - shownRow : shownRow;
                int start = shownRow * cols;
                for (int shownColumn = 0; shownColumn < cols; shownColumn++)
                {
                    int c = mirrorColumns ? cols - 1 - shownColumn : shownColumn;
                    buffer[start + shownColumn] = cells.At(r, c);
                }
            }

            ctx.FillCellGrid(buffer.AsSpan(0, rows * cols), cols, area);
        }
        finally
        {
            System.Buffers.ArrayPool<Color>.Shared.Return(buffer);
        }
    }

    /// <summary>Where the colours of a grid of cells come from: the size of the grid and the colour of each cell.</summary>
    private interface ICellColours
    {
        /// <summary>The number of rows.</summary>
        int Rows { get; }

        /// <summary>The number of columns.</summary>
        int Columns { get; }

        /// <summary>The colour of the cell [<paramref name="row"/>, <paramref name="column"/>].</summary>
        Color At(int row, int column);
    }

    /// <summary>Values, each coloured under the cells' colour range as it is drawn; a missing value takes the map's bad colour.</summary>
    private readonly struct Values(ImageCells cells, double[,] pixels) : ICellColours
    {
        public int Rows => pixels.GetLength(0);

        public int Columns => pixels.GetLength(1);

        public Color At(int row, int column) => cells.Scale.ColorOf(pixels[row, column]);
    }

    /// <summary>The grid of samples a thread resamples an image's colours from, kept for its next image and grown when one needs more.</summary>
    [ThreadStatic] private static ColourSample[]? _sourceSamples;

    /// <summary>The grid of samples a thread resamples an image's colours into, kept and grown as <see cref="_sourceSamples"/>.</summary>
    [ThreadStatic] private static ColourSample[]? _targetSamples;

    /// <summary>Colours resampled premultiplied, taken back cell by cell as they are drawn (<see cref="ColourSample.Unpremultiplied"/>).</summary>
    private readonly struct ResampledColourCells(ColourSample[] samples, int rows, int columns) : ICellColours
    {
        public int Rows => rows;

        public int Columns => columns;

        public Color At(int row, int column) => samples[row * columns + column].Unpremultiplied();
    }
}