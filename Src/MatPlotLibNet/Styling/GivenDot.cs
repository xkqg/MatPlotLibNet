// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What a caller named of the dots of a series, each part <see langword="null"/> while it was not named: the one value every kind
/// of dot holds (a strip, a swarm, a point plot, a residual plot, a pair grid), in place of a hidden field and an accessor for each part.
/// A part that is named is drawn as named, the resting number included; one that is not is the look's
/// (<see cref="DotLook"/>) or, where the look names none, the kind's own (<see cref="RestingDot"/>). The JSON writes the named parts
/// only.</summary>
/// <param name="Diameter">The diameter the caller named, in pixels; <see langword="null"/> while none was named.</param>
/// <param name="Alpha">The opacity the caller named, 0 to 1; <see langword="null"/> while none was named.</param>
/// <param name="Jitter">The jitter the caller named, in category units; <see langword="null"/> while none was named.</param>
/// <remarks>A value with its own equality, so a series stores it through its change mark without boxing it, and a setter that names one
/// part writes a copy with that part (<c>Dot with { Alpha = value }</c>).</remarks>
internal readonly record struct GivenDot(double? Diameter = null, double? Alpha = null, double? Jitter = null);
