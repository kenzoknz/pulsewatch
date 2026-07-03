import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getPublicWebsiteStatus } from '../api/publicApi';
import { getPublicBadgeUrl } from '../api/publicApi';
import { RiPulseLine, RiCheckboxCircleLine, RiLockLine } from 'react-icons/ri';
import './PublicStatusPage.css';

const formatDate = (dateString) => {
  if (!dateString) return '—';
  try {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short', day: 'numeric', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    }).format(new Date(dateString));
  } catch { return '—'; }
};

const formatDuration = (minutes) => {
  if (!minutes) return null;
  const m = Math.round(minutes);
  if (m < 60) return `${m} min`;
  const h = Math.floor(m / 60);
  const rem = m % 60;
  return rem > 0 ? `${h}h ${rem}m` : `${h}h`;
};

export default function PublicStatusPage() {
  const { websiteId } = useParams();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [copied, setCopied] = useState(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        const res = await getPublicWebsiteStatus(websiteId);
        setData(res.data);
      } catch (err) {
        if (err.response?.status === 404) {
          setError('This status page does not exist or is not publicly accessible.');
        } else {
          setError('Failed to load status page. Please try again later.');
        }
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [websiteId]);

  const handleCopy = (text, key) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopied(key);
      setTimeout(() => setCopied(null), 2000);
    });
  };

  if (loading) {
    return (
      <div className="psp-shell">
        <div className="psp-loading">
          <div className="psp-spinner" />
          <p>Loading status page...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="psp-shell">
        <div className="psp-error">
          <div className="psp-error-icon" style={{ color: 'var(--warning)' }}>
            <RiLockLine size={48} />
          </div>
          <h2>Status Page Unavailable</h2>
          <p>{error}</p>
          <Link to="/" className="psp-btn">Go to PulseWatch</Link>
        </div>
      </div>
    );
  }

  const uptimePct = Math.round(data.uptimePercentage || 0);
  const isOnline = data.isOnline;
  const badgeUrl = getPublicBadgeUrl(websiteId);
  const statusPageUrl = `${window.location.origin}/status/${websiteId}`;
  const markdownBadge = `![Uptime Status](${badgeUrl})`;
  const htmlBadge = `<img src="${badgeUrl}" alt="Uptime Status" />`;

  // Build 30-bar sparkline from recentChecks (newest last)
  const barsData = [...(data.recentChecks || [])].reverse().slice(-30);

  return (
    <div className="psp-shell">
      {/* NAV */}
      <nav className="psp-nav">
        <Link to="/" className="psp-nav-brand">
          <RiPulseLine size={18} style={{ color: 'var(--primary)' }} /> PulseWatch
        </Link>
        <span className="psp-nav-label">Public Status</span>
      </nav>

      <div className="psp-content">
        {/* Header */}
        <div className="psp-header">
          <div className="psp-header-top">
            <div>
              <h1 className="psp-site-name">{data.name}</h1>
              <a
                href={data.url}
                target="_blank"
                rel="noopener noreferrer"
                className="psp-site-url"
              >
                {data.url}
              </a>
            </div>
            <span className={`psp-status-badge ${isOnline === true ? 'online' : isOnline === false ? 'offline' : 'unknown'}`}>
              <span className="psp-status-dot" />
              {isOnline === true ? 'Operational' : isOnline === false ? 'Experiencing Issues' : 'Status Unknown'}
            </span>
          </div>
          {data.lastCheckedAt && (
            <p className="psp-last-check">Last checked: {formatDate(data.lastCheckedAt)}</p>
          )}
        </div>

        {/* Stats Grid */}
        <div className="psp-stats">
          <div className="psp-stat-card">
            <div className="psp-stat-value" style={{ color: uptimePct >= 99 ? '#10b981' : uptimePct >= 90 ? '#f59e0b' : '#ef4444' }}>
              {uptimePct}%
            </div>
            <div className="psp-stat-label">Uptime</div>
          </div>
          <div className="psp-stat-card">
            <div className="psp-stat-value">{data.averageResponseTimeMs ? `${Math.round(data.averageResponseTimeMs)} ms` : '—'}</div>
            <div className="psp-stat-label">Avg. Response</div>
          </div>
          <div className="psp-stat-card">
            <div className="psp-stat-value">{data.totalChecks || 0}</div>
            <div className="psp-stat-label">Total Checks</div>
          </div>
          <div className="psp-stat-card">
            <div className="psp-stat-value" style={{ color: '#10b981' }}>{data.onlineChecks || 0}</div>
            <div className="psp-stat-label">Successful</div>
          </div>
        </div>

        {/* Uptime Ring */}
        <div className="psp-ring-row">
          <div className="psp-ring-container">
            <div
              className="psp-ring"
              style={{
                backgroundImage: `conic-gradient(
                  ${uptimePct >= 99 ? '#10b981' : uptimePct >= 90 ? '#f59e0b' : '#ef4444'} ${uptimePct * 3.6}deg,
                  rgba(255,255,255,0.06) ${uptimePct * 3.6}deg
                )`
              }}
            >
              <div className="psp-ring-inner">
                <span className="psp-ring-value">{uptimePct}%</span>
                <span className="psp-ring-sub">uptime</span>
              </div>
            </div>
          </div>

          {/* Sparkline Chart */}
          <div className="psp-chart">
            <div className="psp-chart-label">Response time &amp; availability</div>
            <div className="psp-bars">
              {barsData.length === 0
                ? <p className="psp-no-data">No check history yet</p>
                : barsData.map((c, i) => (
                    <div
                      key={i}
                      className={`psp-bar ${c.isOnline ? 'up' : 'down'}`}
                      title={`${formatDate(c.checkedAt)} — ${c.isOnline ? 'Online' : 'Offline'} ${c.responseTimeMs}ms`}
                      style={{ '--height': `${Math.min(100, Math.max(10, (c.responseTimeMs / 2000) * 100))}%` }}
                    />
                  ))
              }
            </div>
            <div className="psp-bars-legend">
              <span className="psp-legend-item"><span className="psp-legend-dot up" /> Online</span>
              <span className="psp-legend-item"><span className="psp-legend-dot down" /> Offline</span>
            </div>
          </div>
        </div>

        {/* Downtime Events */}
        <div className="psp-section">
          <h2 className="psp-section-title">Recent Downtime Events</h2>
          {(!data.recentDowntimeEvents || data.recentDowntimeEvents.length === 0) ? (
            <div className="psp-no-incidents" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <RiCheckboxCircleLine size={18} /> No recent downtime events — all systems operational!
            </div>
          ) : (
            <div className="psp-incidents">
              {data.recentDowntimeEvents.map((ev, i) => (
                <div key={i} className="psp-incident">
                  <div className="psp-incident-dot" />
                  <div className="psp-incident-body">
                    <div className="psp-incident-header">
                      <span className="psp-incident-time">{formatDate(ev.startedAt)}</span>
                      {ev.durationMinutes ? (
                        <span className="psp-incident-duration">{formatDuration(ev.durationMinutes)}</span>
                      ) : (
                        <span className="psp-incident-ongoing">Ongoing</span>
                      )}
                    </div>
                    {ev.reason && <div className="psp-incident-reason">{ev.reason}</div>}
                    {ev.endedAt && (
                      <div className="psp-incident-resolved">Resolved: {formatDate(ev.endedAt)}</div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Embed Section */}
        <div className="psp-section">
          <h2 className="psp-section-title">Embed This Status</h2>
          <div className="psp-embed-grid">
            <div className="psp-embed-card">
              <div className="psp-embed-label">SVG Badge (Markdown)</div>
              <code className="psp-embed-code">{markdownBadge}</code>
              <button className="psp-copy-btn" onClick={() => handleCopy(markdownBadge, 'md')} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                {copied === 'md' ? <><RiCheckboxCircleLine size={12} style={{ color: 'var(--success)' }} /> Copied!</> : 'Copy'}
              </button>
            </div>
            <div className="psp-embed-card">
              <div className="psp-embed-label">SVG Badge (HTML)</div>
              <code className="psp-embed-code">{htmlBadge}</code>
              <button className="psp-copy-btn" onClick={() => handleCopy(htmlBadge, 'html')} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                {copied === 'html' ? <><RiCheckboxCircleLine size={12} style={{ color: 'var(--success)' }} /> Copied!</> : 'Copy'}
              </button>
            </div>
            <div className="psp-embed-card">
              <div className="psp-embed-label">Public Status Page URL</div>
              <code className="psp-embed-code">{statusPageUrl}</code>
              <button className="psp-copy-btn" onClick={() => handleCopy(statusPageUrl, 'url')} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                {copied === 'url' ? <><RiCheckboxCircleLine size={12} style={{ color: 'var(--success)' }} /> Copied!</> : 'Copy'}
              </button>
            </div>
            <div className="psp-embed-card psp-badge-preview-card">
              <div className="psp-embed-label">Badge Preview</div>
              <img src={badgeUrl} alt="Uptime badge preview" className="psp-badge-img" />
            </div>
          </div>
        </div>
      </div>

      {/* Footer */}
      <footer className="psp-footer">
        <Link to="/" className="psp-footer-brand">
          Powered by <strong>PulseWatch</strong> — Real-time Uptime Monitoring
        </Link>
      </footer>
    </div>
  );
}
