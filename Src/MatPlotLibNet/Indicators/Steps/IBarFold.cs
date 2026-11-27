// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>A step over an indicator's bars: what it says at each bar, the bars taken in order. The indicator's base builds one from
/// a step and the inputs it holds, so the fold over the bars (<c>Indicator{TResult}.FoldBars</c>) is written once, whatever the step
/// reads and whatever it says.</summary>
/// <typeparam name="TOut">What the step says at a bar: a value, or a point of several lines.</typeparam>
internal interface IBarFold<TOut>
{
    /// <summary>Takes the next bar and says the step's output there.</summary>
    /// <param name="bar">The bar, from 0, one more than the bar before.</param>
    /// <returns>The output; NaN while the step has none.</returns>
    TOut Next(int bar);
}

/// <summary>The lines an indicator's values are written to, one entry per bar from its first value on.</summary>
/// <typeparam name="TOut">What one bar gives every line.</typeparam>
internal interface IOutputLines<TOut>
{
    /// <summary>Writes the output of one bar to every line.</summary>
    /// <param name="index">The entry: the bar less the indicator's first value index.</param>
    /// <param name="output">The output of the bar.</param>
    void Set(int index, TOut output);
}

/// <summary>The one line of an indicator whose step says one value a bar.</summary>
/// <param name="count">How many values the line holds.</param>
internal readonly struct ValueLine(int count) : IOutputLines<double>
{
    /// <summary>The values.</summary>
    internal double[] Values { get; } = new double[count];

    /// <inheritdoc />
    public void Set(int index, double output) => Values[index] = output;
}

/// <summary>A step that reads one price a bar.</summary>
/// <typeparam name="TOut">What it says after a price.</typeparam>
internal interface IPriceStep<TOut>
{
    /// <summary>Takes the next price.</summary>
    /// <param name="price">The price.</param>
    /// <returns>The output after it.</returns>
    TOut Next(double price);
}

/// <summary>A step that reads the high, the low and the close of a bar.</summary>
/// <typeparam name="TOut">What it says after a bar.</typeparam>
internal interface IHlcStep<TOut>
{
    /// <summary>Takes the next bar.</summary>
    /// <param name="high">Its high.</param>
    /// <param name="low">Its low.</param>
    /// <param name="close">Its close.</param>
    /// <returns>The output after it.</returns>
    TOut Next(double high, double low, double close);
}

/// <summary>A step that reads the close and the volume of a bar.</summary>
/// <typeparam name="TOut">What it says after a bar.</typeparam>
internal interface ICloseVolumeStep<TOut>
{
    /// <summary>Takes the next bar.</summary>
    /// <param name="close">Its close.</param>
    /// <param name="volume">The volume traded in it.</param>
    /// <returns>The output after it.</returns>
    TOut Next(double close, double volume);
}

/// <summary>A step that reads the high, the low, the close and the volume of a bar.</summary>
/// <typeparam name="TOut">What it says after a bar.</typeparam>
internal interface IHlcvStep<TOut>
{
    /// <summary>Takes the next bar.</summary>
    /// <param name="high">Its high.</param>
    /// <param name="low">Its low.</param>
    /// <param name="close">Its close.</param>
    /// <param name="volume">The volume traded in it.</param>
    /// <returns>The output after it.</returns>
    TOut Next(double high, double low, double close, double volume);
}
