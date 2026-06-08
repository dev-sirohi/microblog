import React from 'react';
import type { Post } from '../interfaces/GlobalInterfaceExport';
import PostApi from '../api/PostApi';
import Layout from '../components/Layout';
import PostCard from '../components/PostCard';

export default function Home() {
    const [posts, setPosts] = React.useState<Post[]>([]);
    const [page, setPage] = React.useState(1);
    const [loading, setLoading] = React.useState(false);
    const [hasMore, setHasMore] = React.useState(true);
    const [error, setError] = React.useState<string | null>(null);
    const [newPostContent, setNewPostContent] = React.useState('');
    const [posting, setPosting] = React.useState(false);

    const loaderRef = React.useRef<HTMLDivElement | null>(null);

    const loadPosts = React.useCallback(async () => {
        if (loading || !hasMore) return;
        setLoading(true);
        try {
            const res = await PostApi.getHomeFeed(page, 10);
            const newPosts: Post[] = res.Data ?? [];
            if (newPosts.length < 10) setHasMore(false);
            setPosts(prev => [...prev, ...newPosts]);
        } catch (ex: any) {
            setError(ex.message ?? 'Failed to load posts');
            setHasMore(false);
        } finally {
            setLoading(false);
        }
    }, [page, loading, hasMore]);

    React.useEffect(() => {
        loadPosts();
    }, [page]);

    React.useEffect(() => {
        if (!loaderRef.current || !hasMore) return;
        const observer = new IntersectionObserver(([entry]) => {
            if (entry.isIntersecting && !loading) setPage(p => p + 1);
        }, { threshold: 1.0 });
        observer.observe(loaderRef.current);
        return () => observer.disconnect();
    }, [loading, hasMore]);

    const handleCreatePost = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newPostContent.trim()) return;
        setPosting(true);
        try {
            const res = await PostApi.createPost(newPostContent.trim());
            if (res.Data) {
                setPosts(prev => [res.Data!, ...prev]);
                setNewPostContent('');
            }
        } catch (ex: any) {
            setError(ex.message ?? 'Failed to create post');
        } finally {
            setPosting(false);
        }
    };

    return (
        <Layout>
            {/* Compose */}
            <form
                onSubmit={handleCreatePost}
                className="rounded-xl p-4 mb-6 border"
                style={{ backgroundColor: 'var(--color-surface)', borderColor: 'var(--color-border)' }}
            >
                <textarea
                    value={newPostContent}
                    onChange={e => setNewPostContent(e.target.value)}
                    placeholder="What's on your mind?"
                    rows={3}
                    className="w-full resize-none text-sm outline-none bg-transparent"
                    style={{ color: 'var(--color-text)' }}
                />
                <div className="flex justify-end mt-2">
                    <button
                        type="submit"
                        disabled={posting || !newPostContent.trim()}
                        className="px-4 py-2 text-sm font-semibold rounded-lg transition-colors disabled:opacity-50"
                        style={{
                            backgroundColor: 'var(--color-primary)',
                            color: '#fff',
                        }}
                    >
                        {posting ? 'Posting…' : 'Post'}
                    </button>
                </div>
            </form>

            {error && (
                <p className="text-sm mb-4 text-center" style={{ color: 'var(--color-danger)' }}>
                    {error}
                </p>
            )}

            {posts.map(post => (
                <PostCard key={post.Id} post={post} />
            ))}

            <div ref={loaderRef} className="h-10" />

            {loading && (
                <p className="text-center text-sm py-4" style={{ color: 'var(--color-muted)' }}>
                    Loading…
                </p>
            )}

            {!hasMore && posts.length > 0 && (
                <p className="text-center text-sm py-4" style={{ color: 'var(--color-muted)' }}>
                    You've reached the end
                </p>
            )}

            {!loading && posts.length === 0 && !error && (
                <p className="text-center text-sm py-8" style={{ color: 'var(--color-muted)' }}>
                    No posts yet. Be the first to post!
                </p>
            )}
        </Layout>
    );
}
