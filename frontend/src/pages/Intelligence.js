import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, apiError } from '../api';
import { jalali, toman, num } from '../format';
import { Spinner, Alert, Badge, Sparkline } from '../components';

// ---------- Articles (News + Analysis) ----------
function ArticleList({ type, title, subtitle }) {
  const [items, setItems] = useState(null);
  const [q, setQ] = useState('');
  const [err, setErr] = useState('');

  const load = (search = '') => {
    setItems(null);
    api.get(`/api/articles?type=${type}${search ? `&q=${encodeURIComponent(search)}` : ''}`)
      .then((r) => setItems(r.data)).catch((e) => setErr(apiError(e)));
  };
  useEffect(() => { load(); /* eslint-disable-next-line */ }, [type]);

  return (
    <div>
      <h1>{title}</h1>
      <p className="sub">{subtitle}</p>
      <div className="flex" style={{ marginBottom: 16 }}>
        <input placeholder="جستجو…" value={q} onChange={(e) => setQ(e.target.value)} style={{ maxWidth: 320 }} />
        <button className="btn sm" onClick={() => load(q)}>جستجو</button>
      </div>
      <Alert kind="error">{err}</Alert>
      {items === null ? <Spinner /> : items.length === 0 ? <p className="muted">موردی یافت نشد.</p> : (
        <div className="grid cols-2">
          {items.map((a) => (
            <div key={a.id} className="card">
              <div className="between">
                <Badge kind="teal">{a.category || (type === 'news' ? 'خبر' : 'تحلیل')}</Badge>
                <span className="muted" style={{ fontSize: 13 }}>{jalali(a.publishedAt)}</span>
              </div>
              <h3 style={{ margin: '10px 0 6px' }}><Link to={`/article/${a.slug}`}>{a.title}</Link></h3>
              <p className="muted">{a.summary}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export const News = () => <ArticleList type="news" title="اخبار صنعت" subtitle="اخبار، اطلاعیه‌های شرکت‌ها و به‌روزرسانی‌های بازار" />;
export const Analysis = () => <ArticleList type="analysis" title="تحلیل بازار" subtitle="تحلیل‌های کارشناسی، گزارش‌ها و مقالات روند بازار" />;

export function ArticleDetail() {
  const { slug } = useParams();
  const [a, setA] = useState(null);
  const [err, setErr] = useState('');
  useEffect(() => {
    api.get(`/api/articles/${slug}`).then((r) => setA(r.data)).catch((e) => setErr(apiError(e)));
  }, [slug]);
  if (err) return <Alert kind="error">{err}</Alert>;
  if (!a) return <Spinner />;
  return (
    <article>
      <Link to={a.type === 'Analysis' ? '/analysis' : '/news'}>← بازگشت</Link>
      <h1 style={{ marginTop: 12 }}>{a.title}</h1>
      <p className="muted">{jalali(a.publishedAt, true)} · {a.category}</p>
      <p style={{ fontSize: 17 }}>{a.summary}</p>
      <div style={{ whiteSpace: 'pre-wrap' }}>{a.body}</div>
    </article>
  );
}

// ---------- Global Prices ----------
export function Prices() {
  const [rows, setRows] = useState(null);
  const [history, setHistory] = useState([]);
  const [selected, setSelected] = useState(null);

  useEffect(() => { api.get('/api/prices/current').then((r) => setRows(r.data)).catch(() => setRows([])); }, []);

  const showHistory = (commodityId, name) => {
    setSelected(name);
    api.get(`/api/prices/history/${commodityId}?days=30`)
      .then((r) => setHistory(r.data.map((d) => ({ value: d.price, label: d.quotedAt }))))
      .catch(() => setHistory([]));
  };

  return (
    <div>
      <h1>قیمت‌های جهانی</h1>
      <p className="sub">قیمت جاری کالاها — برای مشاهده روند ۳۰ روزه روی هر ردیف کلیک کنید.</p>
      {rows === null ? <Spinner /> : (
        <div className="grid cols-2">
          <div className="card">
            <table>
              <thead><tr><th>کالا</th><th>قیمت</th><th>بازار</th><th>تاریخ</th></tr></thead>
              <tbody>
                {rows.map((p) => (
                  <tr key={p.commodityId} style={{ cursor: 'pointer' }} onClick={() => showHistory(p.commodityId, p.commodity)}>
                    <td>{p.commodity}</td>
                    <td className="price">{toman(p.price)}</td>
                    <td className="muted">{p.market}</td>
                    <td className="muted">{jalali(p.quotedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card">
            <h3 style={{ marginTop: 0 }}>{selected ? `روند قیمت: ${selected}` : 'نمودار روند'}</h3>
            {selected ? <Sparkline data={history} /> : <p className="muted">یک کالا را انتخاب کنید.</p>}
          </div>
        </div>
      )}
    </div>
  );
}

// ---------- Trade Statistics ----------
export function TradeStats() {
  const [summary, setSummary] = useState(null);
  const [rows, setRows] = useState([]);
  const [commodities, setCommodities] = useState({});
  useEffect(() => {
    api.get('/api/trade-stats/summary').then((r) => setSummary(r.data)).catch(() => {});
    api.get('/api/trade-stats').then((r) => setRows(r.data)).catch(() => {});
    api.get('/api/commodities').then((r) => setCommodities(Object.fromEntries(r.data.map((c) => [c.id, c.name])))).catch(() => {});
  }, []);
  return (
    <div>
      <h1>آمار معاملات</h1>
      <p className="sub">حجم معاملات، قیمت پایه و قیمت نهایی</p>
      {summary && (
        <div className="grid cols-3" style={{ marginBottom: 20 }}>
          <div className="card kpi"><div className="num">{num(summary.totalVolume)}</div><div className="lbl">مجموع حجم (تن)</div></div>
          <div className="card kpi"><div className="num">{toman(summary.avgBasePrice)}</div><div className="lbl">میانگین قیمت پایه</div></div>
          <div className="card kpi"><div className="num">{toman(summary.avgFinalPrice)}</div><div className="lbl">میانگین قیمت نهایی</div></div>
        </div>
      )}
      <div className="card">
        <table>
          <thead><tr><th>کالا</th><th>دوره</th><th>حجم</th><th>قیمت پایه</th><th>قیمت نهایی</th></tr></thead>
          <tbody>
            {rows.map((t, i) => (
              <tr key={i}>
                <td>{commodities[t.commodityId] || t.commodityId}</td>
                <td className="muted">{jalali(t.period)}</td>
                <td>{num(t.volume)}</td>
                <td className="price">{toman(t.basePrice)}</td>
                <td className="price">{toman(t.finalPrice)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ---------- Future Supplies ----------
export function Supplies() {
  const [rows, setRows] = useState(null);
  useEffect(() => { api.get('/api/future-supplies').then((r) => setRows(r.data)).catch(() => setRows([])); }, []);
  return (
    <div>
      <h1>عرضه‌های آتی</h1>
      <p className="sub">عرضه‌های پیشِ‌رو — تأمین‌کننده، حجم، تاریخ و محل تحویل</p>
      {rows === null ? <Spinner /> : (
        <div className="card">
          <table>
            <thead><tr><th>کالا</th><th>تأمین‌کننده</th><th>حجم</th><th>تاریخ عرضه</th><th>محل</th></tr></thead>
            <tbody>
              {rows.map((f) => (
                <tr key={f.id}>
                  <td>{f.commodity}</td>
                  <td>{f.supplier}</td>
                  <td>{num(f.volume)}</td>
                  <td className="muted">{jalali(f.supplyDate)}</td>
                  <td className="muted">{f.location}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
