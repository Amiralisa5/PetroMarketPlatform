import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { api, tokens } from './api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  const loadMe = useCallback(async () => {
    if (!tokens.access) { setUser(null); setLoading(false); return; }
    try {
      const { data } = await api.get('/api/auth/me');
      setUser(data);
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadMe(); }, [loadMe]);

  const onAuthenticated = useCallback(async (authResponse) => {
    tokens.set(authResponse);
    await loadMe();
  }, [loadMe]);

  const logout = useCallback(async () => {
    try { if (tokens.refresh) await api.post('/api/auth/logout', { refreshToken: tokens.refresh }); } catch {}
    tokens.clear();
    setUser(null);
  }, []);

  const has = useCallback((perm) => !!user?.permissions?.includes(perm), [user]);
  const inRole = useCallback((role) => !!user?.roles?.includes(role), [user]);

  return (
    <AuthContext.Provider value={{ user, loading, onAuthenticated, logout, has, inRole, refresh: loadMe }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
