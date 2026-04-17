// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ThemePresets;

/// <summary>Community-inspired theme presets: developer-favorite color schemes.</summary>
internal static class CommunityThemes
{
    internal static Theme GGPlot() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Color.FromHex("#E5E5E5")).WithAxesBackground(Color.FromHex("#FFFFFF"))
        .WithForegroundText(Color.FromHex("#333333")).Build();

    internal static Theme FiveThirtyEight() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Color.FromHex("#F0F0F0")).WithAxesBackground(Color.FromHex("#F0F0F0"))
        .WithForegroundText(Color.FromHex("#333333")).Build();

    internal static Theme Bmh() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Color.FromHex("#EEEEEE")).WithAxesBackground(Color.FromHex("#EEEEEE"))
        .WithForegroundText(Color.FromHex("#555555")).Build();

    internal static Theme Solarize() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Color.FromHex("#FDF6E3")).WithAxesBackground(Color.FromHex("#FDF6E3"))
        .WithForegroundText(Color.FromHex("#657B83")).Build();

    internal static Theme Grayscale() => Theme.CreateFrom(Theme.Default)
        .WithForegroundText(Color.FromHex("#333333")).Build();

    internal static Theme Paper() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Colors.White).WithAxesBackground(Colors.White)
        .WithForegroundText(Color.FromHex("#222222")).Build();

    internal static Theme Presentation() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Colors.White).WithForegroundText(Color.FromHex("#111111")).Build();

    internal static Theme Poster() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Colors.White).WithForegroundText(Colors.Black).Build();

    internal static Theme Cyberpunk() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#0D0221")).WithAxesBackground(Color.FromHex("#0D0221"))
        .WithForegroundText(Color.FromHex("#00FF41")).Build();

    internal static Theme Nord() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#2E3440")).WithAxesBackground(Color.FromHex("#3B4252"))
        .WithForegroundText(Color.FromHex("#D8DEE9")).Build();

    internal static Theme Dracula() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#282A36")).WithAxesBackground(Color.FromHex("#44475A"))
        .WithForegroundText(Color.FromHex("#F8F8F2")).Build();

    internal static Theme Monokai() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#272822")).WithAxesBackground(Color.FromHex("#3E3D32"))
        .WithForegroundText(Color.FromHex("#F8F8F2")).Build();

    internal static Theme Catppuccin() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#1E1E2E")).WithAxesBackground(Color.FromHex("#313244"))
        .WithForegroundText(Color.FromHex("#CDD6F4")).Build();

    internal static Theme Gruvbox() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#282828")).WithAxesBackground(Color.FromHex("#3C3836"))
        .WithForegroundText(Color.FromHex("#EBDBB2")).Build();

    internal static Theme OneDark() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Color.FromHex("#282C34")).WithAxesBackground(Color.FromHex("#21252B"))
        .WithForegroundText(Color.FromHex("#ABB2BF")).Build();

    internal static Theme GitHub() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Colors.White).WithAxesBackground(Colors.White)
        .WithForegroundText(Color.FromHex("#24292E")).Build();

    internal static Theme Minimal() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Colors.White).WithAxesBackground(Colors.White)
        .WithForegroundText(Color.FromHex("#333333")).Build();

    internal static Theme Retro() => Theme.CreateFrom(Theme.Default)
        .WithBackground(Color.FromHex("#FFF8DC")).WithAxesBackground(Color.FromHex("#FAEBD7"))
        .WithForegroundText(Color.FromHex("#8B4513")).Build();

    internal static Theme Neon() => Theme.CreateFrom(Theme.Dark)
        .WithBackground(Colors.Black).WithAxesBackground(Color.FromHex("#0A0A0A"))
        .WithForegroundText(Color.FromHex("#39FF14")).Build();
}
