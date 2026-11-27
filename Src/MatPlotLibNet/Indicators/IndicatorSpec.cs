// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>What an indicator's batch form and its streaming form both say from the parameters they are made with, said once: the
/// label of the indicator, the labels of its lines in the order of its outputs, and how many bars come before it has a value on every
/// line. It is made only by the batch class's <c>Spec</c> factory, which checks the parameters first, so a parameter is checked, and a
/// label written, in one place for both forms: the batch constructor takes its <see cref="Indicator.Label"/> from it, the streaming
/// constructor its lines and its warm-up.</summary>
internal readonly record struct IndicatorSpec
{
    private readonly string[] _lines;

    private IndicatorSpec(string label, string[] lines, int firstValueIndex)
    {
        Label = label;
        _lines = lines;
        FirstValueIndex = firstValueIndex;
    }

    /// <summary>The spec of an indicator written outside the library, which names itself and places its lines by the warm-up it
    /// passes: no label, no lines, no first value.</summary>
    internal static IndicatorSpec None { get; }

    /// <summary>The label of the indicator: the legend text of its main line.</summary>
    internal string Label { get; }

    /// <summary>The labels of the indicator's lines, in the order of its outputs.</summary>
    internal ReadOnlySpan<string> Lines => _lines;

    /// <summary>How many bars come before the indicator has a value on every line.</summary>
    internal int FirstValueIndex { get; }

    /// <summary>How many inputs a streaming form takes before it has a value on every line: one more than
    /// <see cref="FirstValueIndex"/>.</summary>
    internal int Warmup => FirstValueIndex + 1;

    /// <summary>The spec of an indicator. Called only by the batch class's <c>Spec</c> factory, after it has checked the parameters.</summary>
    /// <param name="firstValueIndex">How many bars come before the indicator has a value on every line; not below 0.</param>
    /// <param name="label">The label of the indicator, its numbers written by <see cref="InvariantTextExtensions"/>.</param>
    /// <param name="lines">The labels of its lines in the order of its outputs; none names one line, by the label.</param>
    /// <returns>The spec.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstValueIndex"/> is below 0.</exception>
    internal static IndicatorSpec Of(int firstValueIndex, string label, params string[] lines)
    {
        return new(label, lines.Length == 0 ? [label] : lines, firstValueIndex.ThrowIfBelow(0, nameof(firstValueIndex)));
    }

    /// <summary>The label of an indicator one number names: the name, the number in brackets (<c>RSI(14)</c>), written by
    /// <see cref="InvariantTextExtensions"/>. The one spelling of such a label; an indicator whose label is also the name of one of its
    /// lines takes it from here and says the lines itself.</summary>
    /// <param name="name">What the indicator is called.</param>
    /// <param name="number">The number that says it (its period, its window).</param>
    /// <returns>The label.</returns>
    internal static string Labelled(string name, int number) => string.Invariant($"{name}({number})");

    /// <summary>The spec of an indicator one window names and that draws one line, labelled <see cref="Labelled"/>.</summary>
    /// <param name="name">What the indicator is called.</param>
    /// <param name="period">The window, as the label says it.</param>
    /// <param name="firstValueIndex">How many bars come before the indicator has a value; not below 0.</param>
    /// <returns>The spec.</returns>
    internal static IndicatorSpec OfWindow(string name, int period, int firstValueIndex) => Of(firstValueIndex, Labelled(name, period));

    /// <summary>The spec of an indicator one window names, after checking that the window is at least <paramref name="least"/>.</summary>
    /// <param name="name">What the indicator is called.</param>
    /// <param name="period">The window, as the label says it; refused below <paramref name="least"/>.</param>
    /// <param name="least">The smallest window accepted.</param>
    /// <param name="firstValueIndex">How many bars come before the indicator has a value; not below 0.</param>
    /// <param name="parameter">The argument the refusal names: the window as the caller wrote it.</param>
    /// <returns>The spec.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> is below <paramref name="least"/>.</exception>
    internal static IndicatorSpec OfCheckedWindow(string name, int period, int least, int firstValueIndex,
        [CallerArgumentExpression(nameof(period))] string? parameter = null) =>
        OfWindow(name, period.ThrowIfBelow(least, parameter!), firstValueIndex);
}
