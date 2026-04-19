// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — drives both null-handler arms
/// of <see cref="ChartSessionOptions"/>'s argument-null guards (lines 34, 45). Pre-X
/// the class was at 100%L / 50%B because only the happy-path register-handler arms
/// were exercised; the `?? throw new ArgumentNullException(nameof(handler))` true
/// branches were unhit.</summary>
public class ChartSessionOptionsTests
{
    [Fact]
    public void OnBrushSelect_ValidHandler_RegistersAndReturnsThis()
    {
        var opts = new ChartSessionOptions();
        var result = opts.OnBrushSelect(_ => default);
        Assert.Same(opts, result);   // fluent chain returns self
    }

    [Fact]
    public void OnHover_ValidHandler_RegistersAndReturnsThis()
    {
        var opts = new ChartSessionOptions();
        var result = opts.OnHover(_ => new ValueTask<string?>("tooltip"));
        Assert.Same(opts, result);
    }

    /// <summary>Null brush-select handler triggers the `?? throw` arm at line 34.</summary>
    [Fact]
    public void OnBrushSelect_NullHandler_ThrowsArgumentNullException()
    {
        var opts = new ChartSessionOptions();
        Assert.Throws<ArgumentNullException>(() => opts.OnBrushSelect(null!));
    }

    /// <summary>Null hover handler triggers the `?? throw` arm at line 45.</summary>
    [Fact]
    public void OnHover_NullHandler_ThrowsArgumentNullException()
    {
        var opts = new ChartSessionOptions();
        Assert.Throws<ArgumentNullException>(() => opts.OnHover(null!));
    }
}
