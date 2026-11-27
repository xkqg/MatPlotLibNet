// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
namespace MatPlotLibNet.Tests;

/// <summary>Verifies the five process-wide defaults every figure reads (<see cref="ChartSerializerDefault"/>,
/// <see cref="ChartRendererDefault"/>, <see cref="SvgRendererDefault"/>, <see cref="FontMetricsDefault"/>,
/// <see cref="GlyphPathProviderDefault"/>): each hands back what it was given, and a missing one is refused where a figure cannot
/// draw without it. They are process-wide, so the class runs on its own.</summary>
[Collection("ChartServicesGlobalState")]
public class ProcessWideDefaultsTests
{

    /// <summary>Holds a slot at <paramref name="value"/> for a scope, runs <paramref name="whileHeld"/> in it, and asserts the slot hands the
    /// value back while it is held and what it held before once the scope has ended, the one way a test sets a slot.</summary>
    private static void AssertHeld<T>(Func<T> current, Func<T, HeldSlot<T>> hold, T value, Action whileHeld) where T : class?
    {
        var before = current();
        using (hold(value))
        {
            Assert.Same(value, current());
            whileHeld();
        }

        Assert.Same(before, current());
    }

    /// <summary>The slots a figure reads: each hands back what it was given while it is held and what it held before once the scope has
    /// ended, and each refuses a missing one.</summary>
    public static TheoryData<Action> NullRefusingSlots => new()
    {
        () => AssertHeld(() => ChartSerializerDefault.Current, ChartSerializerDefault.Holding, new ChartSerializer(),
            () => Assert.Throws<ArgumentNullException>(() => ChartSerializerDefault.Current = null!)),
        () => AssertHeld(() => ChartRendererDefault.Current, ChartRendererDefault.Holding, new ChartRenderer(),
            () => Assert.Throws<ArgumentNullException>(() => ChartRendererDefault.Current = null!)),
        () => AssertHeld(() => SvgRendererDefault.Current, SvgRendererDefault.Holding, new MatPlotLibNet.Transforms.SvgTransform(new ChartRenderer()),
            () => Assert.Throws<ArgumentNullException>(() => SvgRendererDefault.Current = null!)),
        () => AssertHeld(() => FontMetricsDefault.Current, FontMetricsDefault.Holding, new DefaultFontMetrics(),
            () => Assert.Throws<ArgumentNullException>(() => FontMetricsDefault.Current = null!)),
    };

    [Theory]
    [MemberData(nameof(NullRefusingSlots))]
    public void AServiceSlot_GetSet_AndNullThrows(Action check) => check();

    [Fact]
    public void TheGlyphPathSlot_AcceptsNullAndNonNull() =>
        AssertHeld(() => GlyphPathProviderDefault.Current, GlyphPathProviderDefault.Holding, null, () => { });
}
