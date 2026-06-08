import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from './pages/home';
import Login from './pages/login';
import Register from './pages/register';
import PostDetail from './pages/post';
import UserProfile from './pages/userprofile';
import Explore from './pages/explore';

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/post/:id" element={<PostDetail />} />
                <Route path="/profile/:id" element={<UserProfile />} />
                <Route path="/explore" element={<Explore />} />
            </Routes>
        </BrowserRouter>
    );
}
