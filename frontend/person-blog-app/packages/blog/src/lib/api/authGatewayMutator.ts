import axios, { type AxiosRequestConfig, type AxiosResponse } from 'axios';

const authGatewayTransport = axios.create({
  baseURL: import.meta.env.VITE_AUTH_API_URL || 'http://localhost:5078',
  withCredentials: false,
  timeout: 30_000,
});

// Auth endpoints, especially refresh, must not use the application's 401
// interceptor: otherwise a failed refresh recursively starts another refresh.
export const authGatewayInstance = async <T>(
  config: AxiosRequestConfig,
): Promise<AxiosResponse<T>> => authGatewayTransport.request<T>(config);

export default authGatewayInstance;
