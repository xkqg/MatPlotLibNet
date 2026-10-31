// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="FigureImage"/>, an entry of the figure's <c>images</c>: its pixels (a list of
/// rows, a missing cell as the string <c>"NaN"</c>), its place as pixel offsets from the bottom-left corner, and its colouring.
/// The colour map travels as a series' does (<see cref="ColorMapDescriptionExtensions"/>: by name when the library ships it, as
/// what it is made of when it was built in code) and so does the normaliser (<see cref="NormalizableExtensions"/>); each setting
/// is written only when it differs from a new image's.</summary>
internal sealed record FigureImageDto
{
    /// <summary>The pixel values as a list of rows, the first row being the top row of the image.</summary>
    public List<List<double>>? Data { get; init; }

    /// <summary>The distance in pixels from the left edge of the figure to the left edge of the image.</summary>
    public double XOffset { get; init; }

    /// <summary>The distance in pixels from the bottom edge of the figure to the bottom edge of the image.</summary>
    public double YOffset { get; init; }

    /// <summary>The name of the colour map when it is one the library ships (or one registered under its name); absent for the
    /// default colour map and for a map built in code.</summary>
    public string? ColorMapName { get; init; }

    /// <summary>The colour map when it was built in code, described by what it is made of.</summary>
    public ColorMapDto? ColorMap { get; init; }

    /// <summary>How a value is mapped onto the colour map, written by kind; absent for the linear mapping.</summary>
    public NormalizerDto? Normalizer { get; init; }

    /// <summary>The value at the low end of the colour range; absent for the smallest value.</summary>
    public double? VMin { get; init; }

    /// <summary>The value at the high end of the colour range; absent for the largest value.</summary>
    public double? VMax { get; init; }

    /// <summary>The opacity of the whole image; absent for 1.</summary>
    public double? Alpha { get; init; }

    private static readonly FigureImage Rest = new(new double[0, 0], 0, 0);

    /// <summary>The JSON's form of <paramref name="image"/>.</summary>
    internal static FigureImageDto From(FigureImage image) => new()
    {
        Data = image.Data.To2DList(),
        XOffset = image.XOffset,
        YOffset = image.YOffset,
        ColorMapName = image.ColorMap.NameForJson(),
        ColorMap = image.ColorMap.DescriptionForJson(),
        Normalizer = image.Normalizer.DescribeForJson(),
        VMin = image.VMin,
        VMax = image.VMax,
        Alpha = image.Alpha.OrNullAtRest(Rest.Alpha),
    };

    /// <summary>Adds the image this record describes to <paramref name="figure"/> and returns it. A record without pixels is an
    /// image of none, which draws nothing.</summary>
    /// <param name="figure">The figure the image is placed on.</param>
    /// <exception cref="ArgumentException">The rows of <see cref="Data"/> differ in length (the parameter is <c>data</c>), or
    /// the colour map's description cannot make a map (the exception names the field).</exception>
    internal FigureImage AddTo(Figure figure)
    {
        if (Data is { Count: > 0 } rows && rows.Any(row => row.Count != rows[0].Count))
        {
            throw new ArgumentException("The rows of an image must all have the same number of values.", "data");
        }

        var image = figure.AddImage(Data.From2DList(), XOffset, YOffset);
        image.ColorMap = ColorMap.ReadColorMap(ColorMapName);
        image.Normalizer = Normalizer?.Restore();
        image.VMin = VMin;
        image.VMax = VMax;
        image.Alpha = Alpha ?? Rest.Alpha;
        return image;
    }
}
