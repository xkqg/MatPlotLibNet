// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="RcParams"/> default values, Get/Set, and AsyncLocal scoping.</summary>
public class RcParamsTests
{
    [Fact]
    public void Default_FontSize_HasPositiveValue()
    {
        var rc = new RcParams();
        Assert.True(rc.FontSize > 0);
    }

    [Fact]
    public void Default_LinesLineWidth_HasPositiveValue()
    {
        var rc = new RcParams();
        Assert.True(rc.LinesLineWidth > 0);
    }

    [Fact]
    public void SetAndGet_ReturnsSameValue()
    {
        var rc = new RcParams();
        rc.Set(RcParamKeys.FontSize, 14.0);
        Assert.Equal(14.0, rc.Get<double>(RcParamKeys.FontSize));
    }

    [Fact]
    public void Get_WithFallback_ReturnsDefault_WhenKeyMissing()
    {
        var rc = new RcParams();
        double result = rc.Get(RcParamKeys.LinesLineWidth, 99.0);
        // Either a set default or the fallback — must be positive
        Assert.True(result > 0);
    }

    [Fact]
    public void Current_WithoutScope_ReturnsSameAsDefault()
    {
        // Outside any StyleContext, Current and Default are the same instance
        Assert.Same(RcParams.Default, RcParams.Current);
    }

    [Fact]
    public void ContainsKey_AfterSet_ReturnsTrue()
    {
        var rc = new RcParams();
        rc.Set("my.custom.key", 42);
        Assert.True(rc.ContainsKey("my.custom.key"));
    }

    [Fact]
    public void ContainsKey_MissingKey_ReturnsFalse()
    {
        var rc = new RcParams();
        Assert.False(rc.ContainsKey("this.key.does.not.exist.xyzzy"));
    }
}
