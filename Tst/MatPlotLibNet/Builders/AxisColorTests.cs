// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>
/// SetYAxisColor / SetXAxisColor on all four axes (left Y and bottom X on <see cref="AxesBuilder"/>, right Y on
/// <see cref="YAxisSlotBuilder"/>, top X on <see cref="SecondaryXAxisBuilder"/>). One axis colour is four
/// settings: the spine, the tick marks, the tick labels and the axis title. The render facts render the same figure
/// with and without the colour and prove the two SVGs are equal apart from exactly those four things on exactly that
/// axis.
/// </summary>
public class AxisColorTests
{
    private static readonly Color Mark = Color.FromHex("#E4007C");
    private const string MarkHex = "#E4007C";
    private const string ForegroundHex = "#262626";

    private enum Side { Left, Bottom, Right, Top }

    /// <summary>The colour each of the four axes is asked to wear; <see langword="null"/> leaves the axis alone.</summary>
    private readonly record struct AxisColors(Color? Left, Color? Bottom, Color? Right, Color? Top);

    /// <summary>One <c>&lt;line&gt;</c> of the SVG, with the colour it is stroked in.</summary>
    private readonly record struct Segment(double X1, double Y1, double X2, double Y2, string Stroke);

    private static string Svg(AxisColors c) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 2, 3], [1.0, 4, 9]).SetXLabel("Time").SetYLabel("Level");
            if (c.Left is { } left) ax.SetYAxisColor(left);
            if (c.Bottom is { } bottom) ax.SetXAxisColor(bottom);
            ax.WithAddRightYAxis(s =>
            {
                s.Plot([1.0, 2, 3], [10.0, 20, 30]).SetYLabel("Right");
                if (c.Right is { } right) s.SetYAxisColor(right);
            });
            ax.WithSecondaryXAxis(s =>
            {
                s.PlotXSecondary([10.0, 20, 30], [1.0, 2, 3]).SetXLabel("Top");
                if (c.Top is { } top) s.SetXAxisColor(top);
            });
        })
        .Build().ToSvg();

    private static string[] Lines(string svg) => svg.Split('\n').Select(l => l.Trim()).ToArray();

    private static double Attr(string element, string name) =>
        double.Parse(Regex.Match(element, $@"\b{name}=""([^""]+)""").Groups[1].Value, CultureInfo.InvariantCulture);

    private static Segment SegmentOf(string element) => new(Attr(element, "x1"), Attr(element, "y1"),
        Attr(element, "x2"), Attr(element, "y2"), Regex.Match(element, @"stroke=""([^""]+)""").Groups[1].Value);

    /// <summary>The four spines in the order they are drawn: bottom, top, left, right.</summary>
    private static Segment[] Spines(string[] lines)
    {
        var spines = new List<Segment>();
        for (int i = 0; i < lines.Length - 1; i++)
            if (lines[i] == "<g class=\"spine\">") spines.Add(SegmentOf(lines[i + 1]));
        return spines.ToArray();
    }

    private static Segment SpineOf(Segment[] spines, Side side) => side switch
    {
        Side.Bottom => spines[0],
        Side.Top => spines[1],
        Side.Left => spines[2],
        _ => spines[3],
    };

    /// <summary>The tick marks of one side: every <c>&lt;line&gt;</c> outside a spine group that points away from that edge.</summary>
    private static List<Segment> TickMarks(string[] lines, Side side)
    {
        var spines = Spines(lines);
        double left = SpineOf(spines, Side.Left).X1, right = SpineOf(spines, Side.Right).X1;
        double top = SpineOf(spines, Side.Top).Y1, bottom = SpineOf(spines, Side.Bottom).Y1;
        var marks = new List<Segment>();
        bool inSpine = false;
        foreach (var line in lines)
        {
            if (line == "<g class=\"spine\">") { inSpine = true; continue; }
            if (line == "</g>") { inSpine = false; continue; }
            if (inSpine || !line.StartsWith("<line", StringComparison.Ordinal)) continue;
            var s = SegmentOf(line);
            bool horizontal = s.Y1 == s.Y2, vertical = s.X1 == s.X2;
            bool onSide = side switch
            {
                Side.Left => horizontal && Math.Min(s.X1, s.X2) < left - 1,
                Side.Right => horizontal && Math.Max(s.X1, s.X2) > right + 1,
                Side.Bottom => vertical && Math.Max(s.Y1, s.Y2) > bottom + 1,
                _ => vertical && Math.Min(s.Y1, s.Y2) < top - 1,
            };
            if (onSide) marks.Add(s);
        }
        return marks;
    }

    private static List<string> TextsFilledWith(string[] lines, string hex) => lines
        .Where(l => l.StartsWith("<text", StringComparison.Ordinal) && l.Contains($"fill=\"{hex}\"", StringComparison.Ordinal))
        .Select(l => Regex.Match(l, ">([^<]*)</text>").Groups[1].Value)
        .ToList();

    /// <summary>Renders the figure with only <paramref name="colours"/> asked for and proves that axis, and only that
    /// axis, wears <see cref="Mark"/> on its spine, its tick marks, its tick labels and its title.</summary>
    private static void AssertOnlyOneAxisIsColoured(AxisColors colours, Side side, params string[] textsOfTheAxis)
    {
        var baseline = Svg(default);
        var coloured = Svg(colours);

        // Equal apart from the colour: put the foreground back and the two SVGs are the same bytes.
        Assert.NotEqual(baseline, coloured);
        Assert.Equal(baseline, coloured.Replace(MarkHex, ForegroundHex));

        var lines = Lines(coloured);
        var spines = Spines(lines);
        foreach (Side s in Enum.GetValues<Side>())
            Assert.Equal(s == side ? MarkHex : ForegroundHex, SpineOf(spines, s).Stroke);

        foreach (Side s in Enum.GetValues<Side>())
        {
            var marks = TickMarks(lines, s);
            Assert.NotEmpty(marks);
            Assert.All(marks, m => Assert.Equal(s == side ? MarkHex : ForegroundHex, m.Stroke));
        }

        Assert.Equal(textsOfTheAxis, TextsFilledWith(lines, MarkHex));
    }

    [Fact]
    public void SetYAxisColor_ColoursTheLeftSpineTheTickMarksTheTickLabelsAndTheTitle_andNothingElse()
        => AssertOnlyOneAxisIsColoured(new AxisColors(Mark, null, null, null), Side.Left,
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Level");

    [Fact]
    public void SetXAxisColor_ColoursTheBottomSpineTheTickMarksTheTickLabelsAndTheTitle_andNothingElse()
        => AssertOnlyOneAxisIsColoured(new AxisColors(null, Mark, null, null), Side.Bottom,
            "1.00", "1.25", "1.50", "1.75", "2.00", "2.25", "2.50", "2.75", "3.00", "Time");

    [Fact]
    public void RightYAxis_SetYAxisColor_ColoursTheRightSpineTheTickMarksTheTickLabelsAndTheTitle_andNothingElse()
        => AssertOnlyOneAxisIsColoured(new AxisColors(null, null, Mark, null), Side.Right,
            "10.0", "12.5", "15.0", "17.5", "20.0", "22.5", "25.0", "27.5", "30.0", "Right");

    [Fact]
    public void SecondaryXAxis_SetXAxisColor_ColoursTheTopSpineTheTickMarksTheTickLabelsAndTheTitle_andNothingElse()
        => AssertOnlyOneAxisIsColoured(new AxisColors(null, null, null, Mark), Side.Top,
            "10.0", "12.5", "15.0", "17.5", "20.0", "22.5", "25.0", "27.5", "30.0", "Top");

    [Fact]
    public void FourAxesCarryFourColours_eachOnItsOwnAxis()
    {
        var red = Color.FromHex("#CC0000");
        var green = Color.FromHex("#00AA00");
        var blue = Color.FromHex("#0000CC");
        var svg = Svg(new AxisColors(red, green, blue, Mark));
        var spines = Spines(Lines(svg));

        Assert.Equal("#CC0000", SpineOf(spines, Side.Left).Stroke);
        Assert.Equal("#00AA00", SpineOf(spines, Side.Bottom).Stroke);
        Assert.Equal("#0000CC", SpineOf(spines, Side.Right).Stroke);
        Assert.Equal(MarkHex, SpineOf(spines, Side.Top).Stroke);
        Assert.Contains("Level", TextsFilledWith(Lines(svg), "#CC0000"));
        Assert.Contains("Time", TextsFilledWith(Lines(svg), "#00AA00"));
        Assert.Contains("Right", TextsFilledWith(Lines(svg), "#0000CC"));
        Assert.Contains("Top", TextsFilledWith(Lines(svg), MarkHex));
    }

    // ── what each verb writes in the model ──────────────────────────────────

    [Fact]
    public void SetYAxisColor_WritesTheSpineTheTicksAndTheTitleStyleOfTheLeftAxis()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYAxisColor(Mark)).Build().SubPlots[0];

        Assert.Equal(Mark, axes.Spines.Left.Color);
        Assert.Equal(Mark, axes.YAxis.MajorTicks.Color);
        Assert.Equal(Mark, axes.YAxis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axes.YAxis.LabelStyle!.Color);
        Assert.Null(axes.Spines.Bottom.Color);
        Assert.Null(axes.XAxis.MajorTicks.Color);
        Assert.Null(axes.XAxis.LabelStyle);
    }

    [Fact]
    public void SetXAxisColor_WritesTheSpineTheTicksAndTheTitleStyleOfTheBottomAxis()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetXAxisColor(Mark)).Build().SubPlots[0];

        Assert.Equal(Mark, axes.Spines.Bottom.Color);
        Assert.Equal(Mark, axes.XAxis.MajorTicks.Color);
        Assert.Equal(Mark, axes.XAxis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axes.XAxis.LabelStyle!.Color);
        Assert.Null(axes.Spines.Left.Color);
        Assert.Null(axes.YAxis.MajorTicks.Color);
        Assert.Null(axes.YAxis.LabelStyle);
    }

    [Fact]
    public void RightYAxis_SetYAxisColor_WritesTheRightSpineAndTheRightAxis()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithAddRightYAxis(s => s.SetYAxisColor(Mark))).Build().SubPlots[0];

        Assert.Equal(Mark, axes.Spines.Right.Color);
        Assert.Equal(Mark, axes.YAxes.Right[0].Axis.MajorTicks.Color);
        Assert.Equal(Mark, axes.YAxes.Right[0].Axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axes.YAxes.Right[0].Axis.LabelStyle!.Color);
        Assert.Null(axes.Spines.Left.Color);
        Assert.Null(axes.YAxis.MajorTicks.Color);
    }

    [Fact]
    public void SecondaryXAxis_SetXAxisColor_WritesTheTopSpineAndTheSecondaryAxis()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithSecondaryXAxis(s => s.SetXAxisColor(Mark))).Build().SubPlots[0];

        Assert.Equal(Mark, axes.Spines.Top.Color);
        Assert.Equal(Mark, axes.SecondaryXAxis!.MajorTicks.Color);
        Assert.Equal(Mark, axes.SecondaryXAxis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axes.SecondaryXAxis.LabelStyle!.Color);
        Assert.Null(axes.Spines.Bottom.Color);
        Assert.Null(axes.XAxis.MajorTicks.Color);
    }

    [Fact]
    public void SetYAxisColor_KeepsWhatTheLabelStyleAndTheTicksAlreadySay()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
                .SetYLabel("Level", style => style with { FontSize = 21, Pad = 3 })
                .WithYTickLabelRotation(40)
                .SetYAxisColor(Mark))
            .Build().SubPlots[0];

        Assert.Equal(21, axes.YAxis.LabelStyle!.FontSize);
        Assert.Equal(3, axes.YAxis.LabelStyle.Pad);
        Assert.Equal(Mark, axes.YAxis.LabelStyle.Color);
        Assert.Equal(40, axes.YAxis.MajorTicks.LabelRotation);
        Assert.Equal(Mark, axes.YAxis.MajorTicks.Color);
    }

    [Fact]
    public void SetYAxisColor_KeepsTheOtherSettingsOfTheSpine()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
                .WithSpines(s => s with { Left = s.Left with { LineWidth = 3 } })
                .SetYAxisColor(Mark))
            .Build().SubPlots[0];

        Assert.Equal(3, axes.Spines.Left.LineWidth);
        Assert.Equal(Mark, axes.Spines.Left.Color);
    }

    [Fact]
    public void TheVerbsReturnTheirOwnBuilder_soTheyChain()
    {
        Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Same(ax, ax.SetYAxisColor(Mark));
            Assert.Same(ax, ax.SetXAxisColor(Mark));
            ax.WithAddRightYAxis(s => Assert.Same(s, s.SetYAxisColor(Mark)));
            ax.WithSecondaryXAxis(s => Assert.Same(s, s.SetXAxisColor(Mark)));
        }).Build();
    }
}
