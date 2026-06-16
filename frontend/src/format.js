// Persian/Jalali formatting helpers (NFR §7: full RTL, Jalali calendar, Persian digits, Toman).

const FA_DIGITS = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

export function toFa(input) {
  return String(input ?? '').replace(/[0-9]/g, (d) => FA_DIGITS[+d]);
}

// Gregorian -> Jalali (Shamsi) conversion (Birashk-free, well-known algorithm).
function gregorianToJalali(gy, gm, gd) {
  const g_d_m = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
  let jy = gy <= 1600 ? 0 : 979;
  gy -= gy <= 1600 ? 621 : 1600;
  const gy2 = gm > 2 ? gy + 1 : gy;
  let days =
    365 * gy +
    Math.floor((gy2 + 3) / 4) -
    Math.floor((gy2 + 99) / 100) +
    Math.floor((gy2 + 399) / 400) -
    80 +
    gd +
    g_d_m[gm - 1];
  jy += 33 * Math.floor(days / 12053);
  days %= 12053;
  jy += 4 * Math.floor(days / 1461);
  days %= 1461;
  jy += Math.floor((days - 1) / 365);
  if (days > 365) days = (days - 1) % 365;
  const jm = days < 186 ? 1 + Math.floor(days / 31) : 7 + Math.floor((days - 186) / 30);
  const jd = 1 + (days < 186 ? days % 31 : (days - 186) % 30);
  return [jy, jm, jd];
}

const JM = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];

export function jalali(dateLike, withTime = false) {
  if (!dateLike) return '—';
  const d = new Date(dateLike);
  if (isNaN(d)) return '—';
  const [jy, jm, jd] = gregorianToJalali(d.getFullYear(), d.getMonth() + 1, d.getDate());
  let out = `${toFa(jd)} ${JM[jm - 1]} ${toFa(jy)}`;
  if (withTime) {
    const hh = String(d.getHours()).padStart(2, '0');
    const mm = String(d.getMinutes()).padStart(2, '0');
    out += ` - ${toFa(hh)}:${toFa(mm)}`;
  }
  return out;
}

// Convert IRR (Rial) figure to Toman with grouping (1 Toman = 10 Rial).
export function toman(irr) {
  if (irr == null || isNaN(irr)) return '—';
  const t = Math.round(Number(irr) / 10);
  return `${toFa(t.toLocaleString('en-US'))} تومان`;
}

export function num(n) {
  if (n == null || isNaN(n)) return '—';
  return toFa(Number(n).toLocaleString('en-US'));
}

export function kycLabel(status) {
  return {
    None: 'ثبت‌نشده', Draft: 'پیش‌نویس', Submitted: 'ارسال‌شده',
    UnderReview: 'در حال بررسی', Approved: 'تأییدشده', Rejected: 'ردشده', MoreInfo: 'نیاز به اطلاعات',
  }[status] || status;
}

export function compStatusLabel(s) {
  return { Open: 'باز', Closed: 'بسته‌شده', Awarded: 'برنده‌دار', Cancelled: 'لغوشده' }[s] || s;
}
