import React from 'react';
import { Link } from 'react-router-dom';
import type { Post } from '../interfaces/GlobalInterfaceExport';
import UserLikeApi from '../api/UserLikeApi';

interface PostCardProps {
    post: Post;
    showActions?: boolean;
}

export default function PostCard({ post, showActions = true }: PostCardProps) {
    const [liked, setLiked] = React.useState(false);
    const [likeCount, setLikeCount] = React.useState<number>(0);
    const [likeLoading, setLikeLoading] = React.useState(false);

    const handleLike = async () => {
        if (likeLoading) return;
        setLikeLoading(true);

        // Optimistic update
        const wasLiked = liked;
        setLiked(!wasLiked);
        setLikeCount(prev => wasLiked ? Math.max(0, prev - 1) : prev + 1);

        try {
            if (wasLiked) {
                await UserLikeApi.unlikePost(post.Id);
            } else {
                await UserLikeApi.likePost(post.Id);
            }
        } catch {
            // Revert on error
            setLiked(wasLiked);
            setLikeCount(prev => wasLiked ? prev + 1 : Math.max(0, prev - 1));
        } finally {
            setLikeLoading(false);
        }
    };

    const timeAgo = (dateStr: string) => {
        const diff = Date.now() - new Date(dateStr).getTime();
        const mins = Math.floor(diff / 60000);
        if (mins < 1) return 'just now';
        if (mins < 60) return `${mins}m ago`;
        const hrs = Math.floor(mins / 60);
        if (hrs < 24) return `${hrs}h ago`;
        return `${Math.floor(hrs / 24)}d ago`;
    };

    return (
        <article
            className="rounded-xl p-4 mb-4 border transition-shadow hover:shadow-md"
            style={{
                backgroundColor: 'var(--color-surface)',
                borderColor: 'var(--color-border)',
            }}
        >
            <div className="flex items-center justify-between mb-3">
                <Link
                    to={`/profile/${post.UserId}`}
                    className="font-semibold text-sm hover:underline"
                    style={{ color: 'var(--color-primary)' }}
                >
                    User {post.UserId}
                </Link>
                <span className="text-xs" style={{ color: 'var(--color-muted)' }}>
                    {timeAgo(post.CreatedAt)}
                </span>
            </div>

            <Link to={`/post/${post.Id}`}>
                <p className="text-sm leading-relaxed mb-3" style={{ color: 'var(--color-text)' }}>
                    {post.Content}
                </p>
            </Link>

            {post.Tags && post.Tags.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mb-3">
                    {post.Tags.map((tag, i) => (
                        <span
                            key={i}
                            className="text-xs px-2 py-0.5 rounded-full"
                            style={{
                                backgroundColor: 'var(--color-border)',
                                color: 'var(--color-muted)',
                            }}
                        >
                            #{tag}
                        </span>
                    ))}
                </div>
            )}

            {showActions && (
                <div className="flex items-center gap-4 pt-2 border-t" style={{ borderColor: 'var(--color-border)' }}>
                    <button
                        onClick={handleLike}
                        disabled={likeLoading}
                        className="flex items-center gap-1.5 text-sm transition-colors disabled:opacity-50"
                        style={{ color: liked ? 'var(--color-danger)' : 'var(--color-muted)' }}
                    >
                        <span>{liked ? '♥' : '♡'}</span>
                        <span>{likeCount}</span>
                    </button>

                    <Link
                        to={`/post/${post.Id}`}
                        className="text-sm hover:opacity-70 transition-opacity"
                        style={{ color: 'var(--color-muted)' }}
                    >
                        💬 Comment
                    </Link>
                </div>
            )}
        </article>
    );
}
