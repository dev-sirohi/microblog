import { Link, useNavigate } from 'react-router-dom';
import { useTheme } from '../contexts/ThemeContext';
import AuthApi from '../api/AuthApi';

export default function Navbar() {
    const { theme, toggleTheme } = useTheme();
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
            await AuthApi.logoutUser();
        } catch { /* best effort */ }
        navigate('/login');
    };

    return (
        <nav
            className="sticky top-0 z-50 border-b"
            style={{
                backgroundColor: 'var(--color-surface)',
                borderColor: 'var(--color-border)',
            }}
        >
            <div className="max-w-2xl mx-auto px-4 h-14 flex items-center justify-between">
                <Link
                    to="/"
                    className="text-xl font-bold"
                    style={{ color: 'var(--color-primary)' }}
                >
                    microblog
                </Link>

                <div className="flex items-center gap-3">
                    <Link
                        to="/"
                        className="text-sm font-medium hover:opacity-70 transition-opacity"
                        style={{ color: 'var(--color-text)' }}
                    >
                        Feed
                    </Link>
                    <Link
                        to="/explore"
                        className="text-sm font-medium hover:opacity-70 transition-opacity"
                        style={{ color: 'var(--color-muted)' }}
                    >
                        Explore
                    </Link>

                    <button
                        onClick={toggleTheme}
                        className="w-8 h-8 flex items-center justify-center rounded-full hover:opacity-70 transition-opacity"
                        style={{ color: 'var(--color-muted)' }}
                        aria-label="Toggle theme"
                    >
                        {theme === 'dark' ? '☀️' : '🌙'}
                    </button>

                    <button
                        onClick={handleLogout}
                        className="text-sm px-3 py-1.5 rounded-lg font-medium transition-colors"
                        style={{
                            backgroundColor: 'var(--color-border)',
                            color: 'var(--color-text)',
                        }}
                    >
                        Logout
                    </button>
                </div>
            </div>
        </nav>
    );
}
