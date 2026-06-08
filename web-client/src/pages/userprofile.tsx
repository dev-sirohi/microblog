import React from 'react';
import { useParams } from 'react-router-dom';
import type { Post, UserProfile } from '../interfaces/GlobalInterfaceExport';
import Layout from '../components/Layout';
import PostCard from '../components/PostCard';

export default function UserProfile() {
    const { id } = useParams<{ id: string }>();
    const [posts] = React.useState<Post[]>([]);
    const [loading] = React.useState(false);

    // Profile data would be fetched via UserProfileApi once the endpoint is available
    const profile: Partial<UserProfile> = { username: `User ${id}` };

    return (
        <Layout>
            <div
                className="rounded-xl p-6 mb-6 border"
                style={{ backgroundColor: 'var(--color-surface)', borderColor: 'var(--color-border)' }}
            >
                <div className="flex items-center gap-4">
                    <div
                        className="w-16 h-16 rounded-full flex items-center justify-center text-2xl font-bold"
                        style={{ backgroundColor: 'var(--color-primary)', color: '#fff' }}
                    >
                        {profile.username?.[0]?.toUpperCase()}
                    </div>
                    <div>
                        <h1 className="text-xl font-bold" style={{ color: 'var(--color-text)' }}>
                            {profile.username}
                        </h1>
                        <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
                            {profile.bio ?? 'No bio yet'}
                        </p>
                    </div>
                </div>
            </div>

            <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--color-muted)' }}>Posts</h2>

            {loading && (
                <p className="text-center py-8" style={{ color: 'var(--color-muted)' }}>Loading…</p>
            )}

            {!loading && posts.length === 0 && (
                <p className="text-center py-8" style={{ color: 'var(--color-muted)' }}>No posts yet</p>
            )}

            {posts.map(post => (
                <PostCard key={post.Id} post={post} />
            ))}
        </Layout>
    );
}
