// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A cell may say more than one thing about itself: a name, and under it the measures that do not fit
/// beside it. <see cref="TreeNode.Label"/> splits on newlines and the lines stack — the same rule
/// <see cref="StatTileSeries.Caption"/> already has, because a tile and a cell answer the same kind of
/// question and a wall should not carry two idioms for it.</summary>
public class TreemapMultilineLabelTests
{
    private static readonly Regex Text = new("<text[^>]*y=\"([-0-9.]+)\"[^>]*>([^<]*)<", RegexOptions.Compiled);

    private static List<(double Y, string Value)> Texts(TreeNode root, Action<TreemapSeries>? configure = null)
    {
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, configure))
            .ToSvg();
        return [.. Text.Matches(svg).Select(m => (
            double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value))];
    }

    [Fact]
    public void ALabelWithANewline_StacksItsLines()
    {
        var texts = Texts(new TreeNode
        {
            Label = "Ait",
            Children = [new() { Label = "Ait.Binance\n14 920 msg · 6 973 KiB", Value = 1 }],
        });

        var name = texts.Single(t => t.Value == "Ait.Binance");
        var measures = texts.Single(t => t.Value == "14 920 msg · 6 973 KiB");
        Assert.True(measures.Y > name.Y, "the second line sits under the first");
    }

    [Fact]
    public void TheHeadline_StillSitsUnderTheWholeLabel()
    {
        var texts = Texts(new TreeNode
        {
            Label = "Ait",
            Children = [new() { Label = "Ait.Binance\n14 920 msg", Headline = "12 %", Value = 1 }],
        });

        var second = texts.Single(t => t.Value == "14 920 msg");
        var headline = texts.Single(t => t.Value == "12 %");
        Assert.True(headline.Y > second.Y, "the big number comes after every label line");
    }

    [Fact]
    public void ASingleLineLabel_IsExactlyWhatItWas()
    {
        var texts = Texts(new TreeNode { Label = "Ait", Children = [new() { Label = "Ait.Binance", Value = 1 }] });

        Assert.Equal(2, texts.Count);   // the root's header and the leaf's name, and nothing else
    }

    /// <summary>Each line is fitted on its own: a name that fits is drawn even when the measures beside it
    /// do not.</summary>
    [Fact]
    public void EachLine_IsFittedOnItsOwn()
    {
        var texts = Texts(
            new TreeNode
            {
                Label = "Ait",
                Children = [.. Enumerable.Range(0, 6).Select(i => new TreeNode
                {
                    Label = $"P{i}\n{i} 000 000 msg · {i} 000 000 KiB long", Value = 1,
                })],
            },
            s => s.LabelFit = TreemapLabelFit.Fit);

        Assert.Equal(6, texts.Count(t => t.Value.StartsWith('P')));
        Assert.DoesNotContain(texts, t => t.Value.Contains("KiB long", StringComparison.Ordinal));
    }

    /// <summary>An INTERIOR node's header is one strip: it keeps the FIRST line and nothing else, because a
    /// strip has no second row to give.</summary>
    [Fact]
    public void AnInteriorHeader_KeepsOnlyItsFirstLine()
    {
        var texts = Texts(new TreeNode
        {
            Label = "Ait\nnot on a strip",
            Children = [new() { Label = "Ait.Binance", Value = 1 }],
        });

        Assert.Contains(texts, t => t.Value == "Ait");
        Assert.DoesNotContain(texts, t => t.Value == "not on a strip");
    }
}
