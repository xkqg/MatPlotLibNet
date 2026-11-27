// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A scale's rule holds what it maps by: the symmetric-log rule its threshold (<see cref="ScaleRule.SymLogAt"/>), so a reader
/// holds one rule and never a rule and a threshold beside it (<see cref="AxisScaleExtensions"/>' <c>RuleAt</c>, an axis' <c>ScaleRule</c>).
/// Measured (H5, process CPU, 1e6 points): a direction that holds the bound rule places a log value 21 % and a symlog value 8 % cheaper
/// than one that holds the scale and its threshold and picks the rule for every point, at the same 64 bytes.</summary>
public class ScaleRuleBoundTests
{
    [Fact]
    public void ASymmetricLogRule_MapsByTheThresholdItHolds()
    {
        var rule = ScaleRule.SymLogAt(5);

        Assert.Equal(SymlogTransform.Forward(20, 5), rule.Forward(20));
        Assert.Equal(SymlogTransform.Inverse(3, 5), rule.Inverse(3));
        Assert.Equal(5, rule.LinearThreshold);
        Assert.Equal(AxisScale.SymLog, rule.Scale);
    }

    [Fact]
    public void TheDefaultThreshold_IsTheOneSymmetricLogRule_AndNoOtherScaleHasAThreshold()
    {
        Assert.Same(ScaleRule.SymLog, ScaleRule.SymLogAt(SymlogTransform.DefaultLinThresh));
        Assert.Equal(SymlogTransform.DefaultLinThresh, ScaleRule.SymLog.LinearThreshold);
        Assert.Null(ScaleRule.Log.LinearThreshold);
        Assert.Null(ScaleRule.Linear.LinearThreshold);
        Assert.Same(ScaleRule.Log, AxisScale.Log.RuleAt(7));
        Assert.Equal(7, AxisScale.SymLog.RuleAt(7).LinearThreshold);
    }

    [Fact]
    public void AnAxis_HandsOutItsScaleBoundToItsThreshold()
    {
        var axis = new Axis { Scale = AxisScale.SymLog, SymLogLinThresh = 3 };

        Assert.Equal(3, axis.ScaleRuleUnder(Theme.Default).LinearThreshold);
        Assert.Equal(SymlogTransform.Forward(10, 3), axis.ScaleRuleUnder(Theme.Default).Forward(10));
        Assert.Same(ScaleRule.Logit, new Axis { Scale = AxisScale.Logit }.ScaleRuleUnder(Theme.Default));
    }

    [Fact]
    public void ADirection_HoldsTheRuleInTheRoomTheScaleAndTheThresholdTook() =>
        Assert.Equal(64, Unsafe.SizeOf<PixelDirection>());
}
