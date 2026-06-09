import { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import {
    RiAlertLine,
    RiLockLine,
    RiRadarLine,
    RiShieldCheckLine,
    RiTimerFlashLine,
    RiUserLine,
} from 'react-icons/ri';

export default function LoginPage() {
    const [emailOrUsername, setEmailOrUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const { login } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const from = location.state?.from?.pathname || '/';

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            await login(emailOrUsername, password);
            navigate(from, { replace: true });
        } catch (err) {
            setError(err.response?.data?.message || err.message || 'Sign in failed. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-page">
            <div className="auth-shell">
                <section className="auth-hero" aria-label="PulseWatch overview">
                    <div className="auth-brand">
                        <div className="auth-logo">
                            <RiRadarLine size={30} className="logo-pulse" />
                        </div>
                        <span>PulseWatch</span>
                    </div>

                    <div className="auth-hero-copy">
                        <p className="auth-kicker">Website uptime monitoring</p>
                        <h1>Stay ahead of outages before users notice.</h1>
                        <p>
                            Monitor websites, review downtime events, and keep your services visible from one focused dashboard.
                        </p>
                    </div>

                    <div className="auth-feature-grid">
                        <div className="auth-feature-card">
                            <RiTimerFlashLine />
                            <span>Fast status checks</span>
                        </div>
                        <div className="auth-feature-card">
                            <RiShieldCheckLine />
                            <span>Secure workspace</span>
                        </div>
                    </div>
                </section>

                <section className="auth-card" aria-label="Sign in form">
                    <div className="auth-header">
                        <p className="auth-kicker">Welcome back</p>
                        <h2>Sign in to your account</h2>
                        <p>Use your email address or username to continue.</p>
                    </div>

                    {error && (
                        <div className="auth-error">
                            <RiAlertLine size={16} />
                            <span>{error}</span>
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="auth-form">
                        <div className="form-group">
                            <label htmlFor="emailOrUsername">Email or username</label>
                            <div className="auth-input-wrap">
                                <RiUserLine aria-hidden="true" />
                                <input
                                    id="emailOrUsername"
                                    type="text"
                                    value={emailOrUsername}
                                    onChange={(e) => setEmailOrUsername(e.target.value)}
                                    placeholder="Enter email or username"
                                    required
                                    autoFocus
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label htmlFor="password">Password</label>
                            <div className="auth-input-wrap">
                                <RiLockLine aria-hidden="true" />
                                <input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="Enter your password"
                                    required
                                />
                            </div>
                        </div>

                        <button
                            type="submit"
                            className="btn btn-primary auth-submit"
                            disabled={loading}
                        >
                            {loading ? 'Signing in...' : 'Sign in'}
                        </button>
                    </form>

                    <div className="auth-footer">
                        <p>
                            Do not have an account?{' '}
                            <Link to="/register">Create one</Link>
                        </p>
                    </div>
                </section>
            </div>
        </div>
    );
}