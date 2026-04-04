// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { ChartSubscriptionClient } from './chart-subscription.client';

/**
 * Live chart component that receives real-time updates via SignalR.
 * Mirrors MplLiveChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * <mpl-live-chart [chartId]="'sensor-1'" [hubUrl]="'/charts-hub'" [cssClass]="'live'"></mpl-live-chart>
 */
@Component({
  selector: 'mpl-live-chart',
  template: `
    <div class="mpl-chart mpl-live" [ngClass]="cssClass">
      <div [innerHTML]="svgContent"></div>
    </div>
  `,
  standalone: true,
  imports: []
})
export class MplLiveChartComponent implements OnInit, OnDestroy {
  @Input() chartId: string = '';
  @Input() hubUrl: string = '/charts-hub';
  @Input() cssClass: string = '';
  @Input() initialSvg: string = '';

  svgContent: string = '';

  private client = new ChartSubscriptionClient();

  async ngOnInit(): Promise<void> {
    this.svgContent = this.initialSvg;

    this.client.onSvgUpdated((id, svg) => {
      if (id === this.chartId) {
        this.svgContent = svg;
      }
    });

    await this.client.connect(this.hubUrl);
    await this.client.subscribe(this.chartId);
  }

  async ngOnDestroy(): Promise<void> {
    await this.client.unsubscribe(this.chartId);
    await this.client.dispose();
  }
}
