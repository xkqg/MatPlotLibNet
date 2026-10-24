// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The product-limit (Kaplan-Meier) estimate of one <see cref="SurvivalGroup"/>, as the points of a step curve read
/// "after each point, the level holds until the next" (matplotlib's <c>steps-post</c>): <see cref="Times"/> ascending, and
/// <see cref="Survival"/> the estimated probability of surviving past each.</summary>
/// <param name="Times">The time of each point: 0, then every time at which an event happened, then the last follow-up time
/// when it is later than the last event.</param>
/// <param name="Survival">The estimate at each of <paramref name="Times"/>: 1 at 0, then the level the curve drops to at each
/// event time, then that last level again at the last follow-up.</param>
/// <param name="AtRisk">The number at risk at each of <paramref name="Times"/> (n_i): the subjects whose duration is that
/// time or later, counted just before that time's events. Greenwood's variance is computed from this and
/// <paramref name="Events"/>, not re-derived.</param>
/// <param name="Events">The number of events at each of <paramref name="Times"/> (d_i): 0 at the seed point and at an
/// extension to the last follow-up, which hold no event of their own.</param>
internal readonly record struct SurvivalCurve(double[] Times, double[] Survival, int[] AtRisk, int[] Events);
