// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>Pure stateless easing functions. Each maps a normalized progress
/// value <c>t ∈ [0, 1]</c> to an output value, with <c>f(0) = 0</c> and
/// <c>f(1) = 1</c>.</summary>
public static class EasingFunction
{
    /// <summary>Constant velocity — output equals input.</summary>
    public static double Linear(double t) => t;

    /// <summary>Quadratic ease-in: slow start, fast finish.</summary>
    public static double EaseIn(double t) => t * t;

    /// <summary>Quadratic ease-out: fast start, slow finish.</summary>
    public static double EaseOut(double t) => t * (2 - t);

    /// <summary>Cubic ease-in-out: symmetric S-curve.</summary>
    public static double EaseInOut(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    /// <summary>Bounce ease-out: settles with diminishing bounces.</summary>
    public static double Bounce(double t)
    {
        const double n1 = 7.5625, d1 = 2.75;
        if (t < 1 / d1)           return n1 * t * t;
        if (t < 2 / d1)           return n1 * (t -= 1.5 / d1) * t + 0.75;
        if (t < 2.5 / d1)         return n1 * (t -= 2.25 / d1) * t + 0.9375;
                                   return n1 * (t -= 2.625 / d1) * t + 0.984375;
    }

    /// <summary>Elastic ease-out: overshoots then settles.</summary>
    public static double Elastic(double t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        const double c4 = 2 * Math.PI / 3;
        return Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
    }

    /// <summary>Dispatches to the named easing function.</summary>
    public static double Apply(EasingKind kind, double t) => kind switch
    {
        EasingKind.Linear    => Linear(t),
        EasingKind.EaseIn    => EaseIn(t),
        EasingKind.EaseOut   => EaseOut(t),
        EasingKind.EaseInOut => EaseInOut(t),
        EasingKind.Bounce    => Bounce(t),
        EasingKind.Elastic   => Elastic(t),
        _                    => Linear(t),
    };
}
