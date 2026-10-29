// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>Computes light intensity for a surface face given its normal vector, and shades a colour with it.</summary>
public interface ILightSource
{
    /// <summary>Returns a light intensity in [0, 1] for a face with the given outward normal.</summary>
    double ComputeIntensity(double nx, double ny, double nz);

    /// <summary>The colour <paramref name="color"/> is drawn in on a face with the given outward normal: the colour scaled by
    /// the intensity of the light on that face (<see cref="ComputeIntensity"/>), alpha kept. Every surface and every bar that is
    /// lit asks this one question of the light it was given, so a light of your own shades a surface and a 3-D bar alike, and a
    /// light that shades differently (rounding, a tint) says so here.</summary>
    /// <param name="color">The colour of the face when it is unlit.</param>
    /// <param name="nx">X component of the face's outward normal.</param>
    /// <param name="ny">Y component of the face's outward normal.</param>
    /// <param name="nz">Z component of the face's outward normal.</param>
    /// <returns>The shaded colour.</returns>
    Color Shade(Color color, double nx, double ny, double nz) => color.Modulate(ComputeIntensity(nx, ny, nz));
}
