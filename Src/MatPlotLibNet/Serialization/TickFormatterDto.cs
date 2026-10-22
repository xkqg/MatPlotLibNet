// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON names the tick formatter of an axis: <c>"type"</c> says which built-in formatter it is,
/// the other fields carry what it was made with. A formatter of a kind this build does not know reads as this base
/// record, which makes no formatter: the axis keeps its own labels, and the skip is reported through
/// <see cref="ChartDiagnostics"/>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(NumericTickFormatterDto), "numeric")]
[JsonDerivedType(typeof(LogTickFormatterDto), "log")]
[JsonDerivedType(typeof(BarCenterTickFormatterDto), "barCenter")]
[JsonDerivedType(typeof(CategoryTickFormatterDto), "category")]
[JsonDerivedType(typeof(DateTickFormatterDto), "date")]
[JsonDerivedType(typeof(DateTimeTickFormatterDto), "dateTime")]
[JsonDerivedType(typeof(EngTickFormatterDto), "eng")]
[JsonDerivedType(typeof(PercentTickFormatterDto), "percent")]
[JsonDerivedType(typeof(AutoDateTickFormatterDto), "autoDate")]
internal record TickFormatterDto
{
    /// <summary>The formatter this description names, or <see langword="null"/> for a kind this build does not know.</summary>
    /// <param name="axisLocator">The locator the axis was read back with: a formatter that writes what its locator
    /// chose is paired with it again.</param>
    internal virtual ITickFormatter? Restore(ITickLocator? axisLocator)
    {
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "A tick formatter of a kind this build does not know was skipped during deserialization; the axis keeps its own labels.", null));
        return null;
    }
}

/// <summary>Describes a <see cref="NumericTickFormatter"/>, which takes no arguments.</summary>
internal sealed record NumericTickFormatterDto : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator) => new NumericTickFormatter();
}

/// <summary>Describes a <see cref="LogTickFormatter"/>, which takes no arguments.</summary>
internal sealed record LogTickFormatterDto : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator) => new LogTickFormatter();
}

/// <summary>Describes a <see cref="BarCenterFormatter"/>, which takes no arguments.</summary>
internal sealed record BarCenterTickFormatterDto : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator) => new BarCenterFormatter();
}

/// <summary>Describes a <see cref="CategoryFormatter"/>.</summary>
/// <param name="Labels">The category names in tick order; absent reads as none.</param>
/// <param name="Reversed">Whether index 0 names the last category.</param>
/// <param name="Positions">Where each name stands, one per name, for names at positions of their own; absent for names on
/// slots of width one, so the JSON of a formatter on the slot rule is the JSON it was before positions existed.</param>
internal sealed record CategoryTickFormatterDto(string[]? Labels, bool Reversed, double[]? Positions = null) : TickFormatterDto
{
    /// <exception cref="ArgumentException"><see cref="Positions"/> does not carry one position per name.</exception>
    internal override ITickFormatter Restore(ITickLocator? axisLocator) =>
        Positions is { } positions ? new CategoryFormatter(Labels ?? [], positions) : new CategoryFormatter(Labels ?? [], Reversed);
}

/// <summary>Describes a <see cref="DateTickFormatter"/>.</summary>
/// <param name="DateFormat">The .NET date format string; absent reads as the formatter's own default.</param>
internal sealed record DateTickFormatterDto(string? DateFormat) : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator)
    {
        var formatter = new DateTickFormatter();
        if (DateFormat is not null)
        {
            formatter.DateFormat = DateFormat;
        }

        return formatter;
    }
}

/// <summary>Describes a <see cref="DateTimeTickFormatter"/>: indexed into its dates when it has them, reading the tick
/// as Unix milliseconds when it has none.</summary>
/// <param name="Timestamps">The dates a tick index points into; absent for the milliseconds reading.</param>
/// <param name="Format">The .NET date format string; absent reads as the formatter's own default.</param>
internal sealed record DateTimeTickFormatterDto(DateTime[]? Timestamps, string? Format) : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator)
    {
        string format = Format ?? DateTimeTickFormatter.DefaultFormat;
        return Timestamps is { } dates ? DateTimeTickFormatter.FromArray(dates, format) : DateTimeTickFormatter.FromEpochMs(format);
    }
}

/// <summary>Describes an <see cref="EngFormatter"/>.</summary>
/// <param name="Sep">What stands between the number and the prefix; absent reads as the formatter's own default.</param>
internal sealed record EngTickFormatterDto(string? Sep) : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator)
    {
        var formatter = new EngFormatter();
        if (Sep is not null)
        {
            formatter.Sep = Sep;
        }

        return formatter;
    }
}

/// <summary>Describes a <see cref="PercentFormatter"/>.</summary>
/// <param name="Max">The value that reads as 100 %.</param>
internal sealed record PercentTickFormatterDto(double Max) : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator) => new PercentFormatter(Max);
}

/// <summary>Describes an <see cref="AutoDateFormatter"/>. It writes what the axis's <see cref="AutoDateLocator"/> chose,
/// so it is paired with the locator the axis was read back with; on an axis without one it gets a locator of its own.</summary>
internal sealed record AutoDateTickFormatterDto : TickFormatterDto
{
    internal override ITickFormatter Restore(ITickLocator? axisLocator) =>
        new AutoDateFormatter(axisLocator as AutoDateLocator ?? new AutoDateLocator());
}
