// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The camera positions the 3-D interaction tests run over: matplotlib's default, the playground's default, straight down
/// the X axis, shallow and sideways, and high and slightly turned. Each row is the elevation and the azimuth, in degrees.</summary>
public sealed class CameraAngles : TheoryData<int, int>
{
    /// <summary>The five camera positions.</summary>
    public CameraAngles()
    {
        Add(30, -60);
        Add(35, -50);
        Add(45, 0);
        Add(10, -90);
        Add(60, -30);
    }
}

/// <summary>The 3-D page the interaction tests start from.</summary>
public static class ThreeDScenes
{
    extension(InteractionScriptHarness)
    {
        /// <summary>A page with a two-by-two surface under a camera, the scripts of a browser-interactive figure running.</summary>
        /// <param name="elevation">The camera's elevation, in degrees.</param>
        /// <param name="azimuth">The camera's azimuth, in degrees.</param>
        /// <param name="distance">The camera's distance.</param>
        public static InteractionScriptHarness ThreeDScene(double elevation = 30, double azimuth = -60, double distance = 8) =>
            InteractionScriptHarness.FromBuilder(b => b
                .WithSize(600, 500)
                .WithBrowserInteraction()
                .AddSubPlot(1, 1, 1, ax => ax
                    .WithCamera(elevation: elevation, azimuth: azimuth, distance: distance)
                    .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));
    }
}
