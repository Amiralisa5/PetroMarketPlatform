import React, { useEffect, useState } from 'react';
import { NavLink, Link, useNavigate, Navigate } from 'react-router-dom';
import { useAuth } from './auth';
import { api } from './api';
import { toFa } from './format';

// ---- Navigation bar ----
export function Nav() {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    let alive = true;
    async function poll() {
      if (!user) { setUnread(0); return; }
      try {
        const { data } = await api.get('/api/notifications?unreadOnly=true');
        if (alive) setUnread(data.length);
      } catch {}
    }
    poll();
    const id = setInterval(poll, 20000);
    return () => { alive = false; clearInterval(id); };
  }, [user]);

  return (
    <nav className="nav">
      <div className="container nav-inner">
        <Link to="/" className="brand">پتـ<span>کی</span></Link>
        <div className="nav-links">
          <NavLink to="/news">اخبار</NavLink>
          <NavLink to="/analysis">تحلیل بازار</NavLink>
          <NavLink to="/prices">قیمت‌ها</NavLink>
          <NavLink to="/stats">آمار معاملات</NavLink>
          <NavLink to="/supplies">عرضه‌های آتی</NavLink>
          <NavLink to="/marketplace">بازارگاه</NavLink>
          <NavLink to="/competitions">مناقصه‌ها</NavLink>
          {user && <NavLink to="/dashboard">داشبورد</NavLink>}
        </div>
        <div className="nav-right">
          {user && (
            <Link to="/notifications" className="btn ghost sm">
              اعلان‌ها {unread > 0 && <span className="notif-dot">{toFa(unread)}</span>}
            </Link>
          )}
          {user ? (
            <>
              <span className="muted" style={{ fontSize: 13 }}>{user.fullName || toFa(user.mobile)}</span>
              <button className="btn ghost sm" onClick={async () => { await logout(); nav('/'); }}>خروج</button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn ghost sm">ورود</Link>
              <Link to="/register" className="btn sm">ثبت‌نام</Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
}

export function Layout({ children }) {
  return (
    <>
      <Nav />
      <main className="container page">{children}</main>
    </>
  );
}

// ---- Route guards ----
export function Protected({ children, perm, role }) {
  const { user, loading, has, inRole } = useAuth();
  if (loading) return <Layout><Spinner /></Layout>;
  if (!user) return <Navigate to="/login" replace />;
  if (perm && !has(perm)) return <Layout><Alert kind="error">شما دسترسی لازم برای این بخش را ندارید.</Alert></Layout>;
  if (role && !inRole(role)) return <Layout><Alert kind="error">این بخش مخصوص نقش {role} است.</Alert></Layout>;
  return children;
}

// ---- Small UI atoms ----
export function Spinner() {
  return <p className="muted center">در حال بارگذاری…</p>;
}

export function Alert({ kind = 'info', children }) {
  if (!children) return null;
  return <div className={`alert ${kind}`}>{children}</div>;
}

export function Stars({ value = 0 }) {
  const full = Math.round(value);
  return (
    <span className="stars" title={`${value}`}>
      {'★'.repeat(full)}{'☆'.repeat(Math.max(0, 5 - full))}
    </span>
  );
}

export function Badge({ kind = '', children }) {
  return <span className={`badge ${kind}`}>{children}</span>;
}

// ---- Lightweight SVG line chart (no external deps; ECharts in prod per §8) ----
export function Sparkline({ data = [], width = 560, height = 180, color = '#0f766e' }) {
  if (!data.length) return <p className="muted">داده‌ای برای نمودار موجود نیست.</p>;
  const vals = data.map((d) => Number(d.value));
  const min = Math.min(...vals), max = Math.max(...vals);
  const pad = 24;
  const span = max - min || 1;
  const pts = data.map((d, i) => {
    const x = pad + (i * (width - 2 * pad)) / Math.max(1, data.length - 1);
    const y = height - pad - ((Number(d.value) - min) / span) * (height - 2 * pad);
    return [x, y];
  });
  const path = pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(' ');
  const area = `${path} L ${pts[pts.length - 1][0].toFixed(1)} ${height - pad} L ${pts[0][0].toFixed(1)} ${height - pad} Z`;
  return (
    <svg viewBox={`0 0 ${width} ${height}`} style={{ width: '100%', height: 'auto', direction: 'ltr' }}>
      <path d={area} fill={color} opacity="0.08" />
      <path d={path} fill="none" stroke={color} strokeWidth="2" />
      {pts.map((p, i) => <circle key={i} cx={p[0]} cy={p[1]} r="2.5" fill={color} />)}
    </svg>
  );
}
