// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The sentence a verb refuses arrays of another length in, written once for the tests: the counts the verb names, what holds
/// the array and the parameter at fault, each as the library words them.</summary>
public sealed class LengthRefusalsTests
{
    [Fact]
    public void ARefusal_NamesBothCounts()
    {
        Assert.Equal("Array lengths must match. Got 3 and 2.", 3.RefusalAgainst(2));
    }

    [Fact]
    public void ARefusal_NamesTheParameter_AsTheExceptionMessageDoes()
    {
        Assert.Equal("Array lengths must match. Got 3 and 4. (Parameter 'values')", 3.RefusalAgainst(4, "values"));
    }

    [Fact]
    public void ARefusal_NamesWhatHoldsTheArray_AfterBothCounts()
    {
        Assert.Equal("Array lengths must match. Got 2 and 6 in group 'Before'.", 2.RefusalAgainst(6, of: "group 'Before'"));
        Assert.Equal("Array lengths must match. Got 2 and 6 in group 'Before'. (Parameter 'from')", 2.RefusalAgainst(6, "from", "group 'Before'"));
    }

    [Fact]
    public void AGridRefusal_NamesTheValuesAndTheAxes()
    {
        Assert.Equal("Array lengths must match. The values have 2 rows and 3 columns; the axes give 2 y and 4 x values.", LengthRefusals.Grid(2, 3, 2, 4));
    }

    [Fact]
    public void EveryRefusal_BeginsWithTheOpening()
    {
        Assert.StartsWith(LengthRefusals.Opening, 1.RefusalAgainst(2), StringComparison.Ordinal);
        Assert.StartsWith(LengthRefusals.Opening, LengthRefusals.Grid(1, 1, 2, 2), StringComparison.Ordinal);
        Assert.Equal("Array lengths must match.", LengthRefusals.Opening);
    }

    [Fact]
    public void TheLibrarysOwnRefusal_IsTheOneTheFixtureWrites()
    {
        var error = Assert.Throws<ArgumentException>(() => 2.ThrowIfMisaligned(6, "from", "group 'Before'"));

        Assert.Equal(2.RefusalAgainst(6, "from", "group 'Before'"), error.Message);
    }
}
