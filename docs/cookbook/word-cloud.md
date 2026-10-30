---
title: "Word clouds in C#"
description: "Draw a word cloud in C#: words sized by weight and packed without overlap, with every word's weight in its title and a table beside the chart."
---

# Word clouds

> **Install:** `dotnet add package MatPlotLibNet`

`WordCloud(words, weights)` sets each word in a size that follows the square root of its weight, between two sizes (12 and 60 unless
`Sizes` says otherwise). The heaviest word stands at the centre and the rest on a spiral, each at the first place where it fits with
two pixels clear.

```csharp
using MatPlotLibNet;

string[] words = ["price", "support", "speed", "design", "docs", "bugs", "export", "themes", "charts", "install", "license", "maps"];
double[] weights = [42, 35, 30, 24, 22, 18, 16, 14, 13, 9, 7, 5];

Plt.Create()
    .WithTitle("What the survey answers talk about")
    .WithSize(620, 420)
    .TightLayout()
    .AddSubPlot(1, 1, 1, ax => ax.WordCloud(words, weights))
    .Save("survey_words.svg");
```

![Twelve words in different colours and sizes with no overlap, price and support the largest in the middle](../images/survey_words.png)

## What it does when the words do not fit

Nothing is dropped silently. When the words do not all fit in the plot, every size shrinks toward the smallest together, and a word
that still has no room is announced on `ChartDiagnostics`.

## Reading it

A word is moved toward the text colour until it reads against the background (4.5 to 1, 3 to 1 for large words). Each word carries its
weight as a title, so hovering "price" says "price: 42", and the table beside the chart has a row for each word, heaviest first, with
its weight and the size the weight gives it. The SVG asks the browser to set each word to the width it was measured at, so a
different font cannot make one run into its neighbour.

A weight that is not above zero, an empty word and a repeated word are refused when the call is made.
