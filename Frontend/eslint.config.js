import js from '@eslint/js';
import globals from 'globals';
import typescriptEsLint from '@typescript-eslint/eslint-plugin';
import typescriptParser from '@typescript-eslint/parser';
import eslintPluginRxjs from 'eslint-plugin-rxjs';

export default [
  { ignores: ['node_modules/**', 'dist/**'] },
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        project: ['./tsconfig.json', './e2e/tsconfig.json'],
        createDefaultProgram: true,
      },
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
    plugins: {
      '@typescript-eslint': typescriptEsLint,
      rxjs: eslintPluginRxjs,
    },
    rules: {
      ...js.configs.recommended.rules,
      ...typescriptEsLint.configs['recommended'].rules,
      ...typescriptEsLint.configs['recommended-requiring-type-checking'].rules,
      ...eslintPluginRxjs.configs['recommended'].rules,
      // Custom rules can be added here
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
    },
  },
  {
    files: ['**/*.html'],
    rules: {},
  },
];