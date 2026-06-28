import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, apiError } from '../api';
import { toman } from '../format';
import { Alert, Stars, Badge, Icon, EmptyState, Skeleton } from '../components';

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
    <div className="fade-up">
      <h1>بازارگاه</h1>
      <p className="sub">کاتالوگ محصولات فروشندگانِ احرازهویت‌شده</p>

      <div className="card" style={{ padding: 14, marginBottom: 18 }}>
        <div className="flex" style={{ flexWrap: 'wrap' }}>
          <select value={commodityId} onChange={(e) => setCommodityId(e.target.value)} style={{ maxWidth: 240 }}>
            <option value="">همه کالاها</option>
            {commodities.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <input placeholder="جستجوی مشخصات یا کالا…" value={q} onChange={(e) => setQ(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && load()} style={{ maxWidth: 260 }} />
          <button className="btn sm" onClick={load}><Icon name="store" size={15} /> اعمال فیلتر</button>
        </div>
      </div>

      <Alert kind="error">{err}</Alert>
      {products === null ? (
        <div className="grid cols-3">{[0, 1, 2].map((i) => <ProductSkeleton key={i} />)}</div>
      ) : products.length === 0 ? (
        <EmptyState icon="store">محصولی با این فیلترها یافت نشد.</EmptyState>
      ) : (
        <div className="grid cols-3">
          {products.map((p) => (
            <div key={p.id} className="card hover" style={{ display: 'flex', flexDirection: 'column' }}>
              <div className="between">
                <Badge kind="teal">{p.commodityName}</Badge>
                {p.priceIndication != null && <span className="price">{toman(p.priceIndication)}</span>}
              </div>
              <h3 style={{ margin: '12px 0 4px' }}>{p.specs || p.commodityName}</h3>
              <p className="muted" style={{ margin: '2px 0 14px', flex: 1 }}>
                <Icon name="store" size={14} style={{ verticalAlign: '-2px', marginLeft: 4 }} />
                موجودی: {p.availability || '—'}
              </p>
              <Link to={`/seller/${p.sellerId}`} className="btn ghost sm">پروفایل فروشنده <Icon name="arrow" size={15} /></Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function ProductSkeleton() {
  return (
    <div className="card">
      <Skeleton height={22} width="40%" />
      <Skeleton height={20} width="75%" style={{ margin: '14px 0 8px' }} />
      <Skeleton height={16} width="55%" />
      <Skeleton height={34} style={{ marginTop: 16, borderRadius: 8 }} />
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
  if (!p) return <p className="muted center">در حال بارگذاری…</p>;
  let metrics = {};
  try { metrics = JSON.parse(p.metricsJson || '{}'); } catch {}
  const title = p.companyName || p.name || 'فروشنده';
  return (
    <div className="fade-up">
      <Link to="/marketplace">→ بازگشت به بازارگاه</Link>
      <div className="card" style={{ marginTop: 12 }}>
        <div className="between" style={{ flexWrap: 'wrap', gap: 16 }}>
          <div className="flex" style={{ gap: 14 }}>
            <div className="avatar">{title.trim().charAt(0)}</div>
            <div>
              <h1 style={{ margin: 0 }}>{title}</h1>
              {p.name && p.name !== title && <p className="muted" style={{ margin: 0 }}>{p.name}</p>}
              <Badge kind="green" style={{ marginTop: 4 }}><Icon name="shield" size={13} /> احرازهویت‌شده</Badge>
            </div>
          </div>
          <div className="center">
            <Stars value={p.starScore} />
            <div className="muted" style={{ fontSize: 13 }}>{p.starScore} از ۵</div>
          </div>
        </div>
        <div className="stat-band" style={{ marginTop: 18 }}>
          <div><Metric label="معاملات موفق" value={metrics.completedTransactions ?? 0} /></div>
          <div><Metric label="میانگین نظرسنجی" value={metrics.surveyAvg ?? 0} /></div>
          <div><Metric label="تحویل به‌موقع" value={metrics.onTimeAvg ?? 0} /></div>
          <div><Metric label="شکایات" value={metrics.complaints ?? 0} /></div>
        </div>
      </div>
      <h2 className="section-title">محصولات</h2>
      {p.products.length === 0 ? <EmptyState icon="store">این فروشنده هنوز محصولی ثبت نکرده است.</EmptyState> : (
        <div className="grid cols-3">
          {p.products.map((pr) => (
            <div key={pr.id} className="card hover">
              <div className="between">
                <Badge kind="teal">{pr.commodityName}</Badge>
                {pr.priceIndication != null && <span className="price">{toman(pr.priceIndication)}</span>}
              </div>
              <h3 style={{ margin: '12px 0 4px' }}>{pr.specs || pr.commodityName}</h3>
              <p className="muted">موجودی: {pr.availability || '—'}</p>
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
