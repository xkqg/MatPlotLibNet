// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>How the step between two major ticks of an axis whose scale brings no minor locator of its own (linear, date) is split into
/// minor ticks, matplotlib's <c>AutoMinorLocator(n)</c>: in <paramref name="Divisions"/> parts, or, with none, in the parts matplotlib's
/// <c>xtick.minor.ndivs: auto</c> takes for the step (<see cref="PartsOf"/>). A value of the theme's tick look
/// (<see cref="Styling.AxisTickLook.MinorSplit"/>); a log, logit or symmetric-log scale places its minor ticks by its own locator
/// (<see cref="IMinorTickLocator"/>).</summary>
/// <remarks>A value and not an <see cref="IMinorTickLocator"/>: a split of the major step has no ticks of its own over a range (matplotlib's
/// <c>AutoMinorLocator.tick_values</c> raises), and the walk it hands back allocates nothing.</remarks>
/// <param name="Divisions">The parts each step is split into; <see langword="null"/> for matplotlib's automatic count.</param>
internal readonly record struct AutoMinorLocator(int? Divisions)
{
    /// <summary>The library's own split: fifths.</summary>
    internal static AutoMinorLocator Library { get; } = new(5);

    /// <summary>matplotlib's split under both its styles (<c>xtick.minor.ndivs = 'auto'</c>).</summary>
    internal static AutoMinorLocator Auto { get; } = new(null);

    /// <summary>The minor values between <paramref name="majors"/>: the step between the first two split in <see cref="PartsOf"/> parts,
    /// walked over every major step (<see cref="MinorWalk"/>).</summary>
    /// <param name="majors">The major tick values, low to high; fewer than two split nothing.</param>
    /// <returns>The walk; <c>foreach</c> over it allocates nothing.</returns>
    internal MinorWalk Between(double[] majors)
    {
        if (majors.Length < 2) return new(majors, 0);
        double step = majors[1] - majors[0];
        return new(majors, step / PartsOf(step));
    }

    /// <summary>How many parts a step of <paramref name="majorStep"/> is split into: <see cref="Divisions"/>, or matplotlib 3.11.2's
    /// <c>'auto'</c> count, five when the step's mantissa (<c>10 ** (np.log10(majorstep) % 1)</c>) is close to 1, 2.5, 5 or 10
    /// (<c>np.isclose</c>: within 1e-8 plus 1e-5 of it), four otherwise.</summary>
    /// <param name="majorStep">The step between the first two major ticks.</param>
    /// <returns>The parts.</returns>
    internal int PartsOf(double majorStep)
    {
        if (Divisions is { } divisions) return divisions;
        double exponent = Math.Log10(Math.Abs(majorStep));
        double mantissa = Math.Pow(10, exponent - Math.Floor(exponent));
        foreach (double nice in (ReadOnlySpan<double>)[1, 2.5, 5, 10])
            if (Math.Abs(mantissa - nice) <= 1e-8 + 1e-5 * nice) return 5;
        return 4;
    }
}
