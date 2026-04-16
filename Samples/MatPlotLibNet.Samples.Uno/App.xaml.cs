// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Xaml;

namespace MatPlotLibNet.Samples.Uno;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window
        {
            Title = "MatPlotLibNet - Uno Sample"
        };
        _window.Content = new MainPage();
        _window.Activate();
    }
}
