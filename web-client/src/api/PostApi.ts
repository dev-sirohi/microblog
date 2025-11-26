import API from './ApiUtils';
import type {
    ApiResponse,
    Post,
    UserProfile
} from '../interfaces/GlobalInterfaceExport';
import type { GetPostRequest } from '../interfaces/GetPostsRequest';

const POST_ENDPOINT = 'api/post/';

const PostApi = {
    async getHomeFeed(payload?: GetPostRequest): Promise<ApiResponse<Post[]>> {
        const url = `${POST_ENDPOINT}gethomefeed`;
        const response = await API.get<Post[]>(url, payload || {});
        return response;
    },
}

export default PostApi;