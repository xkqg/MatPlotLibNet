// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Uno.UI.Runtime.Skia.Wpf;

namespace MatPlotLibNet.Samples.Uno;

/// <summary>Starts the Uno application in a WPF window on Uno's Skia renderer - the backend
/// <c>MatPlotLibNet.Uno</c> draws on. WPF is only the host here; the application and its page are Uno XAML.</summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Since Uno 5 the WPF host works at the application level: this head owns the WPF Application and its
        // dispatcher, the host starts the Uno App on that dispatcher, and the WPF loop pumps it. WpfHost.Run()
        // only pumps for an Application it created itself, which the public constructor does not - without the
        // second line the process starts the app, has nothing to pump it with, and exits with 0.
        var wpfApplication = new System.Windows.Application();
        var host = new WpfHost(wpfApplication.Dispatcher, () => new App());
        host.Run();
        wpfApplication.Run();
    }
}
