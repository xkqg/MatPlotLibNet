// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests;

/// <summary>The sentence a verb refuses arrays of another length in, written once for the tests that name a refusal: a test says which
/// counts, which holder and which parameter the verb names, and this says it in the library's words. The library's own tests
/// (<c>ArrayLengthExtensionsTests</c>, <c>BarGroupExtensionsTests</c>) pin the sentence itself.</summary>
public static class LengthRefusals
{
    /// <summary>How every refusal of arrays of another length begins.</summary>
    public const string Opening = "Array lengths must match.";

    extension(int length)
    {
        /// <summary>The message of the refusal of an array of <paramref name="length"/> values against one of <paramref name="other"/>.</summary>
        /// <param name="other">The length it had to be.</param>
        /// <param name="paramName">The parameter named in the exception; <see langword="null"/> names none.</param>
        /// <param name="of">What holds the array, named after both counts (<c>group 'south'</c>); <see langword="null"/> names none.</param>
        public string RefusalAgainst(int other, string? paramName = null, string? of = null) =>
            string.Concat($"{Opening} Got {length} and {other}", of is null ? "." : $" in {of}.", paramName is null ? "" : $" (Parameter '{paramName}')");
    }

    /// <summary>Checks that a verb that takes two named groups over <paramref name="categories"/> places refuses a group that does not carry one value
    /// for each, naming the group, both counts and the parameter (<c>from</c> or <c>to</c>) in the words every verb that takes groups uses, and
    /// refuses a group with no values at all by name rather than with a null reference.</summary>
    /// <param name="verb">Draws the two groups.</param>
    /// <param name="from">A group the verb accepts as its first.</param>
    /// <param name="to">A group the verb accepts as its second.</param>
    /// <param name="categories">How many places the verb draws the groups over.</param>
    public static void AssertAGroupThatDoesNotFitIsRefusedByName(Action<BarGroup, BarGroup> verb, BarGroup from, BarGroup to, int categories)
    {
        var shortFrom = new BarGroup(from.Label, [1.0, 2.0]);
        var longTo = new BarGroup(to.Label, [.. Enumerable.Range(1, categories + 1).Select(i => (double)i)]);

        var fromError = Assert.Throws<ArgumentException>(() => verb(shortFrom, to));
        var toError = Assert.Throws<ArgumentException>(() => verb(from, longTo));

        Assert.Equal("from", fromError.ParamName);
        Assert.Equal(2.RefusalAgainst(categories, "from", $"group '{from.Label}'"), fromError.Message);
        Assert.Equal("to", toError.ParamName);
        Assert.Equal((categories + 1).RefusalAgainst(categories, "to", $"group '{to.Label}'"), toError.Message);
        Assert.Equal("from", Assert.Throws<ArgumentException>(() => verb(default, to)).ParamName);
        Assert.Equal("to", Assert.Throws<ArgumentException>(() => verb(from, new BarGroup(to.Label, null!))).ParamName);
    }

    /// <summary>The message of the refusal of a grid of values whose shape the axes do not give.</summary>
    /// <param name="rows">How many rows the values have.</param>
    /// <param name="columns">How many columns the values have.</param>
    /// <param name="y">How many y values the axes give.</param>
    /// <param name="x">How many x values the axes give.</param>
    public static string Grid(int rows, int columns, int y, int x) =>
        $"{Opening} The values have {rows} rows and {columns} columns; the axes give {y} y and {x} x values.";
}
