// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>A value the figure JSON carries by its kind: it writes its own description (a discriminator and the
/// arguments it was made with), and the description makes the value again. The built-in tick locators, tick
/// formatters, colour normalisers and the colour map types implement it; a type written outside the library does not,
/// so the JSON leaves it out and the figure reads back on the axis's or series' own default.</summary>
/// <typeparam name="TDescription">The description record of the value's family.</typeparam>
/// <remarks>The family closes at compile time: each description type is listed on its family's base record by a
/// <c>JsonDerivedType</c> attribute, and making the value again is a virtual member of the description. There is no
/// registry to add to at run time and no switch on the kind.</remarks>
internal interface IJsonDescribed<out TDescription> where TDescription : class
{
    /// <summary>The description the JSON carries for this value, or <see langword="null"/> when a value that holds another
    /// cannot be described because that other one cannot (the reverse of a colour map written outside the library).</summary>
    TDescription? Describe();
}
