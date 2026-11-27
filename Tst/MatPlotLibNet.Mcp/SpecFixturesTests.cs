// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit.Sdk;
using static MatPlotLibNet.Mcp.Tests.SpecFixtures;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The reader, the description and the readings of the schema the spec tests share.</summary>
public sealed class SpecFixturesTests
{
    [Fact]
    public void TheReader_ReadsAFigureSpec()
    {
        var figure = Reader.Read(new ChartSpec("""{"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[3,4]}]}]}"""));

        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void ASection_IsTheLinesUnderItsHeading_ToTheFirstBlankLine()
    {
        var lines = Description.SectionLines("Axis link kinds");

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.NotEqual(0, line.Trim().Length));
        Assert.Throws<TrueException>(() => Description.SectionLines("A heading no schema has"));
    }

    [Fact]
    public void TheFieldsOfAKind_AreTheLinesIndentedUnderIt_ByName()
    {
        Assert.Equal(["scale", "offset"], Description.FieldsListedUnder("Axis link kinds", "linear"));
        var ex = Assert.Throws<TrueException>(() => Description.FieldsListedUnder("Axis link kinds", "no-such-kind"));
        Assert.Contains("'no-such-kind' under 'Axis link kinds'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALinesFieldName_IsTheTextBeforeItsColon()
    {
        Assert.Equal("anchor", "    anchor: point (required)".FieldName());
    }
}
