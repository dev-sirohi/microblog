export interface Post {
    Id: number;
    UserId: number;
    Content: string;
    Tags?: string[];
    CreatedAt: string;
    MediaUrl?: string;
}
