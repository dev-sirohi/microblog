import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthApi from '../api/AuthApi';
import Layout from '../components/Layout';

export default function Login() {
    const navigate = useNavigate();
    const [form, setForm] = React.useState({ identifier: '', password: '' });
    const [loading, setLoading] = React.useState(false);
    const [error, setError] = React.useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);
        try {
            const isEmail = form.identifier.includes('@');
            const res = await AuthApi.loginUser({
                username: isEmail ? '' : form.identifier,
                email: isEmail ? form.identifier : '',
                password: form.password,
            });
            if (res.Data?.id) {
                navigate('/');
            } else {
                setError('Login failed. Check your credentials.');
            }
        } catch (ex: any) {
            setError(ex.message ?? 'Login failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Layout showNav={false}>
            <div className="min-h-screen flex items-center justify-center -mt-6">
                <div
                    className="w-full max-w-sm rounded-2xl p-8 border"
                    style={{ backgroundColor: 'var(--color-surface)', borderColor: 'var(--color-border)' }}
                >
                    <h1 className="text-2xl font-bold mb-1" style={{ color: 'var(--color-text)' }}>
                        Welcome back
                    </h1>
                    <p className="text-sm mb-6" style={{ color: 'var(--color-muted)' }}>
                        Sign in to microblog
                    </p>

                    {error && (
                        <p className="text-sm mb-4 px-3 py-2 rounded-lg" style={{ backgroundColor: 'rgba(239,68,68,.1)', color: 'var(--color-danger)' }}>
                            {error}
                        </p>
                    )}

                    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                        <div>
                            <label className="block text-xs font-medium mb-1" style={{ color: 'var(--color-muted)' }}>
                                Username or Email
                            </label>
                            <input
                                type="text"
                                required
                                value={form.identifier}
                                onChange={e => setForm({ ...form, identifier: e.target.value })}
                                className="w-full px-3 py-2 rounded-lg border text-sm outline-none focus:ring-2"
                                style={{
                                    backgroundColor: 'var(--color-bg)',
                                    borderColor: 'var(--color-border)',
                                    color: 'var(--color-text)',
                                    '--tw-ring-color': 'var(--color-primary)',
                                } as React.CSSProperties}
                            />
                        </div>

                        <div>
                            <label className="block text-xs font-medium mb-1" style={{ color: 'var(--color-muted)' }}>
                                Password
                            </label>
                            <input
                                type="password"
                                required
                                value={form.password}
                                onChange={e => setForm({ ...form, password: e.target.value })}
                                className="w-full px-3 py-2 rounded-lg border text-sm outline-none focus:ring-2"
                                style={{
                                    backgroundColor: 'var(--color-bg)',
                                    borderColor: 'var(--color-border)',
                                    color: 'var(--color-text)',
                                } as React.CSSProperties}
                            />
                        </div>

                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full py-2.5 rounded-lg text-sm font-semibold transition-opacity disabled:opacity-60"
                            style={{ backgroundColor: 'var(--color-primary)', color: '#fff' }}
                        >
                            {loading ? 'Signing in…' : 'Sign in'}
                        </button>
                    </form>

                    <p className="text-sm text-center mt-6" style={{ color: 'var(--color-muted)' }}>
                        Don't have an account?{' '}
                        <Link to="/register" className="font-medium" style={{ color: 'var(--color-primary)' }}>
                            Sign up
                        </Link>
                    </p>
                </div>
            </div>
        </Layout>
    );
}
