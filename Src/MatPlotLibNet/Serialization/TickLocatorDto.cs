// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON names the tick locator of an axis: <c>"type"</c> says which built-in locator it is, the
/// other fields carry what it was made with. A locator of a kind this build does not know (JSON from a later release,
/// or a typo) reads as this base record, which makes no locator: the axis keeps its own ticks, and the skip is
/// reported through <see cref="ChartDiagnostics"/>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(AutoTickLocatorDto), "auto")]
[JsonDerivedType(typeof(MaxNTickLocatorDto), "maxN")]
[JsonDerivedType(typeof(MultipleTickLocatorDto), "multiple")]
[JsonDerivedType(typeof(FixedTickLocatorDto), "fixed")]
[JsonDerivedType(typeof(LogTickLocatorDto), "log")]
[JsonDerivedType(typeof(SymlogTickLocatorDto), "symlog")]
[JsonDerivedType(typeof(AutoDateTickLocatorDto), "autoDate")]
internal record TickLocatorDto
{
    /// <summary>The locator this description names, or <see langword="null"/> for a kind this build does not know.</summary>
    internal virtual ITickLocator? Restore()
    {
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "A tick locator of a kind this build does not know was skipped during deserialization; the axis keeps its own ticks.", null));
        return null;
    }
}

/// <summary>Describes an <see cref="AutoLocator"/>.</summary>
/// <param name="TargetCount">The number of tick intervals it aims for.</param>
internal sealed record AutoTickLocatorDto(int TargetCount) : TickLocatorDto
{
    internal override ITickLocator Restore() => new AutoLocator(TargetCount);
}

/// <summary>Describes a <see cref="MaxNLocator"/>.</summary>
/// <param name="MaxN">The most ticks it places.</param>
internal sealed record MaxNTickLocatorDto(int MaxN) : TickLocatorDto
{
    internal override ITickLocator Restore() => new MaxNLocator(MaxN);
}

/// <summary>Describes a <see cref="MultipleLocator"/>.</summary>
/// <param name="Base">The step between ticks.</param>
/// <param name="Offset">The shift of every tick.</param>
internal sealed record MultipleTickLocatorDto(double Base, double Offset) : TickLocatorDto
{
    internal override ITickLocator Restore() => new MultipleLocator(Base, Offset);
}

/// <summary>Describes a <see cref="FixedLocator"/>.</summary>
/// <param name="Positions">The tick positions; absent reads as none.</param>
internal sealed record FixedTickLocatorDto(double[]? Positions) : TickLocatorDto
{
    internal override ITickLocator Restore() => new FixedLocator(Positions ?? []);
}

/// <summary>Describes a <see cref="LogLocator"/>, which takes no arguments.</summary>
internal sealed record LogTickLocatorDto : TickLocatorDto
{
    internal override ITickLocator Restore() => new LogLocator();
}

/// <summary>Describes a <see cref="SymlogLocator"/>.</summary>
/// <param name="LinThresh">The half-width of the linear band around zero.</param>
internal sealed record SymlogTickLocatorDto(double LinThresh) : TickLocatorDto
{
    internal override ITickLocator Restore() => new SymlogLocator(LinThresh);
}

/// <summary>Describes an <see cref="AutoDateLocator"/>, which takes no arguments (it chooses its interval from the
/// drawn range each time it is asked).</summary>
internal sealed record AutoDateTickLocatorDto : TickLocatorDto
{
    internal override ITickLocator Restore() => new AutoDateLocator();
}
