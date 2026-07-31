export interface ApiResponse<T> {
    Success?: boolean;
    Data?: T;
    Message?: string;
    StatusCode?: number;
}

export interface Post {
    Id: number;
    UserId: number;
    Content: string;
    CreatedAt: string;
}

export interface CurrentUser {
    Id: number;
    Username: string;
    Bio?: string;
    AvatarUrl?: string;
}

export interface Profile {
    Id: number;
    Username: string;
    Bio?: string;
    AvatarUrl?: string;
    FollowersCount: number;
    FollowingCount: number;
    IsFollowing: boolean;
    Posts: Post[];
}

export interface PostLikes {
    LikesCount: number;
    IsLikedByUser: boolean;
}
