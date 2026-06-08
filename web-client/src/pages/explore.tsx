import React from 'react';
import type { Post } from '../interfaces/GlobalInterfaceExport';
import PostApi from '../api/PostApi';
import Layout from '../components/Layout';
import PostCard from '../components/PostCard';

export default function Explore() {
    const [posts, setPosts] = React.useState<Post[]>([]);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState<string | null>(null);

    React.useEffect(() => {
        (async () => {
            try {
                const res = await PostApi.getHomeFeed(1, 20);
                setPosts(res.Data ?? []);
            } catch (ex: any) {
                setError(ex.message ?? 'Failed to load trending posts');
            } finally {
                setLoading(false);
            }
        })();
    }, []);

    return (
        <Layout>
            <h1 className="text-lg font-bold mb-6" style={{ color: 'var(--color-text)' }}>
                Explore
            </h1>

            {error && (
                <p className="text-sm mb-4 text-center" style={{ color: 'var(--color-danger)' }}>
                    {error}
                </p>
            )}

            {loading && (
                <p className="text-center py-12" style={{ color: 'var(--color-muted)' }}>
                    Loading trending posts…
                </p>
            )}

            {!loading && posts.length === 0 && !error && (
                <p className="text-center py-12" style={{ color: 'var(--color-muted)' }}>
                    Nothing trending right now
                </p>
            )}

            {posts.map(post => (
                <PostCard key={post.Id} post={post} />
            ))}
        </Layout>
    );
}
