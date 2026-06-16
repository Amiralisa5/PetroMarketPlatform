import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { api, apiError } from '../api';
import { useAuth } from '../auth';
import { Alert } from '../components';
import { toFa } from '../format';

function OtpFlow({ mode }) {
  const isRegister = mode === 'register';
  const { onAuthenticated } = useAuth();
  const nav = useNavigate();
  const [step, setStep] = useState(1);
  const [mobile, setMobile] = useState('');
  const [fullName, setFullName] = useState('');
  const [roles, setRoles] = useState(['Buyer']);
  const [code, setCode] = useState('');
  const [devCode, setDevCode] = useState('');
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);

  const base = isRegister ? '/api/auth/register' : '/api/auth/login';

  const toggleRole = (r) =>
    setRoles((rs) => (rs.includes(r) ? rs.filter((x) => x !== r) : [...rs, r]));

  const requestOtp = async (e) => {
    e.preventDefault();
    setErr(''); setBusy(true);
    try {
      const { data } = await api.post(`${base}/request-otp`, { mobile });
      setDevCode(data.devCode || '');
      if (data.devCode) setCode(data.devCode);
      setStep(2);
    } catch (e2) { setErr(apiError(e2)); } finally { setBusy(false); }
  };

  const verify = async (e) => {
    e.preventDefault();
    setErr(''); setBusy(true);
    try {
      const body = isRegister ? { mobile, code, fullName, roles } : { mobile, code };
      const { data } = await api.post(`${base}/verify`, body);
      await onAuthenticated(data);
      nav('/dashboard');
    } catch (e2) { setErr(apiError(e2)); } finally { setBusy(false); }
  };

  return (
    <div style={{ maxWidth: 440, margin: '20px auto' }}>
      <div className="card">
        <h1 style={{ marginTop: 0 }}>{isRegister ? 'ثبت‌نام' : 'ورود'}</h1>
        <Alert kind="error">{err}</Alert>
        {step === 1 ? (
          <form onSubmit={requestOtp}>
            <label>شماره موبایل</label>
            <input value={mobile} onChange={(e) => setMobile(e.target.value)}
              placeholder="۰۹۱۲۳۴۵۶۷۸۹" required inputMode="tel" style={{ direction: 'ltr', textAlign: 'left' }} />
            {isRegister && (
              <>
                <label>نام / نام شرکت</label>
                <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="نام کامل" />
                <label>نقش موردنظر برای فعالیت</label>
                <div className="flex">
                  <label className="flex" style={{ margin: 0 }}>
                    <input type="checkbox" checked={roles.includes('Buyer')} onChange={() => toggleRole('Buyer')} style={{ width: 'auto' }} /> خریدار
                  </label>
                  <label className="flex" style={{ margin: 0 }}>
                    <input type="checkbox" checked={roles.includes('Seller')} onChange={() => toggleRole('Seller')} style={{ width: 'auto' }} /> فروشنده
                  </label>
                </div>
              </>
            )}
            <button className="btn" style={{ marginTop: 16, width: '100%' }} disabled={busy}>دریافت کد یک‌بارمصرف</button>
          </form>
        ) : (
          <form onSubmit={verify}>
            <p className="muted">کد پیامک‌شده به {toFa(mobile)} را وارد کنید.</p>
            {devCode && <Alert kind="info">کد توسعه (محیط تست): <strong>{toFa(devCode)}</strong></Alert>}
            <label>کد تأیید</label>
            <input value={code} onChange={(e) => setCode(e.target.value)} required
              style={{ direction: 'ltr', textAlign: 'center', letterSpacing: 6, fontSize: 20 }} />
            <button className="btn" style={{ marginTop: 16, width: '100%' }} disabled={busy}>
              {isRegister ? 'تکمیل ثبت‌نام' : 'ورود'}
            </button>
            <button type="button" className="btn ghost sm" style={{ marginTop: 8, width: '100%' }}
              onClick={() => setStep(1)}>تغییر شماره</button>
          </form>
        )}
        <p className="muted center" style={{ marginTop: 16 }}>
          {isRegister ? <>حساب دارید؟ <Link to="/login">ورود</Link></> : <>حساب ندارید؟ <Link to="/register">ثبت‌نام</Link></>}
        </p>
      </div>
      <p className="muted center" style={{ fontSize: 12, marginTop: 10 }}>
        حساب‌های نمونه: ادمین ۰۹۱۲۰۰۰۰۰۰۱ · اپراتور ۰۹۱۲۰۰۰۰۰۰۲ · خریدار ۰۹۱۲۰۰۰۰۰۰۳ · فروشنده ۰۹۱۲۰۰۰۰۰۰۴
      </p>
    </div>
  );
}

export const Login = () => <OtpFlow mode="login" />;
export const Register = () => <OtpFlow mode="register" />;
