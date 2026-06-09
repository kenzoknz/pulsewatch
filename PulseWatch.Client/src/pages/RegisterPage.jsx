import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import {
    RiAlertLine,
    RiLockLine,
    RiMailLine,
    RiRadarLine,
    RiShieldCheckLine,
    RiTimerFlashLine,
    RiUserLine,
} from 'react-icons/ri';

export default function RegisterPage() {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const { register } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');

        if (password !== confirmPassword) {
            setError('Password and confirmation password do not match.');
            return;
        }

        if (password.length < 8) {
            setError('Password must contain at least 8 characters.');
            return;
        }

        setLoading(true);

        try {
            await register(username, email, password, confirmPassword);
            navigate('/', { replace: true });
        } catch (err) {
            setError(err.response?.data?.message || err.message || 'Registration failed. Please try again.');
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
                        <p className="auth-kicker">Start monitoring today</p>
                        <h1>Create a workspace for reliable uptime tracking.</h1>
                        <p>
                            Add websites, check availability, and keep your operational view clear from the first sign in.
                        </p>
                    </div>

                    <div className="auth-feature-grid">
                        <div className="auth-feature-card">
                            <RiTimerFlashLine />
                            <span>Uptime insights</span>
                        </div>
                        <div className="auth-feature-card">
                            <RiShieldCheckLine />
                            <span>Protected access</span>
                        </div>
                    </div>
                </section>

                <section className="auth-card" aria-label="Create account form">
                    <div className="auth-header">
                        <p className="auth-kicker">Get started</p>
                        <h2>Create your account</h2>
                        <p>Set up your PulseWatch access in a few seconds.</p>
                    </div>

                    {error && (
                        <div className="auth-error">
                            <RiAlertLine size={16} />
                            <span>{error}</span>
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="auth-form">
                        <div className="form-group">
                            <label htmlFor="username">Username</label>
                            <div className="auth-input-wrap">
                                <RiUserLine aria-hidden="true" />
                                <input
                                    id="username"
                                    type="text"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    placeholder="Choose a username"
                                    required
                                    autoFocus
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label htmlFor="email">Email</label>
                            <div className="auth-input-wrap">
                                <RiMailLine aria-hidden="true" />
                                <input
                                    id="email"
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder="Enter email"
                                    required
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
                                    placeholder="At least 8 characters"
                                    required
                                    minLength={8}
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label htmlFor="confirmPassword">Confirm password</label>
                            <div className="auth-input-wrap">
                                <RiLockLine aria-hidden="true" />
                                <input
                                    id="confirmPassword"
                                    type="password"
                                    value={confirmPassword}
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    placeholder="Re-enter your password"
                                    required
                                />
                            </div>
                        </div>

                        <button
                            type="submit"
                            className="btn btn-primary auth-submit"
                            disabled={loading}
                        >
                            {loading ? 'Creating account...' : 'Create account'}
                        </button>
                    </form>

                    <div className="auth-footer">
                        <p>
                            Already have an account?{' '}
                            <Link to="/login">Sign in</Link>
                        </p>
                    </div>
                </section>
            </div>
        </div>
    );
}