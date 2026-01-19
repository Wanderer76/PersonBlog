// // src/api/http-client.ts
// import API from '../scripts/apiMethod'; // ваш существующий axios-инстанс

// export const httpClient = {
//   request: async (options) => {
//     const { url, method, headers, query, body, mediaType } = options;

//     try {
//       const response = await API.request({
//         url,
//         method: method.toUpperCase(),
//         headers,
//         params: query,
//         data: body,
//         ...(mediaType && { headers: { ...headers, 'Content-Type': mediaType } }),
//       });

//       return {
//         status: response.status,
//         headers: response.headers,
//         data: response.data,
//       };
//     } catch (error) {
//       // openapi-typescript-codegen ожидает выброс исключения в случае ошибки
//       // AxiosError уже содержит response, request и т.д.
//       throw error;
//     }
//   },
// };