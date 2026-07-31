import { useEffect, useState } from 'react';
import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router-dom';
import Api from './api';
import type { CurrentUser } from './types';
import Feed from './pages/Feed';
import Login from './pages/Login';
import PostDetail from './pages/PostDetail';
import Profile from './pages/Profile';
import Register from './pages/Register';

function Nav() {
    const [me, setMe] = useState<CurrentUser | null>(null);
    const navigate = useNavigate();

    useEffect(() => {
        Api.me().then(setMe).catch(() => setMe(null));
    }, []);

    const logout = async () => {
        await Api.logout().catch(() => undefined);
        setMe(null);
        navigate('/login');
    };

    return (
        <nav>
            <Link to="/">Feed</Link>
            {me ? (
                <>
                    <Link to={`/profile/${me.Id}`}>{me.Username}</Link>
                    <button className="secondary" onClick={logout}>
                        Log out
                    </button>
                </>
            ) : (
                <Link to="/login">Log in</Link>
            )}
        </nav>
    );
}

export default function App() {
    return (
        <BrowserRouter>
            <Nav />
            <Routes>
                <Route path="/" element={<Feed />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/post/:id" element={<PostDetail />} />
                <Route path="/profile/:id" element={<Profile />} />
            </Routes>
        </BrowserRouter>
    );
}
