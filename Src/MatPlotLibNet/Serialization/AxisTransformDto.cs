// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON names the link of an axis (<see cref="Axis.Link"/>, the <c>link</c> field of an axis): <c>"type"</c>
/// says which named transform it is, the other fields carry what it was made with. A transform made from a pair of functions of
/// the caller's own (<see cref="AxisTransform.Of"/>) is code, which a JSON document cannot carry, so it describes nothing and is left
/// out. A link of a kind this build does not know (JSON from a later release, or a typo) or with no kind reads as this base record,
/// which makes no transform: the axis stays unlinked, the figure still loads, and the skip is reported through
/// <see cref="ChartDiagnostics"/>.</summary>
/// <remarks>The base is not abstract on purpose: a reader falls back to the contract of the base type for a kind it does not know,
/// and an abstract base would fail the whole figure instead (<c>NormalizerDto</c> and <c>PatchDto</c> are built the same way). A
/// number the transform cannot map with is refused when it is read, by the factory that refuses it when it is called
/// (<see cref="ArgumentOutOfRangeException"/> naming the same parameter), so a document cannot hold a link the library could not
/// have made.</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(LinearAxisTransformDto), "linear")]
[JsonDerivedType(typeof(ReciprocalAxisTransformDto), "reciprocal")]
[JsonDerivedType(typeof(LogarithmAxisTransformDto), "logarithm")]
[JsonDerivedType(typeof(PowerAxisTransformDto), "power")]
internal record AxisTransformDto
{
    /// <summary>The transform this description names, or <see langword="null"/> for a kind this build does not know.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A number the transform cannot map with.</exception>
    internal virtual AxisTransform? Restore()
    {
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "An axis link of a kind this build does not know was skipped during deserialization; the axis stays unlinked.", null));
        return null;
    }
}

/// <summary>Describes <see cref="AxisTransform.Linear"/>.</summary>
/// <param name="Scale">What a primary value is multiplied by; absent reads as 0, which the transform refuses.</param>
/// <param name="Offset">What is added after the scale; absent for 0.</param>
internal sealed record LinearAxisTransformDto(double Scale, double? Offset) : AxisTransformDto
{
    internal override AxisTransform Restore() => AxisTransform.Linear(Scale, Offset ?? 0);
}

/// <summary>Describes <see cref="AxisTransform.Reciprocal"/>.</summary>
/// <param name="Numerator">What is divided by the primary value; absent for 1.</param>
internal sealed record ReciprocalAxisTransformDto(double? Numerator) : AxisTransformDto
{
    internal override AxisTransform Restore() => AxisTransform.Reciprocal(Numerator ?? 1);
}

/// <summary>Describes <see cref="AxisTransform.Logarithm"/>.</summary>
/// <param name="Base">The base of the logarithm; absent for 10.</param>
internal sealed record LogarithmAxisTransformDto(double? Base) : AxisTransformDto
{
    internal override AxisTransform Restore() => AxisTransform.Logarithm(Base ?? 10);
}

/// <summary>Describes <see cref="AxisTransform.Power"/>.</summary>
/// <param name="Exponent">The exponent the primary value is raised to; absent reads as 0, which the transform refuses.</param>
internal sealed record PowerAxisTransformDto(double Exponent) : AxisTransformDto
{
    internal override AxisTransform Restore() => AxisTransform.Power(Exponent);
}
