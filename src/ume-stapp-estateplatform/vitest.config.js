// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/vitest.config.js
// Plugins
import vue from '@vitejs/plugin-vue';

import { defineConfig } from 'vite';
import { fileURLToPath, URL } from 'node:url';

// https://vitejs.dev/config/
export default defineConfig({
	plugins: [vue()],
	envPrefix: 'VUE_APP_',
	test: {
		environment: 'happy-dom',
		setupFiles: './vitest.setup.js',
	},
	resolve: {
		alias: {
			'@': fileURLToPath(new URL('./src', import.meta.url)),
		},
		extensions: ['.js', '.json', '.jsx', '.mjs', '.ts', '.tsx', '.vue'],
	},
});
