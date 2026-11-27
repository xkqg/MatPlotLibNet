// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit.Sdk;

namespace MatPlotLibNet.Tests;

/// <summary>The row every theory over what a drawing carries is made of: it holds the markup to what it must carry and to what it
/// must not, and names itself when it is wrong.</summary>
public sealed class DrawnTokenTests
{
    [Fact]
    public void ARowWhoseMarkupCarriesItsTokensAndNoneOfTheOthers_Holds()
    {
        new DrawnToken("a pair", () => "<g><rect/></g>", "<g>", "<rect") { Absent = ["<circle"] }.AssertHolds();
    }

    [Fact]
    public void ATokenTheMarkupLacks_NamesTheRowAndTheToken()
    {
        var wrong = Assert.Throws<TrueException>(() => new DrawnToken("a rect", () => "<g/>", "<rect").AssertHolds());

        Assert.Contains("a rect", wrong.Message, StringComparison.Ordinal);
        Assert.Contains("lacks [<rect]", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATokenTheMarkupCarriesThatTheRowForbids_NamesTheRowAndTheToken()
    {
        var wrong = Assert.Throws<TrueException>(() =>
            new DrawnToken("no circle", () => "<circle/>") { Absent = ["<circle"] }.AssertHolds());

        Assert.Contains("no circle", wrong.Message, StringComparison.Ordinal);
        Assert.Contains("carries [<circle]", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARowThatIgnoresCase_FindsATokenInTheOtherCase_AndOneThatDoesNotMissesIt()
    {
        new DrawnToken("loud", () => "<G ID=\"X\">", "<g id=\"x\">") { Comparison = StringComparison.OrdinalIgnoreCase }.AssertHolds();

        Assert.Throws<TrueException>(() => new DrawnToken("exact", () => "<G/>", "<g/>").AssertHolds());
    }

    [Fact]
    public void ARowThatMustBeFinite_RefusesAMissingValueAndAnInfinity_ButNotAScriptThatTestsForOne_AndNamesItself()
    {
        new DrawnToken("a picture", () => "<path d=\"M 1 2\"/><script>if (isNaN(x)) return;</script>") { Finite = true }.AssertHolds();
        new DrawnToken("a loose one", () => "<path d=\"M NaN 2\"/>").AssertHolds();

        var wrong = Assert.ThrowsAny<XunitException>(() => new DrawnToken("a gap", () => "<circle cx=\"-Infinity\"/>") { Finite = true }.AssertHolds());

        Assert.Contains("a gap carries a missing value or an infinity", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARowReadsAsItsName()
    {
        Assert.Equal("draws a bar", new DrawnToken("draws a bar", () => "", "x").ToString());
    }
}
