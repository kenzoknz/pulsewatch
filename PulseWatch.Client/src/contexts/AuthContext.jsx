import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authApi } from '../api/pulsewatchApi';
import { getCookie, setCookie, removeCookie } from '../utils/cookies';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [token, setToken] = useState(() => getCookie('accessToken'));
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const verifyToken = async () => {
            if (!token) {
                setLoading(false);
                return;
            }

            try {
                const response = await authApi.getMe();
                setUser(response.data);
            } catch (error) {
                console.error('Token verification failed:', error);
                removeCookie('accessToken');
                removeCookie('tokenExpiresAt');
                setToken(null);
                setUser(null);
            } finally {
                setLoading(false);
            }
        };

        verifyToken();
    }, [token]);

    useEffect(() => {
        const handleUnauthorized = () => {
            setToken(null);
            setUser(null);
        };
        window.addEventListener('auth:unauthorized', handleUnauthorized);
        return () => {
            window.removeEventListener('auth:unauthorized', handleUnauthorized);
        };
    }, []);

    const login = useCallback(async (emailOrUsername, password) => {
        const response = await authApi.login({ emailOrUsername, password });
        const data = response.data;

        setCookie('accessToken', data.accessToken, data.expiresAt);
        setCookie('tokenExpiresAt', new Date(data.expiresAt).toISOString(), data.expiresAt);

        setToken(data.accessToken);
        setUser(data.user);

        return data;
    }, []);

    const register = useCallback(async (username, email, password, confirmPassword) => {
        const response = await authApi.register({
            username,
            email,
            password,
            confirmPassword,
        });
        const data = response.data;

        setCookie('accessToken', data.accessToken, data.expiresAt);
        setCookie('tokenExpiresAt', new Date(data.expiresAt).toISOString(), data.expiresAt);

        setToken(data.accessToken);
        setUser(data.user);

        return data;
    }, []);

    const logout = useCallback(() => {
        removeCookie('accessToken');
        removeCookie('tokenExpiresAt');
        setToken(null);
        setUser(null);
    }, []);

    const refreshUser = useCallback(async () => {
        try {
            const response = await authApi.getMe();
            setUser(response.data);
        } catch {
            // Ignore
        }
    }, []);

    const isAuthenticated = !!token && !!user;

    const value = {
        user,
        token,
        loading,
        isAuthenticated,
        login,
        register,
        logout,
        refreshUser,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}