import './App.css';
import { Routes, Route, Navigate, useNavigate, useParams } from 'react-router-dom';
import { useState } from 'react';

import { useAuth } from './contexts/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import MainLayout from './layouts/MainLayout';
import DashboardPage from './pages/DashboardPage';
import WebsitePage from './pages/WebsitePage';
import WebsiteDetailPage from './pages/WebsiteDetailPage';
import BulkAddWebsitesPage from './pages/BulkAddWebsitesPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';

function WebsiteDetailRoute({ refreshKey, onBack }) {
  const { websiteId } = useParams();

  return (
    <WebsiteDetailPage
      key={refreshKey}
      websiteId={websiteId}
      onBack={onBack}
    />
  );
}

export default function App() {
  const [refreshKey, setRefreshKey] = useState(0);
  const { isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();

  const triggerRefresh = () => {
    setRefreshKey(k => k + 1);
  };

  const handleViewDetail = (websiteId) => {
    navigate(`/websites/${websiteId}`);
  };

  const handleBackToWebsites = () => {
    navigate('/websites');
    triggerRefresh();
  };

  if (loading) {
    return (
        <div className="auth-loading">
          <div className="loading-spinner"></div>
          <p>Loading...</p>
        </div>
    );
  }

  return (
    <Routes>
      {/* Public Auth Routes */}
      <Route
        path="/login"
        element={
          isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />
        }
      />
      <Route
        path="/register"
        element={
          isAuthenticated ? <Navigate to="/" replace /> : <RegisterPage />
        }
      />

      {/* Protected Routes utilizing MainLayout */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout triggerRefresh={triggerRefresh} />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route
          path="dashboard"
          element={
            <DashboardPage
              key={refreshKey}
              onViewWebsites={() => navigate('/websites')}
            />
          }
        />
        <Route
          path="websites"
          element={
            <WebsitePage
              key={refreshKey}
              onViewDetail={handleViewDetail}
              onRefresh={triggerRefresh}
            />
          }
        />
        <Route
          path="websites/bulk-add"
          element={
            <BulkAddWebsitesPage
              onCompleted={triggerRefresh}
            />
          }
        />
        <Route
          path="websites/:websiteId"
          element={
            <WebsiteDetailRoute
              refreshKey={refreshKey}
              onBack={handleBackToWebsites}
            />
          }
        />
      </Route>

      {/* Catch-all Route */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}