import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import Api from '../api';
import type { CurrentUser, Profile as ProfileData } from '../types';

export default function Profile() {
    const { id } = useParams<{ id: string }>();
    const userId = Number(id);

    const [profile, setProfile] = useState<ProfileData | null>(null);
    const [me, setMe] = useState<CurrentUser | null>(null);
    const [error, setError] = useState('');

    const load = async () => {
        try {
            setProfile(await Api.profile(userId));
        } catch (ex) {
            setError((ex as Error).message);
        }
    };

    useEffect(() => {
        load();
        Api.me().then(setMe).catch(() => setMe(null));
    }, [userId]);

    const toggleFollow = async () => {
        if (!profile) return;
        setError('');
        try {
            if (profile.IsFollowing) await Api.unfollow(userId);
            else await Api.follow(userId);
            await load();
        } catch (ex) {
            setError((ex as Error).message);
        }
    };

    const uploadAvatar = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;
        setError('');
        try {
            await Api.uploadAvatar(file);
            await load();
        } catch (ex) {
            setError((ex as Error).message);
        }
    };

    if (!profile) {
        return (
            <main>
                {error ? <p className="error">{error}</p> : <p className="muted">Loading…</p>}
            </main>
        );
    }

    const isMe = me?.Id === profile.Id;

    return (
        <main>
            <h1>{profile.Username}</h1>
            {error && <p className="error">{error}</p>}

            <div className="card">
                <div className="row">
                    {profile.AvatarUrl && <img className="avatar" src={profile.AvatarUrl} alt="avatar" />}
                    <span className="muted">
                        {profile.FollowersCount} followers · {profile.FollowingCount} following
                    </span>
                </div>

                <div style={{ marginTop: 12 }}>
                    {isMe ? (
                        <input type="file" accept="image/*" onChange={uploadAvatar} />
                    ) : (
                        <button onClick={toggleFollow}>{profile.IsFollowing ? 'Unfollow' : 'Follow'}</button>
                    )}
                </div>
            </div>

            {profile.Posts?.map(post => (
                <div className="card" key={post.Id}>
                    <p style={{ margin: '0 0 8px' }}>{post.Content}</p>
                    <p className="muted" style={{ margin: 0 }}>
                        <Link to={`/post/${post.Id}`}>View</Link>
                    </p>
                </div>
            ))}
        </main>
    );
}
