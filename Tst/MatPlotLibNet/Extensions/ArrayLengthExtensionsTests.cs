// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The one check a verb makes when it takes an x and a y array that must pair up, worded the same way everywhere:
/// <c>x has 3 values and y has 2; there must be one y per x.</c>, naming the parameter <c>y</c>. Three verbs
/// (<c>ConnectedScatter</c>, <c>Horizon</c>, <c>Triplot</c>) each wrote that sentence by hand before the check had an owner.</summary>
public sealed class ArrayLengthExtensionsTests
{
    private const string Refusal = "x has 3 values and y has 2; there must be one y per x. (Parameter 'y')";

    [Fact]
    public void OneYPerX_Passes()
    {
        var error = Record.Exception(() => new double[3].ThrowIfNotOneYPerX(new double[3]));

        Assert.Null(error);
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(2, 3)]
    public void AnyOtherCount_IsRefusedByBothCountsAndTheParameterY(int xs, int ys)
    {
        var error = Assert.Throws<ArgumentException>(() => new double[xs].ThrowIfNotOneYPerX(new double[ys]));

        Assert.Equal($"x has {xs} values and y has {ys}; there must be one y per x. (Parameter 'y')", error.Message);
        Assert.Equal("y", error.ParamName);
    }

    // ── every row as long as the first ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LengthsThatAreAllTheFirsts_Pass()
    {
        Assert.Null(Record.Exception(() => new[] { 3, 3, 3 }.ThrowIfNotAllEqual("rows")));
        Assert.Null(Record.Exception(() => Array.Empty<int>().ThrowIfNotAllEqual("rows")));
    }

    [Fact]
    public void ALengthThatIsNotTheFirsts_IsRefusedInTheOwnersWords()
    {
        var error = Assert.Throws<ArgumentException>(() => new[] { 3, 3, 4 }.ThrowIfNotAllEqual("rows"));

        Assert.Equal("Array lengths must match. Got 4 and 3 in row 2. (Parameter 'rows')", error.Message);
    }

    [Fact]
    public void LengthsHeldToAGivenLength_AreRefusedFromTheFirstRowOn()
    {
        Assert.Null(Record.Exception(() => new[] { 2, 2 }.ThrowIfNotAllEqual("rows", expected: 2)));

        var error = Assert.Throws<ArgumentException>(() => new[] { 3, 2 }.ThrowIfNotAllEqual("assetCloses", "asset", 2));

        Assert.Equal("Array lengths must match. Got 3 and 2 in asset 0. (Parameter 'assetCloses')", error.Message);
    }

    [Fact]
    public void AQualifier_IsNamedAfterTheTwoLengths()
    {
        var error = Assert.Throws<ArgumentException>(() => 2.ThrowIfMisaligned(3, "groups", "group 'south'"));

        Assert.Equal("Array lengths must match. Got 2 and 3 in group 'south'. (Parameter 'groups')", error.Message);
    }

    /// <summary>The sites that refused arrays of another length in words of their own before the owner took them over (R49), reached
    /// through the public surface: each now refuses in the owner's sentence.</summary>
    public static TheoryData<string, Action> FormerSites => new()
    {
        { "BarSeries(x, values)", () => _ = new MatPlotLibNet.Models.Series.BarSeries([1.0, 2.0], [1.0]) },
        { "StreamingSeries.AppendPoints", () => new MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries().AppendPoints([1.0, 2.0], [1.0]) },
        { "ChartDataTable", () => _ = new MatPlotLibNet.Models.ChartDataTable(null, [new("a")], [[]]) },
        { "PairGridSeries", () => _ = new MatPlotLibNet.Models.Series.PairGridSeries([[1.0, 2.0], [1.0]]) },
        { "RelativeRotationSeries labels", () => _ = new MatPlotLibNet.Models.Series.RelativeRotationSeries([[1.0]], [1.0], ["a", "b"]) },
        { "RelativeRotationSeries assets", () => _ = new MatPlotLibNet.Models.Series.RelativeRotationSeries([[1.0, 2.0]], [1.0], ["a"]) },
        { "Plt.Mosaic", () => Plt.Mosaic("AB\nCDE").Build() },
        { "CategoryFormatter", () => _ = new MatPlotLibNet.Rendering.TickFormatters.CategoryFormatter(["a", "b"], [1.0]) },
    };

    [Theory]
    [MemberData(nameof(FormerSites))]
    public void AFormerSite_RefusesInTheOwnersSentence(string site, Action make)
    {
        var error = Assert.Throws<ArgumentException>(make);

        Assert.True(error.Message.StartsWith("Array lengths must match. Got ", StringComparison.Ordinal), $"{site}: {error.Message}");
    }

    // ── a function's result, one value per sample ──────────────────────────────────────────────────────────────

    [Fact]
    public void AResultOfOneValuePerSample_IsHandedBack()
    {
        double[] result = [1, 2, 3];

        Assert.Same(result, result.ThrowIfNotGivenFor(3, "The window function"));
    }

    [Theory]
    [InlineData(2, "The window function gave 2 values for 3 samples; there must be one value per sample.")]
    [InlineData(-1, "The window function gave no values for 3 samples; there must be one value per sample.")]
    public void AResultOfAnotherLength_IsRefusedAsTheFunctionsFault(int given, string message)
    {
        double[]? result = given < 0 ? null : new double[given];

        var error = Assert.Throws<InvalidOperationException>(() => result.ThrowIfNotGivenFor(3, "The window function"));

        Assert.Equal(message, error.Message);
    }

    /// <summary>A value the function gave that is not finite is the function's fault as well (R52: the window and the detrend held
    /// this by hand, each in its own words): refused naming the sample and the value.</summary>
    [Theory]
    [InlineData(double.NaN, "The window function gave NaN for sample 1; every value is a finite number.")]
    [InlineData(double.PositiveInfinity, "The window function gave Infinity for sample 1; every value is a finite number.")]
    public void AResultThatIsNotFinite_IsRefusedAsTheFunctionsFault(double value, string message)
    {
        double[] result = [1, value, 3];

        var error = Assert.Throws<InvalidOperationException>(() => result.ThrowIfNotGivenFor(3, "The window function"));

        Assert.Equal(message, error.Message);
    }

    // ── the three verbs refuse in exactly those words (they held the sentence by hand before) ─────────────────

    [Theory]
    [InlineData("ConnectedScatter")]
    [InlineData("Horizon")]
    [InlineData("Triplot")]
    public void AVerb_RefusesAMismatchInTheOneWording(string verb)
    {
        double[] x = [1.0, 2.0, 3.0], y = [1.0, 2.0];

        var error = Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => _ = verb switch
            {
                "ConnectedScatter" => ax.ConnectedScatter(x, y),
                "Horizon" => ax.Horizon(x, y),
                _ => ax.Triplot(x, y),
            }).Build());

        Assert.Equal(Refusal, error.Message);
    }

    private const string Sentence = "No groups were given; a chart draws one thing for each group.";

    public static TheoryData<IReadOnlyCollection<double>> Empty => new() { Array.Empty<double>(), new List<double>(), new HashSet<double>() };

    [Theory]
    [MemberData(nameof(Empty))]
    public void Nothing_IsRefusedWithTheSentenceOfTheVerb_AndTheNameTheCallerPassed(IReadOnlyCollection<double> groups)
    {
        var refused = Assert.Throws<ArgumentException>(() => groups.ThrowIfEmpty(Sentence));

        Assert.Equal(Sentence + " (Parameter 'groups')", refused.Message);
        Assert.Equal("groups", refused.ParamName);
    }

    [Fact]
    public void One_IsAccepted_AndANameGiven_IsTheOneTheRefusalNames_ForACollectionThatIsNotTheArgumentItself()
    {
        double[] filtered = [1.0];

        filtered.ThrowIfEmpty(Sentence);

        Assert.Equal("sample", Assert.Throws<ArgumentException>(() => Array.Empty<double>().ThrowIfEmpty(Sentence, "sample")).ParamName);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void AMatrixWithNoRowOrNoColumn_IsRefusedByItsShape_AndTheNameTheCallerPassed(int rows, int columns)
    {
        var matrix = new double[rows, columns];

        var refused = Assert.Throws<ArgumentException>(() => matrix.ThrowIfNoCell("a spy plot"));

        Assert.Equal($"The matrix has {rows} rows and {columns} columns; a spy plot needs at least one cell. (Parameter 'matrix')", refused.Message);
        Assert.Equal("matrix", refused.ParamName);
    }

    [Fact]
    public void AMatrixWithACell_IsAccepted() => new double[1, 1].ThrowIfNoCell("a matrix shown");
}
