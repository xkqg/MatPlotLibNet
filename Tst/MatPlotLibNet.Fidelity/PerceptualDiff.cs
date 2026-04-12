// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Fidelity;

/// <summary>
/// Perceptual image diff engine for the matplotlib fidelity test suite.
///
/// Three complementary metrics are computed from two RGBA8888 bitmaps of the same size:
/// <list type="bullet">
///   <item><description><b>RMS</b> — mean root-mean-square pixel error across all channels (0–255 scale).</description></item>
///   <item><description><b>SSIM</b> — structural similarity index on a downsampled luminance image (block-mean variant, 0–1 scale; 1 = identical).</description></item>
///   <item><description><b>MaxColorDeltaE</b> — maximum CIE ΔE*76 between the 5 dominant colours of each image (identifies palette drift).</description></item>
/// </list>
///
/// Default thresholds:
/// <list type="bullet">
///   <item><description>RMS ≤ 8 (out of 255)</description></item>
///   <item><description>SSIM ≥ 0.92</description></item>
///   <item><description>MaxColorDeltaE ≤ 10</description></item>
/// </list>
///
/// On failure <see cref="WriteDiffImage"/> produces a 3-panel side-by-side PNG
/// (reference | actual | abs-difference heatmap) for visual triage.
/// </summary>
public static class PerceptualDiff
{
    public const double DefaultRmsThreshold    = 8.0;
    public const double DefaultSsimThreshold   = 0.92;
    public const double DefaultDeltaEThreshold = 10.0;

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    public sealed record Result(double Rms, double Ssim, double MaxDeltaE)
    {
        public bool Passed(double rmsThreshold = DefaultRmsThreshold,
                           double ssimThreshold = DefaultSsimThreshold,
                           double deltaEThreshold = DefaultDeltaEThreshold) =>
            Rms <= rmsThreshold && Ssim >= ssimThreshold && MaxDeltaE <= deltaEThreshold;

        public override string ToString() =>
            $"RMS={Rms:F2}/255  SSIM={Ssim:F4}  ΔE={MaxDeltaE:F2}";
    }

    /// <summary>Compares two PNG byte arrays and returns the perceptual diff result.</summary>
    public static Result Compare(byte[] reference, byte[] actual)
    {
        using var refBmp = Decode(reference);
        using var actBmp = Decode(actual);
        EnsureSameSize(refBmp, actBmp);
        return Compute(refBmp, actBmp);
    }

    /// <summary>Compares two PNG files by path and returns the perceptual diff result.</summary>
    public static Result Compare(string referencePath, string actualPath) =>
        Compare(File.ReadAllBytes(referencePath), File.ReadAllBytes(actualPath));

    /// <summary>
    /// Writes a 3-panel side-by-side diff image (reference | actual | abs-diff heatmap) to
    /// <paramref name="outputPath"/>.
    /// </summary>
    public static void WriteDiffImage(byte[] reference, byte[] actual, string outputPath)
    {
        using var refBmp = Decode(reference);
        using var actBmp = Decode(actual);
        EnsureSameSize(refBmp, actBmp);

        int w = refBmp.Width, h = refBmp.Height;
        using var diff = new SKBitmap(w * 3, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(diff);

        // Panel 1 — reference
        canvas.DrawBitmap(refBmp, 0, 0);

        // Panel 2 — actual
        canvas.DrawBitmap(actBmp, w, 0);

        // Panel 3 — abs-diff heatmap (scaled ×4 for visibility)
        var refPx = GetPixels(refBmp);
        var actPx = GetPixels(actBmp);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                byte dr = Clamp(Math.Abs(refPx[idx].Red   - actPx[idx].Red)   * 4);
                byte dg = Clamp(Math.Abs(refPx[idx].Green - actPx[idx].Green) * 4);
                byte db = Clamp(Math.Abs(refPx[idx].Blue  - actPx[idx].Blue)  * 4);
                diff.SetPixel(w * 2 + x, y, new SKColor(dr, dg, db));
            }
        }

        canvas.Flush();
        using var img  = SKImage.FromBitmap(diff);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(outputPath, data.ToArray());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Metrics implementation
    // ──────────────────────────────────────────────────────────────────────────

    private static Result Compute(SKBitmap refBmp, SKBitmap actBmp)
    {
        var refPx = GetPixels(refBmp);
        var actPx = GetPixels(actBmp);

        double rms    = ComputeRms(refPx, actPx);
        double ssim   = ComputeSsim(refBmp, actBmp);
        double deltaE = ComputeMaxDeltaE(refPx, actPx);

        return new Result(rms, ssim, deltaE);
    }

    /// <summary>Mean RMS across all R/G/B channels (0–255 scale).</summary>
    private static double ComputeRms(SKColor[] a, SKColor[] b)
    {
        double sumSq = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double dr = a[i].Red   - b[i].Red;
            double dg = a[i].Green - b[i].Green;
            double db = a[i].Blue  - b[i].Blue;
            sumSq += dr * dr + dg * dg + db * db;
        }
        return Math.Sqrt(sumSq / (3.0 * a.Length));
    }

    /// <summary>
    /// Block-mean SSIM on luminance (Y channel), using 8×8 blocks, windows of 3×3 blocks.
    /// Simplified variant that captures structural similarity without the full sliding-window SSIM.
    /// </summary>
    private static double ComputeSsim(SKBitmap a, SKBitmap b)
    {
        const int blockSize = 8;
        int blocksX = a.Width  / blockSize;
        int blocksY = a.Height / blockSize;
        if (blocksX < 2 || blocksY < 2) return ComputeSsimBruteForce(a, b);

        // Build luminance block-mean arrays
        var lumaA = BuildLumaBlocks(a, blockSize, blocksX, blocksY);
        var lumaB = BuildLumaBlocks(b, blockSize, blocksX, blocksY);

        const double c1 = 6.5025;   // (0.01 * 255)²
        const double c2 = 58.5225;  // (0.03 * 255)²

        double ssimSum = 0;
        int count = 0;
        for (int iy = 0; iy < blocksY; iy++)
        {
            for (int ix = 0; ix < blocksX; ix++)
            {
                double muA = lumaA[iy * blocksX + ix];
                double muB = lumaB[iy * blocksX + ix];

                // Variance and covariance from neighbouring blocks
                double varA = 0, varB = 0, cov = 0;
                int neighbours = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = ix + dx, ny = iy + dy;
                        if (nx < 0 || nx >= blocksX || ny < 0 || ny >= blocksY) continue;
                        double va = lumaA[ny * blocksX + nx];
                        double vb = lumaB[ny * blocksX + nx];
                        varA += (va - muA) * (va - muA);
                        varB += (vb - muB) * (vb - muB);
                        cov  += (va - muA) * (vb - muB);
                        neighbours++;
                    }
                }
                if (neighbours > 1) { varA /= (neighbours - 1); varB /= (neighbours - 1); cov /= (neighbours - 1); }

                double num = (2 * muA * muB + c1) * (2 * cov + c2);
                double den = (muA * muA + muB * muB + c1) * (varA + varB + c2);
                ssimSum += num / den;
                count++;
            }
        }
        return count > 0 ? ssimSum / count : 1.0;
    }

    /// <summary>Fallback pixel-level SSIM for very small images.</summary>
    private static double ComputeSsimBruteForce(SKBitmap a, SKBitmap b)
    {
        var pa = GetPixels(a);
        var pb = GetPixels(b);
        double muA = 0, muB = 0;
        for (int i = 0; i < pa.Length; i++) { muA += Luma(pa[i]); muB += Luma(pb[i]); }
        muA /= pa.Length; muB /= pb.Length;
        double varA = 0, varB = 0, cov = 0;
        for (int i = 0; i < pa.Length; i++)
        {
            double la = Luma(pa[i]) - muA, lb = Luma(pb[i]) - muB;
            varA += la * la; varB += lb * lb; cov += la * lb;
        }
        varA /= pa.Length; varB /= pa.Length; cov /= pa.Length;
        const double c1 = 6.5025, c2 = 58.5225;
        return ((2 * muA * muB + c1) * (2 * cov + c2)) /
               ((muA * muA + muB * muB + c1) * (varA + varB + c2));
    }

    /// <summary>
    /// Maximum CIE ΔE*76 between the 5 dominant colours of each image.
    /// Dominant colours are found by sampling every 10th pixel and k-means-like median-cut
    /// simplified to a 4-bit per channel colour cube histogram.
    /// </summary>
    private static double ComputeMaxDeltaE(SKColor[] a, SKColor[] b)
    {
        var domA = DominantColors(a, 5);
        var domB = DominantColors(b, 5);

        double maxDE = 0;
        foreach (var ca in domA)
        {
            // find closest match in domB
            double minDE = double.MaxValue;
            foreach (var cb in domB)
                minDE = Math.Min(minDE, DeltaE(ca, cb));
            maxDE = Math.Max(maxDE, minDE);
        }
        return maxDE;
    }

    /// <summary>Returns a human-readable dump of the 5 dominant colours in each image (for failure diagnostics).</summary>
    public static string DiagnoseColors(byte[] reference, byte[] actual)
    {
        using var refBmp = Decode(reference);
        using var actBmp = Decode(actual);
        var refPx = GetPixels(refBmp);
        var actPx = GetPixels(actBmp);
        var domA = DominantColors(refPx, 5);
        var domB = DominantColors(actPx, 5);
        string Fmt(IEnumerable<SKColor> cs) =>
            string.Join(", ", cs.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}"));
        return $"Ref: [{Fmt(domA)}]  Actual: [{Fmt(domB)}]";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Colour helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static double Luma(SKColor c) => 0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue;

    /// <summary>CIE ΔE*76 in sRGB approximation (sufficient for palette comparison).</summary>
    private static double DeltaE(SKColor a, SKColor b)
    {
        // Convert sRGB [0-255] → Lab via XYZ
        static (double L, double a_, double b_) ToLab(SKColor c)
        {
            double r = c.Red / 255.0, g = c.Green / 255.0, bl = c.Blue / 255.0;
            // sRGB linearisation
            r  = r  > 0.04045 ? Math.Pow((r  + 0.055) / 1.055, 2.4) : r  / 12.92;
            g  = g  > 0.04045 ? Math.Pow((g  + 0.055) / 1.055, 2.4) : g  / 12.92;
            bl = bl > 0.04045 ? Math.Pow((bl + 0.055) / 1.055, 2.4) : bl / 12.92;
            // → XYZ (D65)
            double x = (r * 0.4124 + g * 0.3576 + bl * 0.1805) / 0.95047;
            double y = (r * 0.2126 + g * 0.7152 + bl * 0.0722) / 1.00000;
            double z = (r * 0.0193 + g * 0.1192 + bl * 0.9505) / 1.08883;
            // f(t)
            static double f(double t) => t > 0.008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116;
            double fx = f(x), fy = f(y), fz = f(z);
            return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));
        }

        var (L1, a1, b1) = ToLab(a);
        var (L2, a2, b2) = ToLab(b);
        return Math.Sqrt((L1 - L2) * (L1 - L2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
    }

    /// <summary>Returns the N most prevalent colours by sampling every 10th pixel and histogram bucketing.</summary>
    private static List<SKColor> DominantColors(SKColor[] pixels, int n)
    {
        // 4-bit per channel (16³ = 4096 buckets) histogram
        var hist = new Dictionary<int, (long r, long g, long b, long count)>(256);
        for (int i = 0; i < pixels.Length; i += 10)
        {
            int key = (pixels[i].Red >> 4) << 8 | (pixels[i].Green >> 4) << 4 | (pixels[i].Blue >> 4);
            var (r, g, b, cnt) = hist.GetValueOrDefault(key);
            hist[key] = (r + pixels[i].Red, g + pixels[i].Green, b + pixels[i].Blue, cnt + 1);
        }

        return hist.OrderByDescending(kv => kv.Value.count)
                   .Take(n)
                   .Select(kv => new SKColor(
                       (byte)(kv.Value.r / kv.Value.count),
                       (byte)(kv.Value.g / kv.Value.count),
                       (byte)(kv.Value.b / kv.Value.count)))
                   .ToList();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Utility
    // ──────────────────────────────────────────────────────────────────────────

    private static SKBitmap Decode(byte[] png)
    {
        using var ms = new MemoryStream(png);
        var bmp = SKBitmap.Decode(ms);
        if (bmp is null) throw new InvalidDataException("Failed to decode PNG.");
        return bmp;
    }

    private static SKColor[] GetPixels(SKBitmap bmp)
    {
        var pixels = new SKColor[bmp.Width * bmp.Height];
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                pixels[y * bmp.Width + x] = bmp.GetPixel(x, y);
        return pixels;
    }

    private static double[] BuildLumaBlocks(SKBitmap bmp, int blockSize, int bx, int by)
    {
        var blocks = new double[bx * by];
        for (int iy = 0; iy < by; iy++)
        {
            for (int ix = 0; ix < bx; ix++)
            {
                double sum = 0;
                for (int dy = 0; dy < blockSize; dy++)
                    for (int dx = 0; dx < blockSize; dx++)
                        sum += Luma(bmp.GetPixel(ix * blockSize + dx, iy * blockSize + dy));
                blocks[iy * bx + ix] = sum / (blockSize * blockSize);
            }
        }
        return blocks;
    }

    private static void EnsureSameSize(SKBitmap a, SKBitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            throw new ArgumentException(
                $"Images must be the same size. Reference={a.Width}×{a.Height}, Actual={b.Width}×{b.Height}.");
    }

    private static byte Clamp(int v) => (byte)Math.Clamp(v, 0, 255);
}
