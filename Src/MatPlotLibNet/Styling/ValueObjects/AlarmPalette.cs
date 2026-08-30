// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The colours a theme reserves for states that need attention, plus the neutral shade every resting
/// state wears.
///
/// <para><b>Why a theme names these once.</b> On a monitored display, colour is a scarce signal. The moment the
/// normal state is coloured — a wall of green tiles — the abnormal one has nothing left to stand out against,
/// and an operator five metres away stops turning their head. So the resting state carries no colour at all,
/// and the two alarm hues are spent on nothing else. This is the long-standing convention of the
/// high-performance-HMI school (ISA-101): colour is reserved for what requires action.</para>
///
/// <para><b>Why these hues.</b> <see cref="Warning"/> and <see cref="Critical"/> are Okabe-Ito amber and
/// vermillion — chosen because roughly eight percent of men have a red-green colour deficiency and these two
/// stay distinguishable to all of them. And because colour must never be the sole carrier of meaning, a mark
/// that uses them is expected to say the same thing in text or in shape as well.</para>
///
/// <para><b>The ground may change; these may not.</b> An operator picks the background they like to look at all
/// shift. What amber and vermillion <i>mean</i> is not theirs to pick: a theme may shift their luminance so
/// they stay equally loud on a dark wall and a bright panel, but never their meaning.</para>
/// </summary>
/// <param name="Resting">The neutral shade of a state that is fine. Not green — the absence of colour IS the
/// confirmation that nothing needs doing.</param>
/// <param name="Warning">Attention: a value has left its normal band but nothing is lost yet. Okabe-Ito amber.</param>
/// <param name="Critical">Action: something is failing now. Okabe-Ito vermillion.</param>
/// <param name="Unknown">The shade of a source that has gone silent. Paired with a hatch, never carried by
/// colour alone: "I can no longer see you" is a different fault from "you are broken", and a wall that paints
/// them the same lies exactly when it matters.</param>
public readonly record struct AlarmPalette(Color Resting, Color Warning, Color Critical, Color Unknown)
{
    /// <summary>The library default: a mid-grey resting state with the Okabe-Ito alarm hues over it. Suits any
    /// theme that has not deliberately tuned its own.</summary>
    /// <summary>The palette as a colour map: <see cref="Resting"/> at 0, <see cref="Warning"/> at 0.5,
    /// <see cref="Critical"/> at 1 — so "half way is a warning, the end is critical" is decided HERE, once,
    /// and every ops caller that colours by intensity reaches for the same ramp.</summary>
    public ColorMaps.IColorMap Ramp => new ColorMaps.LinearColorMap("alarm", [Resting, Warning, Critical]);

    public static AlarmPalette Default { get; } = new(
        Resting: Color.FromHex("#8A8A8A"),
        Warning: Color.FromHex("#E69F00"),
        Critical: Color.FromHex("#D55E00"),
        Unknown: Color.FromHex("#8A8A8A"));
}
