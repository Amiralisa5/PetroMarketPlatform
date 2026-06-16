# Petki.ir — B2B Petrochemical Platform

Petki is **three products in one platform**, implementing the Software Requirements Specification
(`Petki-Requirements-and-Architecture`):

1. **Market-intelligence platform** — news, analysis, global prices, trade statistics, future-supply monitoring.
2. **B2B commodity marketplace** — catalog of verified sellers with public profiles.
3. **Anonymous reverse-auction (RFQ) engine** — the core differentiator: buyers post an RFQ, qualified
   sellers compete on price under mutual anonymity, the buyer shortlists the top 3, identities are revealed,
   a winner is selected, a transaction is recorded, and mandatory post-trade surveys feed a weighted rating.

Two cross-cutting systems sit underneath: **KYC & compliance** (no trading without verified identity) and a
**trust & reputation network** (mandatory post-trade feedback → weighted rating).

## Stack decision

The spec (§12.5) leaves the backend language open and explicitly lists **.NET as a valid choice** because the
existing skeleton is .NET. This build **extends the existing .NET 8 + React skeleton** (the lowest-risk reading
of "use the repo files"):

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 8 (Web API), **modular monolith** with clear seams |
| ORM / DB | EF Core 8 + **SQLite** for dev (provider-switchable to SQL Server / PostgreSQL via config) |
| Auth | Mobile **OTP** + **JWT** access/refresh, **permission-based RBAC** |
| Realtime | Background `IHostedService` enforces the auction deadline (maps to BullMQ delayed jobs in prod, §8) |
| Frontend | React 18 + React Router, **RTL Persian UI**, Jalali dates, Toman formatting, dependency-free SVG charts |

Integrations (SMS gateway, object storage, payments) are abstracted behind interfaces so the platform stays
portable and operable from inside Iran (§7) — only console/local dev implementations are wired here.

## Run

### Backend (API)

```bash
cd backend/PetroMarketPlatform.API
dotnet restore
dotnet run
# API:    http://localhost:5080  (set ASPNETCORE_URLS to change)
# Swagger: http://localhost:5080/swagger
```

The database (`petki.db`) is created and **seeded automatically** on first run (roles, permissions, commodities,
demo users, Phase-1 intelligence content). Delete `petki.db` to reseed.

To switch databases, edit `appsettings.json`:

```json
{ "Database": { "Provider": "SqlServer" }, "ConnectionStrings": { "DefaultConnection": "Server=...;" } }
```

### Frontend (web)

```bash
cd frontend
npm install
npm start          # http://localhost:3000  (proxies /api to http://localhost:5080)
```

### Demo accounts (OTP is exposed in dev via `devCode` in the response)

| Role | Mobile |
| --- | --- |
| Admin | `09120000001` |
| Operator (compliance) | `09120000002` |
| Buyer (KYC approved) | `09120000003` |
| Seller (KYC approved) | `09120000004` |
| Seller + Buyer (KYC approved) | `09120000005` |

Log in with any mobile → the API returns `devCode` (also pre-filled in the UI) → submit it.

## Architecture (modular-monolith seams)

```
backend/PetroMarketPlatform.API/
  Models/        Identity, Kyc, Catalog, Auction, Trade, Intelligence, Platform  (full §5 data model)
  Data/          PetroContext (+ relationships/indexes), DbSeeder
  Auth/          Permissions, RBAC [HasPermission] filter, CurrentUser, claims
  Services/      Auth/Token, Kyc, Competition (auction core), Survey, Rating,
                 Notification (ISmsSender), Storage (IStorageService), Audit, deadline-closer
  Dtos/          Anonymity-safe contracts (public vs revealed bid projections, BR-3)
  Controllers/   Auth, Kyc, Intelligence(news/prices/stats/supplies), Marketplace,
                 Rfq, Competition, Trade(txn/survey/rating/notifications), Admin, Audit
frontend/src/
  api.js auth.js components.js format.js  + pages/ (intelligence, marketplace, auth, competition, dashboard, admin)
```

## Requirements traceability

| Spec | Where |
| --- | --- |
| §3 RBAC (5 actors, named permissions, runtime-editable) | `Auth/Permissions.cs`, `HasPermissionAttribute`, `AdminController` role/permission matrix |
| §4.1 / BR-1 OTP registration, rate-limited & expiring | `AuthService`, `OtpRequest`, `AuthController` |
| §4.1 / BR-2 KYC status machine + operator queue + hard trading gate | `KycService`, `KycController`, `EnsureCanTradeAsync` |
| §4.2 Intelligence (news/analysis/prices/stats/supplies + alerts) | `IntelligenceControllers.cs`, `Intelligence.js` |
| §4.3 Marketplace (products, public seller profiles, browse/search) | `MarketplaceControllers.cs`, `Marketplace.js` |
| §4.4 / BR-3..8 RFQ + anonymous competition engine | `CompetitionService.cs`, `CompetitionControllers.cs`, `Competition.js` |
| BR-3 anonymity at serialization layer | `BidPublicDto` vs `BidRevealedDto`; reveal only on shortlist; audited |
| BR-5 live ranking / BR-7 versioned revisions / BR-8 server-authoritative close | `CompetitionService`, `CompetitionCloserService` |
| §4.5 Transactions | `Transaction`, `TransactionsController` |
| §4.6 / BR-9 mandatory survey gate, BR-10 weighted rating | `SurveyService`, `RatingService` |
| §4.7 Operator dashboard (KYC review, audit) | `KycController` queue, `AuditController` |
| §4.8 Admin dashboard (users/roles, KPIs, intervene, content) | `AdminController`, competition close/cancel |
| §4.9 Notifications (SMS/push/in-app, prefs) | `NotificationService`, `NotificationsController` |
| §5 Data model | `Models/*` |
| §7 Audit, JWT, anonymity-as-security | `AuditService`, `TokenService`, DTO split |
| §7 i18n: RTL, Jalali, Persian digits, Toman | `frontend/src/format.js`, `index.css` (`dir=rtl`) |
| BR-11 configurable price visibility (default confidential) | `CompetitionVisibility`, bids endpoint respects it |

## Open questions (§12) — defaults applied

These spec decisions were left open; sensible, clearly-marked defaults were chosen and are easy to change:

- **Backend language (§12.5):** chose **.NET** (extends existing skeleton).
- **Transparency (BR-11):** default **Confidential** per competition (configurable to AggregateOnly/Public).
- **Settlement (§12.2):** platform **records** the transaction; large-value settlement is off-platform.
- **Qualified seller (§12.7):** Seller role + KYC-approved, restricted to commodity match when sellers list it.
- **Language (§12.8):** Persian-only UI in this build.
- DB is **SQLite** for zero-setup dev; the spec recommends **PostgreSQL** for production.
