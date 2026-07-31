import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import Api from '../api';
import type { Post } from '../types';

export default function PostDetail() {
    const { id } = useParams<{ id: string }>();
    const postId = Number(id);

    const [post, setPost] = useState<Post | null>(null);
    const [likes, setLikes] = useState(0);
    const [liked, setLiked] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        (async () => {
            try {
                setPost(await Api.getPost(postId));
                const l = await Api.getLikes(postId);
                setLikes(l?.LikesCount ?? 0);
                setLiked(l?.IsLikedByUser ?? false);
            } catch (ex) {
                setError((ex as Error).message);
            }
        })();
    }, [postId]);

    const toggleLike = async () => {
        setError('');
        try {
            if (liked) await Api.unlike(postId);
            else await Api.like(postId);
            const l = await Api.getLikes(postId);
            setLikes(l?.LikesCount ?? 0);
            setLiked(l?.IsLikedByUser ?? false);
        } catch (ex) {
            setError((ex as Error).message);
        }
    };

    return (
        <main>
            <p className="muted">
                <Link to="/">Back to feed</Link>
            </p>
            <h1>Post</h1>
            {error && <p className="error">{error}</p>}
            {post && (
                <div className="card">
                    <p>{post.Content}</p>
                    <p className="muted">
                        <Link to={`/profile/${post.UserId}`}>User {post.UserId}</Link>
                    </p>
                    <button onClick={toggleLike}>
                        {liked ? 'Unlike' : 'Like'} ({likes})
                    </button>
                </div>
            )}
        </main>
    );
}
