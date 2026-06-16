import React from 'react';
import { Routes, Route } from 'react-router-dom';
import { Layout, Protected } from './components';

import Home from './pages/Home';
import { News, Analysis, ArticleDetail, Prices, TradeStats, Supplies } from './pages/Intelligence';
import { Marketplace, SellerProfile } from './pages/Marketplace';
import { Login, Register } from './pages/Auth';
import { Competitions, CompetitionRoom, CreateRfq } from './pages/Competition';
import { Dashboard, Kyc, SellerProducts, Notifications, Surveys } from './pages/Dashboard';
import { KycQueue, AdminUsers, AdminKpis, AuditLog } from './pages/Admin';

const P = {
  CompetitionShortlist: 'competition.shortlist', RfqManage: 'rfq.manage',
  ProductManage: 'product.manage', KycComplete: 'kyc.complete', KycReview: 'kyc.review',
  UsersManage: 'users.manage', ReportsView: 'reports.view', AuditView: 'audit.view',
  SurveySubmit: 'survey.submit',
};

export default function App() {
  return (
    <Routes>
      {/* Public — intelligence funnel + marketplace */}
      <Route path="/" element={<Layout><Home /></Layout>} />
      <Route path="/news" element={<Layout><News /></Layout>} />
      <Route path="/analysis" element={<Layout><Analysis /></Layout>} />
      <Route path="/article/:slug" element={<Layout><ArticleDetail /></Layout>} />
      <Route path="/prices" element={<Layout><Prices /></Layout>} />
      <Route path="/stats" element={<Layout><TradeStats /></Layout>} />
      <Route path="/supplies" element={<Layout><Supplies /></Layout>} />
      <Route path="/marketplace" element={<Layout><Marketplace /></Layout>} />
      <Route path="/seller/:id" element={<Layout><SellerProfile /></Layout>} />
      <Route path="/competitions" element={<Layout><Competitions /></Layout>} />
      <Route path="/competition/:id" element={<Layout><CompetitionRoom /></Layout>} />

      {/* Auth */}
      <Route path="/login" element={<Layout><Login /></Layout>} />
      <Route path="/register" element={<Layout><Register /></Layout>} />

      {/* Authenticated */}
      <Route path="/dashboard" element={<Protected><Layout><Dashboard /></Layout></Protected>} />
      <Route path="/kyc" element={<Protected perm={P.KycComplete}><Layout><Kyc /></Layout></Protected>} />
      <Route path="/rfq/new" element={<Protected perm={P.RfqManage}><Layout><CreateRfq /></Layout></Protected>} />
      <Route path="/seller/manage/products" element={<Protected perm={P.ProductManage}><Layout><SellerProducts /></Layout></Protected>} />
      <Route path="/notifications" element={<Protected><Layout><Notifications /></Layout></Protected>} />
      <Route path="/surveys" element={<Protected perm={P.SurveySubmit}><Layout><Surveys /></Layout></Protected>} />

      {/* Operator / Admin */}
      <Route path="/operator/kyc" element={<Protected perm={P.KycReview}><Layout><KycQueue /></Layout></Protected>} />
      <Route path="/admin/users" element={<Protected perm={P.UsersManage}><Layout><AdminUsers /></Layout></Protected>} />
      <Route path="/admin/kpis" element={<Protected perm={P.ReportsView}><Layout><AdminKpis /></Layout></Protected>} />
      <Route path="/admin/audit" element={<Protected perm={P.AuditView}><Layout><AuditLog /></Layout></Protected>} />

      <Route path="*" element={<Layout><h1>صفحه پیدا نشد</h1></Layout>} />
    </Routes>
  );
}
