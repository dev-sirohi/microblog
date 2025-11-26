import axios, { type AxiosRequestConfig } from 'axios';
import type { ApiResponse, LoginRequest } from '../interfaces/GlobalInterfaceExport';
import { GlobalDialog } from '../globalDialogRef';
import AuthApi from './AuthApi';
import * as DialogPresets from '../utils/GlobalDialogProvider/DialogPresets';
import AppUtils from '../utils/AppUtils';
import { AppConstants } from '../utils/Enums';

/* Enums */
export const ApiStatus = {
    ACTIVE: "ACTIVE",
    SUSPENDED: "SUSPENDED",
} as const;
export type TApiStatus = typeof ApiStatus[keyof typeof ApiStatus];

/* Private methods */
let _apiStatus: TApiStatus = ApiStatus.ACTIVE;
let _queuedRequests: Array<() => Promise<void>> = [];

function _pauseApiRequests() {
    if (_apiStatus === ApiStatus.ACTIVE) {
        _apiStatus = ApiStatus.SUSPENDED;
    }
}

async function _resumeApiRequests() {
    if (_apiStatus !== ApiStatus.SUSPENDED) return;

    _apiStatus = ApiStatus.ACTIVE;

    const queue = [..._queuedRequests];
    _queuedRequests = [];

    await Promise.all(queue.map(cb => cb()));
}

function _urlBuilder(baseUrl: string, queryParams?: Record<string, any>): string {
    const url = new URL(baseUrl, import.meta.env.VITE_API_BASE_URL);

    if (queryParams) {
        Object.keys(queryParams).forEach(key => {
            const val = queryParams[key];
            if (val !== undefined && val !== null) url.searchParams.append(key, val);
        });
    }

    return url.toString();
}

async function _validateAuthorization<T>(response: { data: ApiResponse<T> }): Promise<ApiResponse<T>> {
    const data = response.data;

    if (!_resolveApiResponseSuccess(data)) {
        if (data.StatusCode === AppConstants.HttpStatusCode.UNAUTHORIZED) {
            _pauseApiRequests();

            await GlobalDialog.showDialog(
                DialogPresets.LOGIN(async (params) => {
                    let loginRequestObj: LoginRequest = {
                        username: (!AppUtils.isValidEmail(params.identifier) ? params.identifier : ''),
                        email: (AppUtils.isValidEmail(params.identifier) ? params.identifier : ''),
                        password: "", // TODO
                    };

                    let loggedIn = false;
                    try {
                        await AuthApi.loginUser(loginRequestObj);
                        loggedIn = true;
                    } catch { }

                    if (loggedIn) _resumeApiRequests();
                })
            );
        }
    }

    response.data.Data = _resolveResponseData(data);
    return response.data;
}

function _resolveResponseData<T>(response: ApiResponse<T>): T {
    return response.Data ?? {} as T;
}

function _resolveApiResponseSuccess(response: ApiResponse<any>): boolean {
    return response.Success === true && response.StatusCode === 200;
}

/* PUBLIC methods */
const API = {
    async post<T>(endpoint: string, params?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        if (_apiStatus === ApiStatus.SUSPENDED) {
            return new Promise<ApiResponse<T>>((resolve, reject) => {
                _queuedRequests.push(async () => {
                    try {
                        const result = await API.post<T>(endpoint, params, config);
                        resolve(result);
                    } catch (err) {
                        reject(err);
                    }
                });
            });
        }

        const url = endpoint.startsWith("http") ? endpoint : _urlBuilder(endpoint);
        const response = await axios.post<ApiResponse<T>>(url, params, {
            ...config,
            //withCredentials: true,
        });

        return await _validateAuthorization(response);
    },

    async get<T>(endpoint: string, params?: any, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
        if (_apiStatus === ApiStatus.SUSPENDED) {
            return new Promise<ApiResponse<T>>((resolve, reject) => {
                _queuedRequests.push(async () => {
                    try {
                        const result = await API.get<T>(endpoint, config);
                        resolve(result);
                    } catch (err) {
                        reject(err);
                    }
                });
            });
        }

        const url = endpoint.startsWith("http") ? endpoint : _urlBuilder(endpoint, params);
        const response = await axios.get<ApiResponse<T>>(url, {
            ...config,
            //withCredentials: true,
        });

        return await _validateAuthorization(response);
    }
};

export default API;
