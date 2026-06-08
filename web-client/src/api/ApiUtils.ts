import axios, { type AxiosRequestConfig } from 'axios';
import type { ApiResponse } from '../interfaces/GlobalInterfaceExport';

function _urlBuilder(baseUrl: string, queryParams?: Record<string, any>): string {
    const url = new URL(baseUrl, import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5182/');

    if (queryParams) {
        Object.entries(queryParams).forEach(([key, val]) => {
            if (val !== undefined && val !== null) url.searchParams.append(key, String(val));
        });
    }

    return url.toString();
}

async function _handle<T>(response: { data: ApiResponse<T> }): Promise<ApiResponse<T>> {
    if (response.data.StatusCode === 401) {
        // Redirect to login on auth failure
        window.location.href = '/login';
    }
    return response.data;
}

const API = {
    async post<T>(endpoint: string, params?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        const url = endpoint.startsWith('http') ? endpoint : _urlBuilder(endpoint);
        const res = await axios.post<ApiResponse<T>>(url, params, { ...config, withCredentials: true });
        return _handle(res);
    },

    async get<T>(endpoint: string, params?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        const url = endpoint.startsWith('http') ? endpoint : _urlBuilder(endpoint, params);
        const res = await axios.get<ApiResponse<T>>(url, { ...config, withCredentials: true });
        return _handle(res);
    },

    async patch<T>(endpoint: string, params?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        const url = endpoint.startsWith('http') ? endpoint : _urlBuilder(endpoint);
        const res = await axios.patch<ApiResponse<T>>(url, params, { ...config, withCredentials: true });
        return _handle(res);
    },

    async delete<T>(endpoint: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        const url = endpoint.startsWith('http') ? endpoint : _urlBuilder(endpoint);
        const res = await axios.delete<ApiResponse<T>>(url, { ...config, withCredentials: true });
        return _handle(res);
    },
};

export default API;
