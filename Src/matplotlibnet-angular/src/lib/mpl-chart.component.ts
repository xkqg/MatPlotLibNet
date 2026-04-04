// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild } from '@angular/core';
import { ChartService } from './chart.service';

/**
 * Static chart component that fetches SVG from a MatPlotLibNet endpoint and renders it inline.
 * Mirrors MplChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * <mpl-chart [chartUrl]="'/api/chart.svg'" [cssClass]="'my-chart'"></mpl-chart>
 */
@Component({
  selector: 'mpl-chart',
  template: `
    <div class="mpl-chart" [ngClass]="cssClass" #container>
      <div [innerHTML]="svgContent"></div>
    </div>
  `,
  standalone: true,
  imports: []
})
export class MplChartComponent implements OnChanges {
  @Input() chartUrl: string = '';
  @Input() cssClass: string = '';

  svgContent: string = '';

  @ViewChild('container') container!: ElementRef;

  constructor(private chartService: ChartService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['chartUrl'] && this.chartUrl) {
      this.chartService.getSvg(this.chartUrl).subscribe({
        next: svg => this.svgContent = svg,
        error: err => console.error('Failed to load chart:', err)
      });
    }
  }
}
