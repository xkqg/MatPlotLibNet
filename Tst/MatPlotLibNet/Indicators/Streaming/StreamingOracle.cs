// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>How a streaming form gets its value, as the classification map states it for each indicator.</summary>
public enum StreamingKind
{
    /// <summary>The value depends on the last W inputs only: the form keeps a window.</summary>
    Window,

    /// <summary>The value depends on all inputs through a recursion (a filter, a smoothing): the form carries its state.</summary>
    Recursive,

    /// <summary>The value depends on all inputs through a state that flips (a stop, a trend): the form carries its state.</summary>
    StateMachine,

    /// <summary>The value is a running total of all inputs: the form carries the total.</summary>
    Cumulative,

    /// <summary>The value is taken over a row of several series at a time.</summary>
    Multivariate,
}

/// <summary>A deterministic walk of bars the streaming forms and the batch indicators are both given: a price that moves in
/// quarter steps (so equal closes happen), now and then a flat bar, and a volume that varies. A seeded <see cref="Random"/> makes
/// the same bars on every machine; nothing here is a transcendental function.</summary>
public readonly record struct OracleBars(double[] Open, double[] High, double[] Low, double[] Close, double[] Volume)
{
    /// <summary>How many bars.</summary>
    public int Count => Close.Length;

    /// <summary>The bar at <paramref name="i"/>.</summary>
    public OhlcvBar At(int i) => new(Open[i], High[i], Low[i], Close[i], Volume[i]);

    /// <summary>The first <paramref name="m"/> bars: what a batch indicator is given to say what its value is after bar <c>m - 1</c>.</summary>
    public OracleBars Prefix(int m) => new(Open[..m], High[..m], Low[..m], Close[..m], Volume[..m]);

    /// <summary>The walk.</summary>
    /// <param name="count">How many bars.</param>
    /// <param name="seed">The seed; the same seed is the same bars.</param>
    public static OracleBars Walk(int count, int seed = 7)
    {
        var random = new Random(seed);
        var open = new double[count];
        var high = new double[count];
        var low = new double[count];
        var close = new double[count];
        var volume = new double[count];
        double previous = 100;
        for (int i = 0; i < count; i++)
        {
            double step = Math.Round((random.NextDouble() - 0.5) * 6 * 4) / 4;   // quarter steps, from -3 to 3
            open[i] = previous;
            close[i] = Math.Max(1, previous + step);
            bool flat = i % 17 == 16;
            high[i] = flat ? open[i] : Math.Max(open[i], close[i]) + Math.Round(random.NextDouble() * 2 * 4) / 4;
            low[i] = flat ? open[i] : Math.Max(0.25, Math.Min(open[i], close[i]) - Math.Round(random.NextDouble() * 2 * 4) / 4);
            if (flat) close[i] = open[i];
            volume[i] = 500 + Math.Round(random.NextDouble() * 1000);
            previous = close[i];
        }

        return new OracleBars(open, high, low, close, volume);
    }
}

/// <summary>One row of the equality theory: an indicator, a fresh streaming form of it, and the batch
/// indicator over the same inputs with the same parameters (how the form gets its value is the line of <see cref="StreamingMap"/>). The batch indicator is the oracle: after the bar at index <c>i</c> the
/// streaming form says what the batch says over the bars <c>0..i</c>.</summary>
/// <param name="Name">The batch indicator.</param>
/// <param name="Make">A fresh streaming form.</param>
/// <param name="BatchLast">The last value of each output of the batch indicator over the given bars; NaN for an output the batch has no value for yet.</param>
/// <param name="Bars">How many bars the row is run over.</param>
/// <param name="Feed">How the form is given the bar at an index; null gives it the bar. A form that reads a row of several series is given the row.</param>
/// <param name="MinCompared">The fewest values that must be compared (not NaN on both sides); null asks for half the bars. A form that
/// leaves bars out on purpose (a stride) asks for fewer.</param>
public sealed record StreamingRow(string Name, Func<IStreamingIndicator> Make, Func<OracleBars, double[]> BatchLast, int Bars = 200,
    Action<IStreamingIndicator, OracleBars, int>? Feed = null, int? MinCompared = null)
{
    /// <summary>The last value of a batch result, or NaN when it has none.</summary>
    public static double Last(double[] values) => values.Length > 0 ? values[^1] : double.NaN;
}
