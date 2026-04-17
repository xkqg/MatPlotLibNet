# Themes

## 26 built-in themes

```csharp
// Use any theme with .WithTheme()
Plt.Create()
    .WithTheme(Theme.Dracula)
    .Plot(x, y, s => s.Label = "Data")
    .WithLegend()
    .Save("dracula.svg");
```

### Light themes

| Theme | Usage | Style |
|---|---|---|
| `Theme.Default` | General purpose | White bg, tab10 colors |
| `Theme.Seaborn` | Statistical | Light gray bg, subtle grid |
| `Theme.Ggplot` | R ggplot2 style | Gray bg, white grid |
| `Theme.FiveThirtyEight` | Journalism | Bold, no spines |
| `Theme.Bmh` | Academic | Teal/salmon cycle |
| `Theme.Solarize` | Solarized palette | Cream bg |
| `Theme.Grayscale` | Print-friendly | No color |
| `Theme.Paper` | Academic papers | Minimal, thin lines |
| `Theme.Presentation` | Slides | Large fonts |
| `Theme.Poster` | Conference posters | Extra-large fonts |
| `Theme.GitHub` | GitHub style | Clean white, blue accents |
| `Theme.Minimal` | Tufte-inspired | Maximum data-ink ratio |
| `Theme.Retro` | Vintage | Warm yellows/browns |
| `Theme.MatplotlibClassic` | Pre-2.0 matplotlib | bgrcmyk cycle |
| `Theme.MatplotlibV2` | Modern matplotlib | tab10, soft-black |

### Dark themes

| Theme | Usage | Style |
|---|---|---|
| `Theme.Dark` | General dark mode | Dark bg, bright colors |
| `Theme.Cyberpunk` | Neon on dark | Green neon on purple/black |
| `Theme.Nord` | Nord palette | Arctic blues/greens |
| `Theme.Dracula` | Dracula palette | Purple/pink/cyan |
| `Theme.Monokai` | Editor classic | Monokai colors |
| `Theme.Catppuccin` | Warm pastels | Pastel on dark |
| `Theme.Gruvbox` | Retro warm | Gruvbox tones |
| `Theme.OneDark` | Atom editor | Subtle muted |
| `Theme.Neon` | Bright neon | Neon green on black |

### Accessibility themes

| Theme | Usage |
|---|---|
| `Theme.ColorBlindSafe` | Okabe-Ito palette |
| `Theme.HighContrast` | WCAG AAA |

## Theme comparison

```csharp
// Same data, different themes
string[] themes = ["Default", "Dark", "Nord", "Dracula", "Cyberpunk"];
foreach (var name in themes)
{
    var theme = typeof(Theme).GetProperty(name)!.GetValue(null) as Theme;
    Plt.Create()
        .WithTheme(theme!)
        .WithTitle(name)
        .Plot(x, y)
        .Save($"theme_{name}.svg");
}
```
