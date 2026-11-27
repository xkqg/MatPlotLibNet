// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>The one rule a light shades a face by: the colour modulated at the light's intensity for the face. The default of
/// <see cref="ILightSource.Shade"/> and the published <see cref="DirectionalLight.Shade"/> both read it.</summary>
internal static class LightSourceExtensions
{
    extension(ILightSource light)
    {
        /// <summary><paramref name="color"/> scaled by the intensity of this light on a face with the given outward normal
        /// (<see cref="ColorExtensions.Modulate"/> at <see cref="ILightSource.ComputeIntensity"/>), alpha kept.</summary>
        /// <param name="color">The colour of the face when it is unlit.</param>
        /// <param name="nx">X component of the face's outward normal.</param>
        /// <param name="ny">Y component of the face's outward normal.</param>
        /// <param name="nz">Z component of the face's outward normal.</param>
        /// <returns>The shaded colour.</returns>
        internal Color ShadeAtIntensity(Color color, double nx, double ny, double nz) =>
            color.Modulate(light.ComputeIntensity(nx, ny, nz));
    }
}
