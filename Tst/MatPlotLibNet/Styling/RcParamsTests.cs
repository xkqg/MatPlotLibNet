// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet;
using System.Text.Json;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="RcParams"/> default values, Get/Set, and AsyncLocal scoping.</summary>
public class RcParamsTests
{
    public static TheoryData<Func<RcParams, double>> Sizes => new()
    {
        rc => rc.FontSize,
        rc => rc.LinesLineWidth,
    };

    [Theory]
    [MemberData(nameof(Sizes))]
    public void Default_ASize_HasPositiveValue(Func<RcParams, double> size)
    {
        var rc = new RcParams();
        Assert.True(size(rc) > 0);
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

    /// <summary>RcParams.Get&lt;T&gt;(key) — `_params.TryGetValue(...) ? (T)v : throw`
    /// false arm.</summary>
    [Fact]
    public void RcParams_Get_UnknownKey_ThrowsKeyNotFound()
    {
        var rc = new RcParams();
        Assert.Throws<KeyNotFoundException>(() => rc.Get<int>("non-existent-key"));
    }

    // The TryGet missing-key branch.
    [Fact] public void RcParams_GetUnknownKey_ReturnsDefault()
    {
        var rc = new RcParams();
        var result = rc.Get<double>("definitely-not-a-real-key", 42.0);
        Assert.Equal(42.0, result);
    }

    // `_params.TryGetValue(key, out var v) ? (T)v : default` — both arms.
    [Fact] public void RcParams_GetExistingKey_ReturnsValue()
    {
        var rc = new RcParams();
        rc.Set(RcParamKeys.FontSize, 16.0);
        Assert.Equal(16.0, rc.Get<double>(RcParamKeys.FontSize, 0.0));
    }
}
