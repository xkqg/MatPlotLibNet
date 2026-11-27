// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A point normalized to the [−1, 1]³ cube, returned by <see cref="Projection3D.Normalize"/>.</summary>
/// <remarks>One point of the vertex data a browser turns a 3-D scene with (<c>data-v3d</c>) is written by one writer: the
/// three coordinates, <c>nx,ny,nz</c>, in the invariant culture. The frame of the scene (panes, edges, grid lines, ticks and their texts)
/// is written in <see cref="FrameDigits"/>, a mark of a series (a face of a surface, a marker of a scatter) in <see cref="MarkDigits"/>,
/// so the many marks of a series cost fewer characters than the few lines of the frame. The script reads every point the same way,
/// whichever its digits.</remarks>
public readonly record struct Normalized3DPoint(double Nx, double Ny, double Nz)
{
    /// <summary>The digits of a point of the frame, and of the pad of a text beside it: six significant digits.</summary>
    internal const string FrameDigits = "G6";

    /// <summary>The digits of a point of a mark: four significant digits.</summary>
    internal const string MarkDigits = "G4";

    /// <summary>The longest coordinate either form writes: a sign, a digit, the point, five more digits and an exponent of three
    /// digits with its sign (<c>-1.79769E+308</c>).</summary>
    private const int CoordinateLength = 13;

    /// <summary>The room a point takes at most: three coordinates and the two commas between them.</summary>
    internal const int PointLength = 3 * CoordinateLength + 2;

    /// <summary>Writes the point into <paramref name="into"/> as <c>nx,ny,nz</c>, each coordinate in <paramref name="digits"/>.</summary>
    /// <param name="into">Room for the text: <see cref="PointLength"/> characters hold any point.</param>
    /// <param name="digits"><see cref="FrameDigits"/> or <see cref="MarkDigits"/>.</param>
    /// <returns>How many characters were written.</returns>
    internal int FormatWire(Span<char> into, string digits)
    {
        int written = Nx.FormatInvariant(into, digits);
        into[written++] = ',';
        written += Ny.FormatInvariant(into[written..], digits);
        into[written++] = ',';
        written += Nz.FormatInvariant(into[written..], digits);
        return written;
    }
}
