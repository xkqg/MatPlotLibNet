// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies the stat tile's full anatomy: value, target, gap and trend.
/// <para>A bare big number is a failed dashboard pattern. "68%" tells a reader nothing — without a
/// comparative they must supply the missing context from memory, and mostly they cannot. So the tile carries
/// four things: the value, the target it is measured against, the gap between them stated in words, and a
/// trend that says which way it is moving. The gap is the important one: it answers "is this good or bad"
/// without anyone doing arithmetic on a wall.</para></summary>
public class StatTileAnatomyTests
{
    private static readonly double[] Trend = [4.0, 4.5, 5.0, 6.0, 8.0, 12.0];

    /// <summary>A tile with a trend draws the sparkline inside its own area — an extra polyline that a plain
    /// tile does not have.</summary>
    [Fact]
    public void ATileWithATrend_DrawsASparkline()
    {
        string plain = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(12.0)).ToSvg();
        string withTrend = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(12.0, s => s.Trend = Trend)).ToSvg();

        Assert.DoesNotContain("<polyline", plain);
        Assert.Contains("<polyline", withTrend);
    }

    /// <summary>The trend is a Tufte sparkline: no axis, no frame, no ticks. It contributes nothing to the
    /// axes' data range, so a tile's headline scale is never dragged around by its own history.</summary>
    [Fact]
    public void TheTrend_DoesNotContributeToTheDataRange()
    {
        var tile = new StatTileSeries(12.0) { Trend = Trend };

        var range = tile.ComputeDataRange(new NullContext());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    /// <summary>The caption — the gap line — is drawn beneath the value.</summary>
    [Fact]
    public void TheCaption_IsRendered()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(28.1, s =>
            {
                s.Label = "RFx p99";
                s.Caption = "target 25 ms · +3.1 over";
            }))
            .ToSvg();

        Assert.Contains("target 25 ms", svg);
        Assert.Contains("RFx p99", svg);
    }

    /// <summary>A caption may carry MORE THAN ONE LINE. A tile's gap line answers "is this good or bad", and a
    /// second line answers "measured over what" — two different questions that do not belong on one crowded row
    /// (reported from the Ait ops wall, 2026-08-30: "threshold 250 · 2148 msg · 1 s" ran wider than its tile).
    /// Newline-separated, drawn stacked and centred like every other line of the anatomy.</summary>
    [Fact]
    public void ACaptionWithNewlines_IsDrawnAsSTACKEDLines()
    {
        string oneLine = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => s.Caption = "threshold 6 · 2148 msg · 1 s"))
            .ToSvg();
        string twoLines = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => s.Caption = "threshold 6" + Environment.NewLine + "2148 msg · 1 s"))
            .ToSvg();

        Assert.Contains("threshold 6", twoLines);
        Assert.Contains("2148 msg", twoLines);
        Assert.DoesNotContain("threshold 6" + Environment.NewLine + "2148", twoLines); // never ONE glued text element
        Assert.True(CountText(twoLines) == CountText(oneLine) + 1,
            "the second caption line is its own <text> element, stacked under the first");
    }

    private static int CountText(string svg) => svg.Split("<text").Length - 1;

    /// <summary>The tile centres its whole STACK — value, label and every caption line — inside its body, so a
    /// second caption line takes the free room at the TOP instead of growing past the bottom into the
    /// sparkline (owner, 2026-08-30: "there is plenty of room at the top of the tile"). The headline rises by
    /// half the extra line; the block stays optically centred.</summary>
    [Fact]
    public void AnExtraCaptionLine_RaisesTheStack_ItDoesNotGrowOutOfTheBottom()
    {
        double OneLine = HeadlineY("threshold 6");
        double TwoLines = HeadlineY("threshold 6" + Environment.NewLine + "2148 msg · 1 s");
        double LastCaptionOne = LowestTextY("threshold 6");
        double LastCaptionTwo = LowestTextY("threshold 6" + Environment.NewLine + "2148 msg · 1 s");

        Assert.True(TwoLines < OneLine, "the headline moved UP to make room for the second line");
        Assert.True(OneLine - TwoLines >= 6, "by about half the extra line's height");
        Assert.True(LastCaptionTwo - LastCaptionOne <= 8, "and the stack did not simply grow downward");
    }

    // The y of the tile's headline (the 44 pt text), and the y of its lowest text — read straight off the SVG.
    private static double HeadlineY(string caption) => TextYs(caption, "44")[0];

    private static double LowestTextY(string caption)
    {
        double[] ys = TextYs(caption, null);
        return ys[^1];
    }

    private static double[] TextYsIn(string svg, string? fontSize)
    {
        var ys = new List<double>();
        foreach (string chunk in svg.Split("<text").Skip(1))
        {
            string head = chunk[..chunk.IndexOf('>')];
            if (fontSize is not null && !head.Contains("font-size=\"" + fontSize, StringComparison.Ordinal))
            {
                continue;
            }
            int at = head.IndexOf("y=\"", StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }
            string raw = head[(at + 3)..];
            ys.Add(double.Parse(raw[..raw.IndexOf('"')], CultureInfo.InvariantCulture));
        }
        ys.Sort();
        return [.. ys];
    }

    private static double[] TextYs(string caption, string? fontSize)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s =>
            {
                s.Label = "Nexus";
                s.Caption = caption;
            }))
            .ToSvg();
        var ys = new List<double>();
        foreach (string chunk in svg.Split("<text").Skip(1))
        {
            string head = chunk[..chunk.IndexOf('>')];
            if (fontSize is not null && !head.Contains("font-size=\"" + fontSize, StringComparison.Ordinal))
            {
                continue;
            }
            int at = head.IndexOf("y=\"", StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }
            string raw = head[(at + 3)..];
            ys.Add(double.Parse(raw[..raw.IndexOf('"')], CultureInfo.InvariantCulture));
        }
        ys.Sort();
        return [.. ys];
    }

    /// <summary>A caption WRAPS to the tile's width — the CSS behaviour, in the renderer: a caption longer than
    /// its tile is broken at word boundaries instead of running out over the neighbouring tiles (reported from
    /// an ops wall, 2026-08-30: "threshold 250 · 2148 msg · 1 s" ran wider than its card). Explicit newlines
    /// still break where the caller put them; wrapping only adds breaks the width demands.</summary>
    [Fact]
    public void ALongCaption_WrapsToTheTilesWidth_AtWordBoundaries()
    {
        string wide = Plt.Create().WithSize(600, 200)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => s.Caption = LongCaption)).ToSvg();
        string narrow = Plt.Create().WithSize(190, 110)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(3.8, s => s.Caption = LongCaption)).ToSvg();

        Assert.Equal(1, CaptionLines(wide));
        Assert.True(CaptionLines(narrow) > 1, "the narrow tile broke the caption over more rows");
        Assert.Contains("threshold", narrow);
        Assert.Contains("1 s", narrow);          // every word survives the wrap
        Assert.DoesNotContain(">thr<", narrow);  // ...and no word is cut through the middle
    }

    private const string LongCaption = "threshold 250 · 2148 msg · 1 s";

    // The caption lines are the tile's 11 pt texts.
    private static int CaptionLines(string svg)
    {
        int lines = 0;
        foreach (string chunk in svg.Split("<text").Skip(1))
        {
            if (chunk[..chunk.IndexOf('>')].Contains("font-size=\"11", StringComparison.Ordinal))
            {
                lines++;
            }
        }
        return lines;
    }

    /// <summary>A caption never runs into the sparkline. The trend takes the tile's lower fifth, so on a short
    /// tile a two-line caption used to be drawn straight through the line (seen on an ops wall). The stack owns
    /// the body: when the caption needs more room than is left, the SPARKLINE yields — the numbers and what they
    /// were measured over beat the decoration.</summary>
    [Fact]
    public void ACaptionNeverRunsIntoTheSparkline_TheTrendYields()
    {
        double[] trend = [4.0, 4.5, 5.0, 6.0, 8.0, 12.0];
        // The tile as an ops dashboard draws it: its own text only, no axes, no legend.
        string svg = Plt.Create().WithSize(190, 110)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.StatTile(3.8, s =>
                {
                    s.Label = "Nexus";
                    s.Caption = "threshold 6" + Environment.NewLine + "2 148 msg · 1 s";
                    s.Trend = trend;
                });
                ax.HideAllAxes();
                ax.WithLegend(visible: false);
            })
            .ToSvg();

        double lowestText = TextYsIn(svg, null)[^1];
        double sparklineTop = PolylineTop(svg);

        Assert.True(sparklineTop > lowestText,
            $"the sparkline starts at {sparklineTop:0.#} but the caption's last line sits at {lowestText:0.#}");
    }

    // The topmost y of the tile's sparkline polyline.
    private static double PolylineTop(string svg)
    {
        int at = svg.IndexOf("<polyline", StringComparison.Ordinal);
        Assert.True(at >= 0, "the tile drew no sparkline");
        string points = svg[at..svg.IndexOf("/>", at, StringComparison.Ordinal)];
        points = points[(points.IndexOf("points=\"", StringComparison.Ordinal) + 8)..];
        points = points[..points.IndexOf('"')];
        double top = double.MaxValue;
        foreach (string pair in points.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] xy = pair.Split(',');
            if (xy.Length == 2 && double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                top = Math.Min(top, y);
            }
        }
        return top;
    }

    /// <summary>A hatched tile reads as "no information" — the source has gone silent, and that is a different
    /// fault from a bad value. It is a pattern, never a colour.</summary>
    [Fact]
    public void AHatchedTile_RendersAPattern()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(0, s => s.Hatch = HatchPattern.ForwardDiagonal))
            .ToSvg();

        Assert.Contains("<pattern", svg);
    }

    /// <summary>A tile at rest carries no colour from the series cycle — the neutral shade of the theme's
    /// alarm palette. Colour is reserved for what needs attention; a wall of cheerful tiles leaves the one
    /// that matters nothing to stand out against.</summary>
    [Fact]
    public void ATileAtRest_WearsTheNeutralShade_NotACycleColour()
    {
        string svg = Plt.Create()
            .WithTheme(Theme.OpsNight)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42))
            .ToSvg();

        Assert.Contains(Theme.OpsNight.Alarm.Resting.ToHex(), svg);
        Assert.DoesNotContain(Theme.OpsNight.CycleColors[0].ToHex(), svg);
    }

    /// <summary>The whole anatomy survives a round-trip: target, caption, trend and hatch all persist.</summary>
    [Fact]
    public void TheWholeAnatomy_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(28.1, s =>
            {
                s.Target = 25;
                s.Caption = "target 25 ms · +3.1 over";
                s.Trend = Trend;
                s.Hatch = HatchPattern.BackDiagonal;
            }))
            .Build();

        var serializer = new ChartSerializer();
        var tile = serializer.FromJson(serializer.ToJson(figure))
            .SubPlots[0].Series.OfType<StatTileSeries>().Single();

        Assert.Equal(25, tile.Target);
        Assert.Equal("target 25 ms · +3.1 over", tile.Caption);
        Assert.Equal(Trend, tile.Trend);
        Assert.Equal(HatchPattern.BackDiagonal, tile.Hatch);
    }

    private sealed class NullContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    /// <summary>The label wears the theme's own ink, like the value and the caption above and below it.
    /// <para>It did not: the label was drawn with a Font carrying no Color, so it fell back to black — on an
    /// operator ground (Theme.OpsNight, #191C1E) that is a label nobody can read, while the number right
    /// above it is perfectly legible. Reported from the Ait console, where it showed up as tiles with
    /// invisible names.</para></summary>
    [Fact]
    public void TheLabel_WearsTheThemesInk_notABlackFallback()
    {
        string svg = Plt.Create()
            .WithTheme(Theme.OpsNight)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(12.0, s => s.Label = "Latency"))
            .ToSvg();

        var label = System.Text.RegularExpressions.Regex.Match(svg, "<text[^>]*>Latency</text>");
        Assert.True(label.Success, "the label must be drawn at all");
        Assert.Contains("fill=", label.Value, StringComparison.Ordinal);
        Assert.Contains(Theme.OpsNight.ForegroundText.ToHex(), label.Value, StringComparison.OrdinalIgnoreCase);
    }
}
