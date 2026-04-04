// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * HTTP service for fetching chart data from MatPlotLibNet.AspNetCore endpoints.
 * Use MapChartEndpoint (JSON) and MapChartSvgEndpoint (SVG) on the server side.
 */
@Injectable({ providedIn: 'root' })
export class ChartService {
  constructor(private http: HttpClient) {}

  /** Fetches the chart SVG from the specified endpoint URL. */
  getSvg(url: string): Observable<string> {
    return this.http.get(url, { responseType: 'text' });
  }

  /** Fetches the chart JSON spec from the specified endpoint URL. */
  getJson(url: string): Observable<object> {
    return this.http.get(url);
  }
}
