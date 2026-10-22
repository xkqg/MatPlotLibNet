// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON names the normaliser a series maps its values to colours with:
/// <c>"type"</c> says which built-in normaliser it is, the other fields carry what it was made with. A normaliser of a
/// kind this build does not know reads as this base record, which makes no normaliser: the series maps linearly, and the
/// skip is reported through <see cref="ChartDiagnostics"/>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(LinearNormalizerDto), "linear")]
[JsonDerivedType(typeof(LogNormalizerDto), "log")]
[JsonDerivedType(typeof(TwoSlopeNormalizerDto), "twoSlope")]
[JsonDerivedType(typeof(BoundaryNormalizerDto), "boundary")]
[JsonDerivedType(typeof(SymLogNormalizerDto), "symLog")]
[JsonDerivedType(typeof(PowerNormalizerDto), "power")]
[JsonDerivedType(typeof(CenteredNormalizerDto), "centered")]
[JsonDerivedType(typeof(NoNormNormalizerDto), "noNorm")]
internal record NormalizerDto
{
    /// <summary>The normaliser this description names, or <see langword="null"/> for a kind this build does not know.</summary>
    internal virtual INormalizer? Restore()
    {
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "A normaliser of a kind this build does not know was skipped during deserialization; the series maps its values linearly.", null));
        return null;
    }
}

/// <summary>Describes a <see cref="LinearNormalizer"/>, which takes no arguments.</summary>
internal sealed record LinearNormalizerDto : NormalizerDto
{
    internal override INormalizer Restore() => LinearNormalizer.Instance;
}

/// <summary>Describes a <see cref="LogNormalizer"/>, which takes no arguments.</summary>
internal sealed record LogNormalizerDto : NormalizerDto
{
    internal override INormalizer Restore() => new LogNormalizer();
}

/// <summary>Describes a <see cref="TwoSlopeNormalizer"/>.</summary>
/// <param name="Center">The value that maps to the middle of the colour map.</param>
internal sealed record TwoSlopeNormalizerDto(double Center) : NormalizerDto
{
    internal override INormalizer Restore() => new TwoSlopeNormalizer(Center);
}

/// <summary>Describes a <see cref="BoundaryNormalizer"/>.</summary>
/// <param name="Boundaries">The bin edges, ascending; absent reads as none.</param>
internal sealed record BoundaryNormalizerDto(double[]? Boundaries) : NormalizerDto
{
    internal override INormalizer Restore() => new BoundaryNormalizer(Boundaries ?? []);
}

/// <summary>Describes a <see cref="SymLogNormalizer"/>.</summary>
/// <param name="Linthresh">The half-width of the linear band around zero.</param>
/// <param name="Base">The logarithm's base beyond the band.</param>
/// <param name="LinScale">How much of the range the linear band takes.</param>
internal sealed record SymLogNormalizerDto(double Linthresh, double Base, double LinScale) : NormalizerDto
{
    internal override INormalizer Restore() => new SymLogNormalizer(Linthresh, Base, LinScale);
}

/// <summary>Describes a <see cref="PowerNormNormalizer"/>.</summary>
/// <param name="Gamma">The exponent.</param>
internal sealed record PowerNormalizerDto(double Gamma) : NormalizerDto
{
    internal override INormalizer Restore() => new PowerNormNormalizer(Gamma);
}

/// <summary>Describes a <see cref="CenteredNormNormalizer"/>.</summary>
/// <param name="Vcenter">The value that maps to the middle of the colour map.</param>
/// <param name="Halfrange">The half-width of the range around the centre; absent when the data range decides.</param>
internal sealed record CenteredNormalizerDto(double Vcenter, double? Halfrange) : NormalizerDto
{
    internal override INormalizer Restore() => new CenteredNormNormalizer(Vcenter, Halfrange);
}

/// <summary>Describes a <see cref="NoNormNormalizer"/>, which takes no arguments.</summary>
internal sealed record NoNormNormalizerDto : NormalizerDto
{
    internal override INormalizer Restore() => NoNormNormalizer.Instance;
}
