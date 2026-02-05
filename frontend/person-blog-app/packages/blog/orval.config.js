// orval.config.js
module.exports = {
    profile: {
        input: {
            target: 'http://localhost:7892/video/swagger/v1/swagger.json',
        },
        output: {
            target: './src/lib/api/generated',
            schemas: './src/lib/api/generated/models', // Отдельная папка для моделей
            client: 'axios', // Было 'axios' — конфликт с useQuery/useMutation
            mode: 'tags-split',
            override: {
                mutator: {
                    path: './src/lib/api/mutator.ts',
                    name: 'customInstance',
                },
                query: {
                    useQuery: true,
                    useMutation: true,
                },
                tags: {
                    name: (tag) => {
                        return `${tag}Client`;
                    },
                }
            },
            // Ключевые настройки для правильных импортов
            prettier: true,
            indexFiles: true, // Создаёт index.ts файлы
            mock: false,
        },
    },
};