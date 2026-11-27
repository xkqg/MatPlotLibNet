// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Numerics;

/// <summary>Whether the data holds enough items for what a parameter needs of it (two segments of samples for a coherence, two
/// signals in a row for a dispersion index, more samples than lags for a correlation, two stops for a colour map), or no more than it
/// takes (ten features for a turbulence index, a padding no shorter than the signal), written once. The receiver is the count the data
/// holds, whatever holds it (an array, a span, a row of a matrix, a list, a segment), and the refusal is the data's fault, not one
/// parameter's range: an <see cref="ArgumentException"/> that is not an <see cref="ArgumentOutOfRangeException"/>, naming the parameter
/// that asks, in one sentence told by <see cref="DataNeed"/>. A collection with nothing in it is <c>ArrayLengthExtensions</c>' to
/// refuse.</summary>
internal static class DataCountExtensions
{
    extension(int held)
    {
        /// <summary>This count, refused when it is below <paramref name="least"/>.</summary>
        /// <param name="least">How many items the parameter needs; a <see langword="long"/>, so a need one past the largest count
        /// (<c>maxLags + 1L</c>) is refused rather than wrapped round.</param>
        /// <param name="name">The parameter that asks, for the error.</param>
        /// <param name="need">The words of the refusal.</param>
        /// <returns>The count, unchanged.</returns>
        /// <exception cref="ArgumentException">The data holds fewer items than <paramref name="least"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ThrowIfFewerThan(long least, string name, DataNeed need) =>
            held >= least ? held : throw new ArgumentException(Refusal(held, "needs at least", least, need), name);

        /// <summary>This count, refused when it is above <paramref name="most"/>.</summary>
        /// <param name="most">How many items the parameter takes at most.</param>
        /// <param name="name">The parameter that asks, for the error.</param>
        /// <param name="need">The words of the refusal.</param>
        /// <returns>The count, unchanged.</returns>
        /// <exception cref="ArgumentException">The data holds more items than <paramref name="most"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ThrowIfMoreThan(long most, string name, DataNeed need) =>
            held <= most ? held : throw new ArgumentException(Refusal(held, "takes at most", most, need), name);
    }

    /// <summary>The one sentence: <c>The signal holds 3 samples; a segment of SegmentLength needs at least 4; set a shorter
    /// SegmentLength.</c></summary>
    private static string Refusal(int held, string bound, long count, DataNeed need) =>
        string.Invariant($"{need.Holder} holds {held} {need.Unit}{(held == 1 ? "" : "s")}; {need.Taker} {bound} {count}{(need.Remedy is null ? "" : "; " + need.Remedy)}.");
}
