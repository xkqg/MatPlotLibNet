// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>How an image is scaled to its plot where its author names no interpolation. matplotlib 3.11.2, read with python:
/// <c>rcParams['image.interpolation']</c> is <c>'auto'</c> in the default style and <c>'bilinear'</c> in the classic one. <c>'auto'</c>
/// (<c>matplotlib.image._resample</c>) draws <c>'nearest'</c> where the image is shown at an integer multiple (1x, 2x) or more than 3x of its
/// data in both directions, and <c>'hanning'</c> otherwise. The library drew an image as nearest under every theme; the matplotlib themes now
/// follow matplotlib's, the library's own themes keep nearest, and an interpolation that is named is the one drawn.</summary>
public class ImageInterpolationTests
{
    private static double[,] Grid(int rows, int cols)
    {
        var data = new double[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++) data[r, c] = (r * 7 + c * 3) % 11;
        return data;
    }

    private static int Rects(Theme theme, int rows, int cols, string? interpolation, int width = 600, int height = 400) =>
        Plt.Create().WithTheme(theme).WithSize(width, height)
            .AddSubPlot(1, 1, 1, ax => ax.Image(Grid(rows, cols), s => s.Interpolation = interpolation)).ToSvg().Xml()
            .Each("rect").Count();

    /// <summary>The cells an image of <paramref name="rows"/> x <paramref name="cols"/> draws: its rectangles less what the same figure draws around a 1 x 1 image.</summary>
    private static int Cells(Theme theme, int rows, int cols, string? interpolation, int width = 600, int height = 400) =>
        Rects(theme, rows, cols, interpolation, width, height) - Rects(theme, 1, 1, "nearest", width, height) + 1;

    public static TheoryData<Theme> ThemesThatShowAnImageNearest => new()
    {
        Theme.Default,       // the library's themes
        Theme.MatplotlibV2,  // an image shown more than three times its size
    };

    [Theory]
    [MemberData(nameof(ThemesThatShowAnImageNearest))]
    public void AnImageNamedNoInterpolation_IsNearest(Theme theme)
    {
        Assert.Equal(30 * 40, Cells(theme, 30, 40, null));
    }

    [Fact]
    public void UnderClassic_AnImageNamedNoInterpolation_IsSmoothed_AsBilinear()
    {
        Assert.Equal(120 * 160, Cells(Theme.MatplotlibClassic, 30, 40, null));
        Assert.Equal(Cells(Theme.Default, 30, 40, "bilinear"), Cells(Theme.MatplotlibClassic, 30, 40, null));
    }

    [Fact]
    public void UnderV2_AnImageShownLessThanThreeTimesItsSize_IsFiltered()
    {
        // 100 x 100 values on a 300 x 200 figure: the plot is about 2.3 and 1.5 times the data, so matplotlib's 'auto' filters it
        Assert.True(Cells(Theme.MatplotlibV2, 100, 100, null, 300, 200) > 100 * 100);
        Assert.Equal(100 * 100, Cells(Theme.Default, 100, 100, null, 300, 200));
    }

    [Fact]
    public void AnInterpolationThatIsNamed_IsTheOneDrawn_UnderAnyTheme()
    {
        Assert.Equal(30 * 40, Cells(Theme.MatplotlibClassic, 30, 40, "nearest"));
        Assert.Equal(120 * 160, Cells(Theme.Default, 30, 40, "bilinear"));
        Assert.Equal(30 * 40, Cells(Theme.MatplotlibV2, 30, 40, "auto"));
        Assert.True(Cells(Theme.MatplotlibV2, 100, 100, "antialiased", 300, 200) > 100 * 100);
        Assert.True(Cells(Theme.Default, 100, 100, "hanning", 300, 200) > 0);
    }

    [Fact]
    public void AnInterpolation_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string Json(string? name) => Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Image(Grid(30, 40), s => s.Interpolation = name)).Build().ToJson();

        string unnamed = Json(null), named = Json("nearest");

        Assert.DoesNotContain("\"interpolation\"", unnamed);
        Assert.Contains("\"interpolation\":\"nearest\"", named);
        Assert.Equal(120 * 160, Cells(Theme.MatplotlibClassic, 30, 40, null));   // an unnamed one still reads the theme's after the round trip
        Assert.Null(((ImageSeries)ChartSerializerDefault.Current.FromJson(unnamed).SubPlots[0].Series[0]).Interpolation);
    }

    [Fact]
    public void TheSetting_IsTheMatplotlibThemes_AndSurvivesTheThemeJson()
    {
        Assert.Equal("auto", Theme.MatplotlibV2.Looks.Image.Interpolation);
        Assert.Equal("bilinear", Theme.MatplotlibClassic.Looks.Image.Interpolation);
        Assert.Null(Theme.Default.Looks.Image.Interpolation);
        Assert.Equal("bilinear", Theme.MatplotlibClassic.ThroughDto().Looks.Image.Interpolation);
        Assert.Null(Theme.Default.ThroughDto().Looks.Image.Interpolation);
        Assert.Equal("auto", Theme.MatplotlibV2.Cloned().Looks.Image.Interpolation);
        Assert.Equal("nearest", Theme.Default.Looks.Image.InterpolationOver(null));
        Assert.Equal("bilinear", Theme.MatplotlibClassic.Looks.Image.InterpolationOver(null));
        Assert.Equal("nearest", Theme.MatplotlibClassic.Looks.Image.InterpolationOver("nearest"));
    }

    // matplotlib.image._resample, 3.11.2: nearest "if upsampling by an integer number or if zooming in more than a factor of 3":
    //   (dispx > 3 * cols or dispx == cols or dispx == 2 * cols) and (dispy > 3 * rows or dispy == rows or dispy == 2 * rows)
    [Theory]
    [InlineData(496.0, 370.0, 30, 40, true)]     // 12.4 x 12.3 times
    [InlineData(40.0, 30.0, 30, 40, true)]       // 1 x 1
    [InlineData(80.0, 60.0, 30, 40, true)]       // 2 x 2
    [InlineData(100.0, 75.0, 30, 40, false)]     // 2.5 x 2.5
    [InlineData(496.0, 60.0, 30, 40, true)]      // 12.4 across, 2 down
    [InlineData(496.0, 45.0, 30, 40, false)]     // 12.4 across, 1.5 down
    [InlineData(20.0, 15.0, 30, 40, false)]      // a downsample
    public void TheAutoRule_IsMatplotlibs(double width, double height, int rows, int cols, bool nearest)
    {
        Assert.Equal(nearest, new ImageFootprint(width, height, rows, cols).IsUnfiltered);
        Assert.Equal(nearest ? "nearest" : "hanning", new ImageFootprint(width, height, rows, cols).AutoEngineName);
    }

    // matplotlib.image._ImageBase._make_image, 3.11.2, rcParams['image.interpolation_stage'] 'auto' under the default and the classic style:
    //   if (dispx < 3) or (dispy < 3): interpolation_stage = 'rgba'   else: interpolation_stage = 'data'
    [Theory]
    [InlineData(496.0, 370.0, 30, 40, false)]    // 12.4 x 12.3 times: the values
    [InlineData(120.0, 90.0, 30, 40, false)]     // 3 x 3 exactly: the values
    [InlineData(100.0, 75.0, 30, 40, true)]      // 2.5 x 2.5: the colours
    [InlineData(496.0, 60.0, 30, 40, true)]      // 12.4 across, 2 down: the colours
    [InlineData(20.0, 15.0, 30, 40, true)]       // a downsample: the colours
    public void TheAutoStage_IsMatplotlibs(double width, double height, int rows, int cols, bool colours)
    {
        Assert.Equal(colours, new ImageFootprint(width, height, rows, cols).InterpolatesColours);
    }

    [Fact]
    public void TheImshowLook_InterpolatesTheColours_OfAnImageShownUnderThreeTimesItsSize_TheLibrarysNever()
    {
        var small = new ImageFootprint(100, 75, 30, 40);
        var large = new ImageFootprint(496, 370, 30, 40);

        Assert.True(Theme.MatplotlibV2.Looks.Image.InterpolatesColours(small, BilinearInterpolation.Instance));
        Assert.True(Theme.MatplotlibClassic.Looks.Image.InterpolatesColours(small, BilinearInterpolation.Instance));
        Assert.False(Theme.MatplotlibV2.Looks.Image.InterpolatesColours(large, BilinearInterpolation.Instance));
        Assert.False(Theme.Default.Looks.Image.InterpolatesColours(small, BilinearInterpolation.Instance));
        // A nearest image takes the colour of one cell either way: no colour is interpolated.
        Assert.False(Theme.MatplotlibV2.Looks.Image.InterpolatesColours(small, NearestInterpolation.Instance));
    }

    /// <summary>Red to green to blue: a value between the ends is green-tinted, a blend of the two ends' colours is not.</summary>
    private static readonly LinearColorMap RedGreenBlue = new("rgb", [Colors.Red, Color.FromHex("#00FF00"), Colors.Blue]);

    /// <summary>The fills of every rectangle a figure draws that is neither grey nor a pure end of <see cref="RedGreenBlue"/> and holds green:
    /// a cell coloured from a value between the ends.</summary>
    private static List<Color> GreenTintedCells(Theme theme)
    {
        var data = new double[20, 40];
        for (int r = 0; r < 20; r++) for (int c = 0; c < 40; c++) data[r, c] = c % 2;
        var figure = Plt.Create().WithTheme(theme).WithSize(120, 90)
            .AddSubPlot(1, 1, 1, ax => ax.Image(data, s => { s.ColorMap = RedGreenBlue; s.Interpolation = "bilinear"; }))
            .Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Of<DrawRectangleCall>().Select(c => c.Fill).OfType<Color>().Where(c => c.G > 0 && !(c.R == c.G && c.G == c.B))];
    }

    [Fact]
    public void UnderTheMatplotlibThemes_AnImageShownUnderThreeTimesItsSize_BlendsTheColoursOfItsCells()
    {
        // 40 columns of 0 and 1 over a plot about 70 pixels wide: the colours are resampled (matplotlib's 'rgba' stage), so a blend of red and
        // blue is purple, never the green the map puts between them.
        Assert.Empty(GreenTintedCells(Theme.MatplotlibClassic));
        Assert.Empty(GreenTintedCells(Theme.MatplotlibV2));
        // The library's own themes resample the values and colour each.
        Assert.NotEmpty(GreenTintedCells(Theme.Default));
    }

    // matplotlib.image._make_image, 3.11.2, the 'rgba' stage: alpha_in = A[..., 3]; A = A * alpha_in[..., None]; res = _resample(...);
    // res /= np.where(alpha_out != 0, alpha_out, 1)[..., None]; output = self.to_rgba(res, bytes=True, norm=False)  ((x * 255).astype(uint8))
    /// <summary>The colours of the cells <paramref name="engine"/> resamples a 1 x 2 image to, 1 x 3 cells across a rectangle.</summary>
    private static List<Color> ResampledFills(LinearColorMap map, IInterpolationEngine engine)
    {
        var data = new double[,] { { 0, 1 } };
        var ctx = new RecordingRenderContext();
        new ImageCells(new ColormapContext(map, LinearNormalizer.Instance, 0, 1), 1.0)
            .DrawResampledColours(ctx, data, engine, new ResampledGrid(1, 3), new Point(0, 0), new Point(3, 1));
        return [.. ctx.Of<DrawRectangleCall>().Select(c => c.Fill!.Value)];
    }

    [Fact]
    public void TheColours_AreResampledPremultiplied_ThenDivided_AsMatplotlibDoes()
    {
        var map = new LinearColorMap("clear-red-to-blue", [new Color(255, 0, 0, 0), Colors.Blue]);

        var fills = ResampledFills(map, BilinearInterpolation.Instance);

        // The clear red end adds no red: premultiplied it is nothing. Halfway the blue is whole and the opacity half (127.5 cut to 127).
        Assert.Equal(0, fills[0].A);
        Assert.Equal(new Color(0, 0, 255, 127), fills[1]);
        Assert.Equal(new Color(0, 0, 255, 255), fills[2]);
    }

    [Fact]
    public void AnEngineOfYourOwn_IsAskedChannelByChannel_AndGivesTheColoursABuiltInEngineGives()
    {
        Assert.Equal(ResampledFills(RedGreenBlue, BilinearInterpolation.Instance), ResampledFills(RedGreenBlue, new TestInterpolationEngine("own-bilinear", BilinearInterpolation.Instance.Resample)));
        Assert.Equal(new Color(127, 0, 127, 255), ResampledFills(RedGreenBlue, new TestInterpolationEngine("own-bilinear", BilinearInterpolation.Instance.Resample))[1]);
    }
    // matplotlib._image.resample(data, out, Affine2D().scale(n / cols, m / rows), HANNING, True, 1.0, True, 4.0), 3.11.2
    [Fact]
    public void TheHanningEngine_UpsamplesAsAggDoes()
    {
        double[,] data = { { 0, 1, 2.8284271, 5.1961524 }, { 8, 11.18034, 14.696938, 18.520259 }, { 22.627417, 27, 31.622777, 36.48287 } };
        double[,] expected =
        {
            { 0, 0.5, 1.1243, 2.7097, 4.0123, 5.1962 },
            { 2.7451, 3.6192, 4.657, 6.7446, 8.3346, 9.7682 },
            { 8, 9.5902, 11.4194, 14.4686, 16.6086, 18.5203 },
            { 17.6082, 19.5899, 21.8602, 25.5393, 28.067, 30.3192 },
            { 22.6274, 24.8137, 27.3143, 31.3226, 34.0528, 36.4829 },
        };

        var actual = HanningInterpolation.Instance.Resample(data, 5, 6);

        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 6; c++) Assert.Equal(expected[r, c], actual[r, c], 0.05);   // Agg places a sample on a 1/256 of a cell: 0.02 of a range of 36
    }

    [Fact]
    public void TheHanningEngine_DownsamplesAsAggDoes()
    {
        double[,] data = { { 0, 1, 2.8284271, 5.1961524 }, { 8, 11.18034, 14.696938, 18.520259 }, { 22.627417, 27, 31.622777, 36.48287 } };

        var actual = HanningInterpolation.Instance.Resample(data, 2, 2);

        Assert.Equal(3.6736, actual[0, 0], 0.05);
        Assert.Equal(7.9933, actual[0, 1], 0.05);
        Assert.Equal(20.0399, actual[1, 0], 0.05);
        Assert.Equal(27.9143, actual[1, 1], 0.05);
    }

    // the same call on a row of 1 to 8 made 4 and 5 wide: [1.5732, 3.5, 5.5, 7.4268] and [1.374, 2.8884, 4.5, 6.1047, 7.626]
    [Fact]
    public void TheHanningEngine_ReadsEveryValue_AsAggDoes_OnARow()
    {
        double[,] row = { { 1, 2, 3, 4, 5, 6, 7, 8 } };

        var half = HanningInterpolation.Instance.Resample(row, 1, 4);
        var five = HanningInterpolation.Instance.Resample(row, 1, 5);

        double[] expectedHalf = [1.5732, 3.5, 5.5, 7.4268], expectedFive = [1.374, 2.8884, 4.5, 6.1047, 7.626];
        for (int c = 0; c < 4; c++) Assert.Equal(expectedHalf[c], half[0, c], 3);
        for (int c = 0; c < 5; c++) Assert.Equal(expectedFive[c], five[0, c], 0.02);   // a scale of 1.6 is not a whole number of 256ths
    }

    [Fact]
    public void TheHanningEngine_KeepsAFlatImageFlat_AndASameSizeImageAsItIs()
    {
        var flat = new double[4, 4];
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) flat[r, c] = 3.5;
        var scaled = HanningInterpolation.Instance.Resample(flat, 7, 9);
        Assert.All(scaled.Cast<double>(), v => Assert.Equal(3.5, v, 9));

        var same = HanningInterpolation.Instance.Resample(Grid(5, 6), 5, 6);
        for (int r = 0; r < 5; r++) for (int c = 0; c < 6; c++) Assert.Equal(Grid(5, 6)[r, c], same[r, c], 9);
        Assert.Equal("hanning", HanningInterpolation.Instance.Name);
        Assert.Same(HanningInterpolation.Instance, InterpolationRegistry.Get("hanning"));
    }
}
