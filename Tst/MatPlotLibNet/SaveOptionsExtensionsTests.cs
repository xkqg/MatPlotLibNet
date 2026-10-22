// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

/// <summary>The rules that read <see cref="SaveOptions"/>, held in one place so each export asks the same question.</summary>
public class SaveOptionsExtensionsTests
{
    private static Figure FigureAt(double dpi) => new() { Dpi = dpi };

    // ── resolution ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OptionsThatSayNothingOfResolution_LeaveTheFigureResolutionInForce()
    {
        Assert.Equal(300, new SaveOptions().DpiFor(FigureAt(300)));
        Assert.Equal(96, new SaveOptions().DpiFor(FigureAt(96)));
        Assert.Equal(300, new SaveOptions { Title = "x", PrettifySvg = true }.DpiFor(FigureAt(300)));
    }

    [Fact]
    public void AResolutionInTheOptions_BeatsTheFiguresOwn()
    {
        Assert.Equal(150, new SaveOptions { Dpi = 150 }.DpiFor(FigureAt(300)));
        Assert.Equal(600, new SaveOptions { Dpi = 600 }.DpiFor(FigureAt(96)));
    }

    [Fact]
    public void AResolutionThatIsNotValid_IsPassedOnForTheExportToRefuse()
    {
        Assert.Equal(0, new SaveOptions { Dpi = 0 }.DpiFor(FigureAt(96)));
        Assert.Equal(-5, new SaveOptions { Dpi = -5 }.DpiFor(FigureAt(96)));
        Assert.True(double.IsNaN(new SaveOptions().DpiFor(FigureAt(double.NaN))));
    }

    [Fact]
    public void TheDefaultOfTheOptions_IsTheReferenceResolution()
    {
        Assert.Equal(Figure.ReferenceDpi, new SaveOptions().Dpi);
        Assert.Equal(Figure.ReferenceDpi, new Figure().Dpi);
    }

    // ── title and author ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Annual Revenue", "Annual Revenue")]
    [InlineData(" ", " ")]
    public void ATitleIsWrittenUnlessItIsNullOrEmpty(string? title, string? expected)
    {
        Assert.Equal(expected, new SaveOptions { Title = title }.TitleOrNull());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Finance Team", "Finance Team")]
    [InlineData(" ", " ")]
    public void AnAuthorIsWrittenUnlessItIsNullOrEmpty(string? author, string? expected)
    {
        Assert.Equal(expected, new SaveOptions { Author = author }.AuthorOrNull());
    }
}
