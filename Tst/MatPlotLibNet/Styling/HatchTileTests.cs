// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A hatch is a tile of marks repeated over a fill, and three backends draw it: the SVG one as a pattern, the
/// raster one as a shader and the MAUI one as a pattern paint. The marks of the tile were written out in each of them, so a
/// pattern changed in one drew differently in the others. <see cref="HatchTile"/> says them once; the backends draw what it
/// says, and this holds the SVG pattern to it mark for mark.</summary>
public class HatchTileTests
{
    [Fact]
    public void ThePlainFill_HasNoMarks()
    {
        Assert.Empty(HatchTile.MarksOf(HatchPattern.None));
    }

    [Fact]
    public void AnUndefinedPattern_HasNoMarks_InsteadOfThrowing()
    {
        Assert.Empty(HatchTile.MarksOf((HatchPattern)99));
    }

    [Theory]
    [InlineData(HatchPattern.ForwardDiagonal, 3, 0)]
    [InlineData(HatchPattern.BackDiagonal, 3, 0)]
    [InlineData(HatchPattern.Horizontal, 1, 0)]
    [InlineData(HatchPattern.Vertical, 1, 0)]
    [InlineData(HatchPattern.Cross, 2, 0)]
    [InlineData(HatchPattern.DiagonalCross, 2, 0)]
    [InlineData(HatchPattern.Dots, 0, 1)]
    [InlineData(HatchPattern.Stars, 2, 1)]
    public void EachPattern_HasItsLinesAndItsDots(HatchPattern pattern, int lines, int dots)
    {
        var marks = HatchTile.MarksOf(pattern);

        Assert.Equal(lines, marks.Count(m => m.Kind == HatchMarkKind.Line));
        Assert.Equal(dots, marks.Count(m => m.Kind == HatchMarkKind.Dot));
    }

    [Fact]
    public void TheForwardDiagonal_RunsFromTheLowerLeftToTheUpperRightOfTheTile_AndOverlapsItsNeighbours()
    {
        var marks = HatchTile.MarksOf(HatchPattern.ForwardDiagonal);

        Assert.Equal(new HatchMark(HatchMarkKind.Line, 0, 6, 6, 0), marks[0]);
        // The two short strokes at the corners make the line seamless where tiles abut.
        Assert.Equal(new HatchMark(HatchMarkKind.Line, -1, 1, 1, -1), marks[1]);
        Assert.Equal(new HatchMark(HatchMarkKind.Line, 5, 7, 7, 5), marks[2]);
    }

    [Fact]
    public void TheTile_IsSixUnitsAcross()
    {
        Assert.Equal(6.0, HatchTile.Size);
    }

    private static readonly Regex Line = new("""<line x1="(?<x1>[^"]+)" y1="(?<y1>[^"]+)" x2="(?<x2>[^"]+)" y2="(?<y2>[^"]+)" stroke""");
    private static readonly Regex Dot = new("""<circle cx="(?<x>[^"]+)" cy="(?<y>[^"]+)" r="1" fill""");

    private static double N(Match m, string group) => double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(HatchPattern.ForwardDiagonal)]
    [InlineData(HatchPattern.BackDiagonal)]
    [InlineData(HatchPattern.Horizontal)]
    [InlineData(HatchPattern.Vertical)]
    [InlineData(HatchPattern.Cross)]
    [InlineData(HatchPattern.DiagonalCross)]
    [InlineData(HatchPattern.Dots)]
    [InlineData(HatchPattern.Stars)]
    public void TheSvgPattern_DrawsExactlyTheMarksOfTheTile(HatchPattern pattern)
    {
        string svg = new HatchedRectangle(pattern).Svg;

        var drawn = Line.Matches(svg).Select(m => new HatchMark(HatchMarkKind.Line, N(m, "x1"), N(m, "y1"), N(m, "x2"), N(m, "y2")))
            .Concat(Dot.Matches(svg).Select(m => new HatchMark(HatchMarkKind.Dot, N(m, "x"), N(m, "y"), 0, 0)))
            .ToList();
        var expected = HatchTile.MarksOf(pattern);

        Assert.Equal(expected.Count, drawn.Count);
        foreach (var mark in expected)
        {
            Assert.Contains(mark with { X2 = mark.Kind == HatchMarkKind.Dot ? 0 : mark.X2, Y2 = mark.Kind == HatchMarkKind.Dot ? 0 : mark.Y2 }, drawn);
        }
    }

    /// <summary>One hatched rectangle drawn through the SVG context, so its pattern definition can be read back.</summary>
    private sealed class HatchedRectangle
    {
        public HatchedRectangle(HatchPattern pattern)
        {
            var context = new SvgRenderContext();
            context.DrawRectangle(new MatPlotLibNet.Rendering.Rect(0, 0, 40, 40),
                new ShapeStyle(Fill: Color.FromHex("#4C72B0"), Stroke: null, StrokeThickness: 0) { Hatch = pattern, HatchColor = Color.FromHex("#000000") });
            Svg = context.GetOutput();
        }

        public string Svg { get; }
    }
}
