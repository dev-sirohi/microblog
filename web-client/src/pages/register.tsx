import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthApi from '../api/AuthApi';
import Layout from '../components/Layout';

export default function Register() {
    const navigate = useNavigate();
    const [form, setForm] = React.useState({ username: '', email: '', password: '' });
    const [loading, setLoading] = React.useState(false);
    const [error, setError] = React.useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);
        try {
            const res = await AuthApi.registerUser({
                username: form.username,
                email: form.email,
                password: form.password,
            });
            if (res.Data?.id) {
                navigate('/login');
            }
        } catch (ex: any) {
            setError(ex.message ?? 'Registration failed');
        } finally {
            setLoading(false);
        }
    };

    const field = (label: string, key: keyof typeof form, type = 'text') => (
        <div key={key}>
            <label className="block text-xs font-medium mb-1" style={{ color: 'var(--color-muted)' }}>
                {label}
            </label>
            <input
                type={type}
                required
                value={form[key]}
                onChange={e => setForm({ ...form, [key]: e.target.value })}
                className="w-full px-3 py-2 rounded-lg border text-sm outline-none"
                style={{
                    backgroundColor: 'var(--color-bg)',
                    borderColor: 'var(--color-border)',
                    color: 'var(--color-text)',
                }}
            />
        </div>
    );

    return (
        <Layout showNav={false}>
            <div className="min-h-screen flex items-center justify-center -mt-6">
                <div
                    className="w-full max-w-sm rounded-2xl p-8 border"
                    style={{ backgroundColor: 'var(--color-surface)', borderColor: 'var(--color-border)' }}
                >
                    <h1 className="text-2xl font-bold mb-1" style={{ color: 'var(--color-text)' }}>
                        Create account
                    </h1>
                    <p className="text-sm mb-6" style={{ color: 'var(--color-muted)' }}>
                        Join microblog today
                    </p>

                    {error && (
                        <p className="text-sm mb-4 px-3 py-2 rounded-lg" style={{ backgroundColor: 'rgba(239,68,68,.1)', color: 'var(--color-danger)' }}>
                            {error}
                        </p>
                    )}

                    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                        {field('Username', 'username')}
                        {field('Email', 'email', 'email')}
                        {field('Password', 'password', 'password')}

                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full py-2.5 rounded-lg text-sm font-semibold transition-opacity disabled:opacity-60"
                            style={{ backgroundColor: 'var(--color-primary)', color: '#fff' }}
                        >
                            {loading ? 'Creating account…' : 'Create account'}
                        </button>
                    </form>

                    <p className="text-sm text-center mt-6" style={{ color: 'var(--color-muted)' }}>
                        Already have an account?{' '}
                        <Link to="/login" className="font-medium" style={{ color: 'var(--color-primary)' }}>
                            Sign in
                        </Link>
                    </p>
                </div>
            </div>
        </Layout>
    );
}
