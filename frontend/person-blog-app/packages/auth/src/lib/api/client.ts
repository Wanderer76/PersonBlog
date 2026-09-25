// src/lib/api/client.ts
import axios from 'axios';

export const BaseApUrl = import.meta.env.VITE_API_BASE_URL;

export const API = axios.create({
  baseURL: BaseApUrl,
  withCredentials: false,
});

API.interceptors.request.use(config => {
  return config;
});

// Экспортируем для orval
export default API;
