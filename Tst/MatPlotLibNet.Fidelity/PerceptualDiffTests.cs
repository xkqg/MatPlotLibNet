// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Fidelity;

/// <summary>The perceptual diff itself: the metrics are the yardstick every fidelity test is held to, so they are held to a few
/// pictures whose answer is known. The dominant colours are read from every pixel, not from a lattice of them: a lattice
/// reads a different sample of a picture drawn one pixel over, and the colour distance between two samples of one picture
/// is not a property of the picture but of where the lattice fell.</summary>
public class PerceptualDiffTests
{
    private static byte[] Fixture(string style, string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib", style, $"{name}.png"));

    /// <summary>The picture drawn <paramref name="dx"/> pixels to the right on a white page: the same picture, one pixel over.</summary>
    private static byte[] Shifted(byte[] png, int dx)
    {
        using var source = SKBitmap.Decode(png);
        using var shifted = new SKBitmap(source.Width, source.Height);
        using (var canvas = new SKCanvas(shifted))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(source, dx, 0);
        }

        using var image = SKImage.FromBitmap(shifted);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static byte[] Page(int width, int height, Action<SKCanvas>? draw = null)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            draw?.Invoke(canvas);
        }

        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Theory]
    [InlineData("classic", "bar")]
    [InlineData("classic", "line")]
    [InlineData("v2", "bar")]
    public void APictureAndItselfOnePixelOver_AreTheSamePaletteWithinAFewDeltaE(string style, string name)
    {
        byte[] reference = Fixture(style, name);

        var result = PerceptualDiff.Compare(reference, Shifted(reference, 1));

        Assert.True(result.MaxDeltaE < 15, $"{style}/{name} one pixel over: {result}");
    }

    [Fact]
    public void APictureAndItself_ScoreAsIdentical()
    {
        byte[] reference = Fixture("classic", "bar");

        var result = PerceptualDiff.Compare(reference, reference);

        Assert.Equal(0, result.Rms);
        Assert.Equal(1.0, result.Ssim, 9);
        Assert.Equal(0, result.MaxDeltaE);
    }

    [Fact]
    public void EveryPixelCounts_AColumnOnePixelWideIsSeen()
    {
        // A page 100 pixels wide: the pixel at (3, y) has the index 100 * y + 3, never a multiple of ten, so a lattice of every
        // tenth pixel cannot see a column at x = 3. The column is the only red on the page; the page without it has none.
        byte[] withColumn = Page(100, 100, canvas => canvas.DrawRect(3, 0, 1, 100, new SKPaint { Color = SKColors.Red }));
        byte[] without = Page(100, 100);

        var result = PerceptualDiff.Compare(withColumn, without);

        Assert.True(result.MaxDeltaE > 50, $"the red column is a dominant colour of its page: {result}");
    }

    [Fact]
    public void ThePaletteOfAPicture_DoesNotDependOnWhereTheSamplingFalls()
    {
        // The same picture on a page one pixel wider shifts every row's pixels by the row number: a lattice would read
        // another sample each time. Read from every pixel, the palettes agree.
        byte[] reference = Fixture("classic", "bar");

        var shiftedOne = PerceptualDiff.Compare(reference, Shifted(reference, 1));
        var shiftedThree = PerceptualDiff.Compare(reference, Shifted(reference, 3));

        Assert.True(Math.Abs(shiftedOne.MaxDeltaE - shiftedThree.MaxDeltaE) < 10, $"{shiftedOne} vs {shiftedThree}");
    }
}
