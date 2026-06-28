import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { jalali, toman, toFa } from '../format';
import { Badge, Icon, SectionHeader, EmptyState, Skeleton } from '../components';

export default function Home() {
  const [news, setNews] = useState(null);
  const [prices, setPrices] = useState(null);

  useEffect(() => {
    api.get('/api/articles?type=news').then((r) => setNews(r.data.slice(0, 4))).catch(() => setNews([]));
    api.get('/api/prices/current').then((r) => setPrices(r.data.slice(0, 5))).catch(() => setPrices([]));
  }, []);

  return (
    <div className="fade-up">
      <section className="hero">
        <span className="eyebrow"><Icon name="shield" size={15} /> احراز هویت اجباری · ناشناسی کامل</span>
        <h1>پتکی؛ سه پلتفرم در یک بستر</h1>
        <p>
          هوش بازار پتروشیمی، بازارگاه B2B فروشندگانِ احرازهویت‌شده، و موتور مناقصه معکوسِ
          <strong> کاملاً ناشناس</strong> — خریداران درخواست می‌دهند و فروشندگان واجد شرایط بر سر قیمت رقابت می‌کنند.
        </p>
        <div className="cta-row">
          <Link to="/competitions" className="btn lg"><Icon name="gavel" size={18} /> مشاهده مناقصه‌های باز</Link>
          <Link to="/register" className="btn lg outline">ثبت‌نام رایگان</Link>
        </div>
        <div className="trust-row">
          <span className="chip"><Icon name="lock" size={15} /> قیمت‌ها محرمانه (BR-11)</span>
          <span className="chip"><Icon name="check" size={15} /> رتبه‌بندی زنده</span>
          <span className="chip"><Icon name="shield" size={15} /> KYC پیش از معامله</span>
        </div>
      </section>

      <div className="stat-band" style={{ marginTop: 24 }}>
        <div><div className="kpi"><div className="num">۳</div><div className="lbl">پلتفرم در یک بستر</div></div></div>
        <div><div className="kpi"><div className="num">۱۰۰٪</div><div className="lbl">ناشناسی تا مرحله انتخاب</div></div></div>
        <div><div className="kpi"><div className="num">۵★</div><div className="lbl">امتیاز وزنیِ اعتبار</div></div></div>
        <div><div className="kpi"><div className="num">۲۴/۷</div><div className="lbl">مناقصه‌های در جریان</div></div></div>
      </div>

      <div className="grid cols-3" style={{ marginTop: 24 }}>
        <FeatureCard icon="chart" title="هوش بازار" text="اخبار، تحلیل، قیمت‌های جهانی، آمار معاملات و عرضه‌های آتی." to="/prices" cta="قیمت‌ها" />
        <FeatureCard icon="store" title="بازارگاه" text="کاتالوگ فروشندگان احرازهویت‌شده و پروفایل عمومی با امتیاز عملکرد." to="/marketplace" cta="بازارگاه" />
        <FeatureCard icon="gavel" title="موتور مناقصه" text="مناقصه معکوس ناشناس، رتبه‌بندی زنده، فهرست برتر و انتخاب برنده." to="/competitions" cta="مناقصه‌ها" amber />
      </div>

      <h2 className="section-title">چطور کار می‌کند؟</h2>
      <div className="steps">
        <Step n="۱" title="درخواست خرید (RFQ)" text="خریدارِ احرازهویت‌شده مشخصات و مهلت را ثبت می‌کند." />
        <Step n="۲" title="رقابت ناشناس" text="فروشندگان واجد شرایط بدون افشای هویت قیمت می‌دهند." />
        <Step n="۳" title="فهرست برتر" text="خریدار سه پیشنهاد برتر را انتخاب و هویت‌ها آشکار می‌شود." />
        <Step n="۴" title="معامله و امتیاز" text="برنده انتخاب، معامله ثبت و نظرسنجی دوطرفه تکمیل می‌شود." />
      </div>

      <div className="grid cols-2" style={{ marginTop: 28 }}>
        <div className="card">
          <SectionHeader title="تازه‌ترین اخبار" action={<Link to="/news">همه</Link>} />
          {news === null ? (
            <div style={{ marginTop: 12 }}>
              <Skeleton height={18} style={{ marginBottom: 10 }} />
              <Skeleton height={18} width="70%" style={{ marginBottom: 10 }} />
              <Skeleton height={18} width="85%" />
            </div>
          ) : news.length === 0 ? (
            <EmptyState icon="chart">خبری برای نمایش موجود نیست.</EmptyState>
          ) : news.map((a) => (
            <div key={a.id} style={{ padding: '11px 0', borderBottom: '1px solid var(--line-soft)' }}>
              <Link to={`/article/${a.slug}`}><strong>{a.title}</strong></Link>
              <div className="muted" style={{ fontSize: 13, marginTop: 3 }}>{jalali(a.publishedAt)} · <Badge kind="teal">{a.category}</Badge></div>
            </div>
          ))}
        </div>

        <div className="card">
          <SectionHeader title="قیمت‌های روز" action={<Link to="/prices">جزئیات</Link>} />
          {prices === null ? (
            <div style={{ marginTop: 12 }}>
              <Skeleton height={18} style={{ marginBottom: 10 }} />
              <Skeleton height={18} style={{ marginBottom: 10 }} />
              <Skeleton height={18} width="80%" />
            </div>
          ) : prices.length === 0 ? (
            <EmptyState icon="trending">قیمتی برای نمایش موجود نیست.</EmptyState>
          ) : (
            <div className="table-wrap" style={{ marginTop: 6 }}>
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
          )}
        </div>
      </div>

      <section className="card" style={{ marginTop: 28, textAlign: 'center', background: 'linear-gradient(135deg, var(--teal-50), #fff)' }}>
        <h3 style={{ margin: '4px 0 6px', fontSize: 22 }}>آماده‌ی شروع معامله‌ی هوشمند هستید؟</h3>
        <p className="muted" style={{ maxWidth: 540, margin: '0 auto 16px' }}>
          ثبت‌نام کنید، احراز هویت را تکمیل کنید و اولین مناقصه‌ی معکوس خود را باز کنید.
        </p>
        <Link to="/register" className="btn lg">شروع کنید <Icon name="arrow" size={18} /></Link>
      </section>
    </div>
  );
}

function FeatureCard({ icon, title, text, to, cta, amber }) {
  return (
    <Link to={to} className="card hover" style={{ display: 'block' }}>
      <div className={`icon-circle ${amber ? 'amber' : ''}`}><Icon name={icon} size={22} /></div>
      <h3 style={{ margin: '0 0 6px', color: 'var(--teal-dark)' }}>{title}</h3>
      <p className="muted" style={{ marginBottom: 14 }}>{text}</p>
      <span className="btn ghost sm">{cta} <Icon name="arrow" size={15} /></span>
    </Link>
  );
}

function Step({ n, title, text }) {
  return (
    <div className="card step">
      <div className="step-num">{n}</div>
      <h4 style={{ margin: '0 0 6px' }}>{title}</h4>
      <p className="muted" style={{ margin: 0, fontSize: 14 }}>{text}</p>
    </div>
  );
}
