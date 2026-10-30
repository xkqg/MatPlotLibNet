// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { Component, ElementRef, Input, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ChartSubscriptionClient } from './chart-subscription.client';
import { mountSvg, type MplHub } from './mount-svg';

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
      <div #host></div>
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

  @ViewChild('host', { static: true }) host!: ElementRef<HTMLElement>;

  private client = new ChartSubscriptionClient();

  async ngOnInit(): Promise<void> {
    this.show(this.initialSvg);

    this.client.onSvgUpdated((id, svg) => {
      if (id === this.chartId) {
        this.show(svg);
      }
    });

    await this.client.connect(this.hubUrl);
    // The chart is mounted again with the connection now there: its script looks for it when it starts.
    this.show(this.svgContent);
    await this.client.subscribe(this.chartId);
  }

  /** Mounts the markup so that the script inside it runs (a script that arrives through innerHTML never does). */
  private show(svg: string): void {
    this.svgContent = svg;
    mountSvg(this.host.nativeElement, svg, (this.client.hub as MplHub | null) ?? undefined);
  }

  async ngOnDestroy(): Promise<void> {
    await this.client.unsubscribe(this.chartId);
    await this.client.dispose();
  }
}
