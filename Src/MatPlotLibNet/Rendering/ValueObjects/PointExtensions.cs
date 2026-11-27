// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Rendering;

/// <summary>The one place the length of a step in the plane, the distance between two pixels, the direction from one to the other, and
/// a point on the rim round a centre at an angle (as the screen, a polar axes and a bearing lay an angle) are worked out. A length is
/// the square root of the two squares added, in that order, with no other rounding, so a picture drawn from the hand-written sums
/// that stood at every arrow, connector, quiver, force and label keeps every digit.</summary>
internal static class PointExtensions
{
    extension(Point from)
    {
        /// <summary>The step from this point to <paramref name="to"/>: the difference along each axis, a point read as a step from the
        /// origin.</summary>
        /// <param name="to">The point the step runs to.</param>
        /// <returns>The step.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point StepTo(Point to) => new(to.X - from.X, to.Y - from.Y);

        /// <summary>The square of the length of this point read as a step from the origin: where a length is only compared with another
        /// (a collision, a threshold), the root is not needed.</summary>
        internal double SquaredLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => from.X * from.X + from.Y * from.Y;
        }

        /// <summary>The length of this point read as a step from the origin (a quiver's vector, a force, the step of an arrow): the
        /// square root of the sum of the two squares.</summary>
        internal double Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(from.SquaredLength);
        }

        /// <summary>The square of the distance from this point to <paramref name="to"/>: where a distance is only compared with another
        /// (a collision, a threshold), the root is not needed.</summary>
        /// <param name="to">The other point.</param>
        /// <returns>The squared distance, in pixels squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double DistanceSquaredTo(Point to) => from.StepTo(to).SquaredLength;

        /// <summary>How far this point is from <paramref name="to"/>.</summary>
        /// <param name="to">The other point.</param>
        /// <returns>The distance, in pixels; not a number when either point is not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double DistanceTo(Point to) => from.StepTo(to).Length;

        /// <summary>The way from this point to <paramref name="to"/>: the step along each axis, the length and the direction.</summary>
        /// <param name="to">The point the line runs to.</param>
        /// <returns>The heading.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Heading HeadingTo(Point to)
        {
            var step = from.StepTo(to);
            return new Heading(step.X, step.Y, step.Length);
        }

        /// <summary>The point of the rim of the ellipse round this centre with the radii <paramref name="radiusX"/> and
        /// <paramref name="radiusY"/>, at <paramref name="radians"/> laid as the screen lays an angle (clockwise, Y downward: the way an
        /// SVG arc and a wedge's path turn). The one rim formula: the centre plus each radius times the cosine and the sine.</summary>
        /// <param name="radiusX">The radius along X, in pixels.</param>
        /// <param name="radiusY">The radius along Y, in pixels.</param>
        /// <param name="radians">The angle, in radians.</param>
        /// <returns>The point of the rim.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point OnRim(double radiusX, double radiusY, double radians) =>
            new(from.X + radiusX * Math.Cos(radians), from.Y + radiusY * Math.Sin(radians));

        /// <summary>The point <paramref name="radius"/> pixels from this centre at <paramref name="radians"/> laid as the screen lays an
        /// angle (clockwise): the rim of a circle (<see cref="OnRim"/>). A negative radius stands on the other side.</summary>
        /// <param name="radius">The radius, in pixels.</param>
        /// <param name="radians">The angle, in radians.</param>
        /// <returns>The point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point AtAngle(double radius, double radians) => from.OnRim(radius, radius, radians);

        /// <summary>The point <paramref name="radius"/> pixels from this centre at <paramref name="radians"/> laid as a polar axes, a pie
        /// and a gauge lay an angle (counter-clockwise from the right, Y upward, as matplotlib does).</summary>
        /// <param name="radius">The radius, in pixels.</param>
        /// <param name="radians">The angle, in radians.</param>
        /// <returns>The point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point AtAngleCounterClockwise(double radius, double radians) => from.OnRim(radius, -radius, radians);

        /// <summary>The point <paramref name="radius"/> pixels from this centre at the bearing <paramref name="radians"/>: clockwise from
        /// the top, as a chord diagram lays its arcs.</summary>
        /// <param name="radius">The radius, in pixels.</param>
        /// <param name="radians">The bearing, in radians.</param>
        /// <returns>The point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point AtBearing(double radius, double radians) =>
            new(from.X + radius * Math.Sin(radians), from.Y - radius * Math.Cos(radians));

        /// <summary>The point <paramref name="distance"/> pixels below this one in a frame turned by <paramref name="radians"/> (a turned
        /// text's own down, as its baseline stands below its centre).</summary>
        /// <param name="distance">How far below, in pixels.</param>
        /// <param name="radians">How far the frame is turned, in radians.</param>
        /// <returns>The point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point BelowTurnedBy(double distance, double radians) =>
            new(from.X + distance * Math.Sin(radians), from.Y + distance * Math.Cos(radians));

        /// <summary>The point <paramref name="x"/> along and <paramref name="y"/> down a text's own frame from this one, the frame turned
        /// counter-clockwise by the angle whose sine and cosine are given (y downward, as the picture's): where a corner of a turned text's
        /// box, and a run of a turned label of math, stands.</summary>
        /// <param name="x">How far along the text's baseline.</param>
        /// <param name="y">How far below it.</param>
        /// <param name="sin">The sine of the turn.</param>
        /// <param name="cos">The cosine of the turn.</param>
        /// <returns>The point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Point InTurnedFrame(double x, double y, double sin, double cos) =>
            new(from.X + x * cos + y * sin, from.Y - x * sin + y * cos);
    }
}
