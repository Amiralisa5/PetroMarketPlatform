import React, { useEffect, useState } from 'react';
import { NavLink, Link, useNavigate, Navigate } from 'react-router-dom';
import { useAuth } from './auth';
import { api } from './api';
import { toFa } from './format';

// ---- Inline SVG icon set (stroke = currentColor) ----
const PATHS = {
  menu: <><line x1="3" y1="6" x2="21" y2="6" /><line x1="3" y1="12" x2="21" y2="12" /><line x1="3" y1="18" x2="21" y2="18" /></>,
  close: <><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></>,
  chart: <><path d="M3 3v18h18" /><path d="M7 15l3.5-4 3 2.5L21 7" /></>,
  store: <><path d="M3 9l1.5-5h15L21 9" /><path d="M4 9v10a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V9" /><path d="M9 20v-6h6v6" /></>,
  gavel: <><path d="M14 13l-7 7" /><path d="M5 11l6 6" /><path d="M11 5l6 6" /><path d="M13.5 7.5l3-3" /><path d="M16.5 10.5l3-3" /><line x1="3" y1="21" x2="12" y2="21" /></>,
  bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" /><path d="M13.7 21a2 2 0 0 1-3.4 0" /></>,
  shield: <><path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z" /><path d="M9 12l2 2 4-4" /></>,
  lock: <><rect x="4" y="11" width="16" height="10" rx="2" /><path d="M8 11V7a4 4 0 0 1 8 0v4" /></>,
  trending: <><path d="M3 17l6-6 4 4 8-8" /><path d="M17 7h4v4" /></>,
  arrow: <><line x1="19" y1="12" x2="5" y2="12" /><polyline points="12 19 5 12 12 5" /></>,
  check: <polyline points="20 6 9 17 4 12" />,
};

export function Icon({ name, size = 20, strokeWidth = 1.8, style }) {
  const p = PATHS[name];
  if (!p) return null;
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round"
      style={style} aria-hidden="true">{p}</svg>
  );
}

// ---- Navigation bar ----
export function Nav() {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const [unread, setUnread] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);

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
        <Link to="/" className="brand">
          <span className="brand-mark"><Icon name="gavel" size={19} strokeWidth={2} /></span>
          پتـ<span>کی</span>
        </Link>

        <button className="nav-toggle" onClick={() => setMenuOpen((o) => !o)} aria-label="منو">
          <Icon name={menuOpen ? 'close' : 'menu'} />
        </button>

        <div className={`nav-links ${menuOpen ? 'open' : ''}`} onClick={() => setMenuOpen(false)}>
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
            <Link to="/notifications" className="btn ghost sm" aria-label="اعلان‌ها">
              <Icon name="bell" size={17} />
              {unread > 0 && <span className="notif-dot">{toFa(unread)}</span>}
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

export function Footer() {
  const year = new Date().getFullYear();
  return (
    <footer className="footer">
      <div className="container footer-inner">
        <div>
          <Link to="/" className="brand" style={{ fontSize: 19 }}>
            <span className="brand-mark"><Icon name="gavel" size={17} strokeWidth={2} /></span>
            پتـ<span>کی</span>
          </Link>
          <p className="muted" style={{ fontSize: 14, marginTop: 12, maxWidth: 320 }}>
            پلتفرم هوش بازار، بازارگاه B2B و موتور مناقصه معکوسِ ناشناس محصولات پتروشیمی.
          </p>
        </div>
        <div>
          <h4>پلتفرم</h4>
          <Link to="/prices">قیمت‌ها</Link>
          <Link to="/marketplace">بازارگاه</Link>
          <Link to="/competitions">مناقصه‌ها</Link>
          <Link to="/news">اخبار و تحلیل</Link>
        </div>
        <div>
          <h4>حساب کاربری</h4>
          <Link to="/login">ورود</Link>
          <Link to="/register">ثبت‌نام</Link>
          <Link to="/dashboard">داشبورد</Link>
        </div>
      </div>
      <div className="copyright">© {toFa(year)} پتکی — تمامی حقوق محفوظ است.</div>
    </footer>
  );
}

export function Layout({ children }) {
  return (
    <>
      <Nav />
      <main className="container page">{children}</main>
      <Footer />
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

export function Skeleton({ height = 16, width = '100%', style }) {
  return <div className="skeleton" style={{ height, width, ...style }} />;
}

export function SectionHeader({ title, action }) {
  return (
    <div className="between">
      <h3 style={{ margin: 0 }}>{title}</h3>
      {action}
    </div>
  );
}

export function EmptyState({ icon = 'chart', children }) {
  return (
    <div className="empty">
      <div className="icon-circle"><Icon name={icon} /></div>
      <p className="muted" style={{ margin: 0 }}>{children}</p>
    </div>
  );
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
