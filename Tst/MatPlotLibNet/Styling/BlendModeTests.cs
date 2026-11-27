// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="CompositeOperation"/> blending math (the numbers of the <see cref="BlendMode"/> members are held by <c>EnumOrdinalContractTests</c>).</summary>
public class BlendModeTests
{
    /// <summary>A blend and where its result lands: the source and the destination, the mode and the share of the source, and the least
    /// and the most each channel of the result may be.</summary>
    /// <param name="Case">What the row shows.</param>
    /// <param name="Source">The colour blended over.</param>
    /// <param name="Destination">The colour blended onto.</param>
    /// <param name="Mode">The blend.</param>
    /// <param name="Alpha">The share of the blended source.</param>
    /// <param name="Least">The least each channel of the result may be.</param>
    /// <param name="Most">The most each channel of the result may be.</param>
    public readonly record struct BlendRow(string Case, Color Source, Color Destination, BlendMode Mode, double Alpha, Color Least, Color Most)
    {
        public override string ToString() => Case;
    }

    private static readonly Color Black = new(0, 0, 0);
    private static readonly Color White = new(255, 255, 255);

    public static TheoryData<BlendRow> Blends => new()
    {
        // Normal: src * alpha + dst * (1 - alpha).
        new BlendRow("normal, all of the source", new(255, 0, 0), new(0, 0, 255), BlendMode.Normal, 1.0, new(255, 0, 0), new(255, 0, 0)),
        new BlendRow("normal, none of the source", new(255, 0, 0), new(0, 0, 255), BlendMode.Normal, 0.0, new(0, 0, 255), new(0, 0, 255)),
        new BlendRow("normal, half of each", new(200, 0, 0), new(0, 200, 0), BlendMode.Normal, 0.5, new(95, 95, 0), new(105, 105, 0)),
        // Multiply: src * dst, so white keeps the source and black is black.
        new BlendRow("multiply with white", new(128, 64, 32), White, BlendMode.Multiply, 1.0, new(120, 57, 0), new(135, 70, 255)),
        new BlendRow("multiply with black", new(200, 150, 100), Black, BlendMode.Multiply, 1.0, Black, Black),
        // Screen: 1 - (1 - src) * (1 - dst), so black keeps the source and two values brighten past the larger.
        new BlendRow("screen with black", new(200, 100, 50), Black, BlendMode.Screen, 1.0, new(195, 0, 0), new(205, 255, 255)),
        new BlendRow("screen of two values", new(100, 0, 0), new(150, 0, 0), BlendMode.Screen, 1.0, new(150, 0, 0), White),
        // Overlay: multiply over a dark base (2 * src * dst, about 39 here), screen over a light one.
        new BlendRow("overlay on a dark base", new(100, 0, 0), new(50, 0, 0), BlendMode.Overlay, 1.0, new(30, 0, 0), new(50, 255, 255)),
        new BlendRow("overlay on a light base", new(200, 0, 0), new(200, 0, 0), BlendMode.Overlay, 1.0, new(201, 0, 0), White),
    };

    [Theory]
    [MemberData(nameof(Blends))]
    public void ABlend_LandsEachChannelBetweenTheLeastAndTheMostOfItsRow(BlendRow row)
    {
        var result = CompositeOperation.Blend(row.Source, row.Destination, row.Mode, row.Alpha);

        Assert.InRange(result.R, row.Least.R, row.Most.R);
        Assert.InRange(result.G, row.Least.G, row.Most.G);
        Assert.InRange(result.B, row.Least.B, row.Most.B);
    }
}
