// orval.config.js
export const profile = {
    input: {
        target: 'http://localhost:5078/swagger/v1/swagger.json',
    },
    output: {
        target: './src/lib/api/generated',
        schemas: './src/lib/api/generated/models',
        client: 'axios',
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
        prettier: true,
        indexFiles: true,
        mock: false,
    },
};