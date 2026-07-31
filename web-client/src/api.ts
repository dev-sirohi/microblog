import axios, { AxiosError } from 'axios';
import type { ApiResponse, CurrentUser, Post, PostLikes, Profile } from './types';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7282';

const http = axios.create({ baseURL, withCredentials: true });

async function call<T>(fn: () => Promise<{ data: ApiResponse<T> }>): Promise<T> {
    try {
        const res = await fn();
        return res.data.Data as T;
    } catch (err) {
        const axiosErr = err as AxiosError<{ Message?: string }>;
        if (axiosErr.response?.status === 429) {
            throw new Error('Rate limit exceeded. Please slow down and try again.');
        }
        throw new Error(axiosErr.response?.data?.Message ?? 'Request failed');
    }
}

const Api = {
    register: (Username: string, Email: string, Password: string) =>
        call<CurrentUser>(() => http.post('/api/auth/register', { Username, Email, Password })),

    login: (Username: string, Password: string) =>
        call<CurrentUser>(() => http.post('/api/auth/login', { Username, Email: '', Password })),

    logout: () => call<void>(() => http.post('/api/auth/logout')),

    me: () => call<CurrentUser>(() => http.get('/api/users/me')),

    feed: () => call<Post[]>(() => http.get('/api/post/homefeed')),

    createPost: (Content: string) => call<Post>(() => http.post('/api/post', { Content })),

    getPost: (id: number) => call<Post>(() => http.get(`/api/post/${id}`)),

    getLikes: (postId: number) => call<PostLikes>(() => http.get(`/api/userlike/${postId}`)),

    like: (postId: number) => call<void>(() => http.post(`/api/userlike/like/${postId}`)),

    unlike: (postId: number) => call<void>(() => http.post(`/api/userlike/unlike/${postId}`)),

    profile: (id: number) => call<Profile>(() => http.get(`/api/users/${id}`)),

    follow: (id: number) => call<void>(() => http.post(`/api/userfollow/follow/${id}`)),

    unfollow: (id: number) => call<void>(() => http.post(`/api/userfollow/unfollow/${id}`)),

    uploadAvatar: (file: File) => {
        const form = new FormData();
        form.append('file', file);
        return call<{ AvatarUrl: string }>(() => http.post('/api/users/me/avatar', form));
    },
};

export default Api;
