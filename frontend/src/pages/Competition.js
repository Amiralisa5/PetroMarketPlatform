import React, { useEffect, useState, useCallback } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { api, apiError } from '../api';
import { useAuth } from '../auth';
import { jalali, toman, num, compStatusLabel, toFa } from '../format';
import { Spinner, Alert, Badge, Stars, Icon, EmptyState } from '../components';

// ---------- Public list of open competitions ----------
export function Competitions() {
  const [rows, setRows] = useState(null);
  useEffect(() => { api.get('/api/competitions').then((r) => setRows(r.data)).catch(() => setRows([])); }, []);
  return (
    <div>
      <h1>مناقصه‌های باز</h1>
      <p className="sub">مناقصه‌های معکوسِ در جریان. قیمت‌ها به‌صورت پیش‌فرض محرمانه‌اند (BR-11).</p>
      {rows === null ? <Spinner /> : rows.length === 0 ? <EmptyState icon="gavel">در حال حاضر مناقصه‌ی بازی وجود ندارد.</EmptyState> : (
        <div className="grid cols-3">
          {rows.map((c) => (
            <Link key={c.id} to={`/competition/${c.id}`} className="card hover" style={{ display: 'block' }}>
              <div className="between">
                <Badge kind="green">باز</Badge>
                <span className="muted" style={{ fontSize: 13 }}>{toFa(c.bidCount)} پیشنهاد</span>
              </div>
              <div className="icon-circle" style={{ margin: '14px 0 10px' }}><Icon name="gavel" size={22} /></div>
              <h3 style={{ margin: '0 0 8px' }}>{c.commodity}</h3>
              <p className="muted" style={{ margin: '2px 0' }}>مقدار: {num(c.quantity)} تن</p>
              <p className="muted" style={{ margin: '2px 0 14px' }}>
                <Icon name="bell" size={14} style={{ verticalAlign: '-2px', marginLeft: 4 }} />
                مهلت: {jalali(c.bidWindowEnd, true)}
              </p>
              <span className="btn ghost sm">ورود به مناقصه <Icon name="arrow" size={15} /></span>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

// ---------- Create RFQ (buyer) ----------
export function CreateRfq() {
  const nav = useNavigate();
  const [commodities, setCommodities] = useState([]);
  const [f, setF] = useState({ commodityId: '', quantity: '', deliveryTerms: '', paymentTerms: '', notes: '', deadline: '', visibility: 'Confidential' });
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { api.get('/api/commodities').then((r) => setCommodities(r.data)).catch(() => {}); }, []);
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    setErr(''); setBusy(true);
    try {
      const body = {
        commodityId: Number(f.commodityId),
        quantity: Number(f.quantity),
        deliveryTerms: f.deliveryTerms, paymentTerms: f.paymentTerms, notes: f.notes,
        deadline: new Date(f.deadline).toISOString(),
        visibility: f.visibility,
      };
      const { data } = await api.post('/api/rfqs', body);
      nav(`/competition/${data.competitionId}`);
    } catch (e2) { setErr(apiError(e2)); } finally { setBusy(false); }
  };

  return (
    <div style={{ maxWidth: 640 }}>
      <h1>ثبت درخواست خرید (RFQ)</h1>
      <p className="sub">با ثبت RFQ، یک مناقصه‌ی معکوس باز می‌شود و فروشندگان واجد شرایط مطلع می‌گردند.</p>
      <Alert kind="error">{err}</Alert>
      <form className="card" onSubmit={submit}>
        <label>کالا</label>
        <select value={f.commodityId} onChange={set('commodityId')} required>
          <option value="">انتخاب کنید…</option>
          {commodities.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <div className="form-row">
          <div><label>مقدار (تن)</label><input type="number" min="1" value={f.quantity} onChange={set('quantity')} required /></div>
          <div><label>مهلت دریافت پیشنهاد</label><input type="datetime-local" value={f.deadline} onChange={set('deadline')} required /></div>
        </div>
        <div className="form-row">
          <div><label>شرایط تحویل</label><input value={f.deliveryTerms} onChange={set('deliveryTerms')} placeholder="مثلاً FOB عسلویه" /></div>
          <div><label>شرایط پرداخت</label><input value={f.paymentTerms} onChange={set('paymentTerms')} placeholder="مثلاً LC دیداری" /></div>
        </div>
        <label>توضیحات</label>
        <textarea rows="3" value={f.notes} onChange={set('notes')} />
        <label>سطح شفافیت قیمت (BR-11)</label>
        <select value={f.visibility} onChange={set('visibility')}>
          <option value="Confidential">محرمانه (پیش‌فرض)</option>
          <option value="AggregateOnly">فقط آمار تجمیعی</option>
          <option value="Public">عمومی</option>
        </select>
        <button className="btn" style={{ marginTop: 16 }} disabled={busy}>ثبت و باز کردن مناقصه</button>
      </form>
    </div>
  );
}

// ---------- Competition room (bidding / ranking / shortlist / winner) ----------
export function CompetitionRoom() {
  const { id } = useParams();
  const { user, has } = useAuth();
  const [comp, setComp] = useState(null);
  const [bids, setBids] = useState([]);
  const [confidential, setConfidential] = useState(false);
  const [aggregate, setAggregate] = useState(null);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [price, setPrice] = useState('');
  const [qty, setQty] = useState('');
  const [picked, setPicked] = useState([]);

  const isSeller = has('bid.submit');
  const isMonitor = has('competition.monitor');
  const isOwner = !!comp && !!user && comp.rfq?.buyerId === user.id && has('competition.shortlist');

  const load = useCallback(async () => {
    try {
      const { data: c } = await api.get(`/api/competitions/${id}`);
      setComp(c);
      const { data: b } = await api.get(`/api/competitions/${id}/bids`);
      if (b && b.confidential) { setConfidential(true); setAggregate(null); setBids([]); }
      else if (b && b.aggregateOnly) { setConfidential(false); setAggregate(b); setBids(b.myBid ? [b.myBid] : []); }
      else { setConfidential(false); setAggregate(null); setBids(Array.isArray(b) ? b : []); }
    } catch (e) { setErr(apiError(e)); }
  }, [id]);

  useEffect(() => { load(); }, [load]);
  // Live ranking refresh while bidding is open (BR-5).
  useEffect(() => {
    if (comp?.status !== 'Open') return;
    const t = setInterval(load, 8000);
    return () => clearInterval(t);
  }, [comp?.status, load]);

  const act = async (fn) => {
    setErr(''); setMsg('');
    try { await fn(); await load(); } catch (e) { setErr(apiError(e)); }
  };

  const submitBid = (e) => { e.preventDefault(); act(async () => {
    await api.post(`/api/competitions/${id}/bids`, { price: Number(price), quantity: Number(qty) });
    setMsg('پیشنهاد شما ثبت شد.');
  }); };

  const togglePick = (bidId) =>
    setPicked((p) => (p.includes(bidId) ? p.filter((x) => x !== bidId) : p.length < 3 ? [...p, bidId] : p));

  if (err && !comp) return <Alert kind="error">{err}</Alert>;
  if (!comp) return <Spinner />;

  const open = comp.status === 'Open';
  const closed = comp.status === 'Closed';
  const awarded = comp.status === 'Awarded';

  return (
    <div className="fade-up">
      <Link to="/competitions" className="muted">→ همه مناقصه‌ها</Link>
      <div className="card" style={{ marginTop: 12 }}>
        <div className="between" style={{ flexWrap: 'wrap', gap: 14 }}>
          <div className="flex" style={{ gap: 14 }}>
            <div className="icon-circle" style={{ margin: 0 }}><Icon name="gavel" size={22} /></div>
            <div>
              <h1 style={{ margin: 0, fontSize: 24 }}>مناقصه #{toFa(comp.id)} — {comp.commodityName}</h1>
              <p className="muted" style={{ margin: '2px 0 0' }}>
                مقدار: {num(comp.quantity)} تن ·
                <Icon name="bell" size={13} style={{ verticalAlign: '-2px', margin: '0 4px' }} />
                مهلت: {jalali(comp.bidWindowEnd, true)}
              </p>
            </div>
          </div>
          <StatusBadge status={comp.status} />
        </div>
        <LifeStepper status={comp.status} />
        {comp.rfq?.notes && <p className="muted" style={{ marginBottom: 6 }}>یادداشت خریدار: {comp.rfq.notes}</p>}
        <div className="flex" style={{ flexWrap: 'wrap', marginTop: 10 }}>
          {comp.rfq?.deliveryTerms && <Badge>تحویل: {comp.rfq.deliveryTerms}</Badge>}
          {comp.rfq?.paymentTerms && <Badge>پرداخت: {comp.rfq.paymentTerms}</Badge>}
          <Badge kind="teal"><Icon name="lock" size={12} /> شفافیت: {comp.visibility}</Badge>
        </div>
      </div>

      <Alert kind="error">{err}</Alert>
      <Alert kind="ok">{msg}</Alert>

      {/* Seller bidding form */}
      {isSeller && !isOwner && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3 className="flex" style={{ marginTop: 0 }}><Icon name="gavel" size={18} /> ثبت / اصلاح پیشنهاد</h3>
          {open ? (
            <form className="form-row" onSubmit={submitBid}>
              <div><label>قیمت کل (ریال)</label><input type="number" min="1" value={price} onChange={(e) => setPrice(e.target.value)} required /></div>
              <div><label>مقدار (تن)</label><input type="number" min="1" value={qty} onChange={(e) => setQty(e.target.value)} required /></div>
              <div style={{ gridColumn: '1 / -1' }}><button className="btn"><Icon name="check" size={16} /> ثبت پیشنهاد</button></div>
            </form>
          ) : <div className="alert info" style={{ margin: 0 }}>مهلت ارسال پیشنهاد به پایان رسیده است.</div>}
        </div>
      )}

      {/* Bids / ranking */}
      <h2 className="section-title flex">رتبه‌بندی پیشنهادها {open && <span className="badge green"><span className="live-dot" /> به‌روزرسانی زنده</span>}</h2>
      {confidential ? (
        <div className="card"><EmptyState icon="lock">قیمت‌ها در این مناقصه محرمانه است. تنها «باز بودن مناقصه» عمومی است (BR-11).</EmptyState></div>
      ) : (
        <>
          {aggregate && (
            <div className="card" style={{ marginBottom: 12 }}>
              <h3 className="flex" style={{ marginTop: 0 }}><Icon name="chart" size={18} /> آمار تجمیعی پیشنهادها</h3>
              <p className="muted" style={{ marginTop: 0 }}>در این مناقصه فقط آمار تجمیعی و ناشناس قیمت‌ها منتشر می‌شود (BR-11).</p>
              <div className="stat-band">
                <div className="kpi"><div className="num">{toFa(aggregate.bidCount)}</div><div className="lbl">تعداد پیشنهاد</div></div>
                <div className="kpi"><div className="num">{aggregate.lowestPrice != null ? toman(aggregate.lowestPrice) : '—'}</div><div className="lbl">کمترین قیمت</div></div>
                <div className="kpi"><div className="num">{aggregate.averagePrice != null ? toman(aggregate.averagePrice) : '—'}</div><div className="lbl">میانگین قیمت</div></div>
                <div className="kpi"><div className="num">{aggregate.highestPrice != null ? toman(aggregate.highestPrice) : '—'}</div><div className="lbl">بیشترین قیمت</div></div>
              </div>
            </div>
          )}
          {bids.length === 0 ? (
            !aggregate && <div className="card"><EmptyState icon="gavel">هنوز پیشنهادی ثبت نشده است.</EmptyState></div>
          ) : (
          <div className="card" style={{ padding: 0 }}>
          <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>رتبه</th><th>قیمت</th><th>مقدار</th><th>نسخه</th>
                {bids[0].sellerName !== undefined && <th>فروشنده</th>}
                {bids[0].sellerName !== undefined && <th>امتیاز</th>}
                {(closed && isOwner && !comp.identitiesRevealed) && <th>انتخاب (حداکثر ۳)</th>}
                {awarded && <th></th>}
              </tr>
            </thead>
            <tbody>
              {bids.map((b) => {
                const isWinner = awarded && comp.winningBidId === b.id;
                return (
                  <tr key={b.id} style={{ background: isWinner ? '#dcfce7' : b.isMine ? 'var(--teal-light)' : undefined }}>
                    <td>
                      <div className="flex" style={{ gap: 8 }}>
                        <span className={`rank-badge ${b.rank <= 3 ? 'r' + b.rank : ''}`}>{toFa(b.rank)}</span>
                        {b.isMine && <Badge kind="teal">شما</Badge>}
                      </div>
                    </td>
                    <td className="price">{toman(b.price)}</td>
                    <td>{num(b.quantity)}</td>
                    <td className="muted">v{toFa(b.version)}</td>
                    {b.sellerName !== undefined && <td><Link to={`/seller/${b.sellerId}`}>{b.sellerName}</Link>{b.companyName && <div className="muted" style={{ fontSize: 12 }}>{b.companyName}</div>}</td>}
                    {b.sellerName !== undefined && <td><Stars value={b.starScore} /></td>}
                    {(closed && isOwner && !comp.identitiesRevealed) && (
                      <td><input type="checkbox" style={{ width: 'auto' }} checked={picked.includes(b.id)} onChange={() => togglePick(b.id)} /></td>
                    )}
                    {awarded && <td>{isWinner && <Badge kind="green"><Icon name="check" size={12} /> برنده</Badge>}</td>}
                  </tr>
                );
              })}
            </tbody>
          </table>
          </div>

          {/* Buyer actions */}
          {isOwner && closed && !comp.identitiesRevealed && (
            <div style={{ padding: 16, borderTop: '1px solid var(--line-soft)' }}>
              <p className="muted" style={{ marginTop: 0 }}>حداکثر سه پیشنهاد برتر را انتخاب کنید؛ پس از آن هویت فروشندگان آشکار می‌شود (BR-6).</p>
              <button className="btn" disabled={picked.length === 0}
                onClick={() => act(async () => { await api.post(`/api/competitions/${id}/shortlist`, { bidIds: picked }); setMsg('فهرست برتر ثبت و هویت‌ها آشکار شد.'); })}>
                <Icon name="check" size={16} /> ثبت فهرست برتر و آشکارسازی هویت{picked.length > 0 ? ` (${toFa(picked.length)})` : ''}
              </button>
            </div>
          )}
          {isOwner && closed && comp.identitiesRevealed && !awarded && (
            <div style={{ padding: 16, borderTop: '1px solid var(--line-soft)' }}>
              <p className="muted" style={{ marginTop: 0 }}>برنده را از میان فهرست برتر انتخاب کنید.</p>
              <div className="flex" style={{ flexWrap: 'wrap' }}>
                {bids.filter((b) => b.shortlisted).map((b) => (
                  <button key={b.id} className="btn ghost sm"
                    onClick={() => act(async () => { await api.post(`/api/competitions/${id}/winner`, { bidId: b.id }); setMsg('برنده انتخاب و معامله ثبت شد.'); })}>
                    <Icon name="check" size={15} /> انتخاب {b.sellerName} — {toman(b.price)}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
          )}
        </>
      )}

      {/* Operator / admin controls */}
      {isMonitor && open && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3 className="flex" style={{ marginTop: 0 }}><Icon name="shield" size={18} /> کنترل اپراتور</h3>
          <button className="btn ghost sm" onClick={() => act(async () => { await api.post(`/api/competitions/${id}/close`); setMsg('مناقصه بسته شد.'); })}><Icon name="lock" size={15} /> بستن زودهنگام مناقصه</button>
        </div>
      )}

      {awarded && <Alert kind="ok">این مناقصه برنده دارد. معامله ثبت شده و طرفین باید نظرسنجی پس از معامله را تکمیل کنند (BR-9).</Alert>}
    </div>
  );
}

function LifeStepper({ status }) {
  if (status === 'Cancelled') return null;
  const order = ['Open', 'Closed', 'Awarded'];
  const idx = order.indexOf(status);
  const steps = [
    { k: 'Open', label: 'دریافت پیشنهاد' },
    { k: 'Closed', label: 'انتخاب و آشکارسازی' },
    { k: 'Awarded', label: 'ثبت معامله' },
  ];
  return (
    <div className="stepper">
      {steps.map((s, i) => (
        <React.Fragment key={s.k}>
          {i > 0 && <span className="sep" />}
          <span className={`st ${i < idx ? 'done' : i === idx ? 'active' : ''}`}>
            <span className="dot">{i < idx ? '✓' : toFa(i + 1)}</span>{s.label}
          </span>
        </React.Fragment>
      ))}
    </div>
  );
}

function StatusBadge({ status }) {
  const kind = { Open: 'green', Closed: 'amber', Awarded: 'teal', Cancelled: 'red' }[status] || '';
  return <Badge kind={kind}>{compStatusLabel(status)}</Badge>;
}
