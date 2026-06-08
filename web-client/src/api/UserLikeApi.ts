import API from './ApiUtils';
import type { ApiResponse } from '../interfaces/GlobalInterfaceExport';

const LIKE_ENDPOINT = 'api/userlike/';

const UserLikeApi = {
    async likePost(postId: number): Promise<ApiResponse<void>> {
        return API.post<void>(`${LIKE_ENDPOINT}like/${postId}`);
    },
    async unlikePost(postId: number): Promise<ApiResponse<void>> {
        return API.post<void>(`${LIKE_ENDPOINT}unlike/${postId}`);
    },
};

export default UserLikeApi;
