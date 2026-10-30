// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A composition of three parts: how much of the one named at the apex of a ternary diagram, how much of the one at
/// its bottom left, how much of the one at its bottom right. The order is that of mpltern (top, left, right; counter-clockwise
/// from the apex). The parts need not sum to one: <see cref="TernaryPointExtensions.Closed"/> makes them fractions.</summary>
/// <param name="Top">The part at the apex.</param>
/// <param name="Left">The part at the bottom left corner.</param>
/// <param name="Right">The part at the bottom right corner.</param>
public readonly record struct TernaryPoint(double Top, double Left, double Right);

/// <summary>The frame a ternary diagram is drawn in: an equilateral triangle one unit wide with its base on the X axis and its
/// left corner at the origin.</summary>
public static class TernaryFrame
{
    /// <summary>How tall the triangle of unit width is, √3/2. Exact, not the rounded 0.866: a point placed by the rounded number
    /// is 2.5e-5 off its gridline.</summary>
    public static readonly double Height = Math.Sqrt(3) / 2;
}

/// <summary>The one owner of the conversion between a composition and its place in the frame of a ternary diagram
/// (<see cref="TernaryFrame"/>): <c>x = r + t/2</c> and <c>y = (√3/2)·t</c>, and back. The axes renderer, the hit test, the
/// table and the Piper diagram all stand on it.</summary>
public static class TernaryPointExtensions
{
    /// <summary>How far a part may be below zero and the place still count as inside the triangle, for the rounding of a
    /// conversion back from a pixel.</summary>
    private const double InsideTolerance = 1e-9;

    extension(TernaryPoint composition)
    {
        /// <summary>Where this composition stands in the frame. The parts are taken as they are; call
        /// <see cref="Closed"/> first for amounts that do not sum to one.</summary>
        /// <returns>The place in the frame: X from the left corner, Y up from the base, both in units of the triangle's width.</returns>
        public Point InFrame() => new(composition.Right + composition.Top / 2, TernaryFrame.Height * composition.Top);

        /// <summary>This composition as fractions of its sum, so that (2, 5, 3) and (20, 50, 30) are the same place.</summary>
        /// <returns>The same proportions with the parts summing to one.</returns>
        /// <exception cref="ArgumentOutOfRangeException">A part is negative, infinite or not a number; the message names it.</exception>
        /// <exception cref="ArgumentException">All three parts are zero, so there are no proportions to place.</exception>
        public TernaryPoint Closed()
        {
            CheckPart(composition.Top, nameof(TernaryPoint.Top));
            CheckPart(composition.Left, nameof(TernaryPoint.Left));
            CheckPart(composition.Right, nameof(TernaryPoint.Right));

            double sum = composition.Top + composition.Left + composition.Right;
            if (sum == 0)
                throw new ArgumentException("A ternary composition needs proportions to place, and all three parts are zero.", nameof(composition));

            return new TernaryPoint(composition.Top / sum, composition.Left / sum, composition.Right / sum);
        }

        /// <summary>As <see cref="Closed"/>, for a caller that leaves a composition it cannot place out instead of refusing it
        /// (a drawing, a hover): <see langword="false"/> where <see cref="Closed"/> would throw.</summary>
        /// <param name="closed">The composition as fractions of its sum, or the default when it cannot be closed.</param>
        /// <returns><see langword="true"/> when the composition could be closed.</returns>
        public bool TryClosed(out TernaryPoint closed)
        {
            double sum = composition.Top + composition.Left + composition.Right;
            if (IsPart(composition.Top) && IsPart(composition.Left) && IsPart(composition.Right) && sum > 0 && double.IsFinite(sum))
            {
                closed = new TernaryPoint(composition.Top / sum, composition.Left / sum, composition.Right / sum);
                return true;
            }

            closed = default;
            return false;
        }

        /// <summary>Whether this composition lies inside the triangle or on its edge, which is when none of its parts is below
        /// zero (to the rounding of a conversion back from a pixel). A place outside the triangle has a part below zero, so a
        /// hit test on it finds nothing.</summary>
        /// <returns><see langword="true"/> when no part is below zero.</returns>
        public bool IsInside() =>
            composition.Top >= -InsideTolerance && composition.Left >= -InsideTolerance && composition.Right >= -InsideTolerance;
    }

    extension(Point place)
    {
        /// <summary>The composition that stands at this place of the frame: <c>t = y / (√3/2)</c>, <c>r = x − t/2</c>,
        /// <c>l = 1 − t − r</c>. A place outside the triangle gives a part below zero (<see cref="IsInside"/>).</summary>
        /// <returns>The composition, its parts summing to one.</returns>
        public TernaryPoint ToTernary()
        {
            double top = place.Y / TernaryFrame.Height;
            double right = place.X - top / 2;
            return new TernaryPoint(top, 1 - top - right, right);
        }
    }

    private static bool IsPart(double part) => double.IsFinite(part) && part >= 0;

    private static void CheckPart(double part, string name)
    {
        if (!IsPart(part))
            throw new ArgumentOutOfRangeException(name, part, $"The {name} part of a ternary composition must be a finite number that is not negative.");
    }
}
