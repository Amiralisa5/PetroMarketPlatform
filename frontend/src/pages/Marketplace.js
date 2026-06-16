import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, apiError } from '../api';
import { toman } from '../format';
import { Spinner, Alert, Stars, Badge } from '../components';

export function Marketplace() {
  const [products, setProducts] = useState(null);
  const [commodities, setCommodities] = useState([]);
  const [commodityId, setCommodityId] = useState('');
  const [q, setQ] = useState('');
  const [err, setErr] = useState('');

  const load = () => {
    setProducts(null);
    const params = new URLSearchParams();
    if (commodityId) params.set('commodityId', commodityId);
    if (q) params.set('q', q);
    api.get(`/api/products?${params}`).then((r) => setProducts(r.data)).catch((e) => setErr(apiError(e)));
  };
  useEffect(() => { api.get('/api/commodities').then((r) => setCommodities(r.data)).catch(() => {}); }, []);
  useEffect(() => { load(); /* eslint-disable-next-line */ }, []);

  return (
    <div>
      <h1>بازارگاه</h1>
      <p className="sub">کاتالوگ محصولات فروشندگانِ احرازهویت‌شده</p>
      <div className="flex" style={{ marginBottom: 16, flexWrap: 'wrap' }}>
        <select value={commodityId} onChange={(e) => setCommodityId(e.target.value)} style={{ maxWidth: 240 }}>
          <option value="">همه کالاها</option>
          {commodities.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <input placeholder="جستجو…" value={q} onChange={(e) => setQ(e.target.value)} style={{ maxWidth: 240 }} />
        <button className="btn sm" onClick={load}>اعمال فیلتر</button>
      </div>
      <Alert kind="error">{err}</Alert>
      {products === null ? <Spinner /> : products.length === 0 ? <p className="muted">محصولی یافت نشد.</p> : (
        <div className="grid cols-3">
          {products.map((p) => (
            <div key={p.id} className="card">
              <Badge kind="teal">{p.commodityName}</Badge>
              <h3 style={{ margin: '10px 0 4px' }}>{p.specs || p.commodityName}</h3>
              <p className="muted" style={{ margin: '4px 0' }}>موجودی: {p.availability || '—'}</p>
              {p.priceIndication != null && <p className="price">{toman(p.priceIndication)}</p>}
              <Link to={`/seller/${p.sellerId}`} className="btn ghost sm" style={{ marginTop: 8 }}>پروفایل فروشنده</Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function SellerProfile() {
  const { id } = useParams();
  const [p, setP] = useState(null);
  const [err, setErr] = useState('');
  useEffect(() => {
    api.get(`/api/sellers/${id}`).then((r) => setP(r.data)).catch((e) => setErr(apiError(e)));
  }, [id]);
  if (err) return <Alert kind="error">{err}</Alert>;
  if (!p) return <Spinner />;
  let metrics = {};
  try { metrics = JSON.parse(p.metricsJson || '{}'); } catch {}
  return (
    <div>
      <Link to="/marketplace">← بازگشت به بازارگاه</Link>
      <div className="card" style={{ marginTop: 12 }}>
        <div className="between">
          <div>
            <h1 style={{ margin: 0 }}>{p.companyName || p.name || 'فروشنده'}</h1>
            <p className="muted">{p.name}</p>
          </div>
          <div className="center">
            <Stars value={p.starScore} />
            <div className="muted">{p.starScore} از ۵</div>
          </div>
        </div>
        <div className="grid cols-4" style={{ marginTop: 12 }}>
          <Metric label="معاملات موفق" value={metrics.completedTransactions ?? 0} />
          <Metric label="میانگین نظرسنجی" value={metrics.surveyAvg ?? 0} />
          <Metric label="تحویل به‌موقع" value={metrics.onTimeAvg ?? 0} />
          <Metric label="شکایات" value={metrics.complaints ?? 0} />
        </div>
      </div>
      <h2 className="section-title">محصولات</h2>
      {p.products.length === 0 ? <p className="muted">محصولی ثبت نشده.</p> : (
        <div className="grid cols-3">
          {p.products.map((pr) => (
            <div key={pr.id} className="card">
              <Badge kind="teal">{pr.commodityName}</Badge>
              <h3 style={{ margin: '10px 0 4px' }}>{pr.specs || pr.commodityName}</h3>
              <p className="muted">موجودی: {pr.availability || '—'}</p>
              {pr.priceIndication != null && <p className="price">{toman(pr.priceIndication)}</p>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function Metric({ label, value }) {
  return <div className="kpi"><div className="num" style={{ fontSize: 22 }}>{value}</div><div className="lbl">{label}</div></div>;
}
