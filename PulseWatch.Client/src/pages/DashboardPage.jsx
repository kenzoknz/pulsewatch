import React from 'react';
import {
  RiGlobalLine,
  RiCheckLine,
  RiCloseLine,
  RiRefreshLine,
  RiAddLine,
  RiAlertLine,
  RiFlashlightLine,
} from 'react-icons/ri';
import { getDashboardSummary } from '../api/pulsewatchApi';

export default function DashboardPage({ onViewWebsites }) {
  const [summary, setSummary] = React.useState(null);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState(null);

  React.useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getDashboardSummary();
      setSummary(response.data);
    } catch (err) {
      setError('Failed to load dashboard data. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="loading-state">
        <div className="loading-spinner" />
        <p className="loading-state-description">Loading dashboard...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-state">
        <div className="error-state-icon" style={{ fontSize: '2rem', color: 'var(--warning)' }}>
          <RiRefreshLine />
        </div>
        <h3 className="error-state-title">Failed to load dashboard</h3>
        <p className="error-state-description">{error}</p>
        <button className="btn btn-primary" onClick={fetchDashboardData}>
          Try Again
        </button>
      </div>
    );
  }

  const totalWebsites = summary?.totalWebsites || 0;
  const onlineWebsites = summary?.onlineWebsites || 0;
  const healthPercentage = totalWebsites > 0
    ? Math.round((onlineWebsites / totalWebsites) * 100)
    : 0;

  // Inline DashboardCards to avoid import issue
  const cards = [
    {
      id: 'websites',
      Icon: RiGlobalLine,
      label: 'Total Websites',
      value: summary?.totalWebsites || 0,
      description: 'Being monitored',
    },
    {
      id: 'online',
      Icon: RiCheckLine,
      label: 'Online',
      value: summary?.onlineWebsites || 0,
      description: 'Operational',
    },
    {
      id: 'offline',
      Icon: RiCloseLine,
      label: 'Offline',
      value: summary?.offlineWebsites || 0,
      description: 'Down or unreachable',
    },
    {
      id: 'response-time',
      Icon: RiFlashlightLine,
      label: 'Avg Response Time',
      value: summary?.averageResponseTimeMs ? `${Math.round(summary.averageResponseTimeMs)} ms` : '0 ms',
      description: 'Across all checks',
    },
    {
      id: 'downtime',
      Icon: RiAlertLine,
      label: 'Downtime Events',
      value: summary?.totalDowntimeEvents || 0,
      description: 'Total recorded',
    },
  ];

  return (
    <div>
      {/* Main Cards Grid */}
      <div className="grid grid-auto">
        {cards.map(card => (
          <div key={card.id} className="metric-card">
            <div className="metric-icon"><card.Icon size={22} /></div>
            <div className="metric-label">{card.label}</div>
            <div className="metric-value">{card.value}</div>
            <div className="metric-description">{card.description}</div>
          </div>
        ))}
      </div>

      {/* Health Overview Section */}
      {totalWebsites > 0 && (
        <div className="health-overview">
          <div className="health-overview-header">
            <h3 className="health-overview-title">Health Overview</h3>
            <span
              className={`status-badge ${
                healthPercentage >= 90 ? 'status-online' : healthPercentage >= 70 ? 'status-warning' : 'status-offline'
              }`}
            >
              <span className="status-dot" />
              {healthPercentage}% Healthy
            </span>
          </div>

          <div className="progress-bar success" style={{ marginBottom: '20px' }}>
            <div
              className="progress-fill"
              style={{ width: `${healthPercentage}%` }}
            />
          </div>

          <div className="health-stats">
            <div className="health-stat">
              <div className="health-stat-value">{onlineWebsites}</div>
              <div className="health-stat-label">Operational</div>
            </div>
            <div className="health-stat">
              <div className="health-stat-value">{summary?.offlineWebsites || 0}</div>
              <div className="health-stat-label">Down</div>
            </div>
            <div className="health-stat">
              <div className="health-stat-value">{totalWebsites}</div>
              <div className="health-stat-label">Total</div>
            </div>
          </div>

          <div className="section-divider" />

          <p className="text-muted" style={{ fontSize: '12px', marginBottom: '16px' }}>
            System health is calculated based on the latest monitoring checks. Data updates as new checks are performed.
          </p>

          <div className="quick-actions">
            <button className="btn btn-primary" onClick={onViewWebsites}>
              <RiGlobalLine style={{ marginRight: '6px' }} /> View All Websites
            </button>
            <button className="btn btn-secondary" onClick={onViewWebsites}>
              <RiAddLine style={{ marginRight: '6px' }} /> Add New Website
            </button>
          </div>
        </div>
      )}

      {/* Empty State */}
      {totalWebsites === 0 && (
        <div className="empty-state" style={{ padding: '80px 20px' }}>
          <div className="empty-state-icon"><RiGlobalLine size={48} /></div>
          <h3 className="empty-state-title">No websites monitored yet</h3>
          <p className="empty-state-description">
            Start monitoring your first endpoint to see real-time uptime data and health metrics.
          </p>
          <button className="btn btn-primary" onClick={onViewWebsites}>
            Add Your First Website
          </button>
        </div>
      )}
    </div>
  );
}
