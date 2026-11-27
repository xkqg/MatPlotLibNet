// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

/// <summary>Verifies <see cref="NamedTable{TKey, TValue}"/>, the one table behind the library's six name registries:
/// last writer wins, a missing name answers <see langword="null"/>, the comparer decides what counts as the same name,
/// the names are a snapshot, and many threads can read and write at once.</summary>
public class NamedTableTests
{
    private sealed class Item(string label)
    {
        public string Label { get; } = label;
    }

    [Fact]
    public void Get_OfAMissingName_IsNull()
    {
        var table = new NamedTable<string, Item>();

        Assert.Null(table.Get("absent"));
    }

    [Fact]
    public void Get_ReturnsTheSameInstanceThatSetStored()
    {
        var table = new NamedTable<string, Item>();
        var item = new Item("one");

        table.Set("one", item);

        Assert.Same(item, table.Get("one"));
    }

    [Fact]
    public void WithoutAComparer_NamesAreOrdinal_SoCaseMatters()
    {
        var table = new NamedTable<string, Item>();
        var lower = new Item("lower");
        table.Set("line", lower);

        Assert.Null(table.Get("LINE"));
        Assert.Same(lower, table.Get("line"));
    }

    [Fact]
    public void WithAnIgnoreCaseComparer_AnySpellingFindsTheEntry()
    {
        var table = new NamedTable<string, Item>(StringComparer.OrdinalIgnoreCase);
        var item = new Item("viridis");
        table.Set("Viridis", item);

        Assert.Same(item, table.Get("viridis"));
        Assert.Same(item, table.Get("VIRIDIS"));
    }

    /// <summary>A second set of the same name keeps the last writer and one name: under the ordinal comparer the name as it is, and under
    /// an ignore-case comparer the first spelling.</summary>
    [Theory]
    [InlineData(false, "name", "name", "name", "name")]
    [InlineData(true, "Seaborn", "SEABORN", "seaborn", "Seaborn")]
    public void ASecondSetOfTheSameName_KeepsTheLastWriterAndTheFirstSpelling(bool ignoreCase, string firstName, string secondName, string lookup, string kept)
    {
        var table = ignoreCase ? new NamedTable<string, Item>(StringComparer.OrdinalIgnoreCase) : new NamedTable<string, Item>();
        var first = new Item("first");
        var second = new Item("second");

        table.Set(firstName, first);
        table.Set(secondName, second);

        Assert.Same(second, table.Get(lookup));
        Assert.Equal([kept], table.Keys);
    }

    [Fact]
    public void ANullName_IsRefused_ByGetAndBySet()
    {
        var table = new NamedTable<string, Item>();

        Assert.Throws<ArgumentNullException>(() => table.Get(null!));
        Assert.Throws<ArgumentNullException>(() => table.Set(null!, new Item("x")));
    }

    [Fact]
    public void Keys_AreASnapshot_ALaterSetDoesNotChangeIt()
    {
        var table = new NamedTable<string, Item>();
        table.Set("a", new Item("a"));
        var snapshot = table.Keys;

        table.Set("b", new Item("b"));

        Assert.Equal(["a"], snapshot);
        Assert.Equal(2, table.Keys.Count);
    }

    [Fact]
    public void Clear_RemovesEveryEntry()
    {
        var table = new NamedTable<string, Item>();
        table.Set("a", new Item("a"));
        table.Set("b", new Item("b"));

        table.Clear();

        Assert.Empty(table.Keys);
        Assert.Null(table.Get("a"));
    }

    [Fact]
    public void AnEnumCanBeTheName()
    {
        var table = new NamedTable<CoordinateSystem, Item>();
        var polar = new Item("polar");
        table.Set(CoordinateSystem.Polar, polar);

        Assert.Same(polar, table.Get(CoordinateSystem.Polar));
        Assert.Null(table.Get(CoordinateSystem.Ternary));
    }

    [Fact]
    public void ManyThreadsWritingDistinctNames_LoseNone()
    {
        var table = new NamedTable<string, Item>();
        const int threads = 8, perThread = 500;

        Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
        {
            for (int i = 0; i < perThread; i++)
                table.Set($"t{t}-{i}", new Item($"t{t}-{i}"));
        });

        Assert.Equal(threads * perThread, table.Keys.Count);
        for (int t = 0; t < threads; t++)
            for (int i = 0; i < perThread; i++)
                Assert.Equal($"t{t}-{i}", table.Get($"t{t}-{i}")!.Label);
    }

    [Fact]
    public void ManyThreadsWritingOneName_EndWithOneOfTheValuesWritten_AndReadersNeverSeeNothing()
    {
        var table = new NamedTable<string, Item>();
        table.Set("hot", new Item("seed"));
        var written = Enumerable.Range(0, 8).Select(t => new Item($"w{t}")).ToArray();
        int missed = 0;

        Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 16 }, t =>
        {
            for (int i = 0; i < 1000; i++)
            {
                if (t < 8) table.Set("hot", written[t]);
                else if (table.Get("hot") is null) Interlocked.Increment(ref missed);
            }
        });

        Assert.Equal(0, missed);
        Assert.Contains(table.Get("hot"), written);
        Assert.Equal(["hot"], table.Keys);
    }
}
