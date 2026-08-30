// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A cell that reports a MEASURE says the name small and the number big — the stat tile's anatomy,
/// inside a treemap cell (Datadog's host hexes, a finviz market map, Task Manager's per-core grid all read this
/// way). One line with both, at one size, is a line you have to read rather than a number you can see.
/// <para>A LEAF stacks them: name on top, headline under it, larger. An INTERIOR node has only its header
/// strip, so its headline sits at the right end of that strip — the parent's total beside the parent's name.</para></summary>
public class TreemapHeadlineTests
{
    private static readonly Regex Text = new("<text[^>]*x=\"([-0-9.]+)\"[^>]*y=\"([-0-9.]+)\"[^>]*font-size=\"([0-9.]+)\"[^>]*>([^<]*)<", RegexOptions.Compiled);

    private readonly record struct Drawn(double X, double Y, double Size, string Value);

    private static List<Drawn> Texts(TreeNode root, Action<TreemapSeries>? configure = null)
    {
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, configure))
            .ToSvg();
        return [.. Text.Matches(svg).Select(m => new Drawn(
            double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
            m.Groups[4].Value))];
    }

    private static TreeNode Fleet(string? headline = null) => new()
    {
        Label = "Ait",
        Headline = headline,
        Children =
        [
            new() { Label = "Ait.Cortex", Headline = "13 %", Value = 1 },
            new() { Label = "Ait.Obs", Headline = "11 %", Value = 1 },
        ],
    };

    [Fact]
    public void ALeafsHeadline_IsDrawnUnderItsNameAndLarger()
    {
        var texts = Texts(Fleet());

        var name = texts.Single(t => t.Value == "Ait.Cortex");
        var headline = texts.Single(t => t.Value == "13 %");
        Assert.True(headline.Y > name.Y, "the number sits UNDER the name");
        Assert.True(headline.Size > name.Size, "and it is the thing you can see from across a room");
        Assert.Equal(name.X, headline.X);
    }

    [Fact]
    public void TheHeadlineSize_IsTheSeriesKnob()
    {
        var texts = Texts(Fleet(), s => s.HeadlineFontSize = 30);

        Assert.Equal(30, texts.Single(t => t.Value == "13 %").Size);
    }

    [Fact]
    public void WithoutAHeadline_ACellIsExactlyWhatItWas()
    {
        var plain = new TreeNode { Label = "Ait", Children = [new() { Label = "Ait.Cortex", Value = 1 }] };

        var texts = Texts(plain);

        Assert.DoesNotContain(texts, t => t.Value == "13 %");
        Assert.Equal(1, texts.Count(t => t.Value == "Ait.Cortex"));
    }

    /// <summary>An interior node has an 18-px header strip and no body of its own: its headline — the parent's
    /// total — rides the RIGHT end of that strip, at the label's own size.</summary>
    [Fact]
    public void AnInteriorHeadline_RidesTheRightEndOfItsHeader()
    {
        var texts = Texts(Fleet("24 % · 25k msg"));

        var label = texts.Single(t => t.Value == "Ait");
        var total = texts.Single(t => t.Value == "24 % · 25k msg");
        Assert.Equal(label.Y, total.Y);                     // the same header line
        Assert.True(total.X > label.X + 100, "at the far end of it");
        Assert.Equal(label.Size, total.Size);                // a header strip has no room to shout
    }

    /// <summary>A headline that cannot fit its cell is dropped, never painted across the neighbours — and the
    /// NAME goes first when only one of the two fits, because a number without its subject says nothing.</summary>
    [Fact]
    public void ACellTooSmallForBoth_KeepsTheName()
    {
        // Six cells across 600 px: a short name fits, a long measure does not.
        var crowded = new TreeNode
        {
            Label = "Ait",
            Children = [.. Enumerable.Range(0, 6).Select(i => new TreeNode
            {
                Label = $"P{i}", Headline = $"{i}0 % · 12 345 msg · 6 972 KiB", Value = 1,
            })],
        };

        var texts = Texts(crowded, s => s.LabelFit = TreemapLabelFit.Fit);

        Assert.Equal(6, texts.Count(t => t.Value.StartsWith('P')));
        Assert.DoesNotContain(texts, t => t.Value.Contains("msg", StringComparison.Ordinal));
    }

    /// <summary>And a cell whose NAME did not fit shows no bare number either.</summary>
    [Fact]
    public void ACellWhoseNameWasDropped_ShowsNoHeadline()
    {
        var crowded = new TreeNode
        {
            Label = "Ait",
            Children = [.. Enumerable.Range(0, 24).Select(i => new TreeNode
            {
                Label = $"Ait.LongProcessName{i}", Headline = $"{i} %", Value = 1,
            })],
        };

        var texts = Texts(crowded, s => s.LabelFit = TreemapLabelFit.Fit);

        Assert.DoesNotContain(texts, t => t.Value.StartsWith("Ait.LongProcessName", StringComparison.Ordinal));
        Assert.DoesNotContain(texts, t => t.Value.EndsWith(" %", StringComparison.Ordinal));
    }

    [Fact]
    public void TheHeadline_SurvivesTheWire()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Treemap(Fleet("24 %"), s => s.HeadlineFontSize = 26)).Build();
        var serializer = new MatPlotLibNet.Serialization.ChartSerializer();

        var back = serializer.FromJson(serializer.ToJson(figure));

        // The treemap's DTO carries no tree at all (by design — the wall publishes SVG), so this pins the ONE
        // thing a caller could otherwise believe: the round-trip is lossy, and it says so.
        Assert.Empty(back.SubPlots[0].Series.OfType<TreemapSeries>().Single().Root.Children);
    }
}
