// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What a kind of dot rests at: the diameter, opacity and jitter a series of the kind reads, and draws under the library's own
/// themes, where nobody names one. The one value a kind states, in place of a constant for each part.</summary>
/// <param name="Diameter">The diameter, in pixels.</param>
/// <param name="Alpha">The opacity, 0 to 1. A kind with no opacity of its own rests at 1 and is never asked.</param>
/// <param name="Jitter">The jitter, in category units. A kind with no jitter rests at 0 and is never asked.</param>
internal readonly record struct RestingDot(double Diameter, double Alpha = 1, double Jitter = 0);
