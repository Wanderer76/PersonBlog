// openapi-ts.config.ts
import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: './src/swagger/api.json', // или URL, например 'http://localhost:7892/swagger/v1/swagger.json'
  output: {
    path: './src/api/generated',
  },
  // ✅ Важно: отключаем встроенные клиенты, если не хотим дублирования
  client: {
    // мы будем подключать КАСТОМНЫЙ axios-инстанс вручную
    // `axios` как client можно использовать, но лучше — `custom` или явная настройка
  },
  plugins: [
    '@hey-api/typescript',
    '@hey-api/sdk'
  ],
});