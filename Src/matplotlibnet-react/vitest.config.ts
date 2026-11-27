import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    // The client package from its folder, as the type checker reads it (tsconfig paths); SignalR from this package's own
    // node_modules, so a mock of either reaches the client's code too.
    alias: { '@matplotlibnet/client': fileURLToPath(new URL('../matplotlibnet-client/src/index.ts', import.meta.url)) },
    dedupe: ['@microsoft/signalr'],
  },
  test: {
    environment: 'jsdom',
    globals: true,
  },
});
