// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>How bad it is, and whether we can trust the reading, are two different questions. They were one
/// ordered enum in the control-room sample — <c>Normal, Unknown, Degraded, Critical</c> — rolled up with
/// <c>&gt;</c>, which quietly asked whether a silent source is worse than a degraded one. No domain fact answers
/// that, and every alarm standard refuses to ask it: ISA-18.2 keeps priority and shelving apart, OPC UA models
/// active, acknowledged and shelved as separate sub-states beside a numeric severity, Grafana keeps rule health
/// ("no data") off the alert-state ladder, and Alertmanager silences a firing alert without changing what it is.
///
/// <para>So: one ladder for severity, one axis for visibility, one value carrying both.</para></summary>
public class OpsConditionTests
{
    private static readonly AlarmPalette Palette = AlarmPalette.Default;

    // ── the form each condition takes ──────────────────────────────────────────

    [Fact]
    public void ARestingCondition_WearsNoColourAndNoHatch()
    {
        var mark = new OpsCondition(OpsSeverity.Normal, OpsVisibility.Observed).Resolve(Palette);

        Assert.Null(mark.Accent);
        Assert.Equal(HatchPattern.None, mark.Hatch);
    }

    [Fact]
    public void AWarning_TakesTheWarningColourOfThePalette()
    {
        var mark = new OpsCondition(OpsSeverity.Warning, OpsVisibility.Observed).Resolve(Palette);

        Assert.Equal(Palette.Warning, mark.Accent);
        Assert.Equal(HatchPattern.None, mark.Hatch);
    }

    [Fact]
    public void ACritical_TakesTheCriticalColourOfThePalette()
    {
        var mark = new OpsCondition(OpsSeverity.Critical, OpsVisibility.Observed).Resolve(Palette);

        Assert.Equal(Palette.Critical, mark.Accent);
    }

    [Fact]
    public void AReadingWeCannotSee_WearsAHatchAndStillNoColour()
    {
        var mark = OpsCondition.Unknown.Resolve(Palette);

        Assert.Null(mark.Accent);
        Assert.NotEqual(HatchPattern.None, mark.Hatch);
    }

    [Fact]
    public void AShelvedAlarm_WearsAHatchOfItsOwn_NotTheOneAGapWears()
    {
        var shelved = new OpsCondition(OpsSeverity.Critical, OpsVisibility.Shelved).Resolve(Palette);
        var unknown = OpsCondition.Unknown.Resolve(Palette);

        Assert.NotEqual(HatchPattern.None, shelved.Hatch);
        Assert.NotEqual(unknown.Hatch, shelved.Hatch);
    }

    [Fact]
    public void AShelvedAlarm_KeepsTheSeverityItHad()
    {
        // ISA-18.2: shelving takes an alarm out of the active summary; it does not make it less serious.
        var condition = new OpsCondition(OpsSeverity.Critical, OpsVisibility.Shelved);

        Assert.Equal(OpsSeverity.Critical, condition.Severity);
        Assert.Equal(Palette.Critical, condition.Resolve(Palette).Accent);
    }

    // ── the roll-up ────────────────────────────────────────────────────────────

    [Fact]
    public void TheWorstObservedChildWins()
    {
        var rolled = OpsCondition.RollUp(
        [
            new OpsCondition(OpsSeverity.Normal, OpsVisibility.Observed),
            new OpsCondition(OpsSeverity.Critical, OpsVisibility.Observed),
            new OpsCondition(OpsSeverity.Warning, OpsVisibility.Observed),
        ]);

        Assert.Equal(OpsSeverity.Critical, rolled.Severity);
        Assert.Equal(3, rolled.Observed);
    }

    [Fact]
    public void AChildWeCannotSee_NeitherOutranksNorUnderranksARealSeverity()
    {
        var rolled = OpsCondition.RollUp(
        [
            new OpsCondition(OpsSeverity.Warning, OpsVisibility.Observed),
            OpsCondition.Unknown,
        ]);

        Assert.Equal(OpsSeverity.Warning, rolled.Severity);
        Assert.Equal(1, rolled.Unknown);
        Assert.Equal(1, rolled.Observed);
    }

    [Fact]
    public void AShelvedChild_LeavesTheSeverityRollUp_AndIsStillCounted()
    {
        // Someone silenced it deliberately, so it must not colour the parent — and it must not vanish either,
        // or the wall looks calm because a problem was muted.
        var rolled = OpsCondition.RollUp(
        [
            new OpsCondition(OpsSeverity.Normal, OpsVisibility.Observed),
            new OpsCondition(OpsSeverity.Critical, OpsVisibility.Shelved),
        ]);

        Assert.Equal(OpsSeverity.Normal, rolled.Severity);
        Assert.Equal(1, rolled.Shelved);
    }

    [Fact]
    public void RollingUpNothingAtAll_IsRestingAndCountsNothing()
    {
        var rolled = OpsCondition.RollUp([]);

        Assert.Equal(OpsSeverity.Normal, rolled.Severity);
        Assert.Equal(0, rolled.Observed);
        Assert.Equal(0, rolled.Unknown);
        Assert.Equal(0, rolled.Shelved);
    }

    [Fact]
    public void AParentWhoseChildrenAreAllUnseen_IsUnseenItself()
    {
        var rolled = OpsCondition.RollUp([OpsCondition.Unknown, OpsCondition.Unknown]);

        Assert.Equal(OpsVisibility.Unknown, rolled.Condition.Visibility);
        Assert.Equal(OpsSeverity.Normal, rolled.Severity);
    }

    [Fact]
    public void AParentWithOneVisibleChild_IsObserved()
    {
        var rolled = OpsCondition.RollUp([OpsCondition.Unknown, OpsCondition.Resting]);

        Assert.Equal(OpsVisibility.Observed, rolled.Condition.Visibility);
    }

    // ── two windows, one verdict ───────────────────────────────────────────────

    [Fact]
    public void TwoWindowsOfTheSameSignal_TakeTheWorseOfTheTwo()
    {
        // The SRE burn-rate shape: a fast window catches the acute failure, a slow one catches the smoulder.
        // The caller judges each window — the library only says which verdict stands.
        Assert.Equal(OpsSeverity.Critical, OpsSeverity.Warning.Worst(OpsSeverity.Critical));
        Assert.Equal(OpsSeverity.Critical, OpsSeverity.Critical.Worst(OpsSeverity.Warning));
        Assert.Equal(OpsSeverity.Normal, OpsSeverity.Normal.Worst(OpsSeverity.Normal));
    }

    [Fact]
    public void TheRestingConditionIsTheDefault()
    {
        // A tile that says nothing about its state must draw as one nobody has judged — not as an alarm.
        Assert.Equal(OpsSeverity.Normal, default(OpsCondition).Severity);
        Assert.Equal(OpsVisibility.Observed, default(OpsCondition).Visibility);
        Assert.Equal(OpsCondition.Resting, default(OpsCondition));
    }
    [Fact]
    public void AConditionNamedByItsSeverityAlone_IsObserved()
    {
        // The common case at a call site: a threshold was crossed and we can see it perfectly well.
        var condition = new OpsCondition(OpsSeverity.Critical);

        Assert.Equal(OpsSeverity.Critical, condition.Severity);
        Assert.Equal(OpsVisibility.Observed, condition.Visibility);
    }

    [Fact]
    public void ShelvingAReading_KeepsEverythingButTheVisibility()
    {
        // The verb an operator's gesture maps onto: silence this one, and leave the reading exactly as it was.
        var firing = new OpsCondition(OpsSeverity.Critical);

        var shelved = firing.Shelve();

        Assert.Equal(OpsSeverity.Critical, shelved.Severity);
        Assert.Equal(OpsVisibility.Shelved, shelved.Visibility);
        Assert.Equal(OpsVisibility.Observed, firing.Visibility);
    }

    [Fact]
    public void ShelvingSomethingAlreadyShelved_ChangesNothing()
    {
        var shelved = new OpsCondition(OpsSeverity.Warning, OpsVisibility.Shelved);

        Assert.Equal(shelved, shelved.Shelve());
    }

    [Fact]
    public void RollingUpNothing_RefusesANullSet()
    {
        Assert.Throws<ArgumentNullException>(() => OpsCondition.RollUp(null!));
    }

}
