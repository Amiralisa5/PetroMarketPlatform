import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { jalali, toman } from '../format';
import { Badge } from '../components';

export default function Home() {
  const [news, setNews] = useState([]);
  const [prices, setPrices] = useState([]);

  useEffect(() => {
    api.get('/api/articles?type=news').then((r) => setNews(r.data.slice(0, 4))).catch(() => {});
    api.get('/api/prices/current').then((r) => setPrices(r.data.slice(0, 5))).catch(() => {});
  }, []);

  return (
    <div>
      <section className="hero">
        <h1>پتکی؛ سه پلتفرم در یک بستر</h1>
        <p>
          اطلاعات بازار پتروشیمی، بازارگاه B2B فروشندگان احرازهویت‌شده، و موتور مناقصه معکوسِ
          <strong> کاملاً ناشناس</strong> — جایی که خریداران درخواست می‌دهند و فروشندگان واجد شرایط بر سر قیمت رقابت می‌کنند.
        </p>
        <Link to="/competitions" className="btn">مشاهده مناقصه‌های باز</Link>
      </section>

      <div className="grid cols-3" style={{ marginTop: 24 }}>
        <FeatureCard title="هوش بازار" text="اخبار، تحلیل، قیمت‌های جهانی، آمار معاملات و عرضه‌های آتی." to="/prices" cta="قیمت‌ها" />
        <FeatureCard title="بازارگاه" text="کاتالوگ فروشندگان احرازهویت‌شده و پروفایل عمومی با امتیاز عملکرد." to="/marketplace" cta="بازارگاه" />
        <FeatureCard title="موتور مناقصه" text="مناقصه معکوس ناشناس، رتبه‌بندی زنده، فهرست برتر و انتخاب برنده." to="/competitions" cta="مناقصه‌ها" />
      </div>

      <div className="grid cols-2" style={{ marginTop: 24 }}>
        <div className="card">
          <div className="between">
            <h3 style={{ margin: 0 }}>تازه‌ترین اخبار</h3>
            <Link to="/news">همه</Link>
          </div>
          {news.length === 0 ? <p className="muted">خبری موجود نیست.</p> : news.map((a) => (
            <div key={a.id} style={{ padding: '10px 0', borderBottom: '1px solid var(--line)' }}>
              <Link to={`/article/${a.slug}`}><strong>{a.title}</strong></Link>
              <div className="muted" style={{ fontSize: 13 }}>{jalali(a.publishedAt)} · <Badge kind="teal">{a.category}</Badge></div>
            </div>
          ))}
        </div>

        <div className="card">
          <div className="between">
            <h3 style={{ margin: 0 }}>قیمت‌های روز</h3>
            <Link to="/prices">جزئیات</Link>
          </div>
          <table>
            <thead><tr><th>کالا</th><th>قیمت</th><th>تاریخ</th></tr></thead>
            <tbody>
              {prices.map((p) => (
                <tr key={p.commodityId}>
                  <td>{p.commodity}</td>
                  <td className="price">{toman(p.price)}</td>
                  <td className="muted">{jalali(p.quotedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

function FeatureCard({ title, text, to, cta }) {
  return (
    <div className="card">
      <h3 style={{ marginTop: 0, color: 'var(--teal-dark)' }}>{title}</h3>
      <p className="muted">{text}</p>
      <Link to={to} className="btn ghost sm">{cta}</Link>
    </div>
  );
}
