// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>How bad a reading is. The only axis that compares: worse outranks better, and a roll-up takes the
/// worst child.
/// <para>Nothing else belongs on this ladder. "We cannot see it" and "someone silenced it" are statements about
/// whether badness is being measured at all, and asking whether a silent source is worse than a degraded one has
/// no answer — which is why <see cref="OpsVisibility"/> is a separate axis.</para></summary>
public enum OpsSeverity
{
    /// <summary>Nothing to do. Wears no colour: a wall of cheerful tiles spends the contrast the one real alarm
    /// needs to borrow.</summary>
    Normal = 0,

    /// <summary>Out of band, but nothing is lost yet.</summary>
    Warning = 1,

    /// <summary>Failing now.</summary>
    Critical = 2,
}

/// <summary>Whether the reading can be believed, and whether anyone has muted it. Never compared as a severity.
/// <para>The alarm standards keep these apart for a reason: ISA-18.2 shelves an alarm without changing its
/// priority, OPC UA models acknowledged, active and shelved as sub-states beside a numeric severity, Grafana
/// keeps "no data" off the alert-state ladder, and Alertmanager silences an alert without changing what it
/// is.</para></summary>
public enum OpsVisibility
{
    /// <summary>The reading is current and nobody has muted it.</summary>
    Observed = 0,

    /// <summary>The source has gone silent — a gap in knowledge, not a fault. Wears a pattern, not a
    /// colour.</summary>
    Unknown = 1,

    /// <summary>An operator deliberately silenced this one. It keeps the severity it had, it leaves the roll-up,
    /// and it must still be visible as shelved — a wall that hides it reports calm where there is only
    /// silence.</summary>
    Shelved = 2,
}

/// <summary>What a tile or a band is in: how bad, and whether we can see it. One value, so a caller cannot pass
/// half a state.
/// <para>The default is resting and observed, so anything nobody judged draws as quiet rather than as an
/// alarm.</para></summary>
/// <param name="Severity">How bad the reading is.</param>
/// <param name="Visibility">Whether it can be believed, and whether it has been muted.</param>
public readonly record struct OpsCondition(OpsSeverity Severity, OpsVisibility Visibility)
{
    /// <summary>A reading we can see perfectly well, judged at this severity — the common case at a call
    /// site, where a threshold was crossed and nothing is wrong with the measurement itself.</summary>
    /// <param name="severity">How bad the reading is.</param>
    public OpsCondition(OpsSeverity severity) : this(severity, OpsVisibility.Observed)
    {
    }

    /// <summary>Normal and observed — what a reading nobody has judged draws as.</summary>
    public static OpsCondition Resting { get; } = new(OpsSeverity.Normal, OpsVisibility.Observed);

    /// <summary>The source went silent. No severity is claimed, because none was measured.</summary>
    public static OpsCondition Unknown { get; } = new(OpsSeverity.Normal, OpsVisibility.Unknown);

    /// <summary>The same reading, deliberately silenced. The severity travels with it.</summary>
    public OpsCondition Shelve() => this with { Visibility = OpsVisibility.Shelved };

    /// <summary>Rolls a set of children up into one parent reading, the way a control room reads a
    /// federation: the worst child that can actually be seen decides the colour, and the ones that cannot be
    /// seen or have been muted are counted rather than ranked.</summary>
    public static OpsRollUp RollUp(IEnumerable<OpsCondition> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        var worst = OpsSeverity.Normal;
        int observed = 0, unknown = 0, shelved = 0;

        foreach (var child in children)
        {
            switch (child.Visibility)
            {
                case OpsVisibility.Unknown:
                    unknown++;
                    break;

                case OpsVisibility.Shelved:
                    shelved++;
                    break;

                default:
                    observed++;
                    worst = worst.Worst(child.Severity);
                    break;
            }
        }

        // A parent every one of whose children went silent is itself unseen — reporting it as quiet would say
        // the opposite of what is known.
        var visibility = observed == 0 && unknown > 0 ? OpsVisibility.Unknown : OpsVisibility.Observed;

        return new OpsRollUp(new OpsCondition(worst, visibility), observed, unknown, shelved);
    }
}

/// <summary>What a set of children add up to: one condition for the parent, plus how the children were
/// counted. The counts are the point — a shelved child leaves the severity but may never leave the
/// screen.</summary>
/// <param name="Condition">The parent's own condition.</param>
/// <param name="Observed">How many children could be seen and were not muted.</param>
/// <param name="Unknown">How many children went silent.</param>
/// <param name="Shelved">How many children an operator muted.</param>
public readonly record struct OpsRollUp(OpsCondition Condition, int Observed, int Unknown, int Shelved)
{
    /// <summary>The parent's severity — the worst child that could be seen.</summary>
    public OpsSeverity Severity => Condition.Severity;
}

/// <summary>The form a condition takes on the canvas: a colour when the severity earned one, and a pattern when
/// the reading cannot be believed or has been muted. Both may be absent, which is what rest looks like.</summary>
/// <param name="Accent">The colour to draw with, or <see langword="null"/> at rest.</param>
/// <param name="Hatch">The pattern to draw over it, or <see cref="HatchPattern.None"/>.</param>
public readonly record struct OpsMark(Color? Accent, HatchPattern Hatch);
