// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary><c>FromJson</c> reads every list of numbers into an array of exactly its length. System.Text.Json's own reader
/// for a <c>double[]</c> fills a growing list and copies it out, and that growth was most of what reading a figure
/// allocated. The converter must read what System.Text.Json reads, bit for bit, and refuse what it refuses.</summary>
public class DoubleArrayReadConverterTests
{
    private static readonly ChartSerializer Serializer = new();

    /// <summary>System.Text.Json reading a <c>double[]</c> by itself, with the library's number rule.</summary>
    private static readonly JsonSerializerOptions BuiltIn = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

    private static readonly JsonSerializerOptions WithConverter = new(BuiltIn) { Converters = { new DoubleArrayReadConverter() } };

    private static readonly JsonSerializerOptions StrictNumbers = new() { Converters = { new DoubleArrayReadConverter() } };

    public static TheoryData<string> Lists() => new()
    {
        "[]",
        "[1]",
        "[1.5,-2E-300,3.0000000000000004,0,-0,1.7976931348623157E+308,5E-324]",
        """["NaN","Infinity","-Infinity",4]""",
        """["NaN"]""",
        " [ 1 , 2 ] ",
        LongList(5_000),
    };

    [Theory]
    [MemberData(nameof(Lists))]
    public void AList_IsReadBitForBit_AsSystemTextJsonReadsIt(string json)
    {
        double[] expected = JsonSerializer.Deserialize<double[]>(json, BuiltIn)!;

        double[] actual = JsonSerializer.Deserialize<double[]>(json, WithConverter)!;

        Assert.Equal(expected.Select(BitConverter.DoubleToInt64Bits), actual.Select(BitConverter.DoubleToInt64Bits));
    }

    /// <summary>A list inside a list (a heatmap's rows) is read by the converter row by row.</summary>
    [Fact]
    public void ARowOfAGrid_IsReadByTheConverter()
    {
        double[][] rows = JsonSerializer.Deserialize<double[][]>("""[[1,2],[],["NaN"]]""", WithConverter)!;

        Assert.Equal([1.0, 2.0], rows[0]);
        Assert.Empty(rows[1]);
        Assert.True(double.IsNaN(Assert.Single(rows[2])));
    }

    [Fact]
    public void ANullList_StaysNull()
    {
        Assert.Null(JsonSerializer.Deserialize<double[]?>("null", WithConverter));
    }

    public static TheoryData<string> Refused() => new()
    {
        "[1,null]",
        """[1,"x"]""",
        """["nan"]""",
        "[[1]]",
        "[{}]",
        "[true]",
        "1",
        "{}",
        "\"NaN\"",
    };

    /// <summary>What System.Text.Json refuses, the converter refuses, as a <see cref="JsonException"/>.</summary>
    [Theory]
    [MemberData(nameof(Refused))]
    public void WhatSystemTextJsonRefuses_IsRefused(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double[]>(json, BuiltIn));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double[]>(json, WithConverter));
    }

    /// <summary>The number rule is the options', not the converter's: without the named literals a "NaN" is refused, as
    /// System.Text.Json refuses it.</summary>
    [Fact]
    public void ANamedNumber_IsRefused_WhenTheOptionsDoNotAllowIt()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double[]>("""["NaN"]""", new JsonSerializerOptions()));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double[]>("""["NaN"]""", StrictNumbers));
    }

    /// <summary>The converter only reads; the writer keeps System.Text.Json's own list writing, so a written figure keeps its
    /// bytes by construction.</summary>
    [Fact]
    public void Writing_IsNotWhatTheConverterDoes()
    {
        using var writer = new Utf8JsonWriter(new MemoryStream());

        Assert.Throws<NotSupportedException>(() => new DoubleArrayReadConverter().Write(writer, [1.0], WithConverter));
    }

    /// <summary>Reading a line of 10,000 points allocates its two arrays and little else: no growing list per array. Before
    /// the converter the same read allocated about four times the arrays' size.</summary>
    [Fact]
    public void FromJson_ReadsALongLine_WithoutTheGrowthOfAList()
    {
        double[] x = [.. Enumerable.Range(0, 10_000).Select(i => (double)i)];
        string json = Serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, x)).Build());
        Serializer.FromJson(json);
        Serializer.FromJson(json);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Serializer.FromJson(json);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        long arrays = 2L * x.Length * sizeof(double);
        Assert.True(allocated < arrays * 3 / 2, $"reading allocated {allocated:N0} B for {arrays:N0} B of arrays");
    }

    private static string LongList(int count)
    {
        var json = new StringBuilder("[");
        for (int i = 0; i < count; i++)
        {
            if (i > 0) json.Append(',');
            json.Append((i * 0.1 - 7).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
        return json.Append(']').ToString();
    }
}
