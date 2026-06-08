import API from './ApiUtils';
import type {
    ApiResponse,
    GetCommentsRequest,
    AddCommentRequest,
    Comment
} from '../interfaces/GlobalInterfaceExport';

const COMMENT_ENDPOINT = 'api/comment/';

const AuthApi = {
    async getComments(payload: GetCommentsRequest): Promise<ApiResponse<Comment[]>> {
        const url = `${COMMENT_ENDPOINT}getcommentsbypostid`;
        const response = await API.post<Comment[]>(url, payload);
        return response;
    },
    async addComment(payload: AddCommentRequest): Promise<ApiResponse<Comment>> {
        const url = `${COMMENT_ENDPOINT}addcomment`;
        const response = await API.post<Comment>(url, payload);
        return response;
    },
}

export default AuthApi;