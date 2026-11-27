// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>How large an image is shown, against how many values it holds: what matplotlib's <c>'auto'</c> and <c>'antialiased'</c>
/// interpolation decide on, and the grid an image is resampled to.</summary>
/// <param name="Width">The width, in pixels, the image is shown at.</param>
/// <param name="Height">The height, in pixels, the image is shown at.</param>
/// <param name="Rows">How many rows of values the image holds.</param>
/// <param name="Columns">How many columns of values the image holds.</param>
internal readonly record struct ImageFootprint(double Width, double Height, int Rows, int Columns)
{
    /// <summary>The most cells along one side of the grid a smoothed image is drawn as.</summary>
    private const int MostCells = 256;

    /// <summary>Whether matplotlib draws the image unfiltered under <c>'auto'</c> (<c>matplotlib.image._resample</c>, 3.11.2): it is shown
    /// more than 3 times, 1 time or 2 times the size of its values in both directions. Compared exactly, as matplotlib compares.</summary>
    internal bool IsUnfiltered => Whole(Width, Columns) && Whole(Height, Rows);

    /// <summary>Whether matplotlib's <c>'auto'</c> interpolation stage interpolates the image's colours rather than its values
    /// (<c>_ImageBase._make_image</c>, 3.11.2: <c>if (dispx &lt; 3) or (dispy &lt; 3): interpolation_stage = 'rgba'</c>): it is shown at less
    /// than 3 times the size of its values across or down, a downsample included.</summary>
    internal bool InterpolatesColours => Width / Columns < 3 || Height / Rows < 3;

    /// <summary>The engine <c>'auto'</c> and <c>'antialiased'</c> choose: <c>"nearest"</c> where <see cref="IsUnfiltered"/>, else <c>"hanning"</c>.</summary>
    internal string AutoEngineName => IsUnfiltered ? NearestInterpolation.Instance.Name : HanningInterpolation.Instance.Name;

    /// <summary>The number of rows and columns the image is resampled to under <paramref name="engine"/>: its own for nearest, the
    /// pixels it is shown in (up to <see cref="MostCells"/>) for a filter that follows the screen's grid, four times its own (up to the
    /// same) for any other.</summary>
    /// <param name="engine">The engine the image is resampled with.</param>
    /// <returns>The number of rows and columns of the grid the image is resampled to.</returns>
    internal ResampledGrid GridFor(IInterpolationEngine engine) => engine switch
    {
        NearestInterpolation => new ResampledGrid(Rows, Columns),
        HanningInterpolation => new ResampledGrid(Math.Clamp((int)Math.Round(Height), 1, MostCells), Math.Clamp((int)Math.Round(Width), 1, MostCells)),
        _ => new ResampledGrid(Math.Min(Rows * 4, MostCells), Math.Min(Columns * 4, MostCells)),
    };

    private static bool Whole(double shown, int values) => shown > 3 * values || shown == values || shown == 2 * values;
}

/// <summary>The size of the grid an image is resampled to.</summary>
/// <param name="Rows">The number of rows.</param>
/// <param name="Columns">The number of columns.</param>
internal readonly record struct ResampledGrid(int Rows, int Columns);
