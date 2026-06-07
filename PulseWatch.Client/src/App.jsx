import './App.css';
import DashboardPage from './pages/DashboardPage';
import WebsitePage from './pages/WebsitePage';
import WebsiteDetailPage from './pages/WebsiteDetailPage';
import {
  RiDashboardLine,
  RiGlobalLine,
  RiRadarLine,
  RiArrowLeftLine,
  RiRefreshLine,
} from 'react-icons/ri';
import { NavLink, Navigate, Route, Routes, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useState } from 'react';

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

function App() {
  const [refreshKey, setRefreshKey] = useState(0);
  const navigate = useNavigate();
  const location = useLocation();

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

  const navItems = [
    { to: '/dashboard', label: 'Dashboard', Icon: RiDashboardLine },
    { to: '/websites', label: 'Websites', Icon: RiGlobalLine },
  ];

  const isDetailView = /^\/websites\/[^/]+/.test(location.pathname);

  return (
    <div className="app-shell">
      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        {/* Sidebar */}
        <aside className="sidebar">
          <div className="logo">
            <div className="logo-icon"><RiRadarLine size={22} /></div>
            <span>PulseWatch</span>
          </div>

          <nav className="nav">
            {navItems.map(item => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
              >
                <span className="nav-icon"><item.Icon size={18} /></span>
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </aside>

        {/* Main Content */}
        <div className="main-content">
          {/* Top Bar */}
          <header className="topbar">
            <div className="topbar-left">
              {isDetailView && (
                <button
                  className="back-button"
                  onClick={handleBackToWebsites}
                  title="Back to websites"
                >
                  <RiArrowLeftLine size={18} />
                </button>
              )}
              <div className="topbar-title">
                {location.pathname.startsWith('/dashboard') && (
                  <>
                    <h2>Dashboard</h2>
                    <span className="topbar-subtitle">System health overview</span>
                  </>
                )}
                {location.pathname === '/websites' && (
                  <>
                    <h2>Websites</h2>
                    <span className="topbar-subtitle">Manage monitored endpoints</span>
                  </>
                )}
                {isDetailView && (
                  <>
                    <h2>Website Details</h2>
                    <span className="topbar-subtitle">Performance & status monitoring</span>
                  </>
                )}
              </div>
            </div>
            <div className="topbar-right">
              <button
                className="btn-refresh"
                onClick={triggerRefresh}
                title="Refresh data"
              >
                <RiRefreshLine size={18} />
              </button>
            </div>
          </header>

          {/* Page Content */}
          <div className="page">
            <div className="page-container">
              <Routes>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route
                  path="/dashboard"
                  element={(
                    <DashboardPage
                      key={refreshKey}
                      onViewWebsites={() => navigate('/websites')}
                    />
                  )}
                />
                <Route
                  path="/websites"
                  element={(
                    <WebsitePage
                      key={refreshKey}
                      onViewDetail={handleViewDetail}
                      onRefresh={triggerRefresh}
                    />
                  )}
                />
                <Route
                  path="/websites/:websiteId"
                  element={(
                    <WebsiteDetailRoute
                      refreshKey={refreshKey}
                      onBack={handleBackToWebsites}
                    />
                  )}
                />
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;