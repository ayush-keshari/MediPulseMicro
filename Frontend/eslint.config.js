import js from '@eslint/js';
import globals from 'globals';
import typescriptEsLint from '@typescript-eslint/eslint-plugin';
import typescriptParser from '@typescript-eslint/parser';
import eslintPluginRxjs from 'eslint-plugin-rxjs';

const browserGlobals = Object.fromEntries(
  Object.entries(globals.browser).map(([name, value]) => [name.trim(), value]),
);
const vitestGlobals = {
  afterAll: 'readonly',
  afterEach: 'readonly',
  beforeAll: 'readonly',
  beforeEach: 'readonly',
  describe: 'readonly',
  expect: 'readonly',
  jasmine: 'readonly',
  it: 'readonly',
  spyOn: 'readonly',
  test: 'readonly',
  vi: 'readonly',
};

export default [
  { ignores: ['node_modules/**', 'dist/**', '**/*.html'] },
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        project: ['./tsconfig.app.json', './tsconfig.spec.json'],
        createDefaultProgram: true,
      },
      globals: {
        ...browserGlobals,
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
      // Custom rules can be added here
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': 'off',
    },
  },
  {
    files: ['**/*.spec.ts'],
    languageOptions: {
      globals: {
        ...browserGlobals,
        ...globals.node,
        ...vitestGlobals,
      },
    },
  },
  {
    files: ['**/*.html'],
    rules: {},
  },
];
