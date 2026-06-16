import React, { useEffect, useState } from 'react';
import { api, apiError } from '../api';
import { jalali, toman, num, kycLabel, toFa } from '../format';
import { Spinner, Alert, Badge } from '../components';

// ---------- Operator: KYC review queue ----------
export function KycQueue() {
  const [cases, setCases] = useState(null);
  const [notes, setNotes] = useState({});
  const [err, setErr] = useState(''); const [msg, setMsg] = useState('');

  const load = () => api.get('/api/kyc/queue').then((r) => setCases(r.data)).catch((e) => setErr(apiError(e)));
  useEffect(() => { load(); }, []);

  const review = (id, decision) => { setErr(''); setMsg('');
    api.post(`/api/kyc/${id}/review`, { decision, notes: notes[id] || '' })
      .then(() => { setMsg(`پرونده #${id} به‌روزرسانی شد.`); load(); }).catch((e) => setErr(apiError(e))); };

  if (cases === null) return <Spinner />;
  return (
    <div>
      <h1>صف بررسی احراز هویت</h1>
      <p className="sub">بررسی مدارک، تأیید/رد/درخواست اطلاعات بیشتر — همه اقدامات حسابرسی می‌شوند.</p>
      <Alert kind="error">{err}</Alert><Alert kind="ok">{msg}</Alert>
      {cases.length === 0 ? <p className="muted">صف خالی است.</p> : cases.map((c) => (
        <div key={c.id} className="card" style={{ marginBottom: 14 }}>
          <div className="between">
            <div>
              <strong>{c.companyName || c.userName || `کاربر ${c.userId}`}</strong>
              <div className="muted">پرونده #{toFa(c.id)} · وضعیت: {kycLabel(c.status)} · {jalali(c.updatedAt, true)}</div>
            </div>
            <Badge kind="amber">{kycLabel(c.status)}</Badge>
          </div>
          <div className="muted" style={{ margin: '8px 0' }}>
            مدارک: {c.documents.length === 0 ? 'ندارد' : c.documents.map((d) => `${d.type} (${d.fileName})`).join('، ')}
          </div>
          <label>یادداشت/دلیل</label>
          <input value={notes[c.id] || ''} onChange={(e) => setNotes({ ...notes, [c.id]: e.target.value })} placeholder="در صورت رد یا درخواست اطلاعات، الزامی است" />
          <div className="flex" style={{ marginTop: 10 }}>
            <button className="btn sm" onClick={() => review(c.id, 'approve')}>تأیید</button>
            <button className="btn ghost sm" onClick={() => review(c.id, 'more_info')}>اطلاعات بیشتر</button>
            <button className="btn danger sm" onClick={() => review(c.id, 'reject')}>رد</button>
          </div>
        </div>
      ))}
    </div>
  );
}

// ---------- Admin: users, roles & permissions ----------
export function AdminUsers() {
  const [users, setUsers] = useState(null);
  const [roleInfo, setRoleInfo] = useState({ roles: [], allPermissions: [] });
  const [err, setErr] = useState(''); const [msg, setMsg] = useState('');
  const allRoles = ['Buyer', 'Seller', 'Operator', 'Admin'];

  const load = () => api.get('/api/admin/users').then((r) => setUsers(r.data)).catch((e) => setErr(apiError(e)));
  useEffect(() => { load(); api.get('/api/admin/roles').then((r) => setRoleInfo(r.data)).catch(() => {}); }, []);

  const toggleRole = (u, role) => {
    const next = u.roles.includes(role) ? u.roles.filter((r) => r !== role) : [...u.roles, role];
    api.put(`/api/admin/users/${u.id}/roles`, { roles: next })
      .then(() => { setMsg(`نقش‌های کاربر ${u.id} به‌روزرسانی شد.`); load(); }).catch((e) => setErr(apiError(e)));
  };

  const toggleRolePerm = (role, perm) => {
    const next = role.permissions.includes(perm) ? role.permissions.filter((p) => p !== perm) : [...role.permissions, perm];
    api.put(`/api/admin/roles/${role.name}/permissions`, { permissions: next })
      .then(() => { setMsg(`دسترسی‌های نقش ${role.name} به‌روزرسانی شد.`); api.get('/api/admin/roles').then((r) => setRoleInfo(r.data)); })
      .catch((e) => setErr(apiError(e)));
  };

  if (users === null) return <Spinner />;
  return (
    <div>
      <h1>مدیریت کاربران و دسترسی‌ها</h1>
      <p className="sub">نقش‌ها مجموعه‌ای از دسترسی‌های نام‌دار هستند و بدون تغییر کد قابل ویرایش‌اند (RBAC، §3).</p>
      <Alert kind="error">{err}</Alert><Alert kind="ok">{msg}</Alert>

      <div className="card">
        <table>
          <thead><tr><th>#</th><th>موبایل</th><th>نام</th><th>KYC</th><th>نقش‌ها</th></tr></thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{toFa(u.id)}</td><td style={{ direction: 'ltr', textAlign: 'right' }}>{toFa(u.mobile)}</td>
                <td>{u.fullName}<div className="muted" style={{ fontSize: 12 }}>{u.company}</div></td>
                <td><Badge kind={u.kycStatus === 'Approved' ? 'green' : 'amber'}>{kycLabel(u.kycStatus)}</Badge></td>
                <td>
                  <div className="flex" style={{ flexWrap: 'wrap' }}>
                    {allRoles.map((r) => (
                      <label key={r} className="flex" style={{ margin: 0, fontSize: 12 }}>
                        <input type="checkbox" style={{ width: 'auto' }} checked={u.roles.includes(r)} onChange={() => toggleRole(u, r)} /> {r}
                      </label>
                    ))}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h2 className="section-title">ماتریس دسترسی نقش‌ها</h2>
      <div className="card" style={{ overflowX: 'auto' }}>
        <table>
          <thead>
            <tr><th>نقش</th>{roleInfo.allPermissions.map((p) => <th key={p} style={{ fontSize: 11 }}>{p}</th>)}</tr>
          </thead>
          <tbody>
            {roleInfo.roles.map((role) => (
              <tr key={role.id}>
                <td><strong>{role.name}</strong></td>
                {roleInfo.allPermissions.map((p) => (
                  <td key={p} className="center">
                    <input type="checkbox" style={{ width: 'auto' }}
                      checked={role.permissions.includes(p)} onChange={() => toggleRolePerm(role, p)} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ---------- Admin: KPIs ----------
export function AdminKpis() {
  const [k, setK] = useState(null);
  const [err, setErr] = useState('');
  useEffect(() => { api.get('/api/admin/kpis').then((r) => setK(r.data)).catch((e) => setErr(apiError(e))); }, []);
  if (err) return <Alert kind="error">{err}</Alert>;
  if (!k) return <Spinner />;
  return (
    <div>
      <h1>گزارش‌ها و شاخص‌های کلیدی</h1>
      <p className="sub">شاخص‌های ثبت‌نام، نرخ موفقیت مناقصه و KPIهای پلتفرم.</p>
      <div className="grid cols-4">
        <Kpi n={num(k.totalUsers)} l="کل کاربران" />
        <Kpi n={num(k.kycApproved)} l="احرازهویت تأییدشده" />
        <Kpi n={num(k.kycPending)} l="در صف احراز هویت" />
        <Kpi n={num(k.openCompetitions)} l="مناقصه‌های باز" />
        <Kpi n={num(k.totalCompetitions)} l="کل مناقصه‌ها" />
        <Kpi n={num(k.awardedCompetitions)} l="مناقصه‌های برنده‌دار" />
        <Kpi n={`${toFa(Math.round((k.competitionSuccessRate || 0) * 100))}٪`} l="نرخ موفقیت مناقصه" />
        <Kpi n={num(k.transactions)} l="کل معاملات" />
      </div>
      <div className="card" style={{ marginTop: 16 }}>
        <div className="kpi"><div className="num">{toman(k.grossMerchandiseValue)}</div><div className="lbl">ارزش کل معاملات (GMV)</div></div>
      </div>
    </div>
  );
}
function Kpi({ n, l }) { return <div className="card kpi"><div className="num">{n}</div><div className="lbl">{l}</div></div>; }

// ---------- Admin/Operator: audit log ----------
export function AuditLog() {
  const [rows, setRows] = useState(null);
  const [err, setErr] = useState('');
  useEffect(() => { api.get('/api/audit').then((r) => setRows(r.data)).catch((e) => setErr(apiError(e))); }, []);
  if (err) return <Alert kind="error">{err}</Alert>;
  if (rows === null) return <Spinner />;
  return (
    <div>
      <h1>ردگیری حسابرسی</h1>
      <p className="sub">لاگ اقدامات ممتاز و آشکارسازی هویت (§7).</p>
      <div className="card" style={{ overflowX: 'auto' }}>
        <table>
          <thead><tr><th>#</th><th>اقدام</th><th>موجودیت</th><th>شناسه</th><th>کاربر</th><th>زمان</th></tr></thead>
          <tbody>
            {rows.map((a) => (
              <tr key={a.id}>
                <td>{toFa(a.id)}</td><td><Badge>{a.action}</Badge></td>
                <td className="muted">{a.entity}</td><td className="muted">{a.entityId}</td>
                <td className="muted">{a.actorId != null ? toFa(a.actorId) : '—'}</td>
                <td className="muted">{jalali(a.createdAt, true)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
