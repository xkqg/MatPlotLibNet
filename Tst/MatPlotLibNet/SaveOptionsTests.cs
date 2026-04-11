// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>Verifies <see cref="SaveOptions"/> defaults and property accessibility.</summary>
public class SaveOptionsTests
{
    [Fact]
    public void SaveOptions_DefaultDpi_Is96()
    {
        var opts = new SaveOptions();
        Assert.Equal(96, opts.Dpi);
    }

    [Fact]
    public void SaveOptions_DefaultPrettifySvg_IsFalse()
    {
        var opts = new SaveOptions();
        Assert.False(opts.PrettifySvg);
    }

    [Fact]
    public void SaveOptions_SvgDecimalPrecision_DefaultsToNull()
    {
        var opts = new SaveOptions();
        Assert.Null(opts.SvgDecimalPrecision);
    }

    [Fact]
    public void SaveOptions_Title_DefaultsToNull()
    {
        var opts = new SaveOptions();
        Assert.Null(opts.Title);
    }

    [Fact]
    public void SaveOptions_Author_DefaultsToNull()
    {
        var opts = new SaveOptions();
        Assert.Null(opts.Author);
    }

    [Fact]
    public void SaveOptions_CanBeConstructedWithInitProperties()
    {
        var opts = new SaveOptions { Dpi = 150, PrettifySvg = true, Title = "My Chart" };
        Assert.Equal(150, opts.Dpi);
        Assert.True(opts.PrettifySvg);
        Assert.Equal("My Chart", opts.Title);
    }
}
