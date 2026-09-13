// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Styling;

/// <summary>The one place that turns an operational condition into what a reader sees.
/// <para>It was written twice in every application that drew a control room — once for the chart and once for
/// the surrounding markup — and two copies of a rule this small drift without anyone noticing which one is
/// right. The mapping itself is deliberately poor in colour: rest takes none, not even green, because a wall
/// where everything is coloured has nothing left to say when one thing breaks.</para></summary>
public static class OpsConditionExtensions
{
    /// <summary>The pattern a reading wears when it cannot be believed: a gap in knowledge, drawn as a
    /// texture so it can never be mistaken for a shade of "fine".</summary>
    public const HatchPattern UnknownHatch = HatchPattern.ForwardDiagonal;

    /// <summary>The pattern a muted reading wears. Deliberately the other diagonal: shelved and unseen are
    /// different facts, and an operator must be able to tell at a glance which one is on the wall.</summary>
    public const HatchPattern ShelvedHatch = HatchPattern.BackDiagonal;

    /// <summary>Which of two severities stands. Worse wins — the rule a roll-up needs and the rule two
    /// windows of one signal need, where a fast window catches the acute failure and a slow one the
    /// smoulder.</summary>
    public static OpsSeverity Worst(this OpsSeverity severity, OpsSeverity other) =>
        other > severity ? other : severity;

    /// <summary>The form this condition takes against a palette: the colour its severity earned, and the
    /// pattern its visibility demands.</summary>
    public static OpsMark Resolve(this OpsCondition condition, AlarmPalette palette) =>
        new(
            condition.Severity switch
            {
                OpsSeverity.Critical => palette.Critical,
                OpsSeverity.Warning => palette.Warning,

                // Rest carries no colour. Returning the palette's resting shade here would make every quiet
                // tile an explicit accent, and a caller could no longer tell "judged and fine" from "not
                // judged at all".
                _ => null,
            },
            condition.Visibility switch
            {
                OpsVisibility.Unknown => UnknownHatch,
                OpsVisibility.Shelved => ShelvedHatch,
                _ => HatchPattern.None,
            });
}
