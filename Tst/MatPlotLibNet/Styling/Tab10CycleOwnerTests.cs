// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>matplotlib's default colour cycle (blue, orange, green, red, purple, brown, pink, grey, olive, cyan) is written in
/// one place, <see cref="QualitativeColorMaps"/>, and read from there by the themes that carry it as their cycle, the hue
/// palette a grouped chart starts from, and the named colours (<c>Colors.Tab10Blue</c> and its two siblings). Four copies of the
/// same ten hex literals once stood in four files; a change to one would have left the others drawing the old cycle.</summary>
public sealed class Tab10CycleOwnerTests
{
    private static readonly string[] Expected =
        ["#1F77B4", "#FF7F0E", "#2CA02C", "#D62728", "#9467BD", "#8C564B", "#E377C2", "#7F7F7F", "#BCBD22", "#17BECF"];

    private static string[] Hex(IEnumerable<Color> colors) => [.. colors.Select(c => c.ToHex().ToUpperInvariant())];

    [Fact]
    public void TheOwner_HoldsTheTenColoursOfMatplotlibsDefaultCycle()
    {
        Assert.Equal(Expected, Hex(QualitativeColorMaps.Tab10Cycle()));
    }

    [Fact]
    public void TheNamedMap_IsTheSameTenColours()
    {
        var sampled = Enumerable.Range(0, 10).Select(i => QualitativeColorMaps.Tab10.GetColor((i + 0.5) / 10));

        Assert.Equal(Expected, Hex(sampled));
    }

    [Fact]
    public void EveryReaderOfTheCycle_DrawsTheOwnersColours()
    {
        Assert.Equal(Expected, Hex(Theme.Default.CycleColors));
        Assert.Equal(Expected, Hex(Theme.Dark.CycleColors));
        Assert.Equal(Expected, Hex(Theme.MatplotlibV2.CycleColors));
        Assert.Equal(Expected, Hex(HueGrouper.DefaultPalette));
    }

    [Fact]
    public void TheTenNamedColours_AreTheTenOfTheCycle_InOrder()
    {
        var cycle = QualitativeColorMaps.Tab10Cycle();

        Assert.Equal(
            [Colors.Tab10Blue, Colors.Tab10Orange, Colors.Tab10Green, Colors.Tab10Red, Colors.Tab10Purple,
             Colors.Tab10Brown, Colors.Tab10Pink, Colors.Tab10Gray, Colors.Tab10Olive, Colors.Tab10Cyan],
            cycle.Take(10));
    }

    /// <summary>The twenty colours of <c>tab20</c> are the ten hues of the cycle, each followed by its lighter tint: the hues are read
    /// from the owner, the ten tints are the only literals the map adds.</summary>
    [Fact]
    public void Tab20_StandsTheTenHuesOfTheCycleAtItsEvenPlaces()
    {
        var map = (ListedColorMap)QualitativeColorMaps.Tab20;
        var cycle = QualitativeColorMaps.Tab10Cycle();
        var twenty = Enumerable.Range(0, 20).Select(i => map.GetColor((i + 0.5) / 20)).ToArray();

        for (int hue = 0; hue < 10; hue++)
            Assert.Equal(cycle[hue], twenty[2 * hue]);
        Assert.Equal(
            ["#AEC7E8", "#FFBB78", "#98DF8A", "#FF9896", "#C5B0D5", "#C49C94", "#F7B6D2", "#C7C7C7", "#DBDB8D", "#9EDAE5"],
            Hex(Enumerable.Range(0, 10).Select(hue => twenty[2 * hue + 1])));
    }

    /// <summary>The ten hex literals of the cycle stand once in the library's code: the owner's list. Tab20, the named colours and the
    /// Ichimoku cloud read them from it.</summary>
    [Fact]
    public void TheTenHexLiteralsOfTheCycle_AreWrittenOnce()
    {
        var owner = MatPlotLibNet.Tests.Packaging.RepositoryTree.SourceFiles()
            .Single(f => f.Path == "Src/MatPlotLibNet/Styling/ColorMaps/QualitativeColorMaps.cs");
        var spelled = owner.CodeLines
            .Count(line => System.Text.RegularExpressions.Regex.IsMatch(line, "(?i)\"#(1f77b4|ff7f0e|2ca02c|d62728|9467bd|8c564b|e377c2|7f7f7f|bcbd22|17becf)\""));

        Assert.Equal(10, spelled);
    }

    /// <summary>Whoever asks for the cycle gets an array of its own: one that changes its array (a theme edited in place)
    /// cannot repaint the themes, the palette and the map that read the same owner.</summary>
    [Fact]
    public void EachAskForTheCycle_GivesAnArrayOfItsOwn()
    {
        var first = QualitativeColorMaps.Tab10Cycle();
        first[0] = Colors.Black;

        Assert.Equal(Expected[0], Hex(QualitativeColorMaps.Tab10Cycle())[0]);
        Assert.Equal(Expected[0], Hex(Theme.Default.CycleColors)[0]);
        Assert.NotSame(Theme.Default.CycleColors, Theme.Dark.CycleColors);
        Assert.NotSame(Theme.Default.CycleColors, Theme.MatplotlibV2.CycleColors);
    }
}
