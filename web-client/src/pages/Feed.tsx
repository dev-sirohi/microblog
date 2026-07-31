import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import Api from '../api';
import type { Post } from '../types';

export default function Feed() {
    const [posts, setPosts] = useState<Post[]>([]);
    const [content, setContent] = useState('');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);

    const load = async () => {
        try {
            setPosts((await Api.feed()) ?? []);
        } catch (ex) {
            setError((ex as Error).message);
        }
    };

    useEffect(() => {
        load();
    }, []);

    const submit = async (e: React.FormEvent) => {
        e.preventDefault();
        setBusy(true);
        setError('');
        try {
            await Api.createPost(content);
            setContent('');
            await load();
        } catch (ex) {
            setError((ex as Error).message);
        } finally {
            setBusy(false);
        }
    };

    return (
        <main>
            <h1>Feed</h1>
            {error && <p className="error">{error}</p>}

            <form onSubmit={submit}>
                <textarea
                    rows={3}
                    placeholder="What's happening?"
                    value={content}
                    onChange={e => setContent(e.target.value)}
                />
                <button disabled={busy || !content.trim()}>Post</button>
            </form>

            <div style={{ marginTop: 24 }}>
                {posts.map(post => (
                    <div className="card" key={post.Id}>
                        <p style={{ margin: '0 0 8px' }}>{post.Content}</p>
                        <p className="muted" style={{ margin: 0 }}>
                            <Link to={`/profile/${post.UserId}`}>User {post.UserId}</Link>
                            {' · '}
                            <Link to={`/post/${post.Id}`}>View</Link>
                        </p>
                    </div>
                ))}
                {posts.length === 0 && <p className="muted">No posts yet.</p>}
            </div>
        </main>
    );
}
