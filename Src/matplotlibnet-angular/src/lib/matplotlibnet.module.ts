// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { MplChartComponent } from './mpl-chart.component';
import { MplLiveChartComponent } from './mpl-live-chart.component';

/**
 * Angular module that provides MatPlotLibNet charting components and services.
 *
 * Usage in your AppModule:
 * ```
 * import { MatPlotLibNetModule } from '@matplotlibnet/angular';
 *
 * @NgModule({ imports: [MatPlotLibNetModule] })
 * export class AppModule {}
 * ```
 */
@NgModule({
  imports: [
    HttpClientModule,
    MplChartComponent,
    MplLiveChartComponent
  ],
  exports: [
    MplChartComponent,
    MplLiveChartComponent
  ]
})
export class MatPlotLibNetModule {}
