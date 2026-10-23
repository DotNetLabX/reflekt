import pluginVue from 'eslint-plugin-vue'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue}'],
  },
  {
    name: 'app/files-to-ignore',
    ignores: ['**/dist/**', '**/dist-ssr/**', '**/coverage/**'],
  },
  pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,
  skipFormatting,
  {
    name: 'app/no-non-null-assertion',
    rules: {
      '@typescript-eslint/no-non-null-assertion': 'error',
    },
  },
  {
    name: 'app/fetch-only-in-api',
    files: ['src/**/*.{ts,mts,tsx,vue}'],
    ignores: ['src/api/**'],
    rules: {
      'no-restricted-globals': [
        'error',
        { name: 'fetch', message: 'Call the API through src/api/ (apiFetch).' },
      ],
    },
  },
  {
    name: 'app/api-imports-no-store',
    files: ['src/api/**/*.{ts,mts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@/stores/*', '**/stores/*'],
              message: 'src/api/ never imports a store; hand it a getter at start-up instead.',
            },
          ],
        },
      ],
    },
  },
)
