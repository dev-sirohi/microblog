import API from './ApiUtils';
import type {
    ApiResponse,
    LoginRequest,
    RegisterRequest,
    UserProfile
} from '../interfaces/GlobalInterfaceExport';

const AUTH_ENDPOINT = 'api/auth/';

const AuthApi = {
    async registerUser(payload: RegisterRequest): Promise<ApiResponse<UserProfile>> {
        const url = `${AUTH_ENDPOINT}register`;
        const response = await API.post<UserProfile>(url, payload);
        return response;
    },
    async loginUser(payload: LoginRequest): Promise<ApiResponse<UserProfile>> {
        const url = `${AUTH_ENDPOINT}login`;
        const response = await API.post<UserProfile>(url, payload);
        return response;
    },
    async logoutUser(): Promise<void> {
        const url = `${AUTH_ENDPOINT}logout`;
        await API.post<void>(url);
    },
    async refreshToken(): Promise<ApiResponse<UserProfile>> {
        const url = `${AUTH_ENDPOINT}refreshtoken`;
        const response = await API.post<UserProfile>(url);
        return response;
    }
}

export default AuthApi;