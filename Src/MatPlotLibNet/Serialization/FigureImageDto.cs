// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="FigureImage"/>, an entry of the figure's <c>images</c>: its pixels (a list of
/// rows, a missing cell as the string <c>"NaN"</c>), its place as pixel offsets from the bottom-left corner, and its colouring.
/// The colour scale travels as a series' does (<see cref="ColorScaleWire"/>: the map by name when the library ships it, as
/// what it is made of when it was built in code, and the normaliser by kind); each setting
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

    /// <summary>The image's colour scale (its map and its normaliser) as the one value that sits in the three fields that carry it
    /// (<see cref="ColorMapName"/>, <see cref="ColorMap"/>, <see cref="Normalizer"/>). No field of its own: it is not written itself.</summary>
    internal ColorScaleWire ColorScale
    {
        get => new(new ColorMapWire(ColorMapName, ColorMap), Normalizer);
        init
        {
            ColorMapName = value.Map.Name;
            ColorMap = value.Map.Description;
            Normalizer = value.Normalizer;
        }
    }

    /// <summary>The value at the low end of the colour range; absent for the smallest value.</summary>
    public double? VMin { get; init; }

    /// <summary>The value at the high end of the colour range; absent for the largest value.</summary>
    public double? VMax { get; init; }

    /// <summary>The opacity of the whole image; absent for what a new <see cref="FigureImage"/> holds.</summary>
    public double? Alpha { get; init; }

    private static readonly FigureImage Rest = new(new double[0, 0], 0, 0);

    /// <summary>The JSON's form of <paramref name="image"/>.</summary>
    internal static FigureImageDto From(FigureImage image) => new()
    {
        Data = image.Data.To2DList(),
        XOffset = image.XOffset,
        YOffset = image.YOffset,
        ColorScale = ColorScaleWire.Of(image.ColorMap, image.Normalizer),
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
        Data?.Select(row => row.Count).ThrowIfNotAllEqual("data");

        var image = figure.AddImage(Data.From2DList(), XOffset, YOffset);
        var scale = ColorScale;
        image.ColorMap = scale.ReadMap();
        image.Normalizer = scale.ReadNormalizer();
        image.VMin = VMin;
        image.VMax = VMax;
        if (Alpha is { } alpha) image.Alpha = alpha;
        return image;
    }
}
