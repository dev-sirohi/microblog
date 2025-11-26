export interface UserProfile {
    id: number,
    username: string,
    email?: string,
    bio: string,
    avatarUrl?: string,
    createdAt: Date,
    updatedAt?: Date,
}