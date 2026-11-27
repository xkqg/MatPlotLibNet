// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>One thing a test asks of the text a drawing writes (the markup of a figure, the text a render context or a writer has
/// appended, the JSON of a figure): it draws the text, and the text carries some tokens and none of others. A test class holds its
/// rows as one <c>TheoryData&lt;DrawnToken&gt;</c> and one theory that calls <see cref="AssertHolds"/>, so a chart that must write one
/// attribute is one line of data, not a method, however much it arranges before the drawing (the drawing is a block lambda then).</summary>
/// <param name="Name">What the row says, as the fact that held it was named; a failing row is named by it.</param>
/// <param name="Draw">Draws the text the row reads.</param>
/// <param name="Present">The tokens the markup must carry.</param>
public readonly record struct DrawnToken(string Name, Func<string> Draw, params string[] Present)
{
    /// <summary>The tokens the markup must not carry.</summary>
    public string[] Absent { get; init; } = [];

    /// <summary>Whether the markup must carry no missing value and no infinity: no <c>NaN</c> and no <c>Infinity</c> token, the scripts
    /// of a figure, which test <c>isNaN</c>, left out (<c>FiniteAssertions</c> says it). Kept apart from <see cref="Absent"/> for that reason.</summary>
    public bool Finite { get; init; }

    /// <summary>How the tokens are compared with the markup: exactly, unless the row says otherwise.</summary>
    public StringComparison Comparison { get; init; } = StringComparison.Ordinal;

    /// <summary>Draws the markup and holds it to the row.</summary>
    public void AssertHolds()
    {
        string markup = Draw();
        var comparison = Comparison;
        string[] missing = [.. Present.Where(token => !markup.Contains(token, comparison))];
        string[] unwanted = [.. Absent.Where(token => markup.Contains(token, comparison))];

        Assert.True(missing.Length == 0 && unwanted.Length == 0,
            $"{Name}: the markup lacks [{string.Join(", ", missing)}] and carries [{string.Join(", ", unwanted)}]:\n{markup}");
        if (Finite)
        {
            markup.AssertNoNonFiniteNumber(Name);
        }
    }

    /// <summary>The name of the row, so a failing case reads as the fact it replaced.</summary>
    public override string ToString() => Name;
}
