// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { defineComponent, h } from 'vue';
import { mount } from '@vue/test-utils';

const mockConnect = vi.fn().mockResolvedValue(undefined);
const mockSubscribe = vi.fn().mockResolvedValue(undefined);
const mockUnsubscribe = vi.fn().mockResolvedValue(undefined);
const mockDispose = vi.fn().mockResolvedValue(undefined);
const mockOnSvgUpdated = vi.fn();

vi.mock('../lib/chart-subscription.client', () => ({
  ChartSubscriptionClient: vi.fn().mockImplementation(() => ({
    connect: mockConnect,
    subscribe: mockSubscribe,
    unsubscribe: mockUnsubscribe,
    dispose: mockDispose,
    onSvgUpdated: mockOnSvgUpdated,
    onChartUpdated: vi.fn(),
    isConnected: true,
  })),
}));

import { useMplLiveChart } from '../lib/use-mpl-live-chart';

// Wrapper component to test the composable inside a mounted context
function createWrapper(chartId: string, hubUrl?: string) {
  const Comp = defineComponent({
    setup() {
      const result = useMplLiveChart(chartId, hubUrl);
      return { result };
    },
    render() {
      return h('div', this.result.svgContent.value);
    },
  });
  return mount(Comp);
}

describe('useMplLiveChart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('connects to the hub on mount', async () => {
    createWrapper('chart-1');
    await new Promise(r => setTimeout(r, 10));

    expect(mockConnect).toHaveBeenCalledWith('/charts-hub');
  });

  it('subscribes to the chartId after connecting', async () => {
    createWrapper('sensor-42', '/my-hub');
    await new Promise(r => setTimeout(r, 10));

    expect(mockConnect).toHaveBeenCalledWith('/my-hub');
    expect(mockSubscribe).toHaveBeenCalledWith('sensor-42');
  });

  it('registers an SVG update handler', async () => {
    createWrapper('chart-1');
    await new Promise(r => setTimeout(r, 10));

    expect(mockOnSvgUpdated).toHaveBeenCalledWith(expect.any(Function));
  });

  it('cleans up on unmount', async () => {
    const wrapper = createWrapper('chart-1');
    await new Promise(r => setTimeout(r, 10));

    wrapper.unmount();
    await new Promise(r => setTimeout(r, 10));

    expect(mockUnsubscribe).toHaveBeenCalledWith('chart-1');
    expect(mockDispose).toHaveBeenCalled();
  });
});
