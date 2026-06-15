import React, { useState, useEffect } from 'react';
import {
  getWebsite,
  getWebsiteStats,
  getWebsiteChecks,
  getDowntimeEvents,
  updateWebsite,
  runDeepCheck,
} from '../api/pulsewatchApi';
import CheckHistoryTable from '../components/CheckHistoryTable';
import {
  RiAlertLine,
  RiCloseCircleLine,
  RiCheckboxCircleLine,
  RiArrowLeftLine,
} from 'react-icons/ri';

const formatDate = (dateString) => {
  if (!dateString) return '—';
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  } catch {
    return '—';
  }
};

const DEFAULT_PAGE_SIZE = 20;

export default function WebsiteDetailPage({ websiteId, onBack }) {
  const [website, setWebsite] = useState(null);
  const [stats, setStats] = useState(null);
  const [checksPage, setChecksPage] = useState(1);
  const [checksPageSize, setChecksPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [checksPaged, setChecksPaged] = useState(null);
  const [downtimeEvents, setDowntimeEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [checksLoading, setChecksLoading] = useState(false);
  const [error, setError] = useState(null);
  const [togglingStatus, setTogglingStatus] = useState(false);
  const [deepCheckLoading, setDeepCheckLoading] = useState(false);
  const [deepCheckResult, setDeepCheckResult] = useState(null);
  const [deepCheckError, setDeepCheckError] = useState(null);

  useEffect(() => {
    fetchAllData();
  }, [websiteId]);

  useEffect(() => {
    if (!loading) {
      fetchChecksPage();
    }
  }, [checksPage, checksPageSize]);

  const fetchChecksPage = async () => {
    try {
      setChecksLoading(true);
      const checksRes = await getWebsiteChecks(websiteId, checksPage, checksPageSize);
      setChecksPaged(checksRes.data);
    } catch {
      // silently fail for pagination
    } finally {
      setChecksLoading(false);
    }
  };

  const fetchAllData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [websiteRes, statsRes, checksRes, eventsRes] = await Promise.all([
        getWebsite(websiteId),
        getWebsiteStats(websiteId),
        getWebsiteChecks(websiteId, 1, DEFAULT_PAGE_SIZE),
        getDowntimeEvents(websiteId),
      ]);

      setWebsite(websiteRes.data);
      setStats(statsRes.data);
      setChecksPaged(checksRes.data);
      setDowntimeEvents(eventsRes.data || []);
    } catch (err) {
      setError('Failed to load website details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleStatus = async () => {
    if (!website) return;
    try {
      setTogglingStatus(true);
      await updateWebsite(website.id, {
        ...website,
        isActive: !website.isActive,
      });
      setWebsite(prev => ({ ...prev, isActive: !prev.isActive }));
    } catch (err) {
      alert('Failed to update website status.');
    } finally {
      setTogglingStatus(false);
    }
  };

  const handleDeepCheck = async () => {
    try {
      setDeepCheckLoading(true);
      setDeepCheckError(null);
      setDeepCheckResult(null);
      const res = await runDeepCheck(websiteId);
      setDeepCheckResult(res.data);
    } catch (err) {
      if (err.response?.status === 429) {
        setDeepCheckError(err.response.data?.message || 'Please wait before trying again.');
      } else {
        setDeepCheckError('Deep Check failed. Please try again.');
      }
    } finally {
      setDeepCheckLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="loading-state">
        <div className="loading-spinner" />
        <p className="loading-state-description">Loading website details...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-state">
        <div className="error-state-icon" style={{ fontSize: '2rem', color: 'var(--warning)' }}>
          <RiAlertLine />
        </div>
        <h3 className="error-state-title">Failed to load details</h3>
        <p className="error-state-description">{error}</p>
        <button className="btn btn-primary" onClick={fetchAllData}>
          Try Again
        </button>
      </div>
    );
  }

  if (!website || !stats) {
    return (
      <div className="error-state">
        <div className="error-state-icon" style={{ fontSize: '2rem', color: 'var(--danger)' }}>
          <RiCloseCircleLine />
        </div>
        <h3 className="error-state-title">Website not found</h3>
        <p className="error-state-description">The website you're looking for doesn't exist.</p>
        <button className="btn btn-primary" onClick={onBack}>
          Back to Websites
        </button>
      </div>
    );
  }

  const currentStatus = stats.currentStatus;
  const uptimePercentage = Math.round(stats.uptimePercentage || 0);

  return (
    <div>
      {/* Header */}
      <div className="website-detail-header">
        <div className="website-detail-title">
          <h2>{website.name}</h2>
          <a
            href={website.url}
            target="_blank"
            rel="noopener noreferrer"
            className="website-detail-url"
            title={website.url}
          >
            {website.url}
          </a>
        </div>
        <div className="website-detail-actions">
          <button
            className={`btn ${website.isActive ? 'btn-secondary' : 'btn-danger'}`}
            onClick={handleToggleStatus}
            disabled={togglingStatus}
          >
            {website.isActive ? 'Active' : 'Inactive'}
          </button>
        </div>
      </div>

      {/* Status Badge */}
      <div style={{ marginBottom: '20px' }}>
        <span
          className={`status-badge ${
            currentStatus ? 'status-online' : currentStatus === false ? 'status-offline' : 'status-unknown'
          }`}
        >
          <span className="status-dot pulse" />
          {currentStatus ? 'Online' : currentStatus === false ? 'Offline' : 'Unknown'}
        </span>
      </div>

      {/* Deep Check Panel */}
      <div className="deep-check-panel" style={{
        background: 'var(--surface-elevated)',
        border: '1px solid var(--border)',
        borderRadius: '12px',
        padding: '20px 24px',
        marginBottom: '24px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: deepCheckResult || deepCheckError ? '16px' : '0' }}>
          <div>
            <h4 style={{ margin: 0, color: 'var(--text-primary)', fontSize: '15px', fontWeight: 600 }}>Deep Check (Browser)</h4>
            <p style={{ margin: '4px 0 0', color: 'var(--text-muted)', fontSize: '12px' }}>
              Runs a real browser check using headless Chromium &middot; 2-minute cooldown
            </p>
          </div>
          <button
            className="btn btn-primary"
            onClick={handleDeepCheck}
            disabled={deepCheckLoading}
            style={{ minWidth: '160px' }}
          >
            {deepCheckLoading ? (
              <><span className="loading-spinner" style={{ width: '14px', height: '14px', marginRight: '8px', display: 'inline-block', verticalAlign: 'middle' }} />Checking...</>
            ) : 'Run Deep Check'}
          </button>
        </div>

        {deepCheckError && (
          <div style={{
            marginTop: '12px',
            padding: '10px 14px',
            borderRadius: '8px',
            background: 'rgba(239,68,68,0.1)',
            color: 'var(--danger)',
            fontSize: '13px',
          }}>
            {deepCheckError}
          </div>
        )}

        {deepCheckResult && (
          <div style={{ marginTop: '12px' }}>
            <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', marginBottom: '12px' }}>
              <span style={{
                padding: '4px 12px',
                borderRadius: '20px',
                fontSize: '12px',
                fontWeight: 600,
                background: deepCheckResult.isOnline ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                color: deepCheckResult.isOnline ? 'var(--success)' : 'var(--danger)',
              }}>
                {deepCheckResult.isOnline ? 'Online' : 'Offline'}
              </span>
              {deepCheckResult.responseTimeMs > 0 && (
                <span style={{ padding: '4px 12px', borderRadius: '20px', fontSize: '12px', background: 'var(--surface)', color: 'var(--text-secondary)' }}>
                  {deepCheckResult.responseTimeMs} ms
                </span>
              )}
              {deepCheckResult.pageTitle && (
                <span style={{ padding: '4px 12px', borderRadius: '20px', fontSize: '12px', background: 'var(--surface)', color: 'var(--text-secondary)', maxWidth: '260px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {deepCheckResult.pageTitle}
                </span>
              )}
            </div>

            {deepCheckResult.errorMessage && (
              <p style={{ color: 'var(--danger)', fontSize: '13px', margin: '0 0 12px' }}>{deepCheckResult.errorMessage}</p>
            )}

            {deepCheckResult.screenshotBase64 && (
              <div style={{ marginTop: '8px' }}>
                <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '8px' }}>Screenshot:</p>
                <img
                  src={`data:image/png;base64,${deepCheckResult.screenshotBase64}`}
                  alt="Deep Check Screenshot"
                  style={{
                    maxWidth: '100%',
                    borderRadius: '8px',
                    border: '1px solid var(--border)',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
                  }}
                />
              </div>
            )}
          </div>
        )}
      </div>

      {/* Stats Grid */}
      <div className="grid grid-3" style={{ marginBottom: '20px' }}>
        <div className="stat-item">
          <div className="stat-item-value">{uptimePercentage}%</div>
          <div className="stat-item-label">Uptime</div>
        </div>
        <div className="stat-item">
          <div className="stat-item-value">
            {stats.averageResponseTimeMs ? `${Math.round(stats.averageResponseTimeMs)} ms` : '—'}
          </div>
          <div className="stat-item-label">Avg Response Time</div>
        </div>
        <div className="stat-item">
          <div className="stat-item-value">{stats.totalChecks || 0}</div>
          <div className="stat-item-label">Total Checks</div>
        </div>
        <div className="stat-item">
          <div className="stat-item-value">{stats.onlineChecks || 0}</div>
          <div className="stat-item-label">Online Checks</div>
        </div>
        <div className="stat-item">
          <div className="stat-item-value">{stats.offlineChecks || 0}</div>
          <div className="stat-item-label">Offline Checks</div>
        </div>
        <div className="stat-item">
          <div className="stat-item-value">
            {stats.lastCheckedAt ? formatDate(stats.lastCheckedAt) : '—'}
          </div>
          <div className="stat-item-label">Last Checked</div>
        </div>
      </div>

      {/* Uptime Progress */}
      <div className="uptime-ring-container">
        <div
          className="uptime-ring"
          style={{
            '--uptime': uptimePercentage,
            backgroundImage: `conic-gradient(
              var(--success) 0deg var(--success) ${uptimePercentage * 3.6}deg,
              var(--surface-elevated) ${uptimePercentage * 3.6}deg
            )`,
          }}
        >
          <span style={{ position: 'relative', zIndex: 1 }}>{uptimePercentage}%</span>
        </div>
        <div className="uptime-label">Uptime Percentage</div>
      </div>

      {/* Check History Section */}
      <div style={{ marginTop: '40px' }}>
        <h3 className="section-title">
          Check History
          <span className="text-muted" style={{ fontSize: '12px', marginLeft: 'auto', fontWeight: 'normal' }}>
            {checksPaged?.totalItems ?? 0} total checks
          </span>
        </h3>

        <CheckHistoryTable checks={checksPaged?.items || []} />

        {checksPaged && checksPaged.totalPages > 1 && (
          <div className="pagination-controls">
            <button
              className="btn btn-secondary btn-sm"
              disabled={!checksPaged.hasPreviousPage || checksLoading}
              onClick={() => setChecksPage(p => Math.max(1, p - 1))}
            >
              ← Previous
            </button>

            <div className="pagination-info">
              <span>
                Page {checksPaged.page} of {checksPaged.totalPages}
              </span>
              <select
                className="page-size-select"
                value={checksPageSize}
                onChange={(e) => {
                  setChecksPageSize(Number(e.target.value));
                  setChecksPage(1);
                }}
              >
                <option value={10}>10 / page</option>
                <option value={20}>20 / page</option>
                <option value={50}>50 / page</option>
              </select>
            </div>

            <button
              className="btn btn-secondary btn-sm"
              disabled={!checksPaged.hasNextPage || checksLoading}
              onClick={() => setChecksPage(p => p + 1)}
            >
              Next →
            </button>
          </div>
        )}
      </div>

      {/* Downtime Events Section */}
      {downtimeEvents.length > 0 && (
        <div style={{ marginTop: '40px' }}>
          <h3 className="section-title">
            Downtime Events
            <span className="text-muted" style={{ fontSize: '12px', marginLeft: 'auto', fontWeight: 'normal' }}>
              {downtimeEvents.length} total
            </span>
          </h3>

          <div className="downtime-timeline">
            {downtimeEvents.map(event => {
              const startDate = new Date(event.startedAt);
              const endDate = event.endedAt ? new Date(event.endedAt) : null;
              const durationMinutes = event.durationMinutes
                ? Math.round(event.durationMinutes * 10) / 10
                : null;

              return (
                <div key={event.id} className="downtime-event">
                  <div className="downtime-event-header">
                    <span className="downtime-event-time">
                      {formatDate(event.startedAt)}
                    </span>
                    {durationMinutes && (
                      <span className="downtime-event-duration">
                        {durationMinutes} min
                      </span>
                    )}
                    {!endDate && (
                      <span className="downtime-event-duration" style={{ background: 'rgba(245, 158, 11, 0.15)', color: 'var(--warning)' }}>
                        Ongoing
                      </span>
                    )}
                  </div>
                  {event.reason && (
                    <div className="downtime-event-reason">
                      {event.reason}
                    </div>
                  )}
                  {endDate && (
                    <div className="downtime-event-reason" style={{ marginTop: '8px' }}>
                      Ended: {formatDate(event.endedAt)}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {downtimeEvents.length === 0 && (
        <div style={{ marginTop: '40px', marginBottom: '40px' }}>
          <h3 className="section-title">Downtime Events</h3>
          <div className="empty-state">
            <div className="empty-state-icon" style={{ color: 'var(--success)' }}>
              <RiCheckboxCircleLine size={48} />
            </div>
            <h3 className="empty-state-title">No downtime recorded</h3>
            <p className="empty-state-description">
              This website has been stable with no recorded downtime events.
            </p>
          </div>
        </div>
      )}

      {/* Footer */}
      <div style={{ marginTop: '40px', paddingTop: '20px', borderTop: '1px solid var(--border)' }}>
        <button className="btn btn-secondary" onClick={onBack}>
          <RiArrowLeftLine style={{ marginRight: '6px' }} /> Back to Websites
        </button>
      </div>
    </div>
  );
}
