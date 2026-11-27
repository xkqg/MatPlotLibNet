// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What one pass over the contributions of an axes gives the draw: the range the axes is drawn over, and whether the
/// patches can reach past the plot area (<c>AxesRangeExtensions.RangeOf</c>). Both answers stand on the same
/// contributions, so the draw asks every series for its range once.</summary>
/// <param name="Range">The combined X and Y ranges the axes draws.</param>
/// <param name="PatchesMayReachPastPlot">True when a patch of the axes can lie past the plot area, so that the patches are clipped
/// to it as matplotlib clips every patch (<c>clip_on</c>): a limit was set (the range is then exact), or, under the matplotlib
/// themes (<see cref="AxisRangeLook.PatchesLikeAddPatch"/>), the patches take no part in the range of one of the axes. An axes whose
/// range holds every patch needs no clip, and an axes without patches is asked for none.</param>
internal readonly record struct AxesRange(DataRange Range, bool PatchesMayReachPastPlot);
