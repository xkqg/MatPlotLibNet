// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>What the interaction modifiers share about a gesture is written once (the rows of <c>OwnedRules.InteractionRules</c> hold
/// each rule to its owner): the three rubber bands a held key opens are one kind of modifier.</summary>
public class GestureOwnerContractTests
{
    /// <summary>The three rubber bands a held key opens (the zoom rectangle, the span and the brush) are one kind of modifier: each is a
    /// <c>RubberBandModifier</c>, which answers the press, the move and the release with its track; each says only its key, its floor,
    /// the state it shows and the event it sends.</summary>
    [Theory]
    [InlineData(typeof(RectangleZoomModifier))]
    [InlineData(typeof(SpanSelectModifier))]
    [InlineData(typeof(BrushSelectModifier))]
    public void ARubberBand_IsARubberBandModifier_AndAnswersNoPointerItself(Type modifier)
    {
        Assert.Equal(typeof(RubberBandModifier), modifier.BaseType);
        Assert.DoesNotContain(modifier.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly),
            method => method.Name.StartsWith("OnPointer", StringComparison.Ordinal) || method.Name.StartsWith("HandlesPointer", StringComparison.Ordinal));
    }
}
