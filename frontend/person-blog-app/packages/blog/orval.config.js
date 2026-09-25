// orval.config.js
export const profile = {
    input: {
        target: './swagger.json',
    },
    output: {
        target: './src/shared/api/generated',
        schemas: './src/shared/api/generated/models',
        client: 'axios',
        mode: 'tags-split',
        override: {
            mutator: {
                path: './src/shared/api/mutator.ts',
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

export const authGateway = {
    input: {
        target: '../auth/swagger.json',
    },
    output: {
        target: './src/shared/api/generated/auth-gateway',
        schemas: './src/shared/api/generated/auth-gateway/models',
        client: 'axios',
        mode: 'tags-split',
        override: {
            requestOptions: true,
            mutator: {
                path: './src/shared/api/authGatewayMutator.ts',
                name: 'authGatewayInstance',
            },
        },
        prettier: true,
        indexFiles: true,
        mock: false,
    },
};
