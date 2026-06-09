import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useTheme } from '../hooks/useTheme';
import {
  RiDashboardLine,
  RiGlobalLine,
  RiRadarLine,
  RiArrowLeftLine,
  RiMoonLine,
  RiRefreshLine,
  RiSunLine,
  RiArrowDownSLine,
  RiArrowRightSLine,
  RiLogoutBoxLine,
} from 'react-icons/ri';

export default function MainLayout({ triggerRefresh }) {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();

  const isWebsitesRoute = /^\/websites/.test(location.pathname);
  const [websitesExpanded, setWebsitesExpanded] = useState(isWebsitesRoute);

  useEffect(() => {
    if (isWebsitesRoute) {
      setWebsitesExpanded(true);
    }
  }, [isWebsitesRoute]);

  const navItems = [
    { to: '/dashboard', label: 'Dashboard', Icon: RiDashboardLine },
  ];

  const isBulkAddView = location.pathname === '/websites/bulk-add';
  const isDetailView = /^\/websites\/[^/]+/.test(location.pathname) && !isBulkAddView;

  const handleBackToWebsites = () => {
    navigate('/websites');
    triggerRefresh();
  };

  const displayName = user?.username || user?.email || 'User';
  const avatarChar = displayName.charAt(0).toUpperCase();

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

            <div className={`nav-group ${websitesExpanded ? 'expanded' : ''}`}>
              <button
                className={`nav-group-title ${isWebsitesRoute ? 'active' : ''}`}
                onClick={() => setWebsitesExpanded(prev => !prev)}
              >
                <span className="nav-icon"><RiGlobalLine size={18} /></span>
                <span>Websites</span>
                <span className="nav-group-arrow">
                  {websitesExpanded ? <RiArrowDownSLine size={14} /> : <RiArrowRightSLine size={14} />}
                </span>
              </button>
              {websitesExpanded && (
                <>
                  <NavLink
                    to="/websites/bulk-add"
                    className={({ isActive }) => `nav-item nav-subitem ${isActive ? 'active' : ''}`}
                  >
                    <span>Bulk Add</span>
                  </NavLink>
                  <NavLink
                    to="/websites"
                    className={({ isActive }) => `nav-item nav-subitem ${isActive ? 'active' : ''}`}
                    end
                  >
                    <span>Manage Websites</span>
                  </NavLink>
                </>
              )}
            </div>
          </nav>

          {/* User Section at Footer */}
          <div className="sidebar-footer">
            <div className="user-info">
              <div className="user-avatar" title={user?.email}>
                {avatarChar}
              </div>
              <div className="user-details">
                <span className="user-name" title={displayName}>
                  {displayName}
                </span>
              </div>
            </div>
            <button
              className="btn-logout"
              onClick={logout}
              title="Log out"
            >
              <RiLogoutBoxLine size={16} />
              <span>Log out</span>
            </button>
          </div>
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
                    <h2>Manage Websites</h2>
                    <span className="topbar-subtitle">Manage monitored endpoints</span>
                  </>
                )}
                {isBulkAddView && (
                  <>
                    <h2>Bulk Add Websites</h2>
                    <span className="topbar-subtitle">Add multiple monitored endpoints at once</span>
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
                onClick={toggleTheme}
                title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
                aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
              >
                {theme === 'dark' ? <RiSunLine size={18} /> : <RiMoonLine size={18} />}
              </button>
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
              <Outlet />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
