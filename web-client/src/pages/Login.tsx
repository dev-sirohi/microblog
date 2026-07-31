import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Api from '../api';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    const navigate = useNavigate();

    const submit = async (e: React.FormEvent) => {
        e.preventDefault();
        setBusy(true);
        setError('');
        try {
            await Api.login(username, password);
            navigate('/');
        } catch (ex) {
            setError((ex as Error).message);
        } finally {
            setBusy(false);
        }
    };

    return (
        <main>
            <h1>Log in</h1>
            {error && <p className="error">{error}</p>}
            <form onSubmit={submit}>
                <input placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} />
                <input
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                />
                <button disabled={busy}>Log in</button>
            </form>
            <p className="muted">
                No account? <Link to="/register">Register</Link>
            </p>
        </main>
    );
}
