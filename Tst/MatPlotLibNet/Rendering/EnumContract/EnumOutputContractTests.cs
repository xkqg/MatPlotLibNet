// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>Phase L.7 (v1.7.2, 2026-04-21) — stacked-OO contract tests for
/// enum-based rendering branches. Each sealed subclass provides only the
/// <see cref="RenderDelegate"/> (and optional <see cref="Exclude"/> list);
/// the base class <see cref="EveryValue_ProducesDistinctSvg"/> fact runs
/// automatically for every derived class.
///
/// Migrates: HistType, TickDirection, ConnectionStyle, BoxStyle, AxisScale,
/// ArrowStyle (6 standalone files → sealed subclasses here).</summary>
/// <typeparam name="TEnum">Enum under test.</typeparam>
public abstract class EnumOutputContractTests<TEnum> where TEnum : struct, Enum
{
    protected abstract Func<TEnum, string> RenderDelegate { get; }
    protected virtual IEnumerable<TEnum> Exclude => [];

    [Fact]
    public void EveryValue_ProducesDistinctSvg()
        => EnumOutputContract.EveryValueRendersDistinctOutput(RenderDelegate, exclude: Exclude);
}


