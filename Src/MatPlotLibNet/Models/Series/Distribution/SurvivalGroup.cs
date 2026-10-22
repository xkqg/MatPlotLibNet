// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One group of a survival chart: a name and, for every subject of the group, how long it was followed and whether
/// the event was seen. The two arms of a trial are two survival groups, and the name is how the chart labels the group.
/// <para>Subject i was followed for <c>Durations[i]</c> (a time from its start, in whatever unit the chart is read in: days,
/// months) and <c>Observed[i]</c> says how that follow-up ended: <see langword="true"/> when the event happened at that
/// time, <see langword="false"/> when the subject was censored there (lost to follow-up, or still without the event when the
/// study ended), so all that is known is that the event had not happened by then.</para>
/// <para>A group is of free length, one duration and one flag per subject, the two arrays of equal length. A duration is a
/// finite time at or above 0: a subject without a time cannot be counted among those at risk, so a missing duration (NaN)
/// is refused rather than left out (as lifelines refuses it); leave the subject out of the arrays if that is what is
/// meant.</para>
/// <para>Groups are passed as an ordered LIST rather than a map from name to subjects, because the order IS the picture: it
/// decides which colour each group takes from the theme's cycle. A dictionary has no order to give.</para></summary>
/// <param name="Label">The group's name, as the chart shows it.</param>
/// <param name="Durations">How long each subject was followed, in any order.</param>
/// <param name="Observed">For each subject, in the order of <paramref name="Durations"/>: <see langword="true"/> when the event
/// happened at its duration, <see langword="false"/> when it was censored there.</param>
public readonly record struct SurvivalGroup(string Label, double[] Durations, bool[] Observed);
