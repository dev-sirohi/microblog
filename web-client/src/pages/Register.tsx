import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Api from '../api';

export default function Register() {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    const navigate = useNavigate();

    const submit = async (e: React.FormEvent) => {
        e.preventDefault();
        setBusy(true);
        setError('');
        try {
            await Api.register(username, email, password);
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
            <h1>Register</h1>
            {error && <p className="error">{error}</p>}
            <form onSubmit={submit}>
                <input placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} />
                <input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
                <input
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                />
                <button disabled={busy}>Register</button>
            </form>
            <p className="muted">
                Already registered? <Link to="/login">Log in</Link>
            </p>
        </main>
    );
}
