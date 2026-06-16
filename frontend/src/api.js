import axios from 'axios';

const BASE = process.env.REACT_APP_API_URL || '';

export const api = axios.create({ baseURL: BASE });

const ACCESS = 'petki_access';
const REFRESH = 'petki_refresh';

export const tokens = {
  get access() { return localStorage.getItem(ACCESS); },
  get refresh() { return localStorage.getItem(REFRESH); },
  set({ accessToken, refreshToken }) {
    if (accessToken) localStorage.setItem(ACCESS, accessToken);
    if (refreshToken) localStorage.setItem(REFRESH, refreshToken);
  },
  clear() { localStorage.removeItem(ACCESS); localStorage.removeItem(REFRESH); },
};

api.interceptors.request.use((config) => {
  const t = tokens.access;
  if (t) config.headers.Authorization = `Bearer ${t}`;
  return config;
});

// Transparent refresh on 401 (single retry).
let refreshing = null;
api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const { response, config } = error;
    if (response?.status === 401 && tokens.refresh && !config._retried) {
      config._retried = true;
      try {
        refreshing = refreshing || axios.post(`${BASE}/api/auth/refresh`, { refreshToken: tokens.refresh });
        const { data } = await refreshing;
        refreshing = null;
        tokens.set(data);
        config.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(config);
      } catch (e) {
        refreshing = null;
        tokens.clear();
      }
    }
    return Promise.reject(error);
  }
);

// Surface backend { error, message } shape.
export function apiError(e) {
  return e?.response?.data?.message || e?.response?.data?.error || e?.message || 'خطای ناشناخته';
}
