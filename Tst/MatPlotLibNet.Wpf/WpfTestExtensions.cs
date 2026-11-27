// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using MatPlotLibNet.Interaction;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>What every WPF test does before it reaches a control: it runs on a single-threaded apartment, because a WPF element
/// cannot be constructed anywhere else (its keyboard-navigation service demands one), and it paints the control the way WPF does.</summary>
internal static class WpfTestExtensions
{
    /// <param name="body">The test's body.</param>
    extension(Action body)
    {
        /// <summary>Runs the body on a new STA thread and throws what it threw. The dispatcher it creates there never pumps unless the
        /// body runs it.</summary>
        public void OnStaThread()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { body(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <param name="control">The control to paint.</param>
    extension(MplChartSurface control)
    {
        /// <summary>Paints the control once on an 800 by 600 Skia surface, as WPF does (<c>OnPaintSurface</c>), so its host is aimed at
        /// the figure it showed.</summary>
        /// <returns>The control's host.</returns>
        public InteractionHost PaintedOnce()
        {
            var info = new SKImageInfo(800, 600);
            using var surface = SKSurface.Create(info);
            typeof(MplChartSurface).GetMethod("OnPaintSurface", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(control, [new SKPaintSurfaceEventArgs(surface, info)]);
            return control.Host;
        }
    }
}
