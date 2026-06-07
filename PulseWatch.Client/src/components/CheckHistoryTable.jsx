import React from 'react';
import { RiBarChartLine } from 'react-icons/ri';

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
      second: '2-digit',
    }).format(date);
  } catch {
    return '—';
  }
};

export default function CheckHistoryTable({ checks }) {
  if (!checks || checks.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon"><RiBarChartLine size={48} /></div>
        <h3 className="empty-state-title">No check history</h3>
        <p className="empty-state-description">
          No uptime checks have been recorded yet. Check data will appear here once monitoring begins.
        </p>
      </div>
    );
  }

  // Sort by most recent first
  const sortedChecks = [...checks].sort(
    (a, b) => new Date(b.checkedAt) - new Date(a.checkedAt)
  );

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Status</th>
            <th>Status Code</th>
            <th>Response Time</th>
            <th>Error Message</th>
            <th>Checked At</th>
          </tr>
        </thead>
        <tbody>
          {sortedChecks.map((check, index) => (
            <tr
              key={`${check.id}-${index}`}
              className={check.isOnline ? 'check-row-online' : 'check-row-offline'}
            >
              <td data-label="Status">
                <span className={`status-badge ${check.isOnline ? 'status-online' : 'status-offline'}`}>
                  <span className="status-dot pulse" />
                  {check.isOnline ? 'Online' : 'Offline'}
                </span>
              </td>
              <td data-label="Status Code">
                {check.statusCode ? (
                  <span style={{ fontWeight: 600 }}>
                    {check.statusCode}
                  </span>
                ) : (
                  '—'
                )}
              </td>
              <td data-label="Response Time">
                {check.responseTimeMs ? `${check.responseTimeMs} ms` : '—'}
              </td>
              <td data-label="Error Message">
                {check.errorMessage ? (
                  <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                    {check.errorMessage}
                  </span>
                ) : (
                  'No error'
                )}
              </td>
              <td data-label="Checked At">
                {formatDate(check.checkedAt)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
