import API from './ApiUtils';
import type { ApiResponse, Post } from '../interfaces/GlobalInterfaceExport';

const POST_ENDPOINT = 'api/post/';

const PostApi = {
    async getHomeFeed(page = 1, pageSize = 10): Promise<ApiResponse<Post[]>> {
        return API.get<Post[]>(`${POST_ENDPOINT}homefeed`, { page, pageSize });
    },
    async getPostById(id: number): Promise<ApiResponse<Post>> {
        return API.get<Post>(`${POST_ENDPOINT}${id}`);
    },
    async createPost(content: string): Promise<ApiResponse<Post>> {
        return API.post<Post>(POST_ENDPOINT, { Content: content });
    },
    async updatePost(id: number, content: string): Promise<ApiResponse<Post>> {
        return API.patch<Post>(`${POST_ENDPOINT}${id}`, { Content: content });
    },
    async deletePost(id: number): Promise<ApiResponse<void>> {
        return API.delete<void>(`${POST_ENDPOINT}${id}`);
    },
    async getRecommendations(id: number, limit = 5): Promise<ApiResponse<Post[]>> {
        return API.get<Post[]>(`${POST_ENDPOINT}${id}/recommendations`, { limit });
    },
};

export default PostApi;
