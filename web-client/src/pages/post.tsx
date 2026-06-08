import React from 'react';
import { useParams, Link } from 'react-router-dom';
import type { Post } from '../interfaces/GlobalInterfaceExport';
import PostApi from '../api/PostApi';
import Layout from '../components/Layout';
import PostCard from '../components/PostCard';

export default function PostDetail() {
    const { id } = useParams<{ id: string }>();
    const [post, setPost] = React.useState<Post | null>(null);
    const [recommendations, setRecommendations] = React.useState<Post[]>([]);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState<string | null>(null);

    React.useEffect(() => {
        if (!id) return;
        (async () => {
            setLoading(true);
            try {
                const [postRes, recRes] = await Promise.allSettled([
                    PostApi.getPostById(Number(id)),
                    PostApi.getRecommendations(Number(id)),
                ]);
                if (postRes.status === 'fulfilled') setPost(postRes.value.Data ?? null);
                if (recRes.status === 'fulfilled') setRecommendations(recRes.value.Data ?? []);
            } catch (ex: any) {
                setError(ex.message ?? 'Failed to load post');
            } finally {
                setLoading(false);
            }
        })();
    }, [id]);

    if (loading) return (
        <Layout>
            <p className="text-center py-12" style={{ color: 'var(--color-muted)' }}>Loading…</p>
        </Layout>
    );

    if (error || !post) return (
        <Layout>
            <p className="text-center py-12" style={{ color: 'var(--color-danger)' }}>
                {error ?? 'Post not found'}
            </p>
        </Layout>
    );

    return (
        <Layout>
            <Link to="/" className="text-sm mb-4 inline-block hover:underline" style={{ color: 'var(--color-muted)' }}>
                ← Back to feed
            </Link>

            <PostCard post={post} showActions />

            {recommendations.length > 0 && (
                <section className="mt-8">
                    <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--color-muted)' }}>
                        Similar posts
                    </h2>
                    {recommendations.map(rec => (
                        <PostCard key={rec.Id} post={rec} />
                    ))}
                </section>
            )}
        </Layout>
    );
}
