import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, apiError } from '../api';
import { useAuth } from '../auth';
import { jalali, toman, num, kycLabel, compStatusLabel, toFa } from '../format';
import { Spinner, Alert, Badge, Icon, EmptyState } from '../components';

// ---------- Dashboard hub ----------
export function Dashboard() {
  const { user, has } = useAuth();
  const [rfqs, setRfqs] = useState([]);
  const [txns, setTxns] = useState([]);
  const [pending, setPending] = useState([]);

  useEffect(() => {
    if (has('rfq.manage')) api.get('/api/rfqs/mine').then((r) => setRfqs(r.data)).catch(() => {});
    api.get('/api/transactions/mine').then((r) => setTxns(r.data)).catch(() => {});
    if (has('survey.submit')) api.get('/api/surveys/pending').then((r) => setPending(r.data)).catch(() => {});
  }, [has]);

  const approved = user?.kycStatus === 'Approved';

  return (
    <div>
      <h1>داشبورد</h1>
      <p className="sub">خوش آمدید، {user?.fullName || toFa(user?.mobile)} · نقش‌ها: {user?.roles?.join('، ')}</p>

      {!approved && (has('kyc.complete')) && (
        <Alert kind="info">
          وضعیت احراز هویت شما: <strong>{kycLabel(user?.kycStatus)}</strong>. برای انجام معامله باید احراز هویت تأیید شود (BR-2).{' '}
          <Link to="/kyc">تکمیل احراز هویت</Link>
        </Alert>
      )}
      {pending.length > 0 && (
        <Alert kind="error">شما {toFa(pending.length)} نظرسنجی تکمیل‌نشده دارید که مانع معاملات بعدی است (BR-9). <Link to="/surveys">تکمیل نظرسنجی‌ها</Link></Alert>
      )}

      <div className="grid cols-4" style={{ margin: '8px 0 20px' }}>
        {has('rfq.manage') && <Tile icon="gavel" to="/rfq/new" title="ثبت RFQ" desc="درخواست خرید جدید" />}
        {has('product.manage') && <Tile icon="store" to="/seller/manage/products" title="محصولات من" desc="مدیریت کاتالوگ" />}
        {has('kyc.complete') && <Tile icon="shield" to="/kyc" title="احراز هویت" desc={kycLabel(user?.kycStatus)} />}
        {has('kyc.review') && <Tile icon="check" to="/operator/kyc" title="صف احراز هویت" desc="بررسی اپراتور" />}
        {has('users.manage') && <Tile icon="shield" to="/admin/users" title="مدیریت کاربران" desc="نقش‌ها و دسترسی‌ها" />}
        {has('reports.view') && <Tile icon="trending" to="/admin/kpis" title="گزارش‌ها و KPI" desc="شاخص‌های پلتفرم" />}
        {has('audit.view') && <Tile icon="lock" to="/admin/audit" title="ردگیری حسابرسی" desc="لاگ اقدامات" />}
        <Tile icon="gavel" to="/competitions" title="مناقصه‌ها" desc="مشاهده مناقصه‌های باز" />
      </div>

      {has('rfq.manage') && (
        <>
          <h2 className="section-title">درخواست‌های خرید من</h2>
          {rfqs.length === 0 ? <EmptyState icon="gavel">هنوز درخواست خریدی ثبت نکرده‌اید.</EmptyState> : (
            <div className="card"><table>
              <thead><tr><th>#</th><th>کالا</th><th>مقدار</th><th>مهلت</th><th>وضعیت</th><th></th></tr></thead>
              <tbody>
                {rfqs.map((r) => (
                  <tr key={r.id}>
                    <td>{toFa(r.id)}</td><td>{r.commodity}</td><td>{num(r.quantity)}</td>
                    <td className="muted">{jalali(r.deadline)}</td>
                    <td><Badge>{compStatusLabel(r.competitionStatus)}</Badge></td>
                    <td><Link to={`/competition/${r.competitionId}`}>ورود به مناقصه</Link></td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}
        </>
      )}

      <h2 className="section-title">معاملات من</h2>
      {txns.length === 0 ? <EmptyState icon="store">هنوز معامله‌ای ثبت نشده است.</EmptyState> : (
        <div className="card"><table>
          <thead><tr><th>#</th><th>کالا</th><th>مقدار</th><th>قیمت</th><th>وضعیت</th><th>تاریخ</th></tr></thead>
          <tbody>
            {txns.map((t) => (
              <tr key={t.id}>
                <td>{toFa(t.id)}</td><td>{t.commodityName}</td><td>{num(t.quantity)}</td>
                <td className="price">{toman(t.price)}</td><td><Badge kind="green">{t.status}</Badge></td>
                <td className="muted">{jalali(t.completedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table></div>
      )}
    </div>
  );
}

function Tile({ to, title, desc, icon }) {
  return <Link to={to} className="card hover" style={{ textDecoration: 'none', display: 'block' }}>
    <div className="icon-circle" style={{ width: 40, height: 40, borderRadius: 11, marginBottom: 10 }}><Icon name={icon || 'arrow'} size={19} /></div>
    <div style={{ fontWeight: 700, color: 'var(--teal-dark)' }}>{title}</div>
    <div className="muted" style={{ fontSize: 13 }}>{desc}</div>
  </Link>;
}

// ---------- KYC (buyer/seller) ----------
export function Kyc() {
  const { refresh } = useAuth();
  const [kyc, setKyc] = useState(undefined);
  const [company, setCompany] = useState({ legalName: '', registrationNumber: '', nationalId: '', address: '' });
  const [docType, setDocType] = useState('national_id');
  const [file, setFile] = useState(null);
  const [err, setErr] = useState(''); const [msg, setMsg] = useState('');

  const load = () => api.get('/api/kyc/me').then((r) => setKyc(r.data)).catch((e) => setErr(apiError(e)));
  useEffect(() => { load(); }, []);

  const saveCompany = (e) => { e.preventDefault(); setErr(''); setMsg('');
    api.post('/api/kyc/company', company).then(() => { setMsg('اطلاعات شرکت ذخیره شد.'); load(); }).catch((e) => setErr(apiError(e))); };

  const upload = (e) => { e.preventDefault(); setErr(''); setMsg('');
    if (!file) return setErr('یک فایل انتخاب کنید.');
    const fd = new FormData(); fd.append('type', docType); fd.append('file', file);
    api.post('/api/kyc/documents', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
      .then(() => { setMsg('سند بارگذاری شد.'); setFile(null); load(); }).catch((e) => setErr(apiError(e))); };

  const submit = () => { setErr(''); setMsg('');
    api.post('/api/kyc/submit').then(() => { setMsg('احراز هویت برای بررسی ارسال شد.'); load(); refresh(); }).catch((e) => setErr(apiError(e))); };

  return (
    <div style={{ maxWidth: 720 }}>
      <h1>احراز هویت (KYC)</h1>
      <p className="sub">تا تأیید احراز هویت، امکان معامله وجود ندارد (BR-2).</p>
      {kyc && <Alert kind="info">وضعیت فعلی: <strong>{kycLabel(kyc.status)}</strong>{kyc.notes ? ` — ${kyc.notes}` : ''}</Alert>}
      <Alert kind="error">{err}</Alert>
      <Alert kind="ok">{msg}</Alert>

      <form className="card" onSubmit={saveCompany}>
        <h3 style={{ marginTop: 0 }}>۱) اطلاعات شرکت</h3>
        <label>نام حقوقی</label>
        <input value={company.legalName} onChange={(e) => setCompany({ ...company, legalName: e.target.value })} required />
        <div className="form-row">
          <div><label>شماره ثبت</label><input value={company.registrationNumber} onChange={(e) => setCompany({ ...company, registrationNumber: e.target.value })} /></div>
          <div><label>شناسه ملی</label><input value={company.nationalId} onChange={(e) => setCompany({ ...company, nationalId: e.target.value })} /></div>
        </div>
        <label>نشانی</label>
        <input value={company.address} onChange={(e) => setCompany({ ...company, address: e.target.value })} />
        <button className="btn" style={{ marginTop: 14 }}>ذخیره اطلاعات شرکت</button>
      </form>

      <form className="card" style={{ marginTop: 16 }} onSubmit={upload}>
        <h3 style={{ marginTop: 0 }}>۲) بارگذاری مدارک</h3>
        <div className="form-row">
          <div>
            <label>نوع سند</label>
            <select value={docType} onChange={(e) => setDocType(e.target.value)}>
              <option value="national_id">کارت ملی</option>
              <option value="company_registration">روزنامه رسمی / ثبت شرکت</option>
              <option value="other">سایر</option>
            </select>
          </div>
          <div><label>فایل</label><input type="file" onChange={(e) => setFile(e.target.files[0])} /></div>
        </div>
        <button className="btn" style={{ marginTop: 14 }}>بارگذاری</button>
      </form>

      {kyc?.documents?.length > 0 && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3 style={{ marginTop: 0 }}>مدارک بارگذاری‌شده</h3>
          <ul>{kyc.documents.map((d) => <li key={d.id}>{d.type} — {d.fileName} <span className="muted">({jalali(d.uploadedAt)})</span></li>)}</ul>
        </div>
      )}

      <div style={{ marginTop: 16 }}>
        <button className="btn" onClick={submit}>۳) ارسال برای بررسی</button>
      </div>
    </div>
  );
}

// ---------- Seller products ----------
export function SellerProducts() {
  const [items, setItems] = useState([]);
  const [commodities, setCommodities] = useState([]);
  const [f, setF] = useState({ commodityId: '', specs: '', availability: '', priceIndication: '' });
  const [err, setErr] = useState(''); const [msg, setMsg] = useState('');

  const load = () => api.get('/api/products/mine').then((r) => setItems(r.data)).catch((e) => setErr(apiError(e)));
  useEffect(() => { load(); api.get('/api/commodities').then((r) => setCommodities(r.data)).catch(() => {}); }, []);

  const add = (e) => { e.preventDefault(); setErr(''); setMsg('');
    api.post('/api/products', { commodityId: Number(f.commodityId), specs: f.specs, availability: f.availability, priceIndication: f.priceIndication ? Number(f.priceIndication) : null })
      .then(() => { setMsg('محصول افزوده شد.'); setF({ commodityId: '', specs: '', availability: '', priceIndication: '' }); load(); })
      .catch((e) => setErr(apiError(e))); };

  const remove = (id) => api.delete(`/api/products/${id}`).then(load).catch((e) => setErr(apiError(e)));

  return (
    <div>
      <h1>محصولات من</h1>
      <p className="sub">کاتالوگ شما در بازارگاه و برای تطبیق مناقصه‌ها استفاده می‌شود.</p>
      <Alert kind="error">{err}</Alert><Alert kind="ok">{msg}</Alert>

      <form className="card" onSubmit={add}>
        <h3 style={{ marginTop: 0 }}>افزودن محصول</h3>
        <div className="form-row">
          <div>
            <label>کالا</label>
            <select value={f.commodityId} onChange={(e) => setF({ ...f, commodityId: e.target.value })} required>
              <option value="">انتخاب…</option>
              {commodities.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div><label>قیمت پیشنهادی (ریال)</label><input type="number" value={f.priceIndication} onChange={(e) => setF({ ...f, priceIndication: e.target.value })} /></div>
        </div>
        <div className="form-row">
          <div><label>مشخصات</label><input value={f.specs} onChange={(e) => setF({ ...f, specs: e.target.value })} /></div>
          <div><label>موجودی</label><input value={f.availability} onChange={(e) => setF({ ...f, availability: e.target.value })} /></div>
        </div>
        <button className="btn" style={{ marginTop: 14 }}>افزودن</button>
      </form>

      <div className="card" style={{ marginTop: 16 }}>
        {items.length === 0 ? <EmptyState icon="store">هنوز محصولی ثبت نکرده‌اید.</EmptyState> : (
          <table>
            <thead><tr><th>کالا</th><th>مشخصات</th><th>موجودی</th><th>قیمت</th><th></th></tr></thead>
            <tbody>
              {items.map((p) => (
                <tr key={p.id}>
                  <td>{p.commodityName}</td><td>{p.specs}</td><td>{p.availability}</td>
                  <td className="price">{p.priceIndication != null ? toman(p.priceIndication) : '—'}</td>
                  <td><button className="btn danger sm" onClick={() => remove(p.id)}>حذف</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

// ---------- Notifications ----------
export function Notifications() {
  const [rows, setRows] = useState([]);
  const load = () => api.get('/api/notifications').then((r) => setRows(r.data)).catch(() => {});
  useEffect(() => { load(); }, []);
  const markRead = (id) => api.post(`/api/notifications/${id}/read`).then(load).catch(() => {});
  return (
    <div>
      <h1>اعلان‌ها</h1>
      <p className="sub">رویدادهای تراکنشی از طریق سرویس واحد اعلان (SMS / Push / درون‌برنامه‌ای).</p>
      {rows.length === 0 ? <EmptyState icon="bell">اعلانی ندارید.</EmptyState> : (
        <div className="card">
          {rows.map((n) => (
            <div key={n.id} className="between" style={{ padding: '10px 0', borderBottom: '1px solid var(--line)', opacity: n.readAt ? 0.6 : 1 }}>
              <div>
                <div className="flex"><Badge kind="teal">{n.channel}</Badge> <strong>{n.title}</strong></div>
                <div className="muted" style={{ fontSize: 13 }}>{n.payload}</div>
                <div className="muted" style={{ fontSize: 12 }}>{jalali(n.createdAt, true)}</div>
              </div>
              {!n.readAt && <button className="btn ghost sm" onClick={() => markRead(n.id)}>خواندم</button>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ---------- Surveys (post-trade, BR-9) ----------
export function Surveys() {
  const [pending, setPending] = useState([]);
  const [form, setForm] = useState({});
  const [err, setErr] = useState(''); const [msg, setMsg] = useState('');
  const load = () => api.get('/api/surveys/pending').then((r) => setPending(r.data)).catch((e) => setErr(apiError(e)));
  useEffect(() => { load(); }, []);

  const submit = (txnId) => { setErr(''); setMsg('');
    const f = form[txnId] || { score: 5, onTimeScore: 5, infoAccuracyScore: 5, comments: '' };
    api.post(`/api/surveys/${txnId}`, f).then(() => { setMsg('نظرسنجی ثبت شد.'); load(); }).catch((e) => setErr(apiError(e))); };

  const set = (txnId, k, v) => setForm({ ...form, [txnId]: { score: 5, onTimeScore: 5, infoAccuracyScore: 5, comments: '', ...(form[txnId] || {}), [k]: v } });

  return (
    <div style={{ maxWidth: 720 }}>
      <h1>نظرسنجی پس از معامله</h1>
      <p className="sub">تکمیل نظرسنجی برای هر دو طرف الزامی است؛ در غیر این صورت معاملات بعدی مسدود می‌شود (BR-9).</p>
      <Alert kind="error">{err}</Alert><Alert kind="ok">{msg}</Alert>
      {pending.length === 0 ? <EmptyState icon="check">نظرسنجی معلقی ندارید.</EmptyState> : pending.map((s) => {
        const f = form[s.transactionId] || { score: 5, onTimeScore: 5, infoAccuracyScore: 5, comments: '' };
        return (
          <div key={s.id} className="card" style={{ marginBottom: 14 }}>
            <h3 style={{ marginTop: 0 }}>معامله #{toFa(s.transactionId)}</h3>
            <div className="form-row">
              <div><label>امتیاز کلی (۱-۵)</label><input type="number" min="1" max="5" value={f.score} onChange={(e) => set(s.transactionId, 'score', Number(e.target.value))} /></div>
              <div><label>تحویل به‌موقع (۱-۵)</label><input type="number" min="1" max="5" value={f.onTimeScore} onChange={(e) => set(s.transactionId, 'onTimeScore', Number(e.target.value))} /></div>
            </div>
            <div className="form-row">
              <div><label>دقت اطلاعات (۱-۵)</label><input type="number" min="1" max="5" value={f.infoAccuracyScore} onChange={(e) => set(s.transactionId, 'infoAccuracyScore', Number(e.target.value))} /></div>
              <div><label>توضیحات</label><input value={f.comments} onChange={(e) => set(s.transactionId, 'comments', e.target.value)} /></div>
            </div>
            <button className="btn" style={{ marginTop: 12 }} onClick={() => submit(s.transactionId)}>ثبت نظرسنجی</button>
          </div>
        );
      })}
    </div>
  );
}
