import type { Comment } from "./Comment";

export interface Post {
    id: number,
    userId: number,
    content: string,
    tags?: string[],
    createdAt: Date,
    medialUrl?: string,
    topComments?: Comment[],
}